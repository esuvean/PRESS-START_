using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MG27 손상된 실행 회로 (실행 장치 복구 검사)
///  ① 컨베이어벨트에서 부품을 클릭해 조립 슬롯에 넣는다 (POWER / INPUT / DISPLAY / START)
///  ② 정상 부품 4개를 모으면 START 버튼 완성 → 클릭
///  - 함정(BROKEN POWER / WRONG INPUT / BROKEN DISPLAY)도 슬롯에 들어가 'SYSTEM READY'가 잠깐 뜨지만
///    곧 글리치와 함께 'SYSTEM ERROR' → 불량 부품만 빠져나감 (가짜 성공, 업적 29번)
///  - EXIT 은 부품이 아님 (넣을 수 없음)
/// 화면은 스크립트가 직접 만듦. 그림은 에디터 도구(Chapter5SkinTool2630)가 넣어 줌.
/// </summary>
public class Minigame27_DamagedCircuit : MinigameBase
{
    [Header("챕터5 그림 (에디터 도구가 자동으로 넣음)")]
    public TMP_FontAsset uiFont;
    public Sprite panelSprite;           // 23_line_back
    public Sprite labelSprite;           // 26_upline
    public Sprite railSprite;            // 27_rail
    public Sprite assemblyPlateSprite;   // 27_assembly_line
    public Sprite slotSprite;            // 27_big_blank
    public Sprite arrowSprite;           // 26_arrow_left (>>)
    public Sprite powerSprite;           // 27_power
    public Sprite inputSprite;           // 27_input
    public Sprite displaySprite;         // 27_display
    public Sprite startSprite;           // 27_start
    public Sprite brokenPowerSprite;     // 27_brokenpower
    public Sprite wrongInputSprite;      // 27_wronginput
    public Sprite brokenDisplaySprite;   // 27_brokendisplay
    public Sprite exitSprite;            // 27_exit

    [Header("규칙")]
    [Tooltip("가짜 성공(불량 부품 조립)이 나면 챕터5 오류 횟수에 포함")]
    public bool countMistakes = true;

    [Header("컨베이어")]
    public float partSize = 124f;
    public float beltSpeed = 120f;
    public float spawnInterval = 1.6f;
    [Tooltip("벨트 위 부품 사이 최소 빈 공간")]
    public float minPartGap = 70f;
    [Range(0f, 1f)] public float neededPartChance = 0.4f;   // 정답 4 : 오답 6

    private enum GamePhase
    {
        Selecting,
        Verifying,
        StartReady,
        Completed
    }

    private enum PartType
    {
        Power,
        Input,
        Display,
        Start,
        BrokenPower,
        WrongInput,
        BrokenDisplay,
        Exit
    }

    private class MovingPart
    {
        public RectTransform rect;
        public PartType type;
        public bool taken;
    }

    private class Slot
    {
        public RectTransform rect;
        public Image content;
        public bool filled;
        public bool trap;
    }

    // ---- 배치 (1920x1080 기준) ----
    private static readonly Vector2 PanelPos = new Vector2(0f, 25f);
    private static readonly Vector2 PanelSize = new Vector2(1500f, 600f);
    private static readonly Vector2 BeltPos = new Vector2(0f, 128f);
    private static readonly Vector2 BeltSize = new Vector2(1400f, 210f);
    private const float PartY = 22f;          // 벨트 영역 안에서 부품 높이
    private const float SlotY = -180f;
    private static readonly float[] SlotX = { -375f, -225f, -75f, 75f };
    private static readonly Vector2 StartSpotPos = new Vector2(390f, -180f);

    private GamePhase phase;
    private bool finished;
    private float spawnTimer;
    private int filledCount;

    private readonly List<MovingPart> parts = new List<MovingPart>();
    private readonly Slot[] slots = new Slot[4];

    // ---- 만든 UI ----
    private TMP_FontAsset font;
    private RectTransform screen;
    private RectTransform panel;
    private RectTransform belt;
    private TextMeshProUGUI assemblyText;
    private TMP_Text circuitStatusText;     // 업적(MG27FakeReadyAchievement)이 이 글자를 지켜봄
    private CanvasGroup banner;
    private TextMeshProUGUI bannerText;
    private Image startSpot;
    private Button finalStartButton;
    private Coroutine messageRoutine;

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "실행 장치 복구 검사";
        instruction = "정상 부품 4개를 골라 START 버튼을 완성하세요.";

        base.StartMinigame();
        StopAllCoroutines();
        messageRoutine = null;

        finished = false;
        filledCount = 0;
        spawnTimer = 0f;
        parts.Clear();

        BuildScreen();
        C5Kit.PrepareNoise(transform);

        phase = GamePhase.Selecting;
        UpdateAssemblyText();
        SetStatus("정상 부품과 회로를 <color=" + C5Kit.YellowHex + ">복구</color>하세요.");

        // 처음부터 벨트가 비어 있지 않게 몇 개 올려 둠
        float[] xs = { -560f, -365f, -170f, 25f, 220f, 415f };
        PartType[] first =
        {
            PartType.BrokenPower, PartType.Power, PartType.Exit, PartType.WrongInput,
            PartType.Input, PartType.BrokenDisplay
        };
        for (int i = 0; i < xs.Length; i++)
            SpawnPart(first[i], xs[i]);
    }

    // =========================================
    // 화면 만들기
    // =========================================
    private void BuildScreen()
    {
        font = C5Kit.ResolveFont(transform, uiFont);

        // 업적이 지켜보는 글자(CircuitStatusText)는 프리팹 것을 그대로 옮겨 씀
        if (circuitStatusText == null)
            circuitStatusText = FindText("CircuitStatusText");
        if (circuitStatusText != null)
            circuitStatusText.transform.SetParent(transform, false);

        C5Kit.HideLegacy(transform);
        screen = C5Kit.NewScreen(transform, "RT_MG27");

        panel = C5Kit.Panel(screen, panelSprite, PanelPos, PanelSize).rectTransform;

        // ① 부품 선택
        C5Kit.Plate(screen, labelSprite, font, new Vector2(-520f, 300f), new Vector2(340f, 62f),
            "① 부품 선택", 30f);

        Image rail = C5Kit.Img(screen, "RT_Rail", railSprite, railSprite != null ? C5Kit.White : C5Kit.Navy,
            new Vector2(0f, 40f), new Vector2(1380f, 120f));
        if (railSprite == null)
            C5Kit.AddOutline(rail, C5Kit.Cyan, 3f);

        belt = C5Kit.Rect(screen, "RT_Belt", BeltPos, BeltSize);
        belt.gameObject.AddComponent<RectMask2D>();

        // ② 조립 슬롯
        C5Kit.Plate(screen, labelSprite, font, new Vector2(-520f, -62f), new Vector2(340f, 62f),
            "② 조립 슬롯", 30f);

        Image plate = C5Kit.Img(screen, "RT_AssemblyPlate", assemblyPlateSprite,
            assemblyPlateSprite != null ? C5Kit.White : C5Kit.DeepNavy, new Vector2(0f, -62f),
            new Vector2(340f, 70f));
        assemblyText = C5Kit.Txt(plate.transform, "Text", "", 34f, C5Kit.White, TextAlignmentOptions.Center,
            new Vector2(0f, 2f), new Vector2(340f, 70f), font);

        Sprite[] ghosts = { powerSprite, inputSprite, displaySprite, startSprite };
        for (int i = 0; i < 4; i++)
        {
            Image box = C5Kit.Img(screen, "RT_Slot" + i, slotSprite, slotSprite != null ? C5Kit.White : C5Kit.Navy,
                new Vector2(SlotX[i], SlotY), new Vector2(128f, 128f), true);
            if (slotSprite == null)
                C5Kit.AddOutline(box, C5Kit.Cyan, 3f);
            Image content = C5Kit.Img(box.transform, "Content", ghosts[i], new Color(1f, 1f, 1f, 0.16f),
                Vector2.zero, new Vector2(108f, 108f), true);
            slots[i] = new Slot { rect = box.rectTransform, content = content };
        }

        if (arrowSprite != null)
            C5Kit.Img(screen, "RT_Arrow", arrowSprite, C5Kit.White, new Vector2(228f, SlotY), new Vector2(72f, 56f), true);

        // ③ START 자리: 처음엔 흐릿하게, 정상 부품 4개가 모이면 켜져서 누를 수 있음
        Image spotBox = C5Kit.Img(screen, "RT_StartSpot", slotSprite, slotSprite != null ? C5Kit.White : C5Kit.Navy,
            StartSpotPos, new Vector2(150f, 150f), true);
        startSpot = C5Kit.Img(spotBox.transform, "StartArt", startSprite, new Color(1f, 1f, 1f, 0.2f), Vector2.zero,
            new Vector2(130f, 130f), true);
        finalStartButton = C5Kit.MakeButton(startSpot, OnFinalStartClicked);
        finalStartButton.transition = Selectable.Transition.None;   // 밝기는 스크립트가 직접 바꿈
        finalStartButton.interactable = false;

        // 아래 안내 문구 = CircuitStatusText
        if (circuitStatusText == null)
        {
            circuitStatusText = C5Kit.BottomText(screen, font, "");
            circuitStatusText.name = "CircuitStatusText";
        }
        else
        {
            circuitStatusText.transform.SetParent(screen, false);
            circuitStatusText.gameObject.SetActive(true);
            RectTransform rt = circuitStatusText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = C5Kit.BottomTextPos;
            rt.sizeDelta = new Vector2(1500f, 60f);
            rt.localScale = Vector3.one;
            circuitStatusText.font = font;
            circuitStatusText.fontSize = 32f;
            circuitStatusText.enableAutoSizing = false;
            circuitStatusText.color = C5Kit.White;
            circuitStatusText.alignment = TextAlignmentOptions.Center;
            circuitStatusText.richText = true;
            circuitStatusText.textWrappingMode = TextWrappingModes.NoWrap;
            circuitStatusText.overflowMode = TextOverflowModes.Overflow;
            circuitStatusText.raycastTarget = false;
        }

        banner = C5Kit.Banner(screen, font, new Vector2(0f, 120f), new Vector2(760f, 170f), out bannerText);
    }

    private TMP_Text FindText(string objectName)
    {
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            if (t.name == objectName)
                return t;
        return null;
    }

    private void SetStatus(string text)
    {
        if (circuitStatusText != null)
            circuitStatusText.text = text;
    }

    private void UpdateAssemblyText()
    {
        assemblyText.text = "ASSEMBLY  <color=" + C5Kit.YellowHex + "><size=40>" + filledCount + "</size></color> / 4";
    }

    // =========================================
    // 매 프레임
    // =========================================
    protected override void Update()
    {
        // 글자를 다 모아 START 버튼만 남았으면 시간은 멈춤 (다 해 놓고 시간 초과로 실패하지 않게)
        bool timerFrozen = phase == GamePhase.StartReady || phase == GamePhase.Completed;
        if (!timerFrozen)
            base.Update();
        if (!isGameActive || finished)
            return;

        // 플레이 중에 스크립트가 다시 읽히면(코드 수정) 슬롯 정보가 사라짐 → 멈춰 있기
        if (slots[0] == null || belt == null)
            return;

        MoveParts();

        if (phase == GamePhase.Selecting)
        {
            // 맨 뒤 부품이 한 칸만큼 나아가면 바로 다음 부품 (항상 같은 간격)
            float spawnX = -BeltSize.x * 0.5f - partSize * 0.6f;
            if (HasRoomAt(spawnX))
                SpawnPart(PickPart(), LastPartX() - (partSize + minPartGap));
        }


    }

    private float LastPartX()
    {
        float min = float.MaxValue;
        foreach (MovingPart p in parts)
            if (p.rect != null && !p.taken)
                min = Mathf.Min(min, p.rect.anchoredPosition.x);
        float spawnX = -BeltSize.x * 0.5f - partSize * 0.6f;
        return min == float.MaxValue ? spawnX + partSize + minPartGap : Mathf.Max(min, spawnX + partSize + minPartGap);
    }

    // 새 부품이 나올 자리 근처에 다른 부품이 있으면 기다림 (촘촘하지 않게)
    private bool HasRoomAt(float x)
    {
        foreach (MovingPart p in parts)
        {
            if (p.rect == null || p.taken) continue;
            if (p.rect.anchoredPosition.x - x < partSize + minPartGap)
                return false;
        }
        return true;
    }

    private PartType PickPart()
    {
        List<PartType> needed = new List<PartType>();
        for (int i = 0; i < 4; i++)
            if (!slots[i].filled || slots[i].trap)
                needed.Add((PartType)i);

        if (needed.Count > 0 && Random.value < neededPartChance)
            return needed[Random.Range(0, needed.Count)];

        // 나머지(약 60%)는 불량 부품 / EXIT
        PartType[] others =
        {
            PartType.BrokenPower, PartType.WrongInput, PartType.BrokenDisplay, PartType.Exit
        };
        return others[Random.Range(0, others.Length)];
    }

    private void SpawnPart(PartType type, float x)
    {
        Sprite art = PartSprite(type);
        Image img = C5Kit.Img(belt, "Part_" + type, art, art != null ? C5Kit.White : C5Kit.Navy,
            new Vector2(x, PartY), new Vector2(partSize, partSize), true);
        if (art == null)
        {
            C5Kit.AddOutline(img, IsTrap(type) ? C5Kit.Pink : C5Kit.Cyan, 3f);
            C5Kit.Txt(img.transform, "Label", PartName(type), 20f, C5Kit.White, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(partSize, partSize), font);
        }

        MovingPart part = new MovingPart { rect = img.rectTransform, type = type };
        C5Kit.MakeButton(img, () => OnPartClicked(part));
        parts.Add(part);
    }

    // 벨트: 모든 부품이 같은 간격으로 줄지어 이동.
    // 부품을 하나 집어 가서 빈자리가 생기면, 뒤(왼쪽) 부품들이 조금 빨리 따라와서 빈자리를 메움
    private void MoveParts()
    {
        parts.RemoveAll(p => p.rect == null || p.taken);
        parts.Sort((a, b) => b.rect.anchoredPosition.x.CompareTo(a.rect.anchoredPosition.x));   // 오른쪽(앞)부터

        float spacing = partSize + minPartGap;
        float limit = BeltSize.x * 0.5f + partSize;
        float catchUpSpeed = beltSpeed * 3f;

        for (int i = 0; i < parts.Count; i++)
        {
            RectTransform rt = parts[i].rect;
            Vector2 pos = rt.anchoredPosition;
            float speed = beltSpeed;

            if (i > 0)
            {
                float aheadX = parts[i - 1].rect.anchoredPosition.x;
                float gap = aheadX - pos.x;
                if (gap > spacing + 2f)
                    speed = catchUpSpeed;                       // 빈자리 메우기
                pos.x = Mathf.Min(pos.x + speed * Time.deltaTime, aheadX - spacing);
            }
            else
            {
                pos.x += speed * Time.deltaTime;
            }

            rt.anchoredPosition = pos;
        }

        for (int i = parts.Count - 1; i >= 0; i--)
        {
            if (parts[i].rect.anchoredPosition.x > limit)
            {
                Destroy(parts[i].rect.gameObject);
                parts.RemoveAt(i);
            }
        }
    }

    // =========================================
    // 부품 클릭
    // =========================================
    private void OnPartClicked(MovingPart part)
    {
        if (!isGameActive || finished || phase != GamePhase.Selecting || part.taken || part.rect == null)
            return;

        if (part.type == PartType.Exit)
        {
            part.taken = true;
            GameSfx.Play("wrong_buzz", 0.5f);
            StartCoroutine(BreakAway(part.rect));
            ShowMessage("EXIT 은 실행 장치 부품이 아닙니다.");
            return;
        }

        int slotIndex = SlotOf(part.type);
        Slot slot = slots[slotIndex];

        // 정상 부품이 이미 들어 있으면 또 넣을 필요 없음
        if (slot.filled && !slot.trap)
        {
            StartCoroutine(C5Kit.Shake(part.rect, 0.2f, 6f));
            ShowMessage(PartName((PartType)slotIndex) + " 자리에는 이미 정상 부품이 있습니다.");
            return;
        }

        bool wasFilled = slot.filled;
        part.taken = true;
        slot.filled = true;
        slot.trap = IsTrap(part.type);
        if (!wasFilled)
            filledCount++;

        StartCoroutine(InstallRoutine(part, slotIndex));
    }

    private IEnumerator InstallRoutine(MovingPart part, int slotIndex)
    {
        Slot slot = slots[slotIndex];
        Sprite art = PartSprite(part.type);

        // 클릭 막기 + 판 밖으로 꺼내서 슬롯까지 날아감
        Button b = part.rect.GetComponent<Button>();
        if (b != null) b.interactable = false;
        part.rect.SetParent(screen, true);
        part.rect.SetAsLastSibling();

        GameSfx.Play("piece_place", 0.6f);
        yield return C5Kit.FlyTo(part.rect, slot.rect.position, 0.3f, 0.82f);
        if (part.rect != null)
            Destroy(part.rect.gameObject);

        slot.content.sprite = art;
        slot.content.color = C5Kit.White;
        StartCoroutine(C5Kit.Pop(slot.rect, 1.15f));

        UpdateAssemblyText();

        if (phase == GamePhase.Selecting && filledCount >= 4)
            StartCoroutine(VerifyRoutine());
    }

    // =========================================
    // 4개가 모이면 검사
    // =========================================
    private IEnumerator VerifyRoutine()
    {
        phase = GamePhase.Verifying;
        HideMessage();

        bool anyTrap = false;
        foreach (Slot s in slots)
            if (s.trap) anyTrap = true;

        SetStatus("SYSTEM CHECK...");
        yield return new WaitForSeconds(0.45f);

        // 일단은 모두 성공한 것처럼 보임
        SetStatus("<color=" + C5Kit.GreenHex + ">SYSTEM READY</color>");
        C5Kit.SetBanner(banner, bannerText, "SYSTEM READY", C5Kit.Green);
        startSpot.color = C5Kit.White;
        GameSfx.Play("correct_ding", 0.55f);
        StartCoroutine(C5Kit.Pop(startSpot.transform.parent, 1.15f));

        yield return new WaitForSeconds(1.0f);

        if (!anyTrap)
        {
            C5Kit.HideBanner(banner);
            phase = GamePhase.StartReady;
            finalStartButton.interactable = true;
            SetStatus("START 버튼이 완성되었습니다. <color=" + C5Kit.YellowHex + ">START</color>를 누르세요.");
            yield break;
        }

        // 가짜 성공 → 글리치 → SYSTEM ERROR
        bannerText.text = "SYSTEM ERR0R";
        C5Kit.SetBanner(banner, bannerText, "SYSTEM ERR0R\n<size=30>불량 부품이 감지되었습니다</size>", C5Kit.Pink);
        SetStatus("<color=" + C5Kit.PinkHex + ">SYSTEM ERROR</color> - 불량 부품이 섞여 있습니다.");
        if (countMistakes)
            ReportMistake();
        GameSfx.Play("spark", 0.6f);
        C5Kit.TryCrtNoise(this);
        StartCoroutine(C5Kit.Shake(panel, 0.5f, 14f));
        yield return C5Kit.Glitch(panel, null, 0.55f, 1.3f);

        startSpot.color = new Color(1f, 1f, 1f, 0.2f);

        // 불량 부품만 빠져나감
        Sprite[] ghosts = { powerSprite, inputSprite, displaySprite, startSprite };
        for (int i = 0; i < 4; i++)
        {
            if (!slots[i].trap) continue;
            StartCoroutine(C5Kit.Shake(slots[i].rect, 0.35f, 10f));
            slots[i].content.sprite = ghosts[i];
            slots[i].content.color = new Color(1f, 0.4f, 0.6f, 0.35f);
            slots[i].filled = false;
            slots[i].trap = false;
            filledCount--;
        }
        UpdateAssemblyText();

        yield return new WaitForSeconds(0.9f);
        for (int i = 0; i < 4; i++)
            if (!slots[i].filled)
                slots[i].content.color = new Color(1f, 1f, 1f, 0.16f);

        C5Kit.HideBanner(banner);
        phase = GamePhase.Selecting;
        SetStatus("불량 부품을 빼냈습니다. <color=" + C5Kit.YellowHex + ">정상 부품</color>을 다시 찾으세요.");
    }

    private IEnumerator BreakAway(RectTransform rect)
    {
        if (rect == null) yield break;
        Image img = rect.GetComponent<Image>();
        Vector2 from = rect.anchoredPosition;
        float e = 0f;
        while (e < 0.35f && rect != null)
        {
            e += Time.deltaTime;
            float k = e / 0.35f;
            rect.anchoredPosition = from + new Vector2(Random.Range(-6f, 6f), -k * 60f);
            rect.localScale = Vector3.one * (1f - k * 0.4f);
            if (img != null) img.color = new Color(1f, 0.4f, 0.6f, 1f - k);
            yield return null;
        }
        if (rect != null)
            Destroy(rect.gameObject);
    }

    private void ShowMessage(string text)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text));
    }

    private IEnumerator MessageRoutine(string text)
    {
        SetStatus("<color=" + C5Kit.PinkHex + ">" + text + "</color>");
        yield return new WaitForSeconds(1.6f);
        if (phase == GamePhase.Selecting)
            SetStatus("정상 부품과 회로를 <color=" + C5Kit.YellowHex + ">복구</color>하세요.");
        messageRoutine = null;
    }

    private void HideMessage()
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = null;
    }

    // =========================================
    // 부품 정보
    // =========================================
    private static bool IsTrap(PartType t)
    {
        return t == PartType.BrokenPower || t == PartType.WrongInput || t == PartType.BrokenDisplay ||
               t == PartType.Exit;
    }

    // 부품이 들어갈 슬롯 (불량 부품은 정상 부품 자리로 들어감)
    private static int SlotOf(PartType t)
    {
        switch (t)
        {
            case PartType.Power:
            case PartType.BrokenPower: return 0;
            case PartType.Input:
            case PartType.WrongInput: return 1;
            case PartType.Display:
            case PartType.BrokenDisplay: return 2;
            default: return 3;
        }
    }

    private Sprite PartSprite(PartType t)
    {
        switch (t)
        {
            case PartType.Power: return powerSprite;
            case PartType.Input: return inputSprite;
            case PartType.Display: return displaySprite;
            case PartType.Start: return startSprite;
            case PartType.BrokenPower: return brokenPowerSprite;
            case PartType.WrongInput: return wrongInputSprite;
            case PartType.BrokenDisplay: return brokenDisplaySprite;
            case PartType.Exit: return exitSprite;
        }
        return null;
    }

    private static string PartName(PartType t)
    {
        switch (t)
        {
            case PartType.Power: return "POWER";
            case PartType.Input: return "INPUT";
            case PartType.Display: return "DISPLAY";
            case PartType.Start: return "START";
            case PartType.BrokenPower: return "BROKEN\nPOWER";
            case PartType.WrongInput: return "WRONG\nINPUT";
            case PartType.BrokenDisplay: return "BROKEN\nDISPLAY";
            case PartType.Exit: return "EXIT";
        }
        return "?";
    }

    // =========================================
    // 완료
    // =========================================
    private void OnFinalStartClicked()
    {
        if (!isGameActive || finished || phase != GamePhase.StartReady)
            return;

        finished = true;
        phase = GamePhase.Completed;
        Success();
    }

    protected override void GiveHint()
    {
        if (phase == GamePhase.Selecting)
            SetStatus("힌트: 회색·빨간 부품은 불량품입니다. 밝은 색 정상 부품만 고르세요.");
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
