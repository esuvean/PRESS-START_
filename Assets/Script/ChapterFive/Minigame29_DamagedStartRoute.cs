using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MG29 손상된 START 경로 (실행 경로 복구 검사) - 기획서 Chapter 5 / 미니게임 29
///  - 위쪽 S T A R T, 아래쪽 결과 슬롯 1~5, 그 사이 사다리
///  - 가로선 일부는 사라져 있고(빈 자리 클릭으로 추가/제거), 일부는 평소에 보이지 않는 '숨은 경로'
///  - 몇 초마다 글리치가 일어나는 약 0.5초 동안만 숨은 경로가 보임 → 기억해서 경로를 고침
///  - RUN → 문자들이 사다리를 따라 내려감. 결과가 S T A R T 면 결합 → START 클릭
///  - 틀리면 ERROR INVALID ROUTE (사다리 상태는 그대로 유지)
/// </summary>
public class Minigame29_DamagedStartRoute : MinigameBase
{
    [Header("챕터5 그림 (에디터 도구가 자동으로 넣음)")]
    public TMP_FontAsset uiFont;
    public Sprite panelSprite;          // 23_line_back
    public Sprite startButtonSprite;    // normal_button
    public Sprite runButtonSprite;      // 24_button
    public Sprite[] letterSprites = new Sprite[5];   // Chapter1 S / T / A / R / T2
    public Sprite[] numberSprites = new Sprite[5];   // 29_1 ~ 29_5

    [Header("규칙")]
    [Tooltip("RUN 결과가 틀리면 챕터5 오류 횟수에 포함")]
    public bool countMistakes = true;

    [Header("글리치 (숨은 경로가 보이는 순간)")]
    public float firstGlitchDelay = 2f;
    public float minGlitchInterval = 5f;
    public float maxGlitchInterval = 8f;
    public float glitchDuration = 0.5f;

    [Header("힌트 (시간이 지나거나 RUN을 틀릴수록 단계가 올라감)")]
    public float hint1Time = 25f;
    public float hint2Time = 50f;
    public float hint3Time = 80f;

    [Header("연출 속도")]
    public float rowStepTime = 0.13f;
    public float errorDisplayTime = 1.3f;

    private const int Lanes = 5;
    private const int Rows = 6;
    private const int Gaps = 4;
    private const string Word = "START";

    // ---- 배치 ----
    private static readonly Vector2 PanelPos = new Vector2(0f, 15f);
    private static readonly Vector2 PanelSize = new Vector2(1500f, 620f);
    private const float LaneSpacing = 170f;
    private const float LetterY = 240f;
    private const float LineTop = 196f;
    private const float LineBottom = -204f;
    private const float FirstRowY = 150f;
    private const float RowSpacing = 62f;
    private const float SlotY = -246f;
    private static readonly Vector2 LetterSize = new Vector2(80f, 98f);
    private static readonly Color[] RungColors =
    {
        new Color32(255, 232, 26, 255), new Color32(255, 45, 149, 255),
        new Color32(25, 220, 245, 255), new Color32(255, 232, 26, 255)
    };

    // 퍼즐 (숨은 경로 2개 + 처음부터 있는 가로선 3개, 둘 다 지울 수 없음)
    //  - 플레이어는 가로선을 '추가'만 해서 START를 만든다 (추가한 것은 다시 눌러 지울 수 있음)
    //  - 3개를 추가하면 풀리고, 숨은 경로를 모르고 추가하면 풀리지 않게 골라 둔 배치. 시작할 때 하나를 고름.
    //  - 숫자는 (행, 칸) 쌍. 행 0 = 맨 위, 칸 0 = 1번과 2번 세로줄 사이
    private static readonly int[][] PuzzleHidden =
    {
        new[] { 2, 0, 5, 2 },
        new[] { 0, 0, 2, 1 },
        new[] { 0, 2, 2, 3 },
    };
    private static readonly int[][] PuzzleVisible =
    {
        new[] { 1, 3, 2, 3, 4, 1 },
        new[] { 0, 3, 1, 0, 5, 0 },
        new[] { 1, 1, 3, 0, 4, 0 },
    };

    private enum Phase
    {
        Editing,
        Running,
        Error,
        StartReady,
        Completed
    }

    private Phase phase;
    private bool finished;
    private bool hiddenVisible;
    private float glitchTimer;
    private float editTime;
    private int failCount;
    private int hintLevel;

    private bool[,] manualRungs = new bool[Rows, Gaps];   // 플레이어가 추가한 가로선
    private bool[,] fixedRungs = new bool[Rows, Gaps];    // 처음부터 있는 가로선 (지울 수 없음)
    private readonly Image[,] fixedImages = new Image[Rows, Gaps];
    private bool[,] hiddenRungs = new bool[Rows, Gaps];
    private readonly Image[,] rungImages = new Image[Rows, Gaps];
    private readonly RectTransform[,] hiddenImages = new RectTransform[Rows, Gaps];
    private readonly Image[,] previewImages = new Image[Rows, Gaps];
    private readonly RectTransform[] letters = new RectTransform[Lanes];
    private readonly Image[] slotImages = new Image[Lanes];

    // ---- 만든 UI ----
    private TMP_FontAsset font;
    private RectTransform screen;
    private RectTransform panel;
    private RectTransform ladder;
    private Button runButton;
    private TextMeshProUGUI bottomText;
    private CanvasGroup banner;
    private TextMeshProUGUI bannerText;
    private Button finalStartButton;
    private Coroutine messageRoutine;

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "실행 경로 복구 검사";
        instruction = "숨은 경로를 찾아 START 순서를 복구하세요.";

        base.StartMinigame();
        StopAllCoroutines();
        messageRoutine = null;

        finished = false;
        hiddenVisible = false;
        glitchTimer = firstGlitchDelay;
        editTime = 0f;
        failCount = 0;
        hintLevel = 0;

        SetupPuzzle(Random.Range(0, PuzzleHidden.Length));
        BuildScreen();
        C5Kit.PrepareNoise(transform);
        RefreshRungs();
        SetHiddenVisible(false);

        phase = Phase.Editing;
        SetBottom(DefaultGuide());
    }

    private void SetupPuzzle(int index)
    {
        manualRungs = new bool[Rows, Gaps];
        fixedRungs = new bool[Rows, Gaps];
        hiddenRungs = new bool[Rows, Gaps];
        int[] h = PuzzleHidden[index];
        for (int i = 0; i < h.Length; i += 2)
            hiddenRungs[h[i], h[i + 1]] = true;
        int[] v = PuzzleVisible[index];
        for (int i = 0; i < v.Length; i += 2)
            fixedRungs[v[i], v[i + 1]] = true;
    }

    private string DefaultGuide()
    {
        switch (hintLevel)
        {
            case 1:
                return "힌트: 같은 칸에 가로선이 <color=" + C5Kit.YellowHex +
                       ">위아래로 두 개</color> 있으면 서로 없어져요. 숨은 가로선도 마찬가지!";
            case 2:
                return "힌트: 글리치가 <color=" + C5Kit.YellowHex + ">더 자주, 더 길게</color> 일어나요. 점선 자리를 잘 보세요.";
            case 3:
                return "힌트: 숨은 경로가 <color=" + C5Kit.YellowHex + ">희미하게</color> 보여요. 점선 바로 위나 아래 칸에 가로선을 놓아 보세요.";
        }
        return "<color=" + C5Kit.YellowHex + ">글리치</color>로 보이는 길을 기억해 복구하세요.";
    }

    // 시간이 지나거나 RUN을 틀린 횟수만큼 힌트 단계 올리기
    private void UpdateHintLevel()
    {
        int level = 0;
        if (editTime >= hint1Time || failCount >= 1) level = 1;
        if (editTime >= hint2Time || failCount >= 2) level = 2;
        if (editTime >= hint3Time || failCount >= 3) level = 3;
        if (level <= hintLevel)
            return;

        hintLevel = level;
        if (hintLevel >= 2)
        {
            minGlitchInterval = Mathf.Min(minGlitchInterval, 3f);
            maxGlitchInterval = Mathf.Min(maxGlitchInterval, 4f);
            glitchDuration = Mathf.Max(glitchDuration, 0.9f);
            glitchTimer = Mathf.Min(glitchTimer, 1f);
        }
        SetHiddenVisible(hiddenVisible);
        if (messageRoutine == null && phase == Phase.Editing)
            SetBottom(DefaultGuide());
    }

    // =========================================
    // 화면 만들기
    // =========================================
    private void BuildScreen()
    {
        font = C5Kit.ResolveFont(transform, uiFont);
        C5Kit.HideLegacy(transform);
        screen = C5Kit.NewScreen(transform, "RT_MG29");

        panel = C5Kit.Panel(screen, panelSprite, PanelPos, PanelSize).rectTransform;
        ladder = C5Kit.Rect(screen, "RT_Ladder", new Vector2(-60f, 0f), new Vector2(900f, 600f));

        // 세로줄
        for (int lane = 0; lane < Lanes; lane++)
        {
            float x = LaneX(lane);
            Image line = C5Kit.Img(ladder, "Line" + lane, null, new Color(0.92f, 0.97f, 1f, 1f),
                new Vector2(x, (LineTop + LineBottom) * 0.5f), new Vector2(7f, LineTop - LineBottom));
            C5Kit.AddOutline(line, new Color(0.1f, 0.85f, 1f, 0.45f), 2f);
            C5Kit.Img(ladder, "CapTop" + lane, null, C5Kit.White, new Vector2(x, LineTop), new Vector2(18f, 8f));
            C5Kit.Img(ladder, "CapBottom" + lane, null, C5Kit.White, new Vector2(x, LineBottom), new Vector2(18f, 8f));
        }

        // 가로선 자리 (보이는 가로선 / 숨은 경로 / 마우스 올렸을 때 미리보기 / 클릭 영역)
        for (int r = 0; r < Rows; r++)
        {
            for (int g = 0; g < Gaps; g++)
            {
                Vector2 p = RungPos(r, g);
                float w = LaneSpacing - 14f;

                Image preview = C5Kit.Img(ladder, "Preview", null, new Color(1f, 1f, 1f, 0f), p, new Vector2(w, 10f));
                previewImages[r, g] = preview;

                RectTransform hidden = C5Kit.Rect(ladder, "Hidden", p, new Vector2(w, 12f));
                for (int d = 0; d < 7; d++)
                    C5Kit.Img(hidden, "Dash", null, C5Kit.Cyan, new Vector2(-w * 0.5f + 11f + d * (w - 22f) / 6f, 0f),
                        new Vector2(14f, 8f));
                hiddenImages[r, g] = hidden;

                // 처음부터 있는 가로선 (색깔, 지울 수 없음)
                Image fixedRung = C5Kit.Img(ladder, "FixedRung", null, RungColors[g], p, new Vector2(w, 13f));
                C5Kit.Img(fixedRung.transform, "Shine", null, new Color(1f, 1f, 1f, 0.55f), new Vector2(0f, 3f),
                    new Vector2(w - 8f, 3f));
                fixedImages[r, g] = fixedRung;

                // 플레이어가 추가한 가로선 (흰색, 다시 누르면 지워짐)
                Image rung = C5Kit.Img(ladder, "Rung", null, C5Kit.White, p, new Vector2(w, 13f));
                C5Kit.Img(rung.transform, "Glow", null, new Color(0.6f, 1f, 1f, 0.9f), new Vector2(0f, -4f),
                    new Vector2(w - 8f, 3f));
                rungImages[r, g] = rung;

                Image hit = C5Kit.Img(ladder, "Hit", null, C5Kit.Clear, p, new Vector2(w - 10f, RowSpacing - 14f));
                hit.raycastTarget = true;
                int rr = r, gg = g;
                Button b = hit.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                Navigation nav = b.navigation;
                nav.mode = Navigation.Mode.None;
                b.navigation = nav;
                b.onClick.AddListener(() => OnRungClicked(rr, gg));
                AddHover(hit.gameObject, rr, gg);
            }
        }

        // 위쪽 문자 / 아래쪽 번호 슬롯
        for (int lane = 0; lane < Lanes; lane++)
        {
            Sprite num = numberSprites != null && lane < numberSprites.Length ? numberSprites[lane] : null;
            Image slot = C5Kit.Img(ladder, "Slot" + lane, num, num != null ? C5Kit.White : C5Kit.Navy,
                new Vector2(LaneX(lane), SlotY), new Vector2(102f, 80f), true);
            if (num == null)
            {
                C5Kit.AddOutline(slot, C5Kit.Cyan, 3f);
                C5Kit.Txt(slot.transform, "N", (lane + 1).ToString(), 40f, C5Kit.White, TextAlignmentOptions.Center,
                    Vector2.zero, new Vector2(102f, 80f), font);
            }
            slotImages[lane] = slot;
        }

        for (int i = 0; i < Lanes; i++)
        {
            Sprite s = letterSprites != null && i < letterSprites.Length ? letterSprites[i] : null;
            Image img = C5Kit.Img(ladder, "Letter_" + Word[i], s, s != null ? C5Kit.White : C5Kit.Navy,
                new Vector2(LaneX(i), LetterY), LetterSize, true);
            if (s == null)
            {
                C5Kit.AddOutline(img, C5Kit.Pink, 3f);
                C5Kit.Txt(img.transform, "L", Word[i].ToString(), 54f, C5Kit.White, TextAlignmentOptions.Center,
                    Vector2.zero, LetterSize, font);
            }
            letters[i] = img.rectTransform;
        }

        // RUN 버튼 (오른쪽)
        Image run = C5Kit.Img(screen, "RT_RunButton", runButtonSprite,
            runButtonSprite != null ? C5Kit.White : C5Kit.DeepNavy, new Vector2(560f, -10f), new Vector2(230f, 78f));
        if (runButtonSprite == null)
            C5Kit.AddOutline(run, C5Kit.Yellow, 4f);
        C5Kit.Txt(run.transform, "Label", "RUN", 44f, C5Kit.Yellow, TextAlignmentOptions.Center, new Vector2(0f, 2f),
            new Vector2(230f, 78f), font);
        runButton = C5Kit.MakeButton(run, OnRunClicked);

        bottomText = C5Kit.BottomText(screen, font, "");
        banner = C5Kit.Banner(screen, font, new Vector2(-60f, 20f), new Vector2(720f, 160f), out bannerText);

        finalStartButton = C5Kit.StartButton(screen, startButtonSprite, font, new Vector2(0f, 20f),
            new Vector2(360f, 84f), OnFinalStartClicked);
        finalStartButton.gameObject.SetActive(false);
    }

    private void AddHover(GameObject go, int row, int gap)
    {
        EventTrigger trigger = go.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetPreview(row, gap, true));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetPreview(row, gap, false));
        trigger.triggers.Add(exit);
    }

    private void SetPreview(int row, int gap, bool on)
    {
        Image p = previewImages[row, gap];
        if (p == null) return;
        bool show = on && phase == Phase.Editing && !manualRungs[row, gap] && !fixedRungs[row, gap];
        p.color = new Color(1f, 1f, 1f, show ? 0.22f : 0f);
    }

    private static float LaneX(int lane)
    {
        return (lane - 2) * LaneSpacing;
    }

    private static float RowY(int row)
    {
        return FirstRowY - row * RowSpacing;
    }

    private static Vector2 RungPos(int row, int gap)
    {
        return new Vector2((LaneX(gap) + LaneX(gap + 1)) * 0.5f, RowY(row));
    }

    private void RefreshRungs()
    {
        for (int r = 0; r < Rows; r++)
        for (int g = 0; g < Gaps; g++)
        {
            rungImages[r, g].gameObject.SetActive(manualRungs[r, g]);
            fixedImages[r, g].gameObject.SetActive(fixedRungs[r, g]);
            if (manualRungs[r, g] && previewImages[r, g] != null)
                previewImages[r, g].color = C5Kit.Clear;
        }
    }

    private void SetHiddenVisible(bool visible)
    {
        hiddenVisible = visible;
        for (int r = 0; r < Rows; r++)
        for (int g = 0; g < Gaps; g++)
            ShowOneHidden(r, g, visible);
    }

    // 숨은 경로 보이기 (힌트 3단계부터는 평소에도 희미하게 보임)
    private void ShowOneHidden(int r, int g, bool visible)
    {
        RectTransform h = hiddenImages[r, g];
        if (h == null)
            return;

        bool faint = !visible && hintLevel >= 3 && phase == Phase.Editing;
        h.gameObject.SetActive(hiddenRungs[r, g] && (visible || faint));
        float a = visible ? 1f : 0.3f;
        foreach (Image dash in h.GetComponentsInChildren<Image>(true))
            dash.color = new Color(C5Kit.Cyan.r, C5Kit.Cyan.g, C5Kit.Cyan.b, a);
    }

    private bool IsActive(int r, int g)
    {
        if (g < 0 || g >= Gaps) return false;
        return manualRungs[r, g] || hiddenRungs[r, g] || fixedRungs[r, g];
    }

    // =========================================
    // 매 프레임: 주기적인 글리치
    // =========================================
    protected override void Update()
    {
        // 글자를 다 모아 START 버튼만 남았으면 시간은 멈춤 (다 해 놓고 시간 초과로 실패하지 않게)
        bool timerFrozen = phase == Phase.StartReady || phase == Phase.Completed;
        if (!timerFrozen)
            base.Update();
        if (!isGameActive || finished || phase != Phase.Editing)
            return;

        editTime += Time.deltaTime;
        UpdateHintLevel();

        glitchTimer -= Time.deltaTime;
        if (glitchTimer <= 0f)
        {
            glitchTimer = Random.Range(minGlitchInterval, maxGlitchInterval);
            StartCoroutine(GlitchReveal());
        }
    }

    private IEnumerator GlitchReveal()
    {
        SetHiddenVisible(true);
        StartCoroutine(FlickerLetters(glitchDuration));
        yield return C5Kit.Glitch(panel, ladder, glitchDuration, 1f);
        if (phase == Phase.Editing || phase == Phase.Error)
            SetHiddenVisible(false);
    }

    private IEnumerator FlickerLetters(float duration)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            for (int i = 0; i < Lanes; i++)
            {
                if (letters[i] == null) continue;
                Image img = letters[i].GetComponent<Image>();
                if (img != null)
                    img.color = Random.value < 0.25f ? new Color(1f, 0.4f, 0.7f, 0.7f) : C5Kit.White;
            }
            yield return new WaitForSeconds(0.04f);
        }
        for (int i = 0; i < Lanes; i++)
        {
            if (letters[i] == null) continue;
            Image img = letters[i].GetComponent<Image>();
            if (img != null) img.color = img.sprite != null ? C5Kit.White : C5Kit.Navy;
        }
    }

    // =========================================
    // 가로선 클릭 (추가 / 제거)
    // =========================================
    private void OnRungClicked(int r, int g)
    {
        if (!isGameActive || finished || phase != Phase.Editing)
            return;

        if (hiddenRungs[r, g])
        {
            StartCoroutine(RevealBriefly(r, g));
            ShowMessage("<color=" + C5Kit.PinkHex + ">손상된 숨은 경로</color>입니다. 지울 수 없습니다.");
            return;
        }

        if (fixedRungs[r, g])
        {
            StartCoroutine(C5Kit.Shake(fixedImages[r, g].rectTransform, 0.2f, 5f));
            GameSfx.Play("wrong_buzz", 0.35f);
            ShowMessage("원래 있던 가로선은 <color=" + C5Kit.PinkHex + ">지울 수 없습니다</color>. 새 가로선을 추가하세요.");
            return;
        }

        if (manualRungs[r, g])
        {
            manualRungs[r, g] = false;
            GameSfx.Play("piece_place", 0.45f, 0.1f);
            RefreshRungs();
            SetPreview(r, g, true);
            return;
        }

        // 같은 줄 바로 옆에 가로선이 있으면 놓을 수 없음
        int blockedBy = -1;
        if (IsActive(r, g - 1)) blockedBy = g - 1;
        else if (IsActive(r, g + 1)) blockedBy = g + 1;

        if (blockedBy >= 0)
        {
            if (hiddenRungs[r, blockedBy] && !manualRungs[r, blockedBy] && !fixedRungs[r, blockedBy])
            {
                StartCoroutine(RevealBriefly(r, blockedBy));
                ShowMessage("보이지 않는 경로와 <color=" + C5Kit.PinkHex + ">겹칩니다</color>.");
            }
            else
            {
                ShowMessage("바로 옆 가로선과 겹쳐서 놓을 수 없습니다.");
            }
            StartCoroutine(C5Kit.Shake(previewImages[r, g].rectTransform, 0.2f, 5f));
            GameSfx.Play("wrong_buzz", 0.35f);
            return;
        }

        manualRungs[r, g] = true;
        GameSfx.Play("block_place", 0.5f, 0.08f);
        RefreshRungs();
        StartCoroutine(C5Kit.Pop(rungImages[r, g].transform, 1.15f, 0.16f));
    }

    private IEnumerator RevealBriefly(int r, int g)
    {
        ShowOneHidden(r, g, true);
        StartCoroutine(C5Kit.Glitch(hiddenImages[r, g], null, 0.3f, 0.6f, false));
        yield return new WaitForSeconds(0.45f);
        if (!hiddenVisible)
            ShowOneHidden(r, g, false);
    }

    // =========================================
    // RUN
    // =========================================
    private void OnRunClicked()
    {
        if (!isGameActive || finished || phase != Phase.Editing)
            return;

        StartCoroutine(RunRoutine());
    }

    private IEnumerator RunRoutine()
    {
        phase = Phase.Running;
        runButton.interactable = false;
        SetHiddenVisible(false);
        StopMessage();
        SetBottom("RUNNING... 경로 검사 중");
        for (int r = 0; r < Rows; r++)
        for (int g = 0; g < Gaps; g++)
            SetPreview(r, g, false);

        int[] lane = { 0, 1, 2, 3, 4 };   // lane[문자] = 지금 있는 세로줄

        // 세로줄 시작점까지
        yield return MoveLettersTo(i => new Vector2(LaneX(lane[i]), LineTop), 0.16f);

        for (int r = 0; r < Rows; r++)
        {
            // 줄 높이까지 내려가기
            yield return MoveLettersTo(i => new Vector2(LaneX(lane[i]), RowY(r)), rowStepTime);

            // 이 줄의 가로선 건너기 (숨은 경로를 지나면 잠깐 보임)
            bool any = false;
            int[] next = (int[])lane.Clone();
            for (int i = 0; i < Lanes; i++)
            {
                int l = lane[i];
                if (l < Gaps && IsActive(r, l))
                {
                    next[i] = l + 1;
                    any = true;
                    if (hiddenRungs[r, l]) ShowOneHidden(r, l, true);
                }
                else if (l > 0 && IsActive(r, l - 1))
                {
                    next[i] = l - 1;
                    any = true;
                    if (hiddenRungs[r, l - 1]) ShowOneHidden(r, l - 1, true);
                }
            }

            if (any)
            {
                GameSfx.Play("piece_place", 0.3f, 0.12f);
                int[] target = next;
                yield return MoveLettersTo(i => new Vector2(LaneX(target[i]), RowY(r)), rowStepTime * 1.4f);
                lane = next;
            }
        }

        yield return MoveLettersTo(i => new Vector2(LaneX(lane[i]), LineBottom), rowStepTime);
        yield return MoveLettersTo(i => new Vector2(LaneX(lane[i]), SlotY), 0.16f);

        bool ok = true;
        for (int i = 0; i < Lanes; i++)
            if (lane[i] != i) ok = false;

        if (ok)
            StartCoroutine(SuccessRoutine());
        else
            StartCoroutine(FailRoutine());
    }

    private delegate Vector2 LetterTarget(int letterIndex);

    private IEnumerator MoveLettersTo(LetterTarget target, float duration)
    {
        Vector2[] from = new Vector2[Lanes];
        Vector2[] to = new Vector2[Lanes];
        for (int i = 0; i < Lanes; i++)
        {
            from[i] = letters[i].anchoredPosition;
            to[i] = target(i);
        }

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float k = C5Kit.EaseInOut(e / duration);
            for (int i = 0; i < Lanes; i++)
                if (letters[i] != null)
                    letters[i].anchoredPosition = Vector2.Lerp(from[i], to[i], k);
            yield return null;
        }
        for (int i = 0; i < Lanes; i++)
            if (letters[i] != null)
                letters[i].anchoredPosition = to[i];
    }

    private IEnumerator FailRoutine()
    {
        phase = Phase.Error;
        failCount++;
        if (countMistakes)
            ReportMistake();
        GameSfx.Play("wrong_buzz", 0.6f);

        C5Kit.SetBanner(banner, bannerText, "ERROR\n<size=32>INVALID ROUTE</size>", C5Kit.Pink);
        SetBottom("결과가 <color=" + C5Kit.PinkHex + ">START</color>가 아닙니다. 경로를 다시 고치세요.");
        StartCoroutine(C5Kit.Shake(panel, 0.4f, 12f));
        yield return C5Kit.Glitch(panel, ladder, 0.4f, 1.1f);
        yield return new WaitForSeconds(Mathf.Max(0f, errorDisplayTime - 0.4f));

        if (!isGameActive || finished)
            yield break;

        C5Kit.HideBanner(banner);
        SetHiddenVisible(false);
        yield return MoveLettersTo(i => new Vector2(LaneX(i), LetterY), 0.3f);

        phase = Phase.Editing;
        UpdateHintLevel();
        SetHiddenVisible(false);
        runButton.interactable = true;
        glitchTimer = Mathf.Min(glitchTimer, 2.5f);
        SetBottom(DefaultGuide());
    }

    private IEnumerator SuccessRoutine()
    {
        phase = Phase.StartReady;
        SetHiddenVisible(false);
        GameSfx.Play("correct_ding", 0.6f);
        SetBottom("<color=" + C5Kit.GreenHex + ">ROUTE OK</color>");

        for (int i = 0; i < Lanes; i++)
            StartCoroutine(C5Kit.Pop(slotImages[i].transform, 1.2f));
        yield return new WaitForSeconds(0.45f);

        // 사다리를 치우고 문자를 가운데에서 결합
        Vector3[] fromPos = new Vector3[Lanes];
        for (int i = 0; i < Lanes; i++)
            fromPos[i] = letters[i].position;
        ladder.gameObject.SetActive(false);
        runButton.gameObject.SetActive(false);

        Transform[] anchors = new Transform[Lanes];
        for (int i = 0; i < Lanes; i++)
        {
            RectTransform a = C5Kit.Rect(screen, "RT_MergeFrom" + i, Vector2.zero, Vector2.zero);
            a.position = fromPos[i];
            anchors[i] = a;
        }

        yield return C5Kit.MergeLetters(screen, letterSprites, font, new Vector2(0f, 20f), LetterSize, anchors);
        foreach (Transform a in anchors)
            if (a != null) Destroy(a.gameObject);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
        StartCoroutine(C5Kit.Pop(finalStartButton.transform, 1.25f, 0.3f));
        SetBottom("<color=" + C5Kit.YellowHex + ">START</color>를 눌러 검사를 완료하세요.");
    }

    // =========================================
    // 안내 문구
    // =========================================
    private void SetBottom(string text)
    {
        if (bottomText != null)
            bottomText.text = text;
    }

    private void ShowMessage(string text)
    {
        StopMessage();
        messageRoutine = StartCoroutine(MessageRoutine(text));
    }

    private void StopMessage()
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = null;
    }

    private IEnumerator MessageRoutine(string text)
    {
        SetBottom(text);
        yield return new WaitForSeconds(1.8f);
        if (phase == Phase.Editing)
            SetBottom(DefaultGuide());
        messageRoutine = null;
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
        if (phase == Phase.Editing)
            ShowMessage("힌트: 지울 수 없는 숨은 가로선은 바로 아래·위에 같은 가로선을 놓으면 상쇄됩니다.");
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
