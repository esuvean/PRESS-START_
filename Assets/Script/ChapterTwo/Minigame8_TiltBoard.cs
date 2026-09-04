using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Minigame8_TiltBoard : MinigameBase
{
    [Header("UI")]
    public Canvas canvas;
    public TextMeshProUGUI statusText;

    [Header("Board")]
    public RectTransform board;
    public RectTransform movingButton;
    public RectTransform goalSlot;

    [Header("Course 1")]
    public RectTransform course1StartPoint;
    public RectTransform course1GoalPoint;

    [Header("Course 2")]
    public RectTransform course2StartPoint;
    public RectTransform course2GoalPoint;

    public GameObject course2ObstacleRoot;
    public RectTransform[] course2Obstacles;

   [Header("Visual Tilt")]
    public BoardTiltVisual boardTiltVisual;

    [Header("Control Settings")]

 
    // 마우스를 이 정도 움직이면 최대 기울기
    public float dragRange = 220f;

    // 미세한 손떨림 무시
    public float deadZone = 0.08f;

    // 버튼 최대 이동 속도
    public float moveSpeed = 420f;

    // 버튼 움직임 부드러움
    public float movementSmooth = 8f;

    // 기울기 부드러움
    public float tiltSmoothTime = 0.12f;

    // 화면상 판 최대 회전 각도
    public float maxVisualAngle = 6f;

    private bool started = false;
    private bool dragging = false;
    private bool transitioning = false;
    private bool resetting = false;

    private Vector2 dragStartPosition;

    private Vector2 targetTilt = Vector2.zero;
    private Vector2 currentTilt = Vector2.zero;
    private Vector2 tiltSmoothVelocity = Vector2.zero;

    private Vector2 currentVelocity = Vector2.zero;

    private int currentCourse = 1;
    private int failCount = 0;

    // ==========================================
    // 시작
    // ==========================================

    private void Start()
    {
        if (!started)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        if (started)
            return;

        started = true;

        gameName = "화면 방향 보정 검사";

        instruction =
            "게임판을 기울여 START 버튼을 목표 슬롯까지 이동시킨다.";

        base.StartMinigame();

        currentCourse = 1;
        failCount = 0;

        SetupCourse(1);

        if (statusText != null)
        {
            statusText.text =
                "게임판을 상하좌우로 드래그하여 START를 이동하세요.";
        }
    }

    // ==========================================
    // Update
    // ==========================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        if (transitioning || resetting)
            return;

        HandleInput();

        SmoothTilt();

        MoveButton();

        CheckObstacle();

        CheckOutsideBoard();

        CheckGoal();
    }

    // ==========================================
    // 마우스 입력
    // ==========================================

    private void HandleInput()
    {
        if (Mouse.current == null || board == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Camera uiCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        // 클릭 시작
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool insideBoard =
                RectTransformUtility.RectangleContainsScreenPoint(
                    board,
                    mousePosition,
                    uiCamera
                );

            if (insideBoard)
            {
                dragging = true;

                dragStartPosition =
                    mousePosition;
            }
        }

        // 드래그 중
        if (dragging &&
            Mouse.current.leftButton.isPressed)
        {
            Vector2 dragDelta =
                mousePosition -
                dragStartPosition;

            Vector2 rawTilt =
                dragDelta /
                dragRange;

            rawTilt =
                Vector2.ClampMagnitude(
                    rawTilt,
                    1f
                );

            // 작은 손 떨림 제거
            if (Mathf.Abs(rawTilt.x) < deadZone)
                rawTilt.x = 0f;

            if (Mathf.Abs(rawTilt.y) < deadZone)
                rawTilt.y = 0f;

            targetTilt =
                rawTilt;
        }

        // 마우스 놓음
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragging = false;

            targetTilt =
                Vector2.zero;
        }
    }

    // ==========================================
    // 판 기울기 부드럽게
    // ==========================================

    private void SmoothTilt()
    {
        currentTilt =
            Vector2.SmoothDamp(
                currentTilt,
                targetTilt,
                ref tiltSmoothVelocity,
                tiltSmoothTime
            );

        // 실제 Board 자체를 돌리지 않음
        if (board != null)
        {
            board.localRotation =
                Quaternion.identity;
        }

        // BoardVisual에 기울기 전달
        if (boardTiltVisual != null)
        {
            boardTiltVisual.SetTilt(currentTilt);
        }
    }
    // ==========================================
    // START 버튼 이동
    // ==========================================

    private void MoveButton()
    {
        if (movingButton == null)
            return;

        // 원하는 이동 속도
        Vector2 desiredVelocity =
            currentTilt *
            moveSpeed;

        // 급격한 속도 변화 방지
        currentVelocity =
            Vector2.Lerp(
                currentVelocity,
                desiredVelocity,
                1f -
                Mathf.Exp(
                    -movementSmooth *
                    Time.deltaTime
                )
            );

        movingButton.anchoredPosition +=
            currentVelocity *
            Time.deltaTime;
    }

    // ==========================================
    // 장애물
    // ==========================================

    private void CheckObstacle()
    {
        if (currentCourse != 2)
            return;

        if (course2Obstacles == null)
            return;

        foreach (RectTransform obstacle
                 in course2Obstacles)
        {
            if (obstacle == null)
                continue;

            if (WorldRect(movingButton)
                .Overlaps(
                    WorldRect(obstacle)
                ))
            {
                failCount++;

                StartCoroutine(
                    ResetAfterCollision(
                        "장애물에 충돌했습니다."
                    )
                );

                return;
            }
        }
    }

    // ==========================================
    // 화면 밖
    // ==========================================

    private void CheckOutsideBoard()
    {
        if (board == null ||
            movingButton == null)
            return;

        Vector2 pos =
            movingButton.anchoredPosition;

        float halfW =
            movingButton.rect.width *
            0.5f;

        float halfH =
            movingButton.rect.height *
            0.5f;

        float minX =
            board.rect.xMin -
            halfW;

        float maxX =
            board.rect.xMax +
            halfW;

        float minY =
            board.rect.yMin -
            halfH;

        float maxY =
            board.rect.yMax +
            halfH;

        if (pos.x < minX ||
            pos.x > maxX ||
            pos.y < minY ||
            pos.y > maxY)
        {
            failCount++;

            StartCoroutine(
                ResetAfterCollision(
                    "START 버튼이 게임판 밖으로 떨어졌습니다."
                )
            );
        }
    }

    // ==========================================
    // 충돌 후 리셋
    // ==========================================

    private IEnumerator ResetAfterCollision(
        string message
    )
    {
        if (resetting)
            yield break;

        resetting = true;

        currentVelocity =
            Vector2.zero;

        targetTilt =
            Vector2.zero;

        currentTilt =
            Vector2.zero;

        if (board != null)
            board.localRotation =
                Quaternion.identity;

        if (statusText != null)
        {
            statusText.text =
                message +
                $" 실패 {failCount}회";
        }

        yield return
            new WaitForSeconds(0.35f);

        ResetButton();

        resetting = false;
    }

    // ==========================================
    // GOAL
    // ==========================================

    private void CheckGoal()
    {
        if (movingButton == null ||
            goalSlot == null)
            return;

        if (!WorldRect(movingButton)
            .Overlaps(
                WorldRect(goalSlot)
            ))
        {
            return;
        }

        currentVelocity =
            Vector2.zero;

        targetTilt =
            Vector2.zero;

        currentTilt =
            Vector2.zero;

        if (currentCourse == 1)
        {
            StartCoroutine(
                MoveToCourse2()
            );
        }
        else
        {
            if (statusText != null)
            {
                statusText.text =
                    "화면 방향 보정 완료";
            }

            Success();
        }
    }

    // ==========================================
    // 2코스
    // ==========================================

    private IEnumerator MoveToCourse2()
    {
        transitioning = true;

        if (statusText != null)
        {
            statusText.text =
                "1코스 완료";
        }

        yield return
            new WaitForSeconds(0.7f);

        currentCourse = 2;

        SetupCourse(2);

        if (statusText != null)
        {
            statusText.text =
                "장애물을 피해 GOAL까지 이동하세요.";
        }

        transitioning = false;
    }

    // ==========================================
    // 코스 배치
    // ==========================================

    private void SetupCourse(int course)
    {
        currentVelocity =
            Vector2.zero;

        targetTilt =
            Vector2.zero;

        currentTilt =
            Vector2.zero;

        tiltSmoothVelocity =
            Vector2.zero;

        if (board != null)
        {
            board.localRotation =
                Quaternion.identity;
        }

        if (course == 1)
        {
            if (movingButton != null &&
                course1StartPoint != null)
            {
                movingButton.anchoredPosition =
                    course1StartPoint.anchoredPosition;
            }

            if (goalSlot != null &&
                course1GoalPoint != null)
            {
                goalSlot.anchoredPosition =
                    course1GoalPoint.anchoredPosition;
            }

            if (course2ObstacleRoot != null)
            {
                course2ObstacleRoot.SetActive(false);
            }
        }
        else
        {
            if (movingButton != null &&
                course2StartPoint != null)
            {
                movingButton.anchoredPosition =
                    course2StartPoint.anchoredPosition;
            }

            if (goalSlot != null &&
                course2GoalPoint != null)
            {
                goalSlot.anchoredPosition =
                    course2GoalPoint.anchoredPosition;
            }

            if (course2ObstacleRoot != null)
            {
                course2ObstacleRoot.SetActive(true);
            }
        }
        if (boardTiltVisual != null)
        {
            boardTiltVisual.ResetTilt();
        }
    }

    // ==========================================
    // 현재 코스 시작점으로
    // ==========================================

    private void ResetButton()
    {
        currentVelocity =
            Vector2.zero;

        targetTilt =
            Vector2.zero;

        currentTilt =
            Vector2.zero;

        tiltSmoothVelocity =
            Vector2.zero;

        if (board != null)
        {
            board.localRotation =
                Quaternion.identity;
        }

        if (currentCourse == 1)
        {
            if (course1StartPoint != null)
            {
                movingButton.anchoredPosition =
                    course1StartPoint.anchoredPosition;
            }
        }
        else
        {
            if (course2StartPoint != null)
            {
                movingButton.anchoredPosition =
                    course2StartPoint.anchoredPosition;
            }
        }

        if (boardTiltVisual != null)
        {
            boardTiltVisual.ResetTilt();
        }
    }

    // ==========================================
    // 충돌 판정
    // ==========================================

    private Rect WorldRect(
        RectTransform rt
    )
    {
        Vector3[] corners =
            new Vector3[4];

        rt.GetWorldCorners(
            corners
        );

        return Rect.MinMaxRect(
            corners[0].x,
            corners[0].y,
            corners[2].x,
            corners[2].y
        );
    }

    // ==========================================
    // Hint
    // ==========================================

    protected override void GiveHint()
    {
        if (statusText != null)
        {
            statusText.text =
                "힌트: 마우스를 멀리 끌수록 더 빠르게 이동합니다.";
        }
    }

    // ==========================================
    // Restart
    // ==========================================

    protected override void RestartGame()
    {
        currentCourse = 1;
        failCount = 0;

        SetupCourse(1);

        if (statusText != null)
        {
            statusText.text =
                "게임판을 상하좌우로 드래그하여 START를 이동하세요.";
        }
    }
}