using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame15_StartTower : MinigameBase
{
    [Header("Play Area")]
    public RectTransform playArea;
    public RectTransform floor;
    public RectTransform spawnPoint;

    [Header("Letter Blocks - S T A R T Order")]
    public RectTransform[] letterBlocks;

    [Header("Completed START Button")]
    public Button completedStartButton;

    [Header("UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI fallText;

    [Header("Block Movement")]
    public float normalFallSpeed = 70f;
    public float fastFallSpeed = 650f;
    public float horizontalMoveSpeed = 900f;

    [Header("Landing")]
    [Range(0.1f, 1f)]
    public float minimumOverlapRatio = 0.5f;

    public float respawnDelay = 0.6f;

    [Header("Stability")]
    public float stabilityCheckTime = 3f;

    [Header("Fall 2 Assist")]
    public float floorGrowMultiplier = 1.35f;

    [Header("Fall 4 Assist")]
    [Range(0.1f, 1f)]
    public float horizontalSlowMultiplier = 0.6f;

    [Header("70 Second Hint")]
    public float centerHintTime = 70f;
    public RectTransform centerHintLine;

    [Header("Chapter 3 Complete")]
    public GameObject completionPanel;
    public TextMeshProUGUI completionText;
    public float completionHoldTime = 1.5f;

    private RectTransform activeBlock;

    private readonly List<RectTransform> placedBlocks =
        new List<RectTransform>();

    private int currentBlockIndex = 0;
    private int fallCount = 0;

    private float elapsedTime = 0f;
    private float currentHorizontalSpeed;

    private Vector2 initialFloorSize;

    private bool fastDropping = false;
    private bool missedSupport = false;
    private bool isRespawning = false;

    private bool floorAssistApplied = false;
    private bool speedAssistApplied = false;
    private bool centerHintShown = false;

    private bool stabilityChecking = false;
    private bool towerCompleted = false;
    private bool finalizing = false;

    private Canvas parentCanvas;

    private Coroutine messageCoroutine;


    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }


    // =====================================================
    // 게임 시작
    // =====================================================

    public override void StartMinigame()
    {
        gameName = "실행 구조 안정성 검사";

        instruction =
            "S, T, A, R, T 블록을 순서대로 안정적으로 쌓으세요.";

        base.StartMinigame();

        StopAllCoroutines();

        parentCanvas =
            GetComponentInParent<Canvas>();

        elapsedTime = 0f;

        currentBlockIndex = 0;
        fallCount = 0;

        fastDropping = false;
        missedSupport = false;
        isRespawning = false;

        floorAssistApplied = false;
        speedAssistApplied = false;
        centerHintShown = false;

        stabilityChecking = false;
        towerCompleted = false;
        finalizing = false;

        currentHorizontalSpeed =
            horizontalMoveSpeed;

        placedBlocks.Clear();

        if (floor != null)
        {
            initialFloorSize =
                floor.sizeDelta;
        }

        ResetLetterBlocks();

        if (centerHintLine != null)
        {
            centerHintLine.gameObject
                .SetActive(false);
        }

        SetupCompletedStartButton();

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        if (hintText != null)
        {
            hintText.text = instruction;
        }

        UpdateFallUI();
        UpdateTimerUI();

        SpawnCurrentBlock();
    }


    // =====================================================
    // Update
    // =====================================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || finalizing)
            return;

        elapsedTime =
            timeLimit - currentTimer;

        UpdateTimerUI();

        CheckCenterHint();

        if (centerHintShown)
        {
            UpdateCenterHintLine();
        }

        if (activeBlock == null ||
            stabilityChecking ||
            towerCompleted ||
            isRespawning)
        {
            return;
        }

        MoveBlockHorizontally();
        MoveBlockDown();

        // 클릭하면 빠르게 낙하
        if (Input.GetMouseButtonDown(0))
        {
            fastDropping = true;
        }

        CheckLanding();
        CheckBlockMiss();
    }


    // =====================================================
    // 블록 생성
    // =====================================================

    private void SpawnCurrentBlock()
    {
        if (letterBlocks == null ||
            currentBlockIndex >= letterBlocks.Length)
        {
            return;
        }

        activeBlock =
            letterBlocks[currentBlockIndex];

        if (activeBlock == null)
            return;

        activeBlock.gameObject.SetActive(true);

        Vector2 spawnPosition =
            Vector2.zero;

        if (spawnPoint != null)
        {
            spawnPosition =
                spawnPoint.anchoredPosition;
        }
        else if (playArea != null)
        {
            spawnPosition =
                new Vector2(
                    0f,
                    playArea.rect.yMax - 60f
                );
        }

        activeBlock.anchoredPosition =
            spawnPosition;

        activeBlock.localRotation =
            Quaternion.identity;

        fastDropping = false;
        missedSupport = false;
    }


    private IEnumerator RespawnCurrentBlock()
    {
        isRespawning = true;

        yield return new WaitForSeconds(
            respawnDelay
        );

        isRespawning = false;

        if (isGameActive &&
            !towerCompleted)
        {
            SpawnCurrentBlock();
        }
    }


    // =====================================================
    // 좌우 이동
    // =====================================================

    private void MoveBlockHorizontally()
    {
        if (playArea == null ||
            activeBlock == null)
        {
            return;
        }

        Camera uiCamera = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                parentCanvas.worldCamera;
        }

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                playArea,
                Input.mousePosition,
                uiCamera,
                out Vector2 mousePosition
            ))
        {
            return;
        }

        float halfWidth =
            activeBlock.rect.width * 0.5f;

        float targetX =
            Mathf.Clamp(
                mousePosition.x,
                playArea.rect.xMin +
                halfWidth,
                playArea.rect.xMax -
                halfWidth
            );

        Vector2 current =
            activeBlock.anchoredPosition;

        current.x =
            Mathf.MoveTowards(
                current.x,
                targetX,
                currentHorizontalSpeed *
                Time.deltaTime
            );

        activeBlock.anchoredPosition =
            current;
    }


    // =====================================================
    // 낙하
    // =====================================================

    private void MoveBlockDown()
    {
        float speed =
            fastDropping
            ? fastFallSpeed
            : normalFallSpeed;

        activeBlock.anchoredPosition +=
            Vector2.down *
            speed *
            Time.deltaTime;
    }


    // =====================================================
    // 착지 검사
    // =====================================================

    private void CheckLanding()
    {
        if (activeBlock == null ||
            missedSupport)
        {
            return;
        }

        RectTransform support =
            GetCurrentSupport();

        if (support == null)
            return;

        float blockBottom =
            activeBlock.anchoredPosition.y -
            activeBlock.rect.height * 0.5f;

        float supportTop =
            support.anchoredPosition.y +
            support.rect.height * 0.5f;

        if (blockBottom > supportTop)
            return;

        float overlapRatio =
            GetHorizontalOverlapRatio(
                activeBlock,
                support
            );

        if (overlapRatio >=
            minimumOverlapRatio)
        {
            LandBlock(support);
        }
        else
        {
            // 지지대를 제대로 못 밟았으므로
            // 그대로 아래로 떨어짐
            missedSupport = true;

            fastDropping = true;
        }
    }


    private RectTransform GetCurrentSupport()
    {
        if (currentBlockIndex == 0)
        {
            return floor;
        }

        if (placedBlocks.Count == 0)
            return floor;

        return placedBlocks[
            placedBlocks.Count - 1
        ];
    }


    private float GetHorizontalOverlapRatio(
        RectTransform block,
        RectTransform support)
    {
        float blockLeft =
            block.anchoredPosition.x -
            block.rect.width * 0.5f;

        float blockRight =
            block.anchoredPosition.x +
            block.rect.width * 0.5f;

        float supportLeft =
            support.anchoredPosition.x -
            support.rect.width * 0.5f;

        float supportRight =
            support.anchoredPosition.x +
            support.rect.width * 0.5f;

        float overlap =
            Mathf.Max(
                0f,
                Mathf.Min(
                    blockRight,
                    supportRight
                )
                -
                Mathf.Max(
                    blockLeft,
                    supportLeft
                )
            );

        return overlap /
            Mathf.Max(
                1f,
                block.rect.width
            );
    }


    private void LandBlock(
        RectTransform support)
    {
        float correctY =
            support.anchoredPosition.y +
            support.rect.height * 0.5f +
            activeBlock.rect.height * 0.5f;

        Vector2 position =
            activeBlock.anchoredPosition;

        position.y = correctY;

        activeBlock.anchoredPosition =
            position;

        placedBlocks.Add(activeBlock);

        activeBlock = null;

        currentBlockIndex++;

        fastDropping = false;
        missedSupport = false;

        // 다섯 블록 완료
        if (currentBlockIndex >=
            letterBlocks.Length)
        {
            StartCoroutine(
                StabilityCheckRoutine()
            );

            return;
        }

        StartCoroutine(
            SpawnNextBlockRoutine()
        );
    }


    private IEnumerator SpawnNextBlockRoutine()
    {
        yield return new WaitForSeconds(
            0.25f
        );

        SpawnCurrentBlock();
    }


    // =====================================================
    // 블록 떨어짐
    // =====================================================

    private void CheckBlockMiss()
    {
        if (activeBlock == null ||
            playArea == null)
        {
            return;
        }

        float top =
            activeBlock.anchoredPosition.y +
            activeBlock.rect.height * 0.5f;

        if (top >
            playArea.rect.yMin - 80f)
        {
            return;
        }

        OnBlockFallen();
    }


    private void OnBlockFallen()
    {
        if (activeBlock == null)
            return;

        RectTransform failedBlock =
            activeBlock;

        activeBlock = null;

        failedBlock.gameObject
            .SetActive(false);

        fallCount++;

        UpdateFallUI();

        ShowMessage(
            "블록이 떨어졌습니다. 같은 블록이 다시 제공됩니다."
        );

        ApplyFallAssists();

        StartCoroutine(
            RespawnCurrentBlock()
        );
    }


    // =====================================================
    // 실패 보조 기능
    // =====================================================

    private void ApplyFallAssists()
    {
        // 2회 실패
        if (fallCount >= 2 &&
            !floorAssistApplied)
        {
            floorAssistApplied = true;

            if (floor != null)
            {
                Vector2 size =
                    floor.sizeDelta;

                size.x =
                    initialFloorSize.x *
                    floorGrowMultiplier;

                floor.sizeDelta = size;
            }

            ShowMessage(
                "[ HINT ] 바닥의 폭이 증가했습니다."
            );
        }

        // 4회 실패
        if (fallCount >= 4 &&
            !speedAssistApplied)
        {
            speedAssistApplied = true;

            currentHorizontalSpeed =
                horizontalMoveSpeed *
                horizontalSlowMultiplier;

            ShowMessage(
                "[ HINT ] 블록의 좌우 이동 속도가 감소했습니다."
            );
        }
    }


    // =====================================================
    // 70초 힌트
    // =====================================================

    private void CheckCenterHint()
    {
        if (centerHintShown)
            return;

        if (elapsedTime <
            centerHintTime)
        {
            return;
        }

        centerHintShown = true;

        if (centerHintLine != null)
        {
            centerHintLine
                .gameObject
                .SetActive(true);

            UpdateCenterHintLine();
        }

        ShowMessage(
            "[ HINT ] 이전 블록의 중앙 위치가 표시됩니다."
        );
    }


    private void UpdateCenterHintLine()
    {
        if (centerHintLine == null ||
            playArea == null)
        {
            return;
        }

        float targetX = 0f;

        if (placedBlocks.Count > 0)
        {
            targetX =
                placedBlocks[
                    placedBlocks.Count - 1
                ].anchoredPosition.x;
        }
        else if (floor != null)
        {
            targetX =
                floor.anchoredPosition.x;
        }

        Vector2 position =
            centerHintLine
                .anchoredPosition;

        position.x = targetX;
        position.y = 0f;

        centerHintLine
            .anchoredPosition =
            position;

        Vector2 size =
            centerHintLine.sizeDelta;

        size.y =
            playArea.rect.height;

        centerHintLine.sizeDelta =
            size;
    }


    // =====================================================
    // 3초 안정성 검사
    // =====================================================

    private IEnumerator StabilityCheckRoutine()
    {
        stabilityChecking = true;

        float timer =
            stabilityCheckTime;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            if (hintText != null)
            {
                hintText.text =
                    $"구조 안정성 확인 중... {Mathf.Max(0f, timer):0.0}";
            }

            yield return null;
        }

        stabilityChecking = false;

        CreateCompletedStartButton();
    }


    // =====================================================
    // START 버튼 결합
    // =====================================================

    private void CreateCompletedStartButton()
    {
        towerCompleted = true;

        if (letterBlocks != null)
        {
            foreach (RectTransform block
                     in letterBlocks)
            {
                if (block != null)
                {
                    block.gameObject
                        .SetActive(false);
                }
            }
        }

        if (completedStartButton != null)
        {
            completedStartButton
                .gameObject
                .SetActive(true);

            completedStartButton
                .interactable = true;
        }

        if (centerHintLine != null)
        {
            centerHintLine.gameObject
                .SetActive(false);
        }

        if (hintText != null)
        {
            hintText.text =
                "START 버튼이 완성되었습니다. 버튼을 클릭하세요.";
        }
    }


    private void SetupCompletedStartButton()
    {
        if (completedStartButton == null)
            return;

        completedStartButton
            .gameObject
            .SetActive(false);

        completedStartButton
            .onClick
            .RemoveAllListeners();

        completedStartButton
            .onClick
            .AddListener(
                OnCompletedStartClicked
            );
    }


    // =====================================================
    // 최종 START 클릭
    // =====================================================

    private void OnCompletedStartClicked()
    {
        if (!isGameActive ||
            !towerCompleted ||
            finalizing)
        {
            return;
        }

        StartCoroutine(
            ChapterCompleteRoutine()
        );
    }


    // =====================================================
    // Chapter 3 완료 문구
    // =====================================================

    private IEnumerator ChapterCompleteRoutine()
    {
        finalizing = true;

        if (completedStartButton != null)
        {
            completedStartButton
                .interactable = false;
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }

        SetCompletionText(
            "게임 실행 처리 검사 완료\n\n" +
            "물리 처리 정상\n" +
            "입력 반응 정상\n" +
            "실행 경로 정상\n" +
            "장치 연결 정상\n" +
            "실행 구조 정상\n\n" +
            "게임 실행 프로세스 이미 진행 중입니다."
        );

        yield return new WaitForSeconds(
            0.3f
        );

        SetCompletionText(
            "게임 실행 처리 검사 완료\n\n" +
            "물리 처리 정상\n" +
            "입력 반응 정상\n" +
            "실행 경로 정상\n" +
            "장치 연결 정상\n" +
            "실행 구조 정상\n\n" +
            "게임 실행 프로세스 준비 완료\n" +
            "게임 실행 준비도: 75%"
        );

        yield return new WaitForSeconds(
            completionHoldTime
        );

        Success();
    }


    private void SetCompletionText(
        string message)
    {
        if (completionText != null)
        {
            completionText.text =
                message;
        }
        else if (hintText != null)
        {
            hintText.text =
                message;
        }
    }


    // =====================================================
    // 초기화
    // =====================================================

    private void ResetLetterBlocks()
    {
        if (letterBlocks == null)
            return;

        foreach (RectTransform block
                 in letterBlocks)
        {
            if (block != null)
            {
                block.gameObject
                    .SetActive(false);
            }
        }

        if (floor != null &&
            initialFloorSize != Vector2.zero)
        {
            floor.sizeDelta =
                initialFloorSize;
        }
    }


    // =====================================================
    // UI
    // =====================================================

    private void UpdateFallUI()
    {
        if (fallText != null)
        {
            fallText.text =
                $"FALL {fallCount}";
        }
    }


    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        float displayTime =
            Mathf.Max(
                0f,
                currentTimer
            );

        int minutes =
            Mathf.FloorToInt(
                displayTime / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                displayTime % 60f
            );

        timerText.text =
            string.Format(
                "TIME {0:00}:{1:00}",
                minutes,
                seconds
            );
    }


    private void ShowMessage(
        string message)
    {
        if (hintText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }

        hintText.text = message;

        messageCoroutine =
            StartCoroutine(
                ClearMessageRoutine()
            );
    }


    private IEnumerator ClearMessageRoutine()
    {
        yield return new WaitForSeconds(
            2f
        );

        if (hintText != null &&
            isGameActive &&
            !towerCompleted &&
            !stabilityChecking)
        {
            hintText.text =
                instruction;
        }

        messageCoroutine = null;
    }


    protected override void GiveHint()
    {
    }


    protected override void RestartGame()
    {
        StartMinigame();
    }
}