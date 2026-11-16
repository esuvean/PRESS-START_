
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame29_DamagedStartRoute : MinigameBase
{
    [Header("Main UI")]
    public GameObject playArea;
    public RectTransform ladderBoard;
    public RectTransform verticalLineLayer;
    public RectTransform rungLayer;
    public RectTransform letterLayer;
    public RectTransform slotLayer;

    public Image glitchOverlay;
    public Button runButton;
    public TMP_Text statusText;
    public TMP_Text errorText;
    public TMP_Text resultText;
    public Button finalStartButton;

    [Header("ART - Letter Images")]
    public Sprite letterS;
    public Sprite letterT;
    public Sprite letterA;
    public Sprite letterR;

    [Header("ART - Ladder Images")]
    public Sprite verticalLineSprite;
    public Sprite rungSprite;
    public Sprite hiddenRungSprite;
    public Sprite resultSlotSprite;
    public Sprite boardSprite;
    public Sprite glitchSprite;

    [Header("ART - Button Images")]
    public Sprite runButtonSprite;
    public Sprite finalStartSprite;

    [Header("ART - Options")]
    public bool hideRunTextWhenSpriteExists = true;
    public bool hideFinalTextWhenSpriteExists = true;

    [Header("Ladder Settings")]
    public Vector2 letterSize = new Vector2(70f, 70f);
    public Vector2 resultSlotSize = new Vector2(120f, 68f);
    public float verticalLineWidth = 8f;
    public float rungThickness = 15f;

    [Header("Animation Settings")]
    public float letterMoveSpeed = 1050f;
    public float errorDisplayTime = 1.4f;

    [Header("Glitch Settings")]
    public float minGlitchInterval = 2.8f;
    public float maxGlitchInterval = 4.5f;
    public float glitchDuration = 0.5f;

    private const int LaneCount = 5;
    private const int RowCount = 5;
    private const int GapCount = 4;

    private const string TargetWord = "START";

    private enum Phase
    {
        Editing,
        Running,
        Error,
        StartReady,
        Completed
    }

    private Phase phase;

    private bool[,] manualRungs =
        new bool[RowCount, GapCount];

    private bool[,] hiddenRungs =
        new bool[RowCount, GapCount];

    private Image[,] rungImages =
        new Image[RowCount, GapCount];

    private RectTransform[] letters =
        new RectTransform[LaneCount];

    private TMP_Text[] slotTexts =
        new TMP_Text[LaneCount];

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private float laneSpacing;
    private float topY;
    private float bottomY;

    private bool hiddenVisible;
    private bool completed;

    private static readonly Color NormalRungColor =
        new Color32(83, 216, 211, 255);

    private static readonly Color HiddenRungColor =
        new Color32(255, 204, 94, 255);

    private static readonly Color EmptyRungColor =
        new Color32(110, 153, 185, 30);

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "손상된 START 경로";

        instruction =
            "사다리의 가로선을 복구해 START를 만드십시오.";

        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        completed = false;
        phase = Phase.Editing;
        hiddenVisible = false;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG29: UI 연결이 누락되었습니다.",
                this
            );

            isGameActive = false;
            return;
        }

        ClearGeneratedObjects();

        playArea.SetActive(true);
        errorText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        finalStartButton.gameObject.SetActive(false);

        Canvas.ForceUpdateCanvases();

        SetupBoardSize();
        SetupInitialPuzzle();
        ApplyStaticArtwork();
        SetupButtons();

        CreateVerticalLines();
        CreateRungButtons();
        CreateResultSlots();
        CreateLetters();

        SetHiddenRungsVisible(false);

        statusText.text =
            "빈 가로선 자리를 클릭해 추가하거나 " +
            "기존 가로선을 제거하세요.";

        runButton.interactable = true;

        StartCoroutine(GlitchRoutine());

        Debug.Log("MG29: 손상된 START 경로 시작");
    }

    // =======================================
    // UI 자동 검색
    // =======================================

    private Transform FindDeep(
        Transform parent,
        string targetName)
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found =
                FindDeep(child, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private T FindUI<T>(string name)
        where T : Component
    {
        Transform found = FindDeep(transform, name);

        return found == null
            ? null
            : found.GetComponent<T>();
    }

    private GameObject FindObject(string name)
    {
        Transform found = FindDeep(transform, name);

        return found == null
            ? null
            : found.gameObject;
    }

    private bool FindReferences()
    {
        if (playArea == null)
            playArea = FindObject("PlayArea");

        if (ladderBoard == null)
            ladderBoard =
                FindUI<RectTransform>("LadderBoard");

        if (verticalLineLayer == null)
            verticalLineLayer =
                FindUI<RectTransform>("VerticalLineLayer");

        if (rungLayer == null)
            rungLayer =
                FindUI<RectTransform>("RungLayer");

        if (letterLayer == null)
            letterLayer =
                FindUI<RectTransform>("LetterLayer");

        if (slotLayer == null)
            slotLayer =
                FindUI<RectTransform>("SlotLayer");

        if (glitchOverlay == null)
            glitchOverlay = FindUI<Image>("GlitchOverlay");

        if (runButton == null)
            runButton = FindUI<Button>("RunButton");

        if (statusText == null)
            statusText = FindUI<TMP_Text>("StatusText");

        if (errorText == null)
            errorText = FindUI<TMP_Text>("ErrorText");

        if (resultText == null)
            resultText = FindUI<TMP_Text>("ResultText");

        if (finalStartButton == null)
            finalStartButton =
                FindUI<Button>("FinalStartButton");

        bool valid =
            playArea != null &&
            ladderBoard != null &&
            verticalLineLayer != null &&
            rungLayer != null &&
            letterLayer != null &&
            slotLayer != null &&
            glitchOverlay != null &&
            runButton != null &&
            statusText != null &&
            errorText != null &&
            resultText != null &&
            finalStartButton != null;

        return valid;
    }

    // =======================================
    // 게임판 크기 설정
    // =======================================

    private void SetupBoardSize()
    {
        float width = ladderBoard.rect.width;
        float height = ladderBoard.rect.height;

        laneSpacing = Mathf.Min(
            245f,
            (width - 140f) / 4f
        );

        topY = Mathf.Min(
            235f,
            height * 0.5f - 42f
        );

        bottomY = -topY;
    }

    private float LaneX(int lane)
    {
        return (lane - 2) * laneSpacing;
    }

    private float RowY(int row)
    {
        return Mathf.Lerp(
            topY - 65f,
            bottomY + 70f,
            row / (float)(RowCount - 1)
        );
    }

    // =======================================
    // 초기 손상된 퍼즐
    // =======================================

    private void SetupInitialPuzzle()
    {
        manualRungs =
            new bool[RowCount, GapCount];

        hiddenRungs =
            new bool[RowCount, GapCount];

        // 눈에 보이지 않지만 실제 경로에
        // 영향을 주는 숨겨진 가로선
        hiddenRungs[0, 1] = true;
        hiddenRungs[2, 3] = true;

        // 손상된 상태로 시작
        // 불필요한 가로선 두 개
        manualRungs[4, 0] = true;
        manualRungs[4, 2] = true;

        // 복구할 수 있는 정답 구성:
        // 1행 1번 가로선 추가
        // 3행 3번 가로선 추가
        // 4행 0번 가로선 제거
        // 4행 2번 가로선 제거
        //
        // 이 상태는 편집으로 복구 가능함.
    }

    private bool IsHiddenRung(int row, int gap)
    {
        return hiddenRungs[row, gap];
    }

    private bool IsRungActive(int row, int gap)
    {
        return
            hiddenRungs[row, gap] ||
            manualRungs[row, gap];
    }

    // =======================================
    // 아트 이미지 적용
    // =======================================

    private void ApplyStaticArtwork()
    {
        ApplySprite(
            ladderBoard.GetComponent<Image>(),
            boardSprite,
            false
        );

        ApplySprite(
            glitchOverlay,
            glitchSprite,
            false
        );

        ApplySprite(
            runButton.GetComponent<Image>(),
            runButtonSprite,
            true
        );

        ApplySprite(
            finalStartButton.GetComponent<Image>(),
            finalStartSprite,
            true
        );

        SetupButtonText(
            runButton,
            runButtonSprite != null &&
            hideRunTextWhenSpriteExists
        );

        SetupButtonText(
            finalStartButton,
            finalStartSprite != null &&
            hideFinalTextWhenSpriteExists
        );

        glitchOverlay.raycastTarget = false;
        glitchOverlay.color =
            new Color(1f, 1f, 1f, 0f);
    }

    private void ApplySprite(
        Image image,
        Sprite sprite,
        bool preserveAspect)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }

    private void SetupButtonText(
        Button button,
        bool shouldHide)
    {
        foreach (TMP_Text label in
            button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.gameObject.SetActive(!shouldHide);
            label.raycastTarget = false;
        }
    }

    private Sprite GetLetterSprite(char letter)
    {
        switch (letter)
        {
            case 'S': return letterS;
            case 'T': return letterT;
            case 'A': return letterA;
            case 'R': return letterR;
        }

        return null;
    }

    // =======================================
    // 생성된 오브젝트 정리
    // =======================================

    private void ClearGeneratedObjects()
    {
        foreach (GameObject go in generatedObjects)
        {
            if (go != null)
            {
                go.SetActive(false);
                Destroy(go);
            }
        }

        generatedObjects.Clear();

        letters = new RectTransform[LaneCount];
        slotTexts = new TMP_Text[LaneCount];

        rungImages = new Image[RowCount, GapCount];
    }

    // =======================================
    // UI 이미지 자동 생성
    // =======================================

    private RectTransform CreateImage(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Sprite sprite,
        Color defaultColor)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.color = defaultColor;
        }

        image.raycastTarget = false;

        generatedObjects.Add(go);

        return rect;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string content,
        float fontSize,
        Color color)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            go.GetComponent<TextMeshProUGUI>();

        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment =
            TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;

        return text;
    }

    // =======================================
    // 세로선 5개 생성
    // =======================================

    private void CreateVerticalLines()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            CreateImage(
                "VerticalLine_" + i,
                verticalLineLayer,
                new Vector2(LaneX(i), 0f),
                new Vector2(
                    verticalLineWidth,
                    topY * 2f + 10f
                ),
                verticalLineSprite,
                new Color32(117, 169, 206, 255)
            );
        }
    }

    // =======================================
    // 가로선 20개 생성
    // 클릭으로 추가 / 제거
    // =======================================

    private void CreateRungButtons()
    {
        for (int row = 0; row < RowCount; row++)
        {
            for (int gap = 0; gap < GapCount; gap++)
            {
                int capturedRow = row;
                int capturedGap = gap;

                bool hidden =
                    IsHiddenRung(row, gap);

                Sprite sprite = hidden
                    ? (
                        hiddenRungSprite != null
                        ? hiddenRungSprite
                        : rungSprite
                      )
                    : rungSprite;

                RectTransform rect = CreateImage(
                    "Rung_" + row + "_" + gap,
                    rungLayer,
                    new Vector2(
                        (
                            LaneX(gap) +
                            LaneX(gap + 1)
                        ) * 0.5f,
                        RowY(row)
                    ),
                    new Vector2(
                        laneSpacing - 4f,
                        rungThickness
                    ),
                    sprite,
                    EmptyRungColor
                );

                GameObject go = rect.gameObject;

                Image image =
                    go.GetComponent<Image>();

                rungImages[row, gap] = image;

                if (!hidden)
                {
                    Button button =
                        go.AddComponent<Button>();

                    button.transition =
                        Selectable.Transition.None;

                    button.targetGraphic = image;

                    button.onClick.AddListener(
                        () => ToggleRung(
                            capturedRow,
                            capturedGap
                        )
                    );

                    image.raycastTarget = true;
                }
                else
                {
                    // 숨은 경로는 직접 수정 불가
                    image.raycastTarget = false;
                }

                UpdateRungVisual(row, gap);
            }
        }
    }

    private void ToggleRung(int row, int gap)
    {
        if (phase != Phase.Editing ||
            !isGameActive ||
            completed)
        {
            return;
        }

        if (IsHiddenRung(row, gap))
            return;

        bool currentlyActive =
            manualRungs[row, gap];

        if (!currentlyActive)
        {
            // 같은 높이에서 인접한 가로선이
            // 연결되면 경로가 모호해지므로 제한
            bool leftOccupied =
                gap > 0 &&
                IsRungActive(row, gap - 1);

            bool rightOccupied =
                gap < GapCount - 1 &&
                IsRungActive(row, gap + 1);

            if (leftOccupied || rightOccupied)
            {
                statusText.text =
                    "이 위치는 다른 가로선과 " +
                    "겹쳐서 연결할 수 없습니다.";

                return;
            }
        }

        manualRungs[row, gap] =
            !manualRungs[row, gap];

        UpdateRungVisual(row, gap);

        statusText.text =
            manualRungs[row, gap]
            ? "가로선을 연결했습니다."
            : "가로선을 제거했습니다.";
    }

    // =======================================
    // 가로선 표시 상태
    // =======================================

    private void UpdateRungVisual(int row, int gap)
    {
        Image image = rungImages[row, gap];

        if (image == null)
            return;

        if (IsHiddenRung(row, gap))
        {
            Color color = HiddenRungColor;

            color.a =
                hiddenVisible ? 1f : 0f;

            image.color = color;
        }
        else if (manualRungs[row, gap])
        {
            image.color = NormalRungColor;
        }
        else
        {
            image.color = EmptyRungColor;
        }
    }

    // =======================================
    // 글리치로 숨은 가로선 표시
    // =======================================

    private void SetHiddenRungsVisible(bool visible)
    {
        hiddenVisible = visible;

        for (int row = 0; row < RowCount; row++)
        {
            for (int gap = 0; gap < GapCount; gap++)
            {
                UpdateRungVisual(row, gap);
            }
        }

        if (glitchOverlay != null)
        {
            glitchOverlay.color = new Color(
                1f, 1f, 1f,
                visible
                    ? (glitchSprite != null ? 0.3f : 0.06f)
                    : 0f
            );
        }
    }

    private IEnumerator GlitchRoutine()
    {
        while (isGameActive && !completed)
        {
            yield return new WaitForSecondsRealtime(
                Random.Range(
                    Mathf.Min(
                        minGlitchInterval,
                        maxGlitchInterval
                    ),
                    Mathf.Max(
                        minGlitchInterval,
                        maxGlitchInterval
                    )
                )
            );

            if (!isGameActive || completed)
                yield break;

            if (phase != Phase.Editing)
                continue;

            SetHiddenRungsVisible(true);

            yield return new WaitForSecondsRealtime(
                glitchDuration
            );

            SetHiddenRungsVisible(false);
        }
    }

    // =======================================
    // 도착 슬롯 5개 생성
    // =======================================

    private void CreateResultSlots()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            RectTransform rect = CreateImage(
                "ResultSlot_" + i,
                slotLayer,
                new Vector2(
                    LaneX(i),
                    bottomY - 7f
                ),
                resultSlotSize,
                resultSlotSprite,
                new Color32(39, 82, 111, 255)
            );

            slotTexts[i] = CreateText(
                "ResultLetter",
                rect,
                "_",
                34f,
                Color.white
            );
        }
    }

    // =======================================
    // 문자 5개 자동 생성
    // =======================================

    private void CreateLetters()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            char character = TargetWord[i];

            Sprite sprite =
                GetLetterSprite(character);

            RectTransform rect = CreateImage(
                "Letter_" + character + "_" + i,
                letterLayer,
                new Vector2(
                    LaneX(i),
                    topY
                ),
                letterSize,
                sprite,
                new Color32(22, 160, 133, 255)
            );

            Image image = rect.GetComponent<Image>();
            image.preserveAspect = true;

            if (sprite == null)
            {
                CreateText(
                    "LetterText",
                    rect,
                    character.ToString(),
                    36f,
                    Color.white
                );
            }

            letters[i] = rect;
        }
    }

    // =======================================
    // RUN / 최종 START 버튼 연결
    // =======================================

    private void SetupButtons()
    {
        runButton.onClick.RemoveAllListeners();

        runButton.onClick.AddListener(
            OnRunClicked
        );

        runButton.interactable = true;

        finalStartButton.onClick.RemoveAllListeners();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
    }

    private void OnRunClicked()
    {
        if (phase != Phase.Editing ||
            !isGameActive ||
            completed)
        {
            return;
        }

        phase = Phase.Running;

        runButton.interactable = false;

        SetHiddenRungsVisible(false);
        ResetSlotTexts();
        ResetLetterPositions();

        statusText.text =
            "RUNNING... 사다리 경로 검사 중";

        StartCoroutine(RunLadderRoutine());
    }

    // =======================================
    // 사다리 경로 따라 이동
    // =======================================

    private IEnumerator RunLadderRoutine()
    {
        char[] result = new char[LaneCount];

        for (int source = 0; source < LaneCount; source++)
        {
            RectTransform moving = letters[source];

            int currentLane = source;

            for (int row = 0; row < RowCount; row++)
            {
                // 현재 가로선 높이까지 아래로 이동
                Vector2 downward = new Vector2(
                    LaneX(currentLane),
                    RowY(row)
                );

                yield return StartCoroutine(
                    MoveLetterTo(moving, downward)
                );

                if (!isGameActive)
                    yield break;

                int nextLane = currentLane;

                // 왼쪽 연결선 우선 확인
                if (currentLane > 0 &&
                    IsRungActive(row, currentLane - 1))
                {
                    nextLane = currentLane - 1;
                }
                else if (currentLane < LaneCount - 1 &&
                         IsRungActive(row, currentLane))
                {
                    nextLane = currentLane + 1;
                }

                if (nextLane != currentLane)
                {
                    Vector2 across = new Vector2(
                        LaneX(nextLane),
                        RowY(row)
                    );

                    yield return StartCoroutine(
                        MoveLetterTo(moving, across)
                    );

                    if (!isGameActive)
                        yield break;
                }

                currentLane = nextLane;
            }

            // 마지막 도착 슬롯까지 이동
            yield return StartCoroutine(
                MoveLetterTo(
                    moving,
                    new Vector2(
                        LaneX(currentLane),
                        bottomY - 7f
                    )
                )
            );

            if (!isGameActive)
                yield break;

            char character = TargetWord[source];

            result[currentLane] = character;

            slotTexts[currentLane].text =
                character.ToString();

            // 슬롯에 글자를 옮겼으므로
            // 이동하는 원본 이미지는 숨김
            moving.gameObject.SetActive(false);
        }

        string resultWord = new string(result);

        Debug.Log(
            "MG29: 사다리 결과 = " + resultWord
        );

        if (resultWord == TargetWord)
        {
            ShowSuccess();
        }
        else
        {
            StartCoroutine(
                FailureRoutine(resultWord)
            );
        }
    }

    // =======================================
    // 문자 이동 애니메이션
    // =======================================

    private IEnumerator MoveLetterTo(
        RectTransform letter,
        Vector2 target)
    {
        Vector2 start = letter.anchoredPosition;

        float distance =
            Vector2.Distance(start, target);

        float duration =
            distance / Mathf.Max(50f, letterMoveSpeed);

        if (duration <= 0.001f)
        {
            letter.anchoredPosition = target;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!isGameActive)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            letter.anchoredPosition =
                Vector2.Lerp(start, target, t);

            yield return null;
        }

        letter.anchoredPosition = target;
    }

    // =======================================
    // 실패 처리
    // =======================================

    private IEnumerator FailureRoutine(string wrongWord)
    {
        phase = Phase.Error;

        statusText.text =
            "결과 : " + wrongWord +
            " / 올바른 순서는 START입니다.";

        errorText.text =
            "ERROR\nINVALID ROUTE";

        errorText.gameObject.SetActive(true);

        Debug.Log(
            "MG29: 잘못된 경로 - " + wrongWord
        );

        yield return new WaitForSecondsRealtime(
            errorDisplayTime
        );

        if (!isGameActive)
            yield break;

        errorText.gameObject.SetActive(false);

        // 수정된 가로선은 유지
        // 문자와 결과 슬롯만 초기화
        ResetLetterPositions();
        ResetSlotTexts();

        phase = Phase.Editing;

        runButton.interactable = true;

        statusText.text =
            "경로 오류! 가로선을 수정한 뒤 " +
            "RUN을 다시 누르세요.";
    }

    private void ResetLetterPositions()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            if (letters[i] == null)
                continue;

            letters[i].anchoredPosition =
                new Vector2(
                    LaneX(i),
                    topY
                );

            letters[i].gameObject.SetActive(true);
        }
    }

    private void ResetSlotTexts()
    {
        for (int i = 0; i < LaneCount; i++)
        {
            if (slotTexts[i] != null)
                slotTexts[i].text = "_";
        }
    }

    // =======================================
    // START 복구 성공
    // =======================================

    private void ShowSuccess()
    {
        phase = Phase.StartReady;

        statusText.text =
            "START 복구 성공!";

        Debug.Log("MG29: START 경로 복구 성공");

        playArea.SetActive(false);

        resultText.text = "S T A R T";
        resultText.gameObject.SetActive(true);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
    }

    private void OnFinalStartClicked()
    {
        if (phase != Phase.StartReady ||
            !isGameActive ||
            completed)
        {
            return;
        }

        completed = true;
        phase = Phase.Completed;

        Debug.Log("MG29: 검사 완료!");

        Success();
    }

    // =======================================
    // 힌트 및 재시작
    // =======================================

    protected override void GiveHint()
    {
        if (phase == Phase.Editing)
        {
            statusText.text =
                "힌트: 노란색 숨은 가로선도 " +
                "실제 이동 경로에 포함됩니다.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}

