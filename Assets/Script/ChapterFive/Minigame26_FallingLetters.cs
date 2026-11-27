using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MG26 떨어지는 문자 복구 (실행 문자 수신 검사)
///  받침대로 떨어지는 문자 중 S, T, A, R, T 만 받기 (순서 상관없음, 다른 문자를 받으면 처음부터)
///  다 받으면 S·T·A·R·T → START 결합 → START 클릭 (탑 쌓기 단계는 뺌)
/// 화면(네온 판/슬롯/받침대)은 스크립트가 직접 만듦. 그림은 에디터 도구(Chapter5SkinTool2630)가 넣어 줌.
/// </summary>
public class Minigame26_FallingLetters : MinigameBase
{
    [Header("챕터5 그림 (에디터 도구가 자동으로 넣음)")]
    public TMP_FontAsset uiFont;
    public Sprite panelSprite;          // 23_line_back
    public Sprite plateSprite;          // 26_upline
    public Sprite startButtonSprite;    // normal_button
    public Sprite slotFrameSprite;      // 26_miniline
    public Sprite slotEmptySprite;      // 26_blueblank
    public Sprite slotArrowLeft;        // 26_arrow_left
    public Sprite slotArrowRight;       // 26_arrow_right
    public Sprite[] slotLetterSprites = new Sprite[5];   // 26_pink_S/T/A/R/T
    public Sprite[] alphabetSprites = new Sprite[26];    // alpabet/block_A ~ block_Z
    public Sprite[] stackBlockSprites = new Sprite[5];   // 30_block_S/T/A/R/T
    public Sprite stackBaseSprite;

    [Header("규칙")]
    [Tooltip("틀린 문자를 받으면 챕터5 오류 횟수(5번이면 처음부터)에 포함")]
    public bool countMistakes = true;

    [Header("떨어지는 문자")]
    public Vector2 fallingLetterSize = new Vector2(84f, 81f);
    public float spawnInterval = 0.4f;   // 문자가 더 자주 떨어지게
    public float minFallSpeed = 190f;
    public float maxFallSpeed = 270f;
    [Range(0f, 1f)] public float targetChance = 0.35f;
    public float paddleMoveSpeed = 900f;
    public float errorDisplaySeconds = 1.2f;

    [Header("탑 쌓기")]
    public Vector2 blockSize = new Vector2(176f, 62f);
    public float stackGap = 4f;
    public float snapDistance = 70f;
    public float stableSeconds = 3f;

    private const string TargetWord = "START";
    private const string LetterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";   // A~Z 다양하게

    // ---- 배치 (1920x1080 기준, 가운데 0,0) ----
    private static readonly Vector2 PanelPos = new Vector2(0f, 30f);
    private static readonly Vector2 PanelSize = new Vector2(1480f, 580f);
    private static readonly Vector2 PlatePos = new Vector2(0f, 300f);
    private static readonly Vector2 PlayPos = new Vector2(0f, 25f);
    private static readonly Vector2 PlaySize = new Vector2(1420f, 510f);
    private static readonly Vector2 SlotRowPos = new Vector2(0f, -338f);
    private const float PaddleY = -215f;

    // 업적(MG26_FirstCatchPerfectAchievement)이 이름으로 읽는 값들: phase / collectedCount / activeLetters
    private enum Phase
    {
        Catching,
        Error,
        Stacking,
        Stability,
        StartReady,
        Completed
    }

    private Phase phase;
    private int collectedCount;
    private readonly bool[] got = new bool[5];   // S T A R T 중 받은 칸
    private int stackedCount;
    private float spawnTimer;
    private float stabilityTimer;
    private bool finished;

    private class FallingLetter
    {
        public GameObject obj;
        public RectTransform rect;
        public char character;
        public float speed;
        public GameObject spark;
    }

    private readonly List<FallingLetter> activeLetters = new List<FallingLetter>();
    private readonly List<RectTransform> blocks = new List<RectTransform>();
    private readonly List<Vector2> blockHomes = new List<Vector2>();

    // ---- 만든 UI ----
    private TMP_FontAsset font;
    private RectTransform screen;
    private RectTransform panel;
    private RectTransform dropArea;
    private RectTransform lettersLayer;
    private RectTransform paddle;
    private RectTransform stackArea;
    private RectTransform stackBase;
    private Image stackGuide;
    private TextMeshProUGUI plateText;
    private TextMeshProUGUI bottomText;
    private CanvasGroup banner;
    private TextMeshProUGUI bannerText;
    private C5SlotRow slotRow;
    private Button finalStartButton;

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "실행 문자 수신 검사";
        instruction = "S-T-A-R-T 순서대로 문자를 받으세요.";

        base.StartMinigame();
        StopAllCoroutines();

        finished = false;
        collectedCount = 0;
        stackedCount = 0;
        activeLetters.Clear();
        blocks.Clear();
        blockHomes.Clear();

        BuildScreen();
        C5Kit.PrepareNoise(transform);
        BeginCatchStage();
    }

    // =========================================
    // 화면 만들기
    // =========================================
    private void BuildScreen()
    {
        font = C5Kit.ResolveFont(transform, uiFont);
        C5Kit.HideLegacy(transform);
        screen = C5Kit.NewScreen(transform, "RT_MG26");

        panel = C5Kit.Panel(screen, panelSprite, PanelPos, PanelSize).rectTransform;

        // 문자가 떨어지는 영역 (판 밖으로는 안 보이게 잘라 냄)
        dropArea = C5Kit.Rect(screen, "RT_DropArea", PlayPos, PlaySize);
        dropArea.gameObject.AddComponent<RectMask2D>();
        lettersLayer = C5Kit.Rect(dropArea, "RT_Letters", Vector2.zero, Vector2.zero);
        C5Kit.Stretch(lettersLayer);

        // 받침대 (하늘색 판 + 위쪽 밝은 줄)
        Image pad = C5Kit.Img(dropArea, "RT_CatchPaddle", null, C5Kit.Cyan, new Vector2(0f, PaddleY),
            new Vector2(250f, 30f));
        C5Kit.Img(pad.transform, "Shine", null, new Color(0.78f, 0.98f, 1f, 1f), new Vector2(0f, 9f),
            new Vector2(236f, 6f));
        C5Kit.Img(pad.transform, "Shade", null, new Color32(8, 120, 160, 255), new Vector2(0f, -11f),
            new Vector2(250f, 8f));
        paddle = pad.rectTransform;

        // 탑 쌓기 영역 (2단계에서 켜짐)
        stackArea = C5Kit.Rect(screen, "RT_StackArea", PlayPos, PlaySize);
        stackArea.gameObject.SetActive(false);

        plateText = C5Kit.Plate(screen, plateSprite, font, PlatePos, new Vector2(540f, 80f), "");

        slotRow = new C5SlotRow(screen, SlotRowPos, slotFrameSprite, slotEmptySprite, slotLetterSprites,
            slotArrowLeft, slotArrowRight, font);

        bottomText = C5Kit.BottomText(screen, font, "");

        banner = C5Kit.Banner(screen, font, new Vector2(0f, 40f), new Vector2(760f, 170f), out bannerText);

        finalStartButton = C5Kit.StartButton(screen, startButtonSprite, font, new Vector2(0f, 40f),
            new Vector2(360f, 84f), OnFinalStartClicked);
        finalStartButton.gameObject.SetActive(false);
    }

    // =========================================
    // 매 프레임
    // =========================================
    protected override void Update()
    {
        // 글자를 다 모아 START 버튼만 남았으면 시간은 멈춤 (다 해 놓고 시간 초과로 실패하지 않게)
        bool timerFrozen = phase == Phase.Stacking || phase == Phase.StartReady || phase == Phase.Completed;
        if (!timerFrozen)
            base.Update();

        if (!isGameActive || finished)
            return;

        // 잘못 받은 직후(Error)에도 게임은 그대로 계속됨
        if (phase == Phase.Catching || phase == Phase.Error)
        {
            UpdatePaddle();
            UpdateFallingLetters();
            if (phase == Phase.Catching || phase == Phase.Error)
                UpdateSpawning();
            UpdateSparks();
        }
        else if (phase == Phase.Stacking)
        {
            PulseGuide();
        }
        else if (phase == Phase.Stability)
        {
            UpdateStability();
        }
    }

    // =========================================
    // 1단계: 문자 받기
    // =========================================
    private void BeginCatchStage()
    {
        phase = Phase.Catching;
        collectedCount = 0;
        for (int i = 0; i < got.Length; i++)
            got[i] = false;
        spawnTimer = 0f;

        ClearFallingLetters();
        C5Kit.HideBanner(banner);

        dropArea.gameObject.SetActive(true);
        stackArea.gameObject.SetActive(false);
        paddle.anchoredPosition = new Vector2(0f, PaddleY);

        slotRow.SetCount(0);
        UpdateCatchTexts();
        bottomText.text = "<color=" + C5Kit.YellowHex + ">S · T · A · R · T</color> 문자만 받으세요. (순서 상관없음)";

        SpawnLetter(true);
    }

    private void UpdateCatchTexts()
    {
        // 아직 못 받은 글자는 노랑, 받은 글자는 회색
        string letters = "";
        for (int i = 0; i < TargetWord.Length; i++)
            letters += "<color=" + (got[i] ? "#5A6487" : C5Kit.YellowHex) + ">" + TargetWord[i] + "</color>" +
                       (i < TargetWord.Length - 1 ? " " : "");
        plateText.text = "NEED : <size=44>" + letters + "</size>";
    }

    private void UpdatePaddle()
    {
        float nextX = paddle.anchoredPosition.x;
        float key = C5Kit.KeyAxis().x;

        if (Mathf.Abs(key) > 0.01f)
        {
            nextX += key * paddleMoveSpeed * Time.deltaTime;
        }
        else if (C5Kit.TryMouse(out Vector2 mouse))
        {
            Camera cam = C5Kit.UiCamera(dropArea);
            if (RectTransformUtility.RectangleContainsScreenPoint(dropArea, mouse, cam) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(dropArea, mouse, cam, out Vector2 local))
            {
                nextX = Mathf.Lerp(nextX, local.x, 1f - Mathf.Exp(-22f * Time.deltaTime));
            }
        }

        float limit = dropArea.rect.width * 0.5f - paddle.rect.width * 0.5f - 14f;
        paddle.anchoredPosition = new Vector2(Mathf.Clamp(nextX, -limit, limit), PaddleY);
    }

    private void UpdateSpawning()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval)
            return;
        spawnTimer = 0f;
        if (activeLetters.Count < 22)
            SpawnLetter(false);
    }

    // 아직 받아야 하는 글자면 그 칸 번호, 아니면 -1 (T는 비어 있는 T 칸)
    private int NeededIndex(char c)
    {
        for (int i = 0; i < TargetWord.Length; i++)
            if (TargetWord[i] == c && !got[i])
                return i;
        return -1;
    }

    private void SpawnLetter(bool forceTarget)
    {
        if (collectedCount >= TargetWord.Length)
            return;

        // 필요한 글자 중 하나를 고르거나, 필요 없는 글자를 고름
        List<char> needed = new List<char>();
        for (int i = 0; i < TargetWord.Length; i++)
            if (!got[i]) needed.Add(TargetWord[i]);
        char letter = needed[Random.Range(0, needed.Count)];

        if (!forceTarget && Random.value > targetChance)
        {
            do
            {
                letter = LetterPool[Random.Range(0, LetterPool.Length)];
            }
            while (NeededIndex(letter) >= 0);
        }

        float limitX = dropArea.rect.width * 0.5f - fallingLetterSize.x;
        float x = forceTarget ? Random.Range(-200f, 200f) : Random.Range(-limitX, limitX);

        // 이미 떨어지는 문자와 너무 겹치지 않게 몇 번 다시 뽑기
        for (int tries = 0; tries < 4; tries++)
        {
            bool crowded = false;
            foreach (FallingLetter f in activeLetters)
            {
                if (Mathf.Abs(f.rect.anchoredPosition.x - x) < fallingLetterSize.x &&
                    f.rect.anchoredPosition.y > dropArea.rect.height * 0.5f - 200f)
                {
                    crowded = true;
                    break;
                }
            }
            if (!crowded) break;
            x = Random.Range(-limitX, limitX);
        }

        RectTransform rect = C5Kit.Rect(lettersLayer, "FallingLetter_" + letter,
            new Vector2(x, dropArea.rect.height * 0.5f - 45f), fallingLetterSize);

        // 위로 남는 점선 꼬리
        for (int i = 0; i < 4; i++)
        {
            C5Kit.Img(rect, "Trail", null, new Color(0.75f, 0.95f, 1f, 0.55f - i * 0.11f),
                new Vector2(0f, fallingLetterSize.y * 0.5f + 12f + i * 16f), new Vector2(4f, 9f));
        }

        Sprite art = AlphabetSprite(letter);
        Image img = C5Kit.Img(rect, "Art", art, art != null ? C5Kit.White : C5Kit.Navy, Vector2.zero,
            fallingLetterSize, true);
        if (art == null)
        {
            C5Kit.AddOutline(img, C5Kit.Cyan, 3f);
            C5Kit.Txt(rect, "Letter", letter.ToString(), 52f, C5Kit.White, TextAlignmentOptions.Center,
                Vector2.zero, fallingLetterSize, font);
        }

        // 지금 받아야 할 문자에만 반짝임
        RectTransform spark = C5Kit.Rect(rect, "Spark", Vector2.zero, Vector2.zero);
        AddSparkLine(spark, new Vector2(52f, 30f), 20f);
        AddSparkLine(spark, new Vector2(60f, 8f), -10f);
        AddSparkLine(spark, new Vector2(40f, 50f), 55f);

        FallingLetter data = new FallingLetter
        {
            obj = rect.gameObject,
            rect = rect,
            character = letter,
            speed = Random.Range(minFallSpeed, maxFallSpeed),
            spark = spark.gameObject
        };
        data.spark.SetActive(NeededIndex(letter) >= 0);
        activeLetters.Add(data);
    }

    private void AddSparkLine(RectTransform parent, Vector2 pos, float angle)
    {
        Image line = C5Kit.Img(parent, "SparkLine", null, C5Kit.Yellow, pos, new Vector2(18f, 6f));
        line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateSparks()
    {
        if (collectedCount >= TargetWord.Length)
            return;
        float a = 0.55f + 0.45f * Mathf.Sin(Time.time * 10f);
        foreach (FallingLetter f in activeLetters)
        {
            if (f.spark == null) continue;
            bool on = NeededIndex(f.character) >= 0;
            if (f.spark.activeSelf != on) f.spark.SetActive(on);
            if (on)
                foreach (Image i in f.spark.GetComponentsInChildren<Image>())
                    i.color = new Color(C5Kit.Yellow.r, C5Kit.Yellow.g, C5Kit.Yellow.b, a);
        }
    }

    private Sprite AlphabetSprite(char c)
    {
        int i = c - 'A';
        if (alphabetSprites == null || i < 0 || i >= alphabetSprites.Length)
            return null;
        return alphabetSprites[i];
    }

    private void UpdateFallingLetters()
    {
        Rect pad = C5Kit.RectIn(dropArea, paddle);
        // 받침대 판정은 위쪽 절반만 (옆에서 스치면 안 받힘)
        pad = Rect.MinMaxRect(pad.xMin + 6f, pad.center.y, pad.xMax - 6f, pad.yMax + 4f);

        for (int i = activeLetters.Count - 1; i >= 0; i--)
        {
            FallingLetter item = activeLetters[i];
            Vector2 p = item.rect.anchoredPosition;
            p.y -= item.speed * Time.deltaTime;
            item.rect.anchoredPosition = p;

            Rect r = new Rect(p.x - fallingLetterSize.x * 0.4f, p.y - fallingLetterSize.y * 0.5f,
                fallingLetterSize.x * 0.8f, fallingLetterSize.y * 0.5f);

            if (pad.Overlaps(r))
            {
                activeLetters.RemoveAt(i);
                ReceiveLetter(item);
                if (phase != Phase.Catching && phase != Phase.Error)
                    return;
                continue;
            }

            if (p.y < -dropArea.rect.height * 0.5f - 80f)
            {
                activeLetters.RemoveAt(i);
                Destroy(item.obj);
            }
        }
    }

    private void ReceiveLetter(FallingLetter item)
    {
        int index = NeededIndex(item.character);

        if (index < 0)
        {
            StartCoroutine(Shatter(item.rect));
            if (wrongRoutine != null)
                StopCoroutine(wrongRoutine);
            wrongRoutine = StartCoroutine(WrongLetterRoutine());
            return;
        }

        got[index] = true;
        collectedCount++;
        GameSfx.Play("piece_place", 0.6f);
        StartCoroutine(FlyToSlot(item, index));

        if (collectedCount >= TargetWord.Length)
        {
            StartCoroutine(FinishCatch());
            return;
        }

        UpdateCatchTexts();
    }

    private IEnumerator FlyToSlot(FallingLetter item, int index)
    {
        if (item.spark != null)
            item.spark.SetActive(false);
        foreach (Image trail in item.obj.GetComponentsInChildren<Image>())
            if (trail.name == "Trail") trail.enabled = false;

        // 잘리지 않게 판 밖 화면 루트로 옮겨서 슬롯까지 날아감
        item.rect.SetParent(screen, true);
        item.rect.SetAsLastSibling();
        yield return C5Kit.FlyTo(item.rect, slotRow.Slot(index).position, 0.32f, 0.75f);

        if (item.obj != null)
            Destroy(item.obj);

        slotRow.SetSlot(index, true);
        yield return C5Kit.Pop(slotRow.Slot(index), 1.25f);
    }

    private IEnumerator Shatter(RectTransform rect)
    {
        if (rect == null) yield break;
        Image[] imgs = rect.GetComponentsInChildren<Image>();
        float e = 0f;
        while (e < 0.3f && rect != null)
        {
            e += Time.deltaTime;
            rect.localScale = Vector3.one * (1f + e * 1.6f);
            foreach (Image i in imgs)
                if (i != null) i.color = new Color(1f, 0.35f, 0.55f, 1f - e / 0.3f);
            yield return null;
        }
        if (rect != null)
            Destroy(rect.gameObject);
    }

    // 잘못 받으면: 오류 횟수(ERROR x/5)만 1 올라가고, 받은 글자는 그대로 · 게임은 계속
    private Coroutine wrongRoutine;

    private IEnumerator WrongLetterRoutine()
    {
        phase = Phase.Error;   // 업적(한 글자도 놓치지 않아)이 이걸 보고 실수로 기록
        if (countMistakes)
            ReportMistake();

        GameSfx.Play("wrong_buzz", 0.6f);
        C5Kit.SetBanner(banner, bannerText, "ERROR\n<size=34>INVALID CHARACTER</size>", C5Kit.Pink);
        bottomText.text = "필요 없는 문자입니다! <color=" + C5Kit.PinkHex + ">오류 +1</color>";
        StartCoroutine(C5Kit.Shake(panel, 0.3f, 10f));

        yield return new WaitForSeconds(0.3f);
        if (phase == Phase.Error)
            phase = Phase.Catching;

        yield return new WaitForSeconds(0.6f);
        C5Kit.HideBanner(banner);
        if (phase == Phase.Catching)
            bottomText.text = "<color=" + C5Kit.YellowHex + ">S · T · A · R · T</color> 문자만 받으세요. (순서 상관없음)";
        wrongRoutine = null;
    }

    private void ClearFallingLetters()
    {
        foreach (FallingLetter item in activeLetters)
            if (item.obj != null)
                Destroy(item.obj);
        activeLetters.Clear();
    }

    // 다 받았으면 바로 START 결합
    private IEnumerator FinishCatch()
    {
        phase = Phase.Stacking;   // 더 이상 문자 안 떨어짐
        plateText.text = "CHARACTER <color=" + C5Kit.GreenHex + ">RECEIVED</color>";
        bottomText.text = "";

        yield return new WaitForSeconds(0.6f);   // 마지막 글자가 슬롯에 들어갈 때까지
        ClearFallingLetters();
        dropArea.gameObject.SetActive(false);

        Transform[] from = new Transform[5];
        for (int i = 0; i < 5; i++)
            from[i] = slotRow.Slot(i);
        yield return C5Kit.MergeLetters(screen, slotLetterSprites, font, new Vector2(0f, 40f), new Vector2(88f, 81f), from);

        phase = Phase.StartReady;
        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
        StartCoroutine(C5Kit.Pop(finalStartButton.transform, 1.25f, 0.3f));
        bottomText.text = "<color=" + C5Kit.YellowHex + ">START</color>를 눌러 검사를 완료하세요.";
    }

    // =========================================
    // (예전) 2단계: 탑 쌓기 - 지금은 쓰지 않음
    // =========================================
    private IEnumerator EnterStackStage()
    {
        phase = Phase.Stacking;
        ClearFallingLetters();
        plateText.text = "CHARACTER RECEIVED";
        bottomText.text = "수신 완료!";

        yield return new WaitForSeconds(0.75f);
        yield return C5Kit.Glitch(panel, null, 0.25f, 0.6f);

        dropArea.gameObject.SetActive(false);
        stackArea.gameObject.SetActive(true);
        slotRow.SetCount(0);
        stackedCount = 0;

        CreateStackObjects();
        UpdateStackTexts();
        bottomText.text = "블록을 <color=" + C5Kit.YellowHex + ">S-T-A-R-T</color> 순서대로 끌어서 쌓으세요.";
    }

    private void CreateStackObjects()
    {
        Image baseImg = C5Kit.Img(stackArea, "RT_StackBase", stackBaseSprite,
            stackBaseSprite != null ? C5Kit.White : C5Kit.Yellow, new Vector2(0f, -228f),
            new Vector2(blockSize.x + 120f, 18f));
        stackBase = baseImg.rectTransform;

        Sprite firstArt = BlockSprite(0);
        stackGuide = C5Kit.Img(stackArea, "RT_StackGuide", firstArt, new Color(0.6f, 1f, 1f, 0.3f),
            StackTarget(0), blockSize, true);

        // 왼쪽/오른쪽에 흩어 놓기 (위치는 매번 섞음)
        List<Vector2> homes = new List<Vector2>
        {
            new Vector2(-540f, 135f), new Vector2(-540f, -10f), new Vector2(-540f, -155f),
            new Vector2(540f, 70f), new Vector2(540f, -90f)
        };
        for (int i = homes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 t = homes[i];
            homes[i] = homes[j];
            homes[j] = t;
        }

        for (int i = 0; i < TargetWord.Length; i++)
        {
            Sprite art = BlockSprite(i);
            Image img = C5Kit.Img(stackArea, "StackBlock_" + TargetWord[i] + "_" + i, art,
                art != null ? C5Kit.White : C5Kit.Cyan, homes[i], blockSize, true);
            img.raycastTarget = true;
            if (art == null)
                C5Kit.Txt(img.transform, "Letter", TargetWord[i].ToString(), 40f, C5Kit.Navy,
                    TextAlignmentOptions.Center, Vector2.zero, blockSize, font);

            MG26StackBlockDrag drag = img.gameObject.AddComponent<MG26StackBlockDrag>();
            drag.Configure(this, i, img.rectTransform, stackArea);

            blocks.Add(img.rectTransform);
            blockHomes.Add(homes[i]);
        }

        RefreshBlockDim();
    }

    private Sprite BlockSprite(int i)
    {
        return stackBlockSprites != null && i < stackBlockSprites.Length ? stackBlockSprites[i] : null;
    }

    private Vector2 StackTarget(int index)
    {
        float baseTop = -228f + 9f;
        return new Vector2(0f, baseTop + 2f + blockSize.y * 0.5f + index * (blockSize.y + stackGap));
    }

    private void RefreshBlockDim()
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            Image img = blocks[i].GetComponent<Image>();
            bool active = i == stackedCount;
            bool placed = i < stackedCount;
            Color c = img.sprite != null ? C5Kit.White : C5Kit.Cyan;
            c.a = active || placed ? 1f : 0.45f;
            img.color = c;
        }

        if (stackGuide != null)
        {
            bool show = stackedCount < TargetWord.Length;
            stackGuide.gameObject.SetActive(show);
            if (show)
            {
                stackGuide.sprite = BlockSprite(stackedCount);
                stackGuide.rectTransform.anchoredPosition = StackTarget(stackedCount);
                stackGuide.transform.SetAsFirstSibling();
            }
        }
    }

    private void PulseGuide()
    {
        if (stackGuide == null || !stackGuide.gameObject.activeSelf)
            return;
        float a = 0.18f + 0.17f * (0.5f + 0.5f * Mathf.Sin(Time.time * 5f));
        stackGuide.color = new Color(0.6f, 1f, 1f, a);
    }

    private void UpdateStackTexts()
    {
        string letter = stackedCount < TargetWord.Length ? TargetWord[stackedCount].ToString() : "-";
        plateText.text = "STACK : <color=" + C5Kit.YellowHex + "><size=46>" + letter + "</size></color>";
    }

    public bool CanDragBlock(int index)
    {
        return isGameActive && !finished && phase == Phase.Stacking && blocks.Count == TargetWord.Length &&
               index == stackedCount;
    }

    public void TryPlaceBlock(int index, RectTransform block)
    {
        if (!CanDragBlock(index))
            return;

        Vector2 target = StackTarget(index);
        if (Vector2.Distance(block.anchoredPosition, target) > snapDistance)
        {
            StartCoroutine(ReturnHome(block, blockHomes[index]));
            bottomText.text = "빛나는 자리에 맞춰 놓으세요.";
            return;
        }

        block.anchoredPosition = target;
        MG26StackBlockDrag drag = block.GetComponent<MG26StackBlockDrag>();
        if (drag != null)
            drag.SetPlaced(true);

        GameSfx.Play("block_place", 0.6f);
        StartCoroutine(C5Kit.Pop(block, 1.1f, 0.18f));
        slotRow.SetSlot(index, true);
        StartCoroutine(C5Kit.Pop(slotRow.Slot(index), 1.2f));

        stackedCount++;
        RefreshBlockDim();

        if (stackedCount >= TargetWord.Length)
        {
            BeginStabilityCheck();
        }
        else
        {
            UpdateStackTexts();
            bottomText.text = "다음 블록 : <color=" + C5Kit.YellowHex + ">" + TargetWord[stackedCount] + "</color>";
        }
    }

    private IEnumerator ReturnHome(RectTransform block, Vector2 home)
    {
        Vector2 from = block.anchoredPosition;
        float e = 0f;
        while (e < 0.2f && block != null)
        {
            e += Time.deltaTime;
            block.anchoredPosition = Vector2.Lerp(from, home, C5Kit.EaseOut(e / 0.2f));
            yield return null;
        }
    }

    private void BeginStabilityCheck()
    {
        phase = Phase.Stability;
        stabilityTimer = 0f;
        bottomText.text = "탑이 안정될 때까지 기다리세요...";
    }

    private void UpdateStability()
    {
        stabilityTimer += Time.deltaTime;
        float remain = Mathf.Max(0f, stableSeconds - stabilityTimer);
        plateText.text = "STABILITY CHECK  <color=" + C5Kit.YellowHex + ">" + remain.ToString("0.0") + "</color>";

        // 탑이 살짝 흔들렸다가 잦아듦
        float wobble = Mathf.Sin(stabilityTimer * 9f) * 1.6f * (remain / stableSeconds);
        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i] != null)
                blocks[i].localRotation = Quaternion.Euler(0f, 0f, wobble * (i + 1) * 0.3f);

        if (stabilityTimer >= stableSeconds)
            StartCoroutine(ShowFinalStart());
    }

    private IEnumerator ShowFinalStart()
    {
        phase = Phase.StartReady;
        plateText.text = "START <color=" + C5Kit.GreenHex + ">READY</color>";
        bottomText.text = "";

        Transform[] from = new Transform[blocks.Count];
        for (int i = 0; i < blocks.Count; i++)
        {
            from[i] = blocks[i];
            blocks[i].localRotation = Quaternion.identity;
        }

        // 탑 블록 자리에서 글자가 빠져나와 가운데로 모임 (블록·받침은 숨김)
        Vector3[] fromPos = new Vector3[from.Length];
        for (int i = 0; i < from.Length; i++)
            fromPos[i] = from[i].position;
        foreach (RectTransform b in blocks)
            if (b != null) b.gameObject.SetActive(false);
        if (stackBase != null) stackBase.gameObject.SetActive(false);

        Transform[] anchors = new Transform[from.Length];
        for (int i = 0; i < from.Length; i++)
        {
            RectTransform a = C5Kit.Rect(screen, "RT_MergeFrom" + i, Vector2.zero, Vector2.zero);
            a.position = fromPos[i];
            anchors[i] = a;
        }

        Vector2 center = new Vector2(0f, 40f);
        yield return C5Kit.MergeLetters(screen, slotLetterSprites, font, center, new Vector2(88f, 81f), anchors);

        foreach (Transform a in anchors)
            if (a != null) Destroy(a.gameObject);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
        StartCoroutine(C5Kit.Pop(finalStartButton.transform, 1.25f, 0.3f));
        bottomText.text = "<color=" + C5Kit.YellowHex + ">START</color>를 눌러 검사를 완료하세요.";
    }

    private void OnFinalStartClicked()
    {
        if (!isGameActive || finished || phase != Phase.StartReady)
            return;

        finished = true;
        phase = Phase.Completed;
        Success();
    }

    protected override void GiveHint()
    {
        if (bottomText == null) return;
        if (phase == Phase.Catching)
            bottomText.text = "힌트: 반짝이는 문자만 받으세요. 다른 문자는 피하세요.";
        else if (phase == Phase.Stacking)
            bottomText.text = "힌트: 빛나는 자리로 블록을 끌어다 놓으세요.";
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}

// =========================================
// 탑 블록 드래그
// =========================================
public class MG26StackBlockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Minigame26_FallingLetters manager;
    private int blockIndex;
    private RectTransform blockRect;
    private RectTransform parentArea;
    private Vector2 pointerOffset;
    private bool dragging;
    private bool placed;

    public void Configure(Minigame26_FallingLetters owner, int index, RectTransform rect, RectTransform area)
    {
        manager = owner;
        blockIndex = index;
        blockRect = rect;
        parentArea = area;
        dragging = false;
        placed = false;
    }

    public void SetPlaced(bool value)
    {
        placed = value;
        dragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed || manager == null || !manager.CanDragBlock(blockIndex))
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentArea, eventData.position,
                eventData.pressEventCamera, out Vector2 local))
            return;

        pointerOffset = blockRect.anchoredPosition - local;
        dragging = true;
        blockRect.SetAsLastSibling();
        blockRect.localScale = Vector3.one * 1.06f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || placed)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentArea, eventData.position,
                eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 next = local + pointerOffset;
        float limitX = parentArea.rect.width * 0.5f - blockRect.rect.width * 0.5f;
        float limitY = parentArea.rect.height * 0.5f - blockRect.rect.height * 0.5f;
        next.x = Mathf.Clamp(next.x, -limitX, limitX);
        next.y = Mathf.Clamp(next.y, -limitY, limitY);
        blockRect.anchoredPosition = next;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        blockRect.localScale = Vector3.one;
        if (!placed && manager != null)
            manager.TryPlaceBlock(blockIndex, blockRect);
    }
}
