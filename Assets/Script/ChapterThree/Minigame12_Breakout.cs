using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Minigame12_Breakout : MinigameBase
{
    [Header("Play Area")]
    public RectTransform playArea;

    [Header("Paddle / Ball")]
    public RectTransform paddle;
    public RectTransform ball;

    [Header("START Blocks")]
    public RectTransform[] startBlocks;

    [Header("Normal Blocks")]
    public RectTransform[] normalBlocks;

    [Header("END Blocks")]
    public RectTransform[] endBlocks;

    [Header("UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI missText;

    [Header("Ball Settings")]
    public float ballSpeed = 430f;
    public float ballRespawnDelay = 0.7f;

    [Header("Paddle Settings")]
    public float paddleMoveSpeed = 1200f;

    [Header("END Block Effect")]
    [Range(0.1f, 1f)]
    public float endSlowMultiplier = 0.45f;
    public float endSlowDuration = 3f;

    [Header("Miss Assist")]
    public float paddleGrowMultiplier = 1.4f;

    [Range(0.1f, 1f)]
    public float ballSlowMultiplier = 0.75f;

    [Header("70 Second Hint")]
    public float startOutlineTime = 70f;
    public Color startOutlineColor = Color.yellow;

    [Header("Message")]
    public float messageDuration = 2f;

    private Vector2 ballDirection;

    private Vector2 initialBallPosition;
    private Vector2 initialPaddleSize;

    private float paddleY;

    private float currentBallSpeed;
    private float currentPaddleSpeed;

    private float elapsedTime = 0f;

    private int missCount = 0;
    private int remainingStartBlocks = 0;

    private bool paddleGrowApplied = false;
    private bool ballSlowApplied = false;
    private bool startOutlineShown = false;

    private bool ballRespawning = false;

    private Coroutine messageCoroutine;
    private Coroutine endSlowCoroutine;

    private RectTransform lastHitBlock;
    private float lastHitTime;

    private bool initialValuesCached = false;

    private Canvas parentCanvas;


    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }


    public override void StartMinigame()
    {
        gameName = "실행 방해 요소 제거 검사";

        instruction =
            "공을 튕겨 START 글자 블록을 모두 제거하세요.";

        base.StartMinigame();

        StopAllCoroutines();

        CacheInitialValues();

        elapsedTime = 0f;
        missCount = 0;

        paddleGrowApplied = false;
        ballSlowApplied = false;
        startOutlineShown = false;

        ballRespawning = false;

        currentBallSpeed = ballSpeed;
        currentPaddleSpeed = paddleMoveSpeed;

        paddle.sizeDelta = initialPaddleSize;

        ResetBlocks();

        SetupStartBlockOutlines(false);

        remainingStartBlocks = CountStartBlocks();

        if (hintText != null)
        {
            hintText.text = instruction;
        }

        UpdateMissUI();
        UpdateTimerUI();

        SpawnBall();
    }


    private void CacheInitialValues()
    {
        if (initialValuesCached)
            return;

        if (ball != null)
        {
            initialBallPosition =
                ball.anchoredPosition;
        }

        if (paddle != null)
        {
            initialPaddleSize =
                paddle.sizeDelta;

            paddleY =
                paddle.anchoredPosition.y;
        }

        parentCanvas =
            GetComponentInParent<Canvas>();

        initialValuesCached = true;
    }


    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        elapsedTime += Time.deltaTime;

        MovePaddle();

        if (!ballRespawning &&
            ball != null &&
            ball.gameObject.activeSelf)
        {
            MoveBall();
        }

        Check70SecondHint();

        UpdateTimerUI();
    }


    // =====================================================
    // 패들
    // =====================================================

    private void MovePaddle()
    {
        if (paddle == null ||
            playArea == null)
        {
            return;
        }

        Camera uiCamera = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                playArea,
                Input.mousePosition,
                uiCamera,
                out Vector2 mousePosition))
        {
            return;
        }

        float halfWidth =
            paddle.rect.width * 0.5f;

        float targetX =
            Mathf.Clamp(
                mousePosition.x,
                playArea.rect.xMin + halfWidth,
                playArea.rect.xMax - halfWidth
            );

        Vector2 targetPosition =
            new Vector2(
                targetX,
                paddleY
            );

        paddle.anchoredPosition =
            Vector2.MoveTowards(
                paddle.anchoredPosition,
                targetPosition,
                currentPaddleSpeed *
                Time.deltaTime
            );
    }


    // =====================================================
    // 공
    // =====================================================

    private void SpawnBall()
    {
        if (ball == null)
            return;

        ball.gameObject.SetActive(true);

        ball.anchoredPosition =
            initialBallPosition;

        float randomX =
            Random.Range(-0.45f, 0.45f);

        ballDirection =
            new Vector2(
                randomX,
                1f
            ).normalized;

        ballRespawning = false;
    }


    private void MoveBall()
    {
        ball.anchoredPosition +=
            ballDirection *
            currentBallSpeed *
            Time.deltaTime;

        CheckWallCollision();

        CheckPaddleCollision();

        CheckBlockCollisions();
    }


    // =====================================================
    // 벽 충돌
    // =====================================================

    private void CheckWallCollision()
    {
        float ballHalfWidth =
            ball.rect.width * 0.5f;

        float ballHalfHeight =
            ball.rect.height * 0.5f;

        Vector2 pos =
            ball.anchoredPosition;

        // 왼쪽
        if (pos.x - ballHalfWidth <=
            playArea.rect.xMin)
        {
            pos.x =
                playArea.rect.xMin +
                ballHalfWidth;

            ballDirection.x =
                Mathf.Abs(ballDirection.x);
        }

        // 오른쪽
        if (pos.x + ballHalfWidth >=
            playArea.rect.xMax)
        {
            pos.x =
                playArea.rect.xMax -
                ballHalfWidth;

            ballDirection.x =
                -Mathf.Abs(ballDirection.x);
        }

        // 위
        if (pos.y + ballHalfHeight >=
            playArea.rect.yMax)
        {
            pos.y =
                playArea.rect.yMax -
                ballHalfHeight;

            ballDirection.y =
                -Mathf.Abs(ballDirection.y);
        }

        // 아래로 떨어짐
        if (pos.y - ballHalfHeight <=
            playArea.rect.yMin)
        {
            OnBallMiss();
            return;
        }

        ball.anchoredPosition = pos;
    }


    // =====================================================
    // 패들 충돌
    // =====================================================

    private void CheckPaddleCollision()
    {
        if (paddle == null)
            return;

        // 아래로 내려가는 공만 패들과 충돌
        if (ballDirection.y >= 0f)
            return;

        if (!RectsOverlap(ball, paddle))
            return;

        float contact =
            (ball.anchoredPosition.x -
             paddle.anchoredPosition.x)
            /
            (paddle.rect.width * 0.5f);

        contact =
            Mathf.Clamp(
                contact,
                -1f,
                1f
            );

        ballDirection =
            new Vector2(
                contact * 0.75f,
                1f
            ).normalized;

        Vector2 pos =
            ball.anchoredPosition;

        pos.y =
            paddle.anchoredPosition.y +
            paddle.rect.height * 0.5f +
            ball.rect.height * 0.5f +
            2f;

        ball.anchoredPosition = pos;
    }


    // =====================================================
    // 블록 충돌
    // =====================================================

    private void CheckBlockCollisions()
    {
        // START
        if (CheckBlockArray(
            startBlocks,
            BlockType.Start))
        {
            return;
        }

        // 일반
        if (CheckBlockArray(
            normalBlocks,
            BlockType.Normal))
        {
            return;
        }

        // END
        CheckBlockArray(
            endBlocks,
            BlockType.End
        );
    }


    private enum BlockType
    {
        Start,
        Normal,
        End
    }


    private bool CheckBlockArray(
        RectTransform[] blocks,
        BlockType blockType)
    {
        if (blocks == null)
            return false;

        foreach (RectTransform block in blocks)
        {
            if (block == null ||
                !block.gameObject.activeSelf)
            {
                continue;
            }

            if (!RectsOverlap(ball, block))
                continue;

            // 같은 블록에 너무 연속으로 충돌하는 것 방지
            if (lastHitBlock == block &&
                Time.time - lastHitTime < 0.06f)
            {
                continue;
            }

            lastHitBlock = block;
            lastHitTime = Time.time;

            BounceFromBlock(block);

            if (blockType ==
                BlockType.Start)
            {
                HitStartBlock(block);
            }
            else if (blockType ==
                     BlockType.End)
            {
                HitEndBlock();
            }

            return true;
        }

        return false;
    }


    private void BounceFromBlock(
        RectTransform block)
    {
        Rect ballRect =
            GetWorldRect(ball);

        Rect blockRect =
            GetWorldRect(block);

        float overlapLeft =
            ballRect.xMax -
            blockRect.xMin;

        float overlapRight =
            blockRect.xMax -
            ballRect.xMin;

        float overlapBottom =
            ballRect.yMax -
            blockRect.yMin;

        float overlapTop =
            blockRect.yMax -
            ballRect.yMin;

        float overlapX =
            Mathf.Min(
                overlapLeft,
                overlapRight
            );

        float overlapY =
            Mathf.Min(
                overlapBottom,
                overlapTop
            );

        if (overlapX < overlapY)
        {
            ballDirection.x *= -1f;
        }
        else
        {
            ballDirection.y *= -1f;
        }

        // 블록 내부에 공이 붙는 현상 방지
        ball.anchoredPosition +=
            ballDirection.normalized * 4f;
    }


    private void HitStartBlock(
        RectTransform block)
    {
        block.gameObject.SetActive(false);

        remainingStartBlocks--;

        if (remainingStartBlocks <= 0)
        {
            CompleteGame();
        }
    }


    private void HitEndBlock()
    {
        if (endSlowCoroutine != null)
        {
            StopCoroutine(
                endSlowCoroutine
            );
        }

        endSlowCoroutine =
            StartCoroutine(
                EndSlowRoutine()
            );
    }


    private IEnumerator EndSlowRoutine()
    {
        currentPaddleSpeed =
            paddleMoveSpeed *
            endSlowMultiplier;

        ShowMessage(
            "END 접촉 : 패들 이동 속도가 감소했습니다."
        );

        yield return new WaitForSeconds(
            endSlowDuration
        );

        currentPaddleSpeed =
            paddleMoveSpeed;

        endSlowCoroutine = null;
    }


    // =====================================================
    // 공 놓침
    // =====================================================

    private void OnBallMiss()
    {
        if (ballRespawning)
            return;

        ballRespawning = true;

        missCount++;

        UpdateMissUI();

        // 3회 놓침
        if (missCount >= 3 &&
            !paddleGrowApplied)
        {
            paddleGrowApplied = true;

            Vector2 newSize =
                paddle.sizeDelta;

            newSize.x =
                initialPaddleSize.x *
                paddleGrowMultiplier;

            paddle.sizeDelta = newSize;

            ShowMessage(
                "[ HINT ] 패들의 크기가 증가했습니다."
            );
        }

        // 5회 놓침
        if (missCount >= 5 &&
            !ballSlowApplied)
        {
            ballSlowApplied = true;

            currentBallSpeed *=
                ballSlowMultiplier;

            ShowMessage(
                "[ HINT ] 공의 속도가 감소했습니다."
            );
        }

        ball.gameObject.SetActive(false);

        StartCoroutine(
            RespawnBallRoutine()
        );
    }


    private IEnumerator RespawnBallRoutine()
    {
        yield return new WaitForSeconds(
            ballRespawnDelay
        );

        if (isGameActive)
        {
            SpawnBall();
        }
    }


    // =====================================================
    // 70초 힌트
    // =====================================================

    private void Check70SecondHint()
    {
        if (startOutlineShown)
            return;

        if (elapsedTime <
            startOutlineTime)
        {
            return;
        }

        startOutlineShown = true;

        SetupStartBlockOutlines(true);

        ShowMessage(
            "[ HINT ] START 글자 블록을 확인하세요."
        );
    }


    private void SetupStartBlockOutlines(
        bool show)
    {
        if (startBlocks == null)
            return;

        foreach (RectTransform block
                 in startBlocks)
        {
            if (block == null)
                continue;

            Outline outline =
                block.GetComponent<Outline>();

            if (outline == null)
            {
                outline =
                    block.gameObject
                    .AddComponent<Outline>();

                outline.effectColor =
                    startOutlineColor;

                outline.effectDistance =
                    new Vector2(4f, -4f);
            }

            outline.enabled = show;
        }
    }


    // =====================================================
    // 초기화
    // =====================================================

    private void ResetBlocks()
    {
        SetBlockArrayActive(
            startBlocks,
            true
        );

        SetBlockArrayActive(
            normalBlocks,
            true
        );

        SetBlockArrayActive(
            endBlocks,
            true
        );
    }


    private void SetBlockArrayActive(
        RectTransform[] blocks,
        bool active)
    {
        if (blocks == null)
            return;

        foreach (RectTransform block
                 in blocks)
        {
            if (block != null)
            {
                block.gameObject
                    .SetActive(active);
            }
        }
    }


    private int CountStartBlocks()
    {
        if (startBlocks == null)
            return 0;

        int count = 0;

        foreach (RectTransform block
                 in startBlocks)
        {
            if (block != null)
                count++;
        }

        return count;
    }


    // =====================================================
    // UI
    // =====================================================

    private void UpdateMissUI()
    {
        if (missText != null)
        {
            missText.text =
                $"MISS {missCount}";
        }
    }


    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        // 시간 초과로 실패하는 게임이 아니므로
        // 0초가 되어도 게임은 계속됨.
        float remaining =
            Mathf.Max(
                0f,
                timeLimit - elapsedTime
            );

        int minutes =
            Mathf.FloorToInt(
                remaining / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                remaining % 60f
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
            messageDuration
        );

        if (hintText != null &&
            isGameActive)
        {
            hintText.text =
                instruction;
        }

        messageCoroutine = null;
    }


    // =====================================================
    // Rect 충돌
    // =====================================================

    private bool RectsOverlap(
        RectTransform a,
        RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Rect rectA =
            GetWorldRect(a);

        Rect rectB =
            GetWorldRect(b);

        return rectA.Overlaps(rectB);
    }


    private Rect GetWorldRect(
        RectTransform rect)
    {
        Vector3[] corners =
            new Vector3[4];

        rect.GetWorldCorners(corners);

        return Rect.MinMaxRect(
            corners[0].x,
            corners[0].y,
            corners[2].x,
            corners[2].y
        );
    }


    // =====================================================
    // 완료
    // =====================================================

    private void CompleteGame()
    {
        if (!isGameActive)
            return;

        if (hintText != null)
        {
            hintText.text =
                "실행 방해 요소 제거 검사 완료";
        }

        Success();
    }


    // MinigameBase 기본 힌트는 사용하지 않음
    protected override void GiveHint()
    {
    }


    protected override void RestartGame()
    {
        StartMinigame();
    }
}
