
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Minigame23_WildStart : MinigameBase
{
    [Header("Main UI")]
    public RectTransform playArea;
    public TMP_Text startText;
    public TMP_Text currentTargetText;
    public TMP_Text progressText;

    [Header("Moving Letters")]
    public Button letterS;
    public Button letterT1;
    public Button letterA;
    public Button letterR;
    public Button letterT2;

    [Header("Final Button")]
    public Button finalStartButton;

    [Header("Start Settings")]
    public float approachDistance = 240f;

    [Header("Movement Settings")]
    public float sSpeed = 1000f;
    public float firstTSpeed = 100f;
    public Vector2 rVelocity = new Vector2(370f, 250f);

    [Header("A Visibility")]
    public float aVisibleTime = 1.1f;
    public float aHiddenTime = 0.65f;

    [Header("Last T Teleport")]
    public float teleportInterval = 1.5f;

    [Header("Finish Animation")]
    public float mergeDuration = 0.7f;
    public float mergedLetterSpacing = 105f;

    private readonly char[] answer =
    {
        'S', 'T', 'A', 'R', 'T'
    };

    private Button[] letterButtons;
    private RectTransform[] letterRects;
    private Vector2[] initialPositions;

    private Canvas rootCanvas;
    private Camera uiCamera;

    private bool positionsSaved;
    private bool referencesReady;
    private bool isSplit;
    private bool isReassembling;
    private bool finalReady;

    private int currentTargetIndex;

    private float movementTime;
    private float lastTeleportTime;
    private float sDirection;

    private Vector2 currentRVelocity;
    private float wrongNoticeUntil;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "제멋대로 START";

        instruction =
            "움직이는 글자를 S-T-A-R-T 순서로 잡으세요.";

        // 제한시간 없음
        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        referencesReady = FindReferences();

        if (!referencesReady)
        {
            Debug.LogError(
                "MG23: UI 연결이 부족합니다. " +
                "Hierarchy 이름을 확인하세요.",
                this
            );

            isGameActive = false;
            return;
        }

        SetupButtons();
        ResetWholeGame();

        Debug.Log("MG23: 게임 시작");
    }

    // =========================================
    // UI 자동 연결
    // =========================================

    private bool FindReferences()
    {
        if (playArea == null)
        {
            Transform found =
                transform.Find("PlayArea");

            if (found != null)
            {
                playArea =
                    found.GetComponent<RectTransform>();
            }
        }

        if (startText == null)
        {
            Transform found =
                transform.Find("PlayArea/StartText");

            if (found != null)
            {
                startText =
                    found.GetComponent<TMP_Text>();
            }
        }

        if (currentTargetText == null)
        {
            Transform found =
                transform.Find("CurrentTargetText");

            if (found != null)
            {
                currentTargetText =
                    found.GetComponent<TMP_Text>();
            }
        }

        if (progressText == null)
        {
            Transform found =
                transform.Find("ProgressText");

            if (found != null)
            {
                progressText =
                    found.GetComponent<TMP_Text>();
            }
        }

        if (letterS == null)
            letterS = FindButton("Letter_S");

        if (letterT1 == null)
            letterT1 = FindButton("Letter_T1");

        if (letterA == null)
            letterA = FindButton("Letter_A");

        if (letterR == null)
            letterR = FindButton("Letter_R");

        if (letterT2 == null)
            letterT2 = FindButton("Letter_T2");

        if (finalStartButton == null)
            finalStartButton =
                FindButton("FinalStartButton");

        bool valid =
            playArea != null &&
            startText != null &&
            currentTargetText != null &&
            progressText != null &&
            letterS != null &&
            letterT1 != null &&
            letterA != null &&
            letterR != null &&
            letterT2 != null &&
            finalStartButton != null;

        if (!valid)
            return false;

        letterButtons = new Button[]
        {
            letterS,
            letterT1,
            letterA,
            letterR,
            letterT2
        };

        letterRects = new RectTransform[5];

        if (initialPositions == null)
            initialPositions = new Vector2[5];

        for (int i = 0; i < 5; i++)
        {
            letterRects[i] =
                letterButtons[i]
                    .GetComponent<RectTransform>();

            if (letterRects[i] == null)
                return false;

            if (!positionsSaved)
            {
                initialPositions[i] =
                    letterRects[i].anchoredPosition;
            }
        }

        positionsSaved = true;

        rootCanvas =
            GetComponentInParent<Canvas>();

        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;

            if (rootCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null;
            }
            else
            {
                uiCamera = rootCanvas.worldCamera;

                if (uiCamera == null)
                    uiCamera = Camera.main;
            }
        }

        return true;
    }

    private Button FindButton(string objectName)
    {
        Transform found =
            transform.Find("PlayArea/" + objectName);

        if (found == null)
        {
            Debug.LogWarning(
                "MG23: 버튼을 찾지 못했습니다 - " +
                objectName,
                this
            );

            return null;
        }

        return found.GetComponent<Button>();
    }

    // =========================================
    // 버튼 이벤트 연결
    // =========================================

    private void SetupButtons()
    {
        for (int i = 0; i < 5; i++)
        {
            int letterIndex = i;

            Button button = letterButtons[i];

            button.onClick =
                new Button.ButtonClickedEvent();

            button.onClick.AddListener(
                () => OnLetterClicked(letterIndex)
            );

            button.interactable = true;

            button.transition =
                Selectable.Transition.None;

            Image background =
                button.GetComponent<Image>();

            if (background != null)
            {
                background.raycastTarget = true;
            }

            TMP_Text[] texts =
                button.GetComponentsInChildren<TMP_Text>(
                    true
                );

            foreach (TMP_Text text in texts)
            {
                text.raycastTarget = false;
            }
        }

        finalStartButton.onClick =
            new Button.ButtonClickedEvent();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
    }

    // =========================================
    // 게임 초기화
    // =========================================

    private void ResetWholeGame()
    {
        isSplit = false;
        isReassembling = false;
        finalReady = false;

        currentTargetIndex = 0;

        movementTime = 0f;
        lastTeleportTime = 0f;

        sDirection = 1f;
        currentRVelocity = rVelocity;

        wrongNoticeUntil = 0f;

        startText.gameObject.SetActive(true);

        for (int i = 0; i < 5; i++)
        {
            letterRects[i].anchoredPosition =
                initialPositions[i];

            letterButtons[i].interactable = true;
            letterButtons[i].gameObject.SetActive(false);
        }

        finalStartButton.interactable = true;
        finalStartButton.gameObject.SetActive(false);

        UpdateProgressText();
        UpdateCurrentTargetText();
    }

    // =========================================
    // 매 프레임 실행
    // =========================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || !referencesReady)
            return;

        if (isReassembling || finalReady)
            return;

        if (!isSplit)
        {
            DetectMouseNearStart();
            return;
        }

        movementTime += Time.deltaTime;

        MoveS();
        MoveFirstT();
        MoveA();
        MoveR();
        MoveLastT();

        UpdateCurrentTargetText();
    }

    // =========================================
    // 마우스 위치
    // =========================================

    private bool TryGetMousePosition(
        out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            position =
                Mouse.current.position.ReadValue();

            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        position = Input.mousePosition;
        return true;
#else
        position = Vector2.zero;
        return false;
#endif
    }

    // =========================================
    // START 접근 감지
    // =========================================

    private void DetectMouseNearStart()
    {
        Vector2 mousePosition;

        if (!TryGetMousePosition(out mousePosition))
            return;

        Vector2 localMouse;

        bool valid =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    playArea,
                    mousePosition,
                    uiCamera,
                    out localMouse
                );

        if (!valid)
            return;

        Vector3 startLocal3 =
            playArea.InverseTransformPoint(
                startText.rectTransform.position
            );

        Vector2 startLocal =
            new Vector2(
                startLocal3.x,
                startLocal3.y
            );

        float distance =
            Vector2.Distance(
                localMouse,
                startLocal
            );

        if (distance <= approachDistance)
        {
            SplitStart();
        }
    }

    // =========================================
    // START 분리
    // =========================================

    private void SplitStart()
    {
        if (isSplit)
            return;

        isSplit = true;

        movementTime = 0f;
        lastTeleportTime = 0f;

        startText.gameObject.SetActive(false);

        for (int i = 0; i < 5; i++)
        {
            letterRects[i].anchoredPosition =
                initialPositions[i];

            letterButtons[i].interactable = true;
            letterButtons[i].gameObject.SetActive(true);
        }

        Debug.Log("MG23: START 글자 분리");

        UpdateCurrentTargetText();
    }

    // =========================================
    // 이동 범위 계산
    // =========================================

    private float GetMaxX(RectTransform rect)
    {
        return Mathf.Max(
            0f,
            (playArea.rect.width -
             rect.rect.width) * 0.5f - 10f
        );
    }

    private float GetMaxY(RectTransform rect)
    {
        return Mathf.Max(
            0f,
            (playArea.rect.height -
             rect.rect.height) * 0.5f - 10f
        );
    }

    // =========================================
    // S - 빠르게 좌우 이동
    // =========================================

    private void MoveS()
    {
        if (!letterS.gameObject.activeSelf)
            return;

        RectTransform rect = letterRects[0];

        Vector2 pos = rect.anchoredPosition;

        pos.x +=
            sDirection * sSpeed * Time.deltaTime;

        float maxX = GetMaxX(rect);

        if (pos.x >= maxX)
        {
            pos.x = maxX;
            sDirection = -1f;
        }
        else if (pos.x <= -maxX)
        {
            pos.x = -maxX;
            sDirection = 1f;
        }

        rect.anchoredPosition = pos;
    }

    // =========================================
    // 첫 번째 T - 위쪽 이동
    // =========================================

    private void MoveFirstT()
    {
        if (!letterT1.gameObject.activeSelf)
            return;

        RectTransform rect = letterRects[1];

        Vector2 pos = rect.anchoredPosition;

        pos.y += firstTSpeed * Time.deltaTime;

        float maxY = GetMaxY(rect);

        if (pos.y > maxY)
        {
            pos.y = -maxY;
        }

        rect.anchoredPosition = pos;
    }

    // =========================================
    // A - 깜빡임
    // ★ 수정: A를 획득하면 다시 나타나지 않음
    // =========================================

    private void MoveA()
    {
        // A는 세 번째 글자(index 2).
        // 다음 목표가 R(index 3)이 되었다면
        // 이미 A를 획득했으므로 완전히 숨긴다.
        if (currentTargetIndex > 2)
        {
            if (letterA.gameObject.activeSelf)
            {
                letterA.gameObject.SetActive(false);
            }

            return;
        }

        float visibleDuration =
            Mathf.Max(0.1f, aVisibleTime);

        float hiddenDuration =
            Mathf.Max(0.1f, aHiddenTime);

        float cycle =
            visibleDuration + hiddenDuration;

        float phase =
            Mathf.Repeat(movementTime, cycle);

        bool visible =
            phase < visibleDuration;

        if (letterA.gameObject.activeSelf != visible)
        {
            letterA.gameObject.SetActive(visible);
        }
    }

    // =========================================
    // R - 대각선 이동 및 반사
    // =========================================

    private void MoveR()
    {
        if (!letterR.gameObject.activeSelf)
            return;

        RectTransform rect = letterRects[3];

        Vector2 pos = rect.anchoredPosition;

        pos += currentRVelocity * Time.deltaTime;

        float maxX = GetMaxX(rect);
        float maxY = GetMaxY(rect);

        if (pos.x >= maxX)
        {
            pos.x = maxX;
            currentRVelocity.x =
                -Mathf.Abs(currentRVelocity.x);
        }
        else if (pos.x <= -maxX)
        {
            pos.x = -maxX;
            currentRVelocity.x =
                Mathf.Abs(currentRVelocity.x);
        }

        if (pos.y >= maxY)
        {
            pos.y = maxY;
            currentRVelocity.y =
                -Mathf.Abs(currentRVelocity.y);
        }
        else if (pos.y <= -maxY)
        {
            pos.y = -maxY;
            currentRVelocity.y =
                Mathf.Abs(currentRVelocity.y);
        }

        rect.anchoredPosition = pos;
    }

    // =========================================
    // 마지막 T - 순간이동
    // =========================================

    private void MoveLastT()
    {
        if (!letterT2.gameObject.activeSelf)
            return;

        if (movementTime - lastTeleportTime <
            Mathf.Max(0.1f, teleportInterval))
        {
            return;
        }

        lastTeleportTime = movementTime;

        RectTransform rect = letterRects[4];

        float maxX = GetMaxX(rect);
        float maxY = GetMaxY(rect);

        Vector2 newPosition = new Vector2(
            Random.Range(-maxX, maxX),
            Random.Range(-maxY, maxY)
        );

        rect.anchoredPosition = newPosition;
    }

    // =========================================
    // 글자 클릭 판정
    // =========================================

    private void OnLetterClicked(int clickedIndex)
    {
        if (!isGameActive ||
            !isSplit ||
            isReassembling ||
            finalReady)
        {
            return;
        }

        if (clickedIndex != currentTargetIndex)
        {
            ResetAfterWrongClick();
            return;
        }

        char foundLetter = answer[clickedIndex];

        Debug.Log(
            "MG23: 글자 획득 - " + foundLetter
        );

        // 획득한 글자는 즉시 숨기기
        letterButtons[clickedIndex]
            .gameObject.SetActive(false);

        currentTargetIndex++;

        UpdateProgressText();
        UpdateCurrentTargetText();

        if (currentTargetIndex >= answer.Length)
        {
            StartCoroutine(ReassembleStart());
        }
    }

    // =========================================
    // 잘못 클릭하면 초기화
    // =========================================

    private void ResetAfterWrongClick()
    {
        Debug.Log(
            "MG23: 잘못된 글자 클릭! 처음부터 시작"
        );

        currentTargetIndex = 0;

        movementTime = 0f;
        lastTeleportTime = 0f;

        sDirection = 1f;
        currentRVelocity = rVelocity;

        wrongNoticeUntil =
            Time.unscaledTime + 1.3f;

        for (int i = 0; i < 5; i++)
        {
            letterRects[i].anchoredPosition =
                initialPositions[i];

            letterButtons[i].interactable = true;
            letterButtons[i].gameObject.SetActive(true);
        }

        UpdateProgressText();
        UpdateCurrentTargetText();
    }

    // =========================================
    // 현재 목표 표시
    // =========================================

    private void UpdateCurrentTargetText()
    {
        if (currentTargetText == null)
            return;

        if (finalReady ||
            currentTargetIndex >= answer.Length)
        {
            currentTargetText.text =
                "CURRENT TARGET : COMPLETE";

            return;
        }

        if (Time.unscaledTime < wrongNoticeUntil)
        {
            currentTargetText.text =
                "WRONG! CURRENT TARGET : S";

            return;
        }

        currentTargetText.text =
            "CURRENT TARGET : " +
            answer[currentTargetIndex];
    }

    // =========================================
    // 진행 상황 표시
    // =========================================

    private void UpdateProgressText()
    {
        if (progressText == null)
            return;

        string result = "";

        for (int i = 0; i < answer.Length; i++)
        {
            if (i > 0)
                result += " - ";

            if (i < currentTargetIndex)
            {
                result += answer[i];
            }
            else
            {
                result += "_";
            }
        }

        progressText.text = result;
    }

    // =========================================
    // 다섯 글자 중앙으로 모으기
    // =========================================

    private IEnumerator ReassembleStart()
    {
        isReassembling = true;

        Vector2[] fromPositions = new Vector2[5];
        Vector2[] toPositions = new Vector2[5];

        for (int i = 0; i < 5; i++)
        {
            letterButtons[i].gameObject.SetActive(true);
            letterButtons[i].interactable = false;

            fromPositions[i] =
                letterRects[i].anchoredPosition;

            toPositions[i] = new Vector2(
                (i - 2) * mergedLetterSpacing,
                0f
            );
        }

        float duration =
            Mathf.Max(0.05f, mergeDuration);

        float timer = 0f;

        while (timer < duration)
        {
            if (!isGameActive)
                yield break;

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < 5; i++)
            {
                letterRects[i].anchoredPosition =
                    Vector2.Lerp(
                        fromPositions[i],
                        toPositions[i],
                        t
                    );
            }

            yield return null;
        }

        for (int i = 0; i < 5; i++)
        {
            letterButtons[i].gameObject.SetActive(false);
        }

        isReassembling = false;
        finalReady = true;

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.interactable = true;

        finalStartButton.transform.SetAsLastSibling();

        UpdateCurrentTargetText();

        Debug.Log("MG23: START 완성!");
    }

    // =========================================
    // 최종 START 클릭
    // =========================================

    private void OnFinalStartClicked()
    {
        if (!isGameActive || !finalReady)
            return;

        finalStartButton.interactable = false;

        Debug.Log("MG23: 검사 완료!");

        Success();
    }

    // =========================================
    // MinigameBase
    // =========================================

    protected override void GiveHint()
    {
        // 기획서에 별도 힌트 없음
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
