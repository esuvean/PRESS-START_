using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Minigame12_Breakout : MinigameBase
{
    [System.Serializable]
    public class YellowBrickPosition
    {
        [Min(1)]
        public int row = 1;

        [Min(1)]
        public int column = 1;
    }

    [Header("Play Area")]
    public RectTransform playArea;
    public RectTransform paddle;
    public RectTransform ballSpawnPoint;
    public BreakoutBallUI ballPrefab;

    [Header("Brick Generation")]
    public RectTransform brickRowTemplate;
    public RectTransform brickParent;
    public int brickRowCount = 6;
    public float brickRowSpacing = 30f;
    public Color[] brickRowColors;

    [HideInInspector]
    public List<RectTransform> normalBricks =
        new List<RectTransform>();

    private bool bricksGenerated = false;

    [Header("Special Yellow Bricks")]
    public YellowBrickPosition[] yellowBrickPositions =
    {
        new YellowBrickPosition { row = 1, column = 4 },
        new YellowBrickPosition { row = 2, column = 8 },
        new YellowBrickPosition { row = 4, column = 12 },
        new YellowBrickPosition { row = 5, column = 16 }
    };

    public Color specialYellowColor = Color.yellow;

    public int maxActiveBalls = 2;

    private readonly HashSet<RectTransform> specialYellowBricks =
        new HashSet<RectTransform>();

    [Header("START Button")]
    public RectTransform startButtonBrick;

    [Header("UI")]
    public TMP_Text missText;
    public TMP_Text timerText;
    public TMP_Text hintText;

    [Header("Settings")]
    public float ballSpeed = 700f;
    public int maxMissCount = 5;
    public float paddleBonusWidth = 80f;
    public float startHintRemainingTime = 15f;

    private readonly List<BreakoutBallUI> activeBalls =
        new List<BreakoutBallUI>();

    private float remainingTime;
    private int missCount;

    private Vector2 originalPaddleSize;
    private bool paddleSizeCaptured;

    private Image startButtonImage;
    private Color startButtonOriginalColor;
    private bool startButtonColorCaptured;

    private bool hintShown;

    public bool IsGameRunning()
    {
        return isGameActive;
    }

    public RectTransform GetPlayArea()
    {
        return playArea;
    }

    public RectTransform GetPaddle()
    {
        return paddle;
    }

    public override void StartMinigame()
    {
        StopAllCoroutines();
        ClearAllBalls();

        gameName = "벽돌깨기 실행 검사";
        instruction = "벽돌을 제거하고 START 버튼을 맞추세요.";

        base.StartMinigame();

        remainingTime = timeLimit;
        missCount = 0;
        hintShown = false;

        if (paddle != null)
        {
            if (!paddleSizeCaptured)
            {
                originalPaddleSize = paddle.sizeDelta;
                paddleSizeCaptured = true;
            }

            paddle.sizeDelta = originalPaddleSize;
        }

        if (startButtonBrick != null)
        {
            startButtonImage =
                startButtonBrick.GetComponent<Image>();

            if (startButtonImage != null &&
                !startButtonColorCaptured)
            {
                startButtonOriginalColor =
                    startButtonImage.color;

                startButtonColorCaptured = true;
            }
        }

        GenerateBrickRows();
        ResetAllBricks();
        UpdateUI();
        SpawnBall();
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateUI();
            Fail();
            return;
        }

        MovePaddleWithMouse();
        UpdateUI();

        if (!hintShown &&
            remainingTime <= startHintRemainingTime)
        {
            hintShown = true;
            ShowStartButtonHint();
        }
    }

    private void GenerateBrickRows()
    {
        if (bricksGenerated)
            return;

        if (brickRowTemplate == null)
        {
            Debug.LogError(
                "Brick Row Template이 연결되지 않았습니다."
            );
            return;
        }

        if (brickParent == null)
            brickParent = playArea;

        if (brickParent == null)
        {
            Debug.LogError(
                "Brick Parent와 Play Area를 확인해주세요."
            );
            return;
        }

        normalBricks.Clear();
        specialYellowBricks.Clear();

        brickRowTemplate.name = "BrickRow_01";

        AddRowBricksToList(brickRowTemplate, 0);

        for (int row = 1; row < brickRowCount; row++)
        {
            GameObject newRowObject = Instantiate(
                brickRowTemplate.gameObject,
                brickParent
            );

            newRowObject.name =
                $"BrickRow_{row + 1:00}";

            RectTransform newRow =
                newRowObject.GetComponent<RectTransform>();

            newRow.anchoredPosition =
                brickRowTemplate.anchoredPosition +
                Vector2.down * brickRowSpacing * row;

            AddRowBricksToList(newRow, row);
        }

        ApplySpecialYellowBricks();

        bricksGenerated = true;

        Debug.Log(
            $"벽돌 생성 완료: {normalBricks.Count}개"
        );
    }

    private void AddRowBricksToList(
        RectTransform row,
        int rowIndex)
    {
        if (row == null)
            return;

        Color rowColor = Color.white;

        if (brickRowColors != null &&
            brickRowColors.Length > 0)
        {
            int colorIndex =
                rowIndex % brickRowColors.Length;

            rowColor = brickRowColors[colorIndex];
        }

        for (int i = 0; i < row.childCount; i++)
        {
            Transform child = row.GetChild(i);

            RectTransform brick =
                child.GetComponent<RectTransform>();

            if (brick == null)
                continue;

            normalBricks.Add(brick);

            brick.name =
                $"Normal_Block_R{rowIndex + 1}_C{i + 1}";

            Image image = brick.GetComponent<Image>();

            if (image != null)
            {
                image.color = rowColor;
                image.raycastTarget = false;
            }
        }
    }

    private void ApplySpecialYellowBricks()
    {
        if (brickRowTemplate == null)
            return;

        int columnsPerRow = brickRowTemplate.childCount;

        if (columnsPerRow <= 0)
            return;

        if (yellowBrickPositions == null)
            return;

        foreach (YellowBrickPosition position
                 in yellowBrickPositions)
        {
            if (position == null)
                continue;

            if (position.row < 1 ||
                position.row > brickRowCount ||
                position.column < 1 ||
                position.column > columnsPerRow)
            {
                Debug.LogWarning(
                    $"노란색 블록 위치 오류: " +
                    $"Row {position.row}, Column {position.column}"
                );

                continue;
            }

            int index =
                (position.row - 1) * columnsPerRow +
                (position.column - 1);

            if (index < 0 || index >= normalBricks.Count)
                continue;

            RectTransform brick = normalBricks[index];

            if (brick == null)
                continue;

            Image image = brick.GetComponent<Image>();

            if (image == null)
                continue;

            image.color = specialYellowColor;
            specialYellowBricks.Add(brick);
        }
    }

    private void MovePaddleWithMouse()
    {
        if (playArea == null || paddle == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();

        Camera uiCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                playArea,
                Input.mousePosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float halfWidth = paddle.rect.width * 0.5f;

        float minX = playArea.rect.xMin + halfWidth;
        float maxX = playArea.rect.xMax - halfWidth;

        Vector2 position = paddle.anchoredPosition;

        position.x = Mathf.Clamp(
            localPoint.x,
            minX,
            maxX
        );

        paddle.anchoredPosition = position;
    }

    private void SpawnBall()
    {
        if (!isGameActive)
            return;

        if (ballPrefab == null ||
            ballSpawnPoint == null ||
            playArea == null)
        {
            Debug.LogError(
                "Ball Prefab, Ball Spawn Point, Play Area를 확인해주세요."
            );

            return;
        }

        Vector2 direction = new Vector2(
            Random.Range(-0.45f, 0.45f),
            1f
        ).normalized;

        SpawnBallAt(
            ballSpawnPoint.anchoredPosition,
            direction
        );
    }

    private BreakoutBallUI SpawnBallAt(
        Vector2 position,
        Vector2 direction)
    {
        if (!isGameActive ||
            ballPrefab == null ||
            playArea == null)
        {
            return null;
        }

        BreakoutBallUI newBall =
            Instantiate(ballPrefab, playArea);

        RectTransform ballRect =
            newBall.GetComponent<RectTransform>();

        if (ballRect == null)
        {
            Destroy(newBall.gameObject);
            return null;
        }

        ballRect.anchoredPosition = position;

        newBall.Launch(
            this,
            direction,
            ballSpeed
        );

        activeBalls.Add(newBall);

        return newBall;
    }

    public void OnBallLost(BreakoutBallUI ball)
    {
        if (ball != null)
        {
            activeBalls.Remove(ball);
            Destroy(ball.gameObject);
        }

        if (!isGameActive)
            return;

        RemoveMissingBallsFromList();

        missCount++;

        if (missCount >= 2 && paddle != null)
        {
            paddle.sizeDelta = new Vector2(
                originalPaddleSize.x + paddleBonusWidth,
                originalPaddleSize.y
            );
        }

        UpdateUI();

        if (missCount >= maxMissCount)
        {
            ClearAllBalls();
            Fail();
            return;
        }

        if (activeBalls.Count == 0)
        {
            ResetAllBricks();
            SpawnBall();

            if (hintText != null)
            {
                hintText.text =
                    "모든 공을 놓쳤습니다. 블록과 공이 초기화되었습니다.";
            }
        }
    }

    public void OnHitNormalBrick(
        RectTransform brick,
        BreakoutBallUI hittingBall)
    {
        if (!isGameActive || brick == null)
            return;

        bool isYellowBrick =
            specialYellowBricks.Contains(brick);

        brick.gameObject.SetActive(false);

        if (isYellowBrick)
        {
            TrySpawnExtraBall(hittingBall);
        }
    }

    private void TrySpawnExtraBall(
        BreakoutBallUI hittingBall)
    {
        RemoveMissingBallsFromList();

        if (hittingBall == null ||
            activeBalls.Count >= maxActiveBalls)
        {
            return;
        }

        Vector2 sourcePosition =
            hittingBall.GetAnchoredPosition();

        Vector2 sourceDirection =
            hittingBall.GetDirection();

        Vector2 sourceSize =
            hittingBall.GetBallSize();

        float side =
            Random.value < 0.5f ? -1f : 1f;

        Vector2 extraPosition =
            sourcePosition +
            new Vector2(
                side * sourceSize.x * 0.8f,
                sourceSize.y * 0.5f
            );

        float halfWidth = sourceSize.x * 0.5f;

        extraPosition.x = Mathf.Clamp(
            extraPosition.x,
            playArea.rect.xMin + halfWidth,
            playArea.rect.xMax - halfWidth
        );

        Vector2 extraDirection = new Vector2(
            -sourceDirection.x +
            Random.Range(-0.3f, 0.3f),
            Mathf.Max(0.45f, Mathf.Abs(sourceDirection.y))
        ).normalized;

        if (SpawnBallAt(extraPosition, extraDirection) != null)
        {
            Debug.Log("노란색 블록 명중: 추가 공 생성!");
        }
    }

    private void RemoveMissingBallsFromList()
    {
        activeBalls.RemoveAll(ball => ball == null);
    }

    private void ClearAllBalls()
    {
        foreach (BreakoutBallUI ball in activeBalls)
        {
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }

        activeBalls.Clear();
    }

    public void OnHitStartButton()
    {
        if (!isGameActive)
            return;

        Debug.Log("START 버튼 명중! 미니게임 성공!");

        ClearAllBalls();
        Success();
    }

    private void ResetAllBricks()
    {
        foreach (RectTransform brick in normalBricks)
        {
            if (brick != null)
            {
                brick.gameObject.SetActive(true);
            }
        }

        if (startButtonBrick != null)
        {
            startButtonBrick.gameObject.SetActive(true);

            if (startButtonImage != null &&
                startButtonColorCaptured)
            {
                startButtonImage.color =
                    startButtonOriginalColor;
            }
        }
    }

    private void ShowStartButtonHint()
    {
        if (hintText != null)
        {
            hintText.text =
                "HINT : START 버튼을 맞추세요.";
        }

        if (startButtonImage != null)
        {
            startButtonImage.color = Color.green;
        }
    }

    private void UpdateUI()
    {
        if (missText != null)
        {
            missText.text = "MISS " + missCount;
        }

        if (timerText != null)
        {
            int minute =
                Mathf.FloorToInt(remainingTime / 60f);

            int second =
                Mathf.FloorToInt(remainingTime % 60f);

            timerText.text =
                $"TIME {minute:00}:{second:00}";
        }
    }

    protected override void GiveHint()
    {
        ShowStartButtonHint();
    }

    protected override void RestartGame()
    {
        isGameActive = false;

        ClearAllBalls();

        StartMinigame();
    }
}