using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame11_PathPuzzle : MinigameBase
{
    [System.Serializable]
    public class StageData
    {
        [Header("Stage Root")]
        public GameObject stageRoot;

        [Header("Play Area")]
        public RectTransform playArea;

        [Header("START / EXIT")]
        public RectTransform startButton;
        public RectTransform exit;

        [Header("Movable Blocks")]
        public RectTransform[] horizontalBlocks;
        public RectTransform[] verticalBlocks;

        [Header("Fixed Blocks")]
        public RectTransform[] fixedBlocks;

        [Header("70 Second Route Hint")]
        public GameObject routeHint;
    }

    [Header("Stage 1")]
    public StageData stage1;

    [Header("Stage 2")]
    public StageData stage2;

    [Header("UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;

    [Header("Message Settings")]
    public float messageDuration = 2f;

    private StageData currentStage;
    private int currentStageNumber = 1;

    private float elapsedTime;

    private bool hint30Shown;
    private bool hint50Shown;
    private bool hint70Shown;

    private bool isStageChanging = false;

    // =========================
    // 블록 드래그
    // =========================

    private enum DragAxis
    {
        Horizontal,
        Vertical
    }

    private RectTransform draggingBlock;

    private Vector2 blockPointerStart;
    private Vector2 blockPositionStart;
    private Vector2 lastValidBlockPosition;

    // =========================
    // START 드래그
    // =========================

    private Vector2 startPointerStart;
    private Vector2 startPositionStart;
    private bool startDragFailed;

    // 최초 위치 저장
    private Dictionary<RectTransform, Vector2> initialPositions
        = new Dictionary<RectTransform, Vector2>();

    private Coroutine messageCoroutine;


    // ChapterManager 없이 단독 테스트할 때도 실행 가능
    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }


    // =========================================================
    // 게임 시작
    // =========================================================

    public override void StartMinigame()
    {
        gameName = "실행 경로 확보 검사";
        instruction =
            "블록을 움직여 START 버튼이 출구까지 이동할 수 있는 경로를 확보하세요.";

        base.StartMinigame();

        currentStageNumber = 1;
        currentStage = stage1;

        elapsedTime = 0f;

        hint30Shown = false;
        hint50Shown = false;
        hint70Shown = false;

        isStageChanging = false;
        startDragFailed = false;

        // 최초 위치 저장
        CacheInitialPositions(stage1);
        CacheInitialPositions(stage2);

        // 위치 초기화
        ResetStagePositions(stage1);
        ResetStagePositions(stage2);

        // Stage 활성화
        if (stage1.stageRoot != null)
            stage1.stageRoot.SetActive(true);

        if (stage2.stageRoot != null)
            stage2.stageRoot.SetActive(false);

        // 힌트 경로 숨기기
        if (stage1.routeHint != null)
            stage1.routeHint.SetActive(false);

        if (stage2.routeHint != null)
            stage2.routeHint.SetActive(false);

        if (hintText != null)
            hintText.text = instruction;

        // 드래그 이벤트 자동 세팅
        SetupStageEvents(stage1);
        SetupStageEvents(stage2);

        UpdateTimerUI();
    }


    // =========================================================
    // Update
    // =========================================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        // MinigameBase에서 currentTimer가 이미 감소하고 있음.
        elapsedTime = timeLimit - currentTimer;

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;

            UpdateTimerUI();
            OnTimeOut();

            return;
        }

        UpdateTimerUI();
        CheckHints();
    }


    // =========================================================
    // 힌트
    // =========================================================

    private void CheckHints()
    {
        if (elapsedTime >= 30f && !hint30Shown)
        {
            hint30Shown = true;

            ShowMessage(
                "[ HINT ] 움직일 수 있는 블록은 하나가 아닙니다."
            );
        }

        if (elapsedTime >= 50f && !hint50Shown)
        {
            hint50Shown = true;

            ShowMessage(
                "[ HINT ] 먼저 움직여야 하는 블록이 있을 수 있습니다."
            );
        }

        if (elapsedTime >= 70f && !hint70Shown)
        {
            hint70Shown = true;

            ShowMessage(
                "[ HINT ] START 버튼과 출구 사이의 목표 경로가 표시됩니다."
            );

            ShowCurrentRouteHint();
        }
    }


    private void ShowCurrentRouteHint()
    {
        if (currentStage != null &&
            currentStage.routeHint != null)
        {
            currentStage.routeHint.SetActive(true);
        }
    }


    // =========================================================
    // EventTrigger 생성
    // =========================================================

    private void SetupStageEvents(StageData stage)
    {
        if (stage == null)
            return;

        SetupBlockEvents(
            stage,
            stage.horizontalBlocks,
            DragAxis.Horizontal
        );

        SetupBlockEvents(
            stage,
            stage.verticalBlocks,
            DragAxis.Vertical
        );

        SetupStartEvents(stage);
    }


    private void SetupBlockEvents(
        StageData stage,
        RectTransform[] blocks,
        DragAxis axis)
    {
        if (blocks == null)
            return;

        foreach (RectTransform block in blocks)
        {
            if (block == null)
                continue;

            EventTrigger trigger =
                block.GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger =
                    block.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();

            // Begin Drag
            EventTrigger.Entry beginEntry =
                new EventTrigger.Entry();

            beginEntry.eventID =
                EventTriggerType.BeginDrag;

            beginEntry.callback.AddListener(
                (data) =>
                {
                    OnBeginBlockDrag(
                        stage,
                        block,
                        axis,
                        (PointerEventData)data
                    );
                });

            trigger.triggers.Add(beginEntry);


            // Drag
            EventTrigger.Entry dragEntry =
                new EventTrigger.Entry();

            dragEntry.eventID =
                EventTriggerType.Drag;

            dragEntry.callback.AddListener(
                (data) =>
                {
                    OnBlockDrag(
                        stage,
                        block,
                        axis,
                        (PointerEventData)data
                    );
                });

            trigger.triggers.Add(dragEntry);


            // End Drag
            EventTrigger.Entry endEntry =
                new EventTrigger.Entry();

            endEntry.eventID =
                EventTriggerType.EndDrag;

            endEntry.callback.AddListener(
                (data) =>
                {
                    OnEndBlockDrag();
                });

            trigger.triggers.Add(endEntry);
        }
    }


    private void SetupStartEvents(StageData stage)
    {
        if (stage.startButton == null)
            return;

        EventTrigger trigger =
            stage.startButton.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                stage.startButton.gameObject
                .AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();


        // Begin Drag
        EventTrigger.Entry beginEntry =
            new EventTrigger.Entry();

        beginEntry.eventID =
            EventTriggerType.BeginDrag;

        beginEntry.callback.AddListener(
            (data) =>
            {
                OnBeginStartDrag(
                    stage,
                    (PointerEventData)data
                );
            });

        trigger.triggers.Add(beginEntry);


        // Drag
        EventTrigger.Entry dragEntry =
            new EventTrigger.Entry();

        dragEntry.eventID =
            EventTriggerType.Drag;

        dragEntry.callback.AddListener(
            (data) =>
            {
                OnStartDrag(
                    stage,
                    (PointerEventData)data
                );
            });

        trigger.triggers.Add(dragEntry);


        // End Drag
        EventTrigger.Entry endEntry =
            new EventTrigger.Entry();

        endEntry.eventID =
            EventTriggerType.EndDrag;

        endEntry.callback.AddListener(
            (data) =>
            {
                OnEndStartDrag(stage);
            });

        trigger.triggers.Add(endEntry);
    }


    // =========================================================
    // 블록 드래그
    // =========================================================

    private void OnBeginBlockDrag(
        StageData stage,
        RectTransform block,
        DragAxis axis,
        PointerEventData data)
    {
        if (!isGameActive)
            return;

        if (stage != currentStage)
            return;

        draggingBlock = block;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stage.playArea,
            data.position,
            data.pressEventCamera,
            out blockPointerStart
        );

        blockPositionStart =
            block.anchoredPosition;

        lastValidBlockPosition =
            block.anchoredPosition;

        block.SetAsLastSibling();
    }


    private void OnBlockDrag(
        StageData stage,
        RectTransform block,
        DragAxis axis,
        PointerEventData data)
    {
        if (!isGameActive)
            return;

        if (stage != currentStage)
            return;

        if (draggingBlock != block)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stage.playArea,
            data.position,
            data.pressEventCamera,
            out Vector2 currentPointer
        );

        Vector2 delta =
            currentPointer - blockPointerStart;

        Vector2 newPosition =
            blockPositionStart;

        if (axis == DragAxis.Horizontal)
        {
            newPosition.x += delta.x;
        }
        else
        {
            newPosition.y += delta.y;
        }

        block.anchoredPosition =
            newPosition;

        ClampInsidePlayArea(
            stage.playArea,
            block
        );

        // 다른 블록과 겹치면 마지막 정상 위치로 돌아감
        if (IsBlockOverlappingOtherBlock(stage, block))
        {
            block.anchoredPosition =
                lastValidBlockPosition;
        }
        else
        {
            lastValidBlockPosition =
                block.anchoredPosition;
        }
    }


    private void OnEndBlockDrag()
    {
        draggingBlock = null;
    }


   
    private void OnBeginStartDrag(
        StageData stage,
        PointerEventData data)
    {
        if (!isGameActive ||
            isStageChanging ||
            stage != currentStage)
        {
            return;
        }

        startDragFailed = false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stage.playArea,
            data.position,
            data.pressEventCamera,
            out startPointerStart
        );

        startPositionStart =
            stage.startButton.anchoredPosition;

        stage.startButton.SetAsLastSibling();
    }


    private void OnStartDrag(
        StageData stage,
        PointerEventData data)
    {
        if (!isGameActive ||
            startDragFailed ||
            stage != currentStage)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stage.playArea,
            data.position,
            data.pressEventCamera,
            out Vector2 currentPointer
        );

        Vector2 delta =
            currentPointer - startPointerStart;

        stage.startButton.anchoredPosition =
            startPositionStart + delta;

        ClampInsidePlayArea(
            stage.playArea,
            stage.startButton
        );

        // 장애물과 충돌
        if (IsStartTouchingBlock(stage))
        {
            startDragFailed = true;

            ResetStartPosition(stage);

            ShowMessage(
                "실행 경로가 확보되지 않았습니다."
            );
        }
    }


    private void OnEndStartDrag(StageData stage)
    {
        if (!isGameActive ||
            stage != currentStage)
        {
            return;
        }

        if (startDragFailed)
        {
            return;
        }

        // EXIT 도착
        if (RectsOverlap(
            stage.startButton,
            stage.exit))
        {
            ClearCurrentStage();
            return;
        }

        // 출구가 아닌 곳에서 놓으면 START만 원위치
        ResetStartPosition(stage);
    }

    

    private void ClearCurrentStage()
    {
        if (isStageChanging)
            return;

        if (currentStageNumber == 1)
        {
            isStageChanging = true;

            ShowMessage(
                "1차 실행 경로 확보 완료"
            );

            if (stage1.stageRoot != null)
                stage1.stageRoot.SetActive(false);

            if (stage2.stageRoot != null)
                stage2.stageRoot.SetActive(true);

            currentStageNumber = 2;
            currentStage = stage2;

          
            if (elapsedTime >= 70f &&
                stage2.routeHint != null)
            {
                stage2.routeHint.SetActive(true);
            }

            isStageChanging = false;
        }
        else
        {
            
            Success();
        }
    }


    

    private bool IsStartTouchingBlock(
        StageData stage)
    {
        if (CheckArrayCollision(
            stage.startButton,
            stage.horizontalBlocks))
        {
            return true;
        }

        if (CheckArrayCollision(
            stage.startButton,
            stage.verticalBlocks))
        {
            return true;
        }

        if (CheckArrayCollision(
            stage.startButton,
            stage.fixedBlocks))
        {
            return true;
        }

        return false;
    }


    private bool CheckArrayCollision(
        RectTransform target,
        RectTransform[] blocks)
    {
        if (blocks == null)
            return false;

        foreach (RectTransform block in blocks)
        {
            if (block == null)
                continue;

            if (RectsOverlap(target, block))
            {
                return true;
            }
        }

        return false;
    }


    private bool IsBlockOverlappingOtherBlock(
        StageData stage,
        RectTransform targetBlock)
    {
        if (CheckBlockArray(
            targetBlock,
            stage.horizontalBlocks))
        {
            return true;
        }

        if (CheckBlockArray(
            targetBlock,
            stage.verticalBlocks))
        {
            return true;
        }

        if (CheckBlockArray(
            targetBlock,
            stage.fixedBlocks))
        {
            return true;
        }

        return false;
    }


    private bool CheckBlockArray(
        RectTransform target,
        RectTransform[] blocks)
    {
        if (blocks == null)
            return false;

        foreach (RectTransform block in blocks)
        {
            if (block == null ||
                block == target)
            {
                continue;
            }

            if (RectsOverlap(target, block))
            {
                return true;
            }
        }

        return false;
    }


    private bool RectsOverlap(
        RectTransform a,
        RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];

        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        Rect rectA =
            Rect.MinMaxRect(
                aCorners[0].x,
                aCorners[0].y,
                aCorners[2].x,
                aCorners[2].y
            );

        Rect rectB =
            Rect.MinMaxRect(
                bCorners[0].x,
                bCorners[0].y,
                bCorners[2].x,
                bCorners[2].y
            );

        return rectA.Overlaps(rectB);
    }


 

    private void ClampInsidePlayArea(
        RectTransform playArea,
        RectTransform target)
    {
        if (playArea == null ||
            target == null)
        {
            return;
        }

        Vector2 position =
            target.anchoredPosition;

        float halfWidth =
            target.rect.width * 0.5f;

        float halfHeight =
            target.rect.height * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            playArea.rect.xMin + halfWidth,
            playArea.rect.xMax - halfWidth
        );

        position.y = Mathf.Clamp(
            position.y,
            playArea.rect.yMin + halfHeight,
            playArea.rect.yMax - halfHeight
        );

        target.anchoredPosition =
            position;
    }


 
    private void CacheInitialPositions(
        StageData stage)
    {
        if (stage == null)
            return;

        CachePosition(stage.startButton);

        CacheArray(stage.horizontalBlocks);
        CacheArray(stage.verticalBlocks);
        CacheArray(stage.fixedBlocks);
    }


    private void CacheArray(
        RectTransform[] array)
    {
        if (array == null)
            return;

        foreach (RectTransform rect in array)
        {
            CachePosition(rect);
        }
    }


    private void CachePosition(
        RectTransform rect)
    {
        if (rect == null)
            return;

        if (!initialPositions.ContainsKey(rect))
        {
            initialPositions.Add(
                rect,
                rect.anchoredPosition
            );
        }
    }


    private void ResetStagePositions(
        StageData stage)
    {
        if (stage == null)
            return;

        ResetPosition(stage.startButton);

        ResetArray(stage.horizontalBlocks);
        ResetArray(stage.verticalBlocks);
        ResetArray(stage.fixedBlocks);
    }


    private void ResetArray(
        RectTransform[] array)
    {
        if (array == null)
            return;

        foreach (RectTransform rect in array)
        {
            ResetPosition(rect);
        }
    }


    private void ResetPosition(
        RectTransform rect)
    {
        if (rect == null)
            return;

        if (initialPositions.ContainsKey(rect))
        {
            rect.anchoredPosition =
                initialPositions[rect];
        }
    }


    private void ResetStartPosition(
        StageData stage)
    {
        ResetPosition(stage.startButton);
    }


  
    private void ShowMessage(string message)
    {
        if (hintText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
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
            messageDuration
        );

        if (hintText != null &&
            isGameActive)
        {
            hintText.text = instruction;
        }

        messageCoroutine = null;
    }


    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        float displayTime =
            Mathf.Max(0f, currentTimer);

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


    private void OnTimeOut()
    {
        isGameActive = false;

        if (hintText != null)
        {
            hintText.text =
                "시간 초과! 실행 경로를 확보하지 못했습니다.";
        }
    }


    // 이번 게임은 30 / 50 / 70초
    // 커스텀 힌트를 사용
    protected override void GiveHint()
    {
    }


    protected override void RestartGame()
    {
        StartMinigame();
    }
}