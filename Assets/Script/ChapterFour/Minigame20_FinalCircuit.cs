using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame20_FinalCircuit : MinigameBase
{
    private const int GridSize = 9;
    private const int TotalTiles = 81;

    private const int Up = 1;
    private const int Right = 2;
    private const int Down = 4;
    private const int Left = 8;

    private class CircuitTile
    {
        public RectTransform rect;
        public Image background;
        public Image[] arms = new Image[4];
        public Image center;
        public Image pipe;
        public Button button;
        public int correctMask;
        public int rotation;
    }

    [Header("Circuit Board")]
    public RectTransform boardArea;
    public float cellSize = 65f;

    [Header("Art (현민)")]
    public Sprite tileSprite;
    [Tooltip("위-아래로 이어진 곧은 파이프")]
    public Sprite straightPipeSprite;
    [Tooltip("아래-오른쪽으로 꺾인 파이프")]
    public Sprite anglePipeSprite;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public TMP_Text timerText;
    public TMP_Text progressText;
    public TMP_Text statusText;
    public TMP_Text powerLabelText;

    [Header("Buttons")]
    public Button resetButton;
    public Button checkButton;
    public Button startButton;

    [Header("Final Screen")]
    public GameObject completionPanel;
    public TMP_Text finalSummaryText;

    [Header("Settings")]
    public float circuitTimeLimit = 150f;
    public float powerAnimationSpeed = 0.018f;

    [Header("Colors")]
    public Color tileColor =
        new Color(0.80f, 0.95f, 0.92f);

    public Color circuitColor =
        new Color(0.08f, 0.50f, 0.92f);

    public Color poweredColor =
        new Color(0.20f, 0.90f, 0.48f);

    public Color poweredTileColor =
        new Color(0.59f, 0.94f, 0.72f);

    public Color hintColor =
        new Color(1f, 0.81f, 0.28f);

    private CircuitTile[,] tiles =
        new CircuitTile[GridSize, GridSize];

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private readonly List<Vector2Int> solutionPath =
        new List<Vector2Int>();

    private float remainingTime;
    private float currentAttemptTimeLimit;

    private int failureCount;
    private int hintPreviewCount;

    private bool isProcessing;
    private bool powerConnected;
    private bool finalScreenVisible;

    private readonly int[] solutionOrder =
    {
        36, 27, 18, 9, 0, 1, 2, 3, 4,
        5, 6, 7, 8, 17, 16, 15, 14,
        13, 12, 11, 10, 19, 20, 21,
        22, 23, 24, 25, 26, 35, 34,
        33, 32, 31, 30, 29, 28, 37,
        38, 39, 40, 41, 42, 43, 52,
        51, 50, 49, 48, 47, 46, 45,
        54, 55, 56, 57, 58, 59, 60,
        61, 70, 69, 68, 67, 66, 65,
        64, 63, 72, 73, 74, 75, 76,
        77, 78, 79, 80, 71, 62, 53,
        44
    };

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        StopAllCoroutines();

        gameName = "최종 실행 회로 검사";
        instruction =
            "복잡하게 뒤섞인 회로 조각을 회전시켜 전원 장치부터 START 버튼까지 하나의 연결된 경로를 완성한다.";

        timeLimit = circuitTimeLimit;

        base.StartMinigame();

        if (boardArea == null)
        {
            Debug.LogError(
                "Board Area가 연결되지 않았습니다."
            );

            isGameActive = false;
            return;
        }

        failureCount = 0;

        currentAttemptTimeLimit = circuitTimeLimit;
        remainingTime = currentAttemptTimeLimit;

        hintPreviewCount = 0;

        isProcessing = false;
        powerConnected = false;
        finalScreenVisible = false;

        if (titleText != null)
        {
            titleText.text = gameName;
        }

        if (instructionText != null)
        {
            instructionText.text =
                "클릭하여\n블록을 회전합니다.";
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnReset);
            resetButton.onClick.AddListener(OnReset);

            resetButton.interactable = true;
        }

        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(OnCheck);
            checkButton.onClick.AddListener(OnCheck);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);

            startButton.interactable = false;
        }

        if (powerLabelText != null)
        {
            powerLabelText.text = "POWER ▶";
        }

        GenerateSolution();
        GenerateBoard();
        RandomizeCircuit();

        SetStatus("회로 블록을 회전해 연결하세요.");

        UpdateTimerText();
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive ||
            isProcessing ||
            powerConnected ||
            finalScreenVisible)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            UpdateTimerText();

            StartCoroutine(
                FailureRoutine("제한시간이 종료되었습니다.")
            );

            return;
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, remainingTime)
        );

        timerText.text =
            $"남은 시간: {seconds}초";
    }

    private void GenerateSolution()
    {
        solutionPath.Clear();

        for (int i = 0; i < solutionOrder.Length; i++)
        {
            int index = solutionOrder[i];

            int row = index / GridSize;
            int column = index % GridSize;

            solutionPath.Add(
                new Vector2Int(column, row)
            );
        }
    }

    private void GenerateBoard()
    {
        ClearGeneratedObjects();

        tiles = new CircuitTile[GridSize, GridSize];

        boardArea.sizeDelta = new Vector2(
            cellSize * GridSize,
            cellSize * GridSize
        );

        int[,] correctMasks =
            new int[GridSize, GridSize];

        for (int i = 0; i < solutionPath.Count; i++)
        {
            Vector2Int current = solutionPath[i];

            int mask = 0;

            if (i == 0)
            {
                mask |= Left;
            }
            else
            {
                mask |= GetDirection(
                    current,
                    solutionPath[i - 1]
                );
            }

            if (i == solutionPath.Count - 1)
            {
                mask |= Right;
            }
            else
            {
                mask |= GetDirection(
                    current,
                    solutionPath[i + 1]
                );
            }

            correctMasks[current.y, current.x] = mask;
        }

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                CreateTile(
                    row,
                    col,
                    correctMasks[row, col]
                );
            }
        }
    }

    private void CreateTile(
        int row,
        int col,
        int correctMask)
    {
        float x = (col - 4) * cellSize;
        float y = (4 - row) * cellSize;

        Image background = CreateImage(
            $"CircuitTile_{row}_{col}",
            boardArea,
            new Vector2(x, y),
            new Vector2(
                cellSize - 2f,
                cellSize - 2f
            ),
            tileColor,
            true,
            tileSprite
        );

        CircuitTile tile = new CircuitTile();

        tile.rect = background.rectTransform;
        tile.background = background;
        tile.correctMask = correctMask;

        if (UsePipeSprites())
        {
            // 파이프 이미지 한 장을 회전시켜서 연결 모양을 표현
            tile.pipe = CreateImage(
                "Pipe",
                tile.rect,
                Vector2.zero,
                new Vector2(
                    cellSize - 2f,
                    cellSize - 2f
                ),
                circuitColor,
                false
            );

            AddTileButton(tile, background, row, col);
            tiles[row, col] = tile;
            return;
        }

        float halfLength = cellSize * 0.26f;
        float armLength = cellSize * 0.53f;

        float armWidth =
            Mathf.Max(7f, cellSize * 0.13f);

        tile.arms[0] = CreateImage(
            "UpArm",
            tile.rect,
            new Vector2(0f, halfLength),
            new Vector2(armWidth, armLength),
            circuitColor,
            false
        );

        tile.arms[1] = CreateImage(
            "RightArm",
            tile.rect,
            new Vector2(halfLength, 0f),
            new Vector2(armLength, armWidth),
            circuitColor,
            false
        );

        tile.arms[2] = CreateImage(
            "DownArm",
            tile.rect,
            new Vector2(0f, -halfLength),
            new Vector2(armWidth, armLength),
            circuitColor,
            false
        );

        tile.arms[3] = CreateImage(
            "LeftArm",
            tile.rect,
            new Vector2(-halfLength, 0f),
            new Vector2(armLength, armWidth),
            circuitColor,
            false
        );

        tile.center = CreateImage(
            "CircuitCenter",
            tile.rect,
            Vector2.zero,
            new Vector2(
                armWidth + 3f,
                armWidth + 3f
            ),
            circuitColor,
            false
        );

        AddTileButton(tile, background, row, col);

        tiles[row, col] = tile;
    }

    private void AddTileButton(
        CircuitTile tile,
        Image background,
        int row,
        int col)
    {
        Button button =
            background.gameObject.AddComponent<Button>();

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        int capturedRow = row;
        int capturedCol = col;

        button.onClick.AddListener(
            () => RotateTile(capturedRow, capturedCol)
        );

        tile.button = button;
    }

    private bool UsePipeSprites()
    {
        return straightPipeSprite != null &&
               anglePipeSprite != null;
    }

    private void ApplyPipeVisual(
        CircuitTile tile,
        int mask,
        Color lineColor)
    {
        if (tile.pipe == null)
            return;

        float angle = 0f;

        if (mask == (Up | Down) || mask == (Left | Right))
        {
            tile.pipe.sprite = straightPipeSprite;
            angle = mask == (Left | Right) ? 90f : 0f;
        }
        else
        {
            // 기본 이미지는 아래-오른쪽 연결. 시계 방향으로 90도씩 돌려서 맞춤
            tile.pipe.sprite = anglePipeSprite;

            int baseMask = Right | Down;

            for (int k = 0; k < 4; k++)
            {
                if (baseMask == mask)
                {
                    angle = -90f * k;
                    break;
                }

                baseMask = ((baseMask << 1) | (baseMask >> 3)) & 15;
            }
        }

        tile.pipe.rectTransform.localRotation =
            Quaternion.Euler(0f, 0f, angle);

        tile.pipe.color = lineColor;
    }

    private void RandomizeCircuit()
    {
        foreach (CircuitTile tile in tiles)
        {
            if (tile == null)
                continue;

            tile.rotation = Random.Range(0, 4);
        }

        remainingTime = currentAttemptTimeLimit;

        powerConnected = false;
        isProcessing = false;
        hintPreviewCount = 0;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        SetBoardInteractable(true);

        if (checkButton != null)
        {
            checkButton.interactable = true;
        }

        RefreshCircuitVisuals();
        UpdateTimerText();
    }


    private void RotateTile(int row, int col)
    {
        if (!isGameActive ||
            isProcessing ||
            powerConnected ||
            finalScreenVisible)
        {
            return;
        }

        CircuitTile tile = tiles[row, col];

        tile.rotation = (tile.rotation + 1) % 4;

        RefreshCircuitVisuals();

        bool completed = TraceCircuit(
            out List<Vector2Int> path
        );

        if (completed)
        {
            SetStatus("회로 연결 성공! 전력을 공급합니다.");

            StartCoroutine(PowerSupplyRoutine(path));
        }
    }

    private int GetCurrentMask(CircuitTile tile)
    {
        int mask = tile.correctMask;

        for (int i = 0; i < tile.rotation; i++)
        {
            mask = ((mask << 1) | (mask >> 3)) & 15;
        }

        return mask;
    }

    private int GetDirection(
        Vector2Int from,
        Vector2Int to)
    {
        if (to.y < from.y)
            return Up;

        if (to.x > from.x)
            return Right;

        if (to.y > from.y)
            return Down;

        return Left;
    }

    private int Opposite(int direction)
    {
        if (direction == Up)
            return Down;

        if (direction == Right)
            return Left;

        if (direction == Down)
            return Up;

        return Right;
    }

    private Vector2Int DirectionOffset(int direction)
    {
        if (direction == Up)
            return new Vector2Int(0, -1);

        if (direction == Right)
            return new Vector2Int(1, 0);

        if (direction == Down)
            return new Vector2Int(0, 1);

        return new Vector2Int(-1, 0);
    }

    private bool TraceCircuit(
        out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        HashSet<int> visited =
            new HashSet<int>();

        int row = 4;
        int col = 0;
        int incomingDirection = Left;

        for (int step = 0; step < TotalTiles + 1; step++)
        {
            if (row < 0 ||
                row >= GridSize ||
                col < 0 ||
                col >= GridSize)
            {
                return false;
            }

            int index = row * GridSize + col;

            if (!visited.Add(index))
                return false;

            CircuitTile tile = tiles[row, col];

            int mask = GetCurrentMask(tile);

            if ((mask & incomingDirection) == 0)
                return false;

            int outgoingDirection =
                mask & ~incomingDirection;

            if (outgoingDirection != Up &&
                outgoingDirection != Right &&
                outgoingDirection != Down &&
                outgoingDirection != Left)
            {
                return false;
            }

            path.Add(new Vector2Int(col, row));

            Vector2Int offset =
                DirectionOffset(outgoingDirection);

            int nextRow = row + offset.y;
            int nextCol = col + offset.x;

            if (nextRow < 0 ||
                nextRow >= GridSize ||
                nextCol < 0 ||
                nextCol >= GridSize)
            {
                return (
                    row == 4 &&
                    col == 8 &&
                    outgoingDirection == Right &&
                    path.Count == TotalTiles
                );
            }

            CircuitTile nextTile =
                tiles[nextRow, nextCol];

            int nextMask = GetCurrentMask(nextTile);

            int requiredDirection =
                Opposite(outgoingDirection);

            if ((nextMask & requiredDirection) == 0)
                return false;

            row = nextRow;
            col = nextCol;
            incomingDirection = requiredDirection;
        }

        return false;
    }

    private void RefreshCircuitVisuals()
    {
        TraceCircuit(
            out List<Vector2Int> connectedPath
        );

        HashSet<int> connected =
            new HashSet<int>();

        foreach (Vector2Int position in connectedPath)
        {
            connected.Add(
                position.y * GridSize + position.x
            );
        }

        HashSet<int> hints =
            new HashSet<int>();

        for (
            int i = 0;
            i < Mathf.Min(
                hintPreviewCount,
                solutionPath.Count
            );
            i++
        )
        {
            Vector2Int position = solutionPath[i];

            hints.Add(
                position.y * GridSize + position.x
            );
        }

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                CircuitTile tile = tiles[row, col];

                int index = row * GridSize + col;

                bool isConnected =
                    connected.Contains(index);

                bool showHint =
                    hints.Contains(index);

                int mask = showHint
                    ? tile.correctMask
                    : GetCurrentMask(tile);

                Color lineColor = showHint
                    ? hintColor
                    : (
                        isConnected
                        ? poweredColor
                        : circuitColor
                    );

                tile.background.color = showHint
                    ? new Color(0.95f, 0.88f, 0.58f)
                    : (
                        isConnected
                        ? poweredTileColor
                        : tileColor
                    );

                int[] directions =
                {
                    Up,
                    Right,
                    Down,
                    Left
                };

                if (tile.pipe != null)
                {
                    ApplyPipeVisual(tile, mask, lineColor);
                    continue;
                }

                for (int d = 0; d < 4; d++)
                {
                    bool enabled =
                        (mask & directions[d]) != 0;

                    tile.arms[d].gameObject.SetActive(enabled);

                    tile.arms[d].color = lineColor;
                }

                tile.center.color = lineColor;
            }
        }

        if (progressText != null)
        {
            progressText.text =
                $"연결된 블록: {connectedPath.Count}/81";
        }
    }

    private void SetBoardInteractable(bool enabled)
    {
        foreach (CircuitTile tile in tiles)
        {
            if (tile != null &&
                tile.button != null)
            {
                tile.button.interactable = enabled;
            }
        }
    }

    private void OnCheck()
    {
        if (!isGameActive ||
            isProcessing ||
            powerConnected ||
            finalScreenVisible)
        {
            return;
        }

        bool completed = TraceCircuit(
            out List<Vector2Int> path
        );

        if (completed)
        {
            StartCoroutine(
                PowerSupplyRoutine(path)
            );
        }
        else
        {
            StartCoroutine(
                FailureRoutine(
                    "전력 공급 실패 ? 연결 경로를 확인하세요."
                )
            );
        }
    }

    private IEnumerator PowerSupplyRoutine(
        List<Vector2Int> path)
    {
        isProcessing = true;

        SetBoardInteractable(false);

        if (checkButton != null)
        {
            checkButton.interactable = false;
        }

        if (resetButton != null)
        {
            resetButton.interactable = false;
        }

        SetStatus(
            "전체 회로 검사 완료. 전력 공급 중..."
        );

        // 회로가 다 이어지면 공기 주입되는 '쉬이익' 소리
        GameSfx.Play("air_pump", 0.7f, 0f);

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            CircuitTile tile =
                tiles[position.y, position.x];

            tile.background.color = poweredTileColor;

            if (tile.pipe != null)
            {
                tile.pipe.color = poweredColor;
            }

            for (int d = 0; d < 4; d++)
            {
                if (tile.arms[d] != null)
                    tile.arms[d].color = poweredColor;
            }

            if (tile.center != null)
                tile.center.color = poweredColor;

            if (progressText != null)
            {
                progressText.text =
                    $"전력 공급: {i + 1}/81";
            }

            yield return new WaitForSeconds(
                powerAnimationSpeed
            );
        }

        powerConnected = true;

        SetStatus(
            "POWER CONNECTED ? 최종 회로 연결 완료"
        );

        if (powerLabelText != null)
        {
            powerLabelText.text = "POWER CONNECTED";
        }

        yield return new WaitForSeconds(1.5f);

        if (startButton != null)
        {
            startButton.interactable = true;
        }

        SetStatus(
            "START 버튼을 클릭해 최종 검사를 완료하세요."
        );

        isProcessing = false;
    }

    private IEnumerator FailureRoutine(string message)
    {
        if (isProcessing)
            yield break;

        isProcessing = true;

        failureCount++;

        SetBoardInteractable(false);

        if (checkButton != null)
        {
            checkButton.interactable = false;
        }

        SetStatus(message);

        yield return new WaitForSeconds(1.5f);

        if (failureCount == 5)
        {
            currentAttemptTimeLimit = Mathf.Max(
                currentAttemptTimeLimit,
                180f
            );

            timeLimit = currentAttemptTimeLimit;
        }

        RandomizeCircuit();

        if (resetButton != null)
        {
            resetButton.interactable = true;
        }

        if (failureCount >= 7)
        {
            SetStatus(
                "7회 실패: 전체 정답 경로를 3초 동안 표시합니다."
            );

            StartCoroutine(
                ShowHintPreview(81, 3f)
            );
        }
        else if (failureCount >= 5)
        {
            SetStatus(
                "5회 실패: 제한시간이 180초로 증가했습니다.\n" +
                "전체 정답 회로를 10초 동안 확인하세요."
            );

            StartCoroutine(
                ShowHintPreview(81, 10f)
            );
        }
        else if (failureCount >= 3)
        {
            SetStatus(
                "3회 실패: 일부 정답 회로 방향을 표시합니다."
            );

            StartCoroutine(
                ShowHintPreview(6, 3f)
            );
        }
        else
        {
            SetStatus(
                $"회로 초기화 완료. 다시 도전하세요. 실패 {failureCount}회"
            );
        }

        UpdateTimerText();
    }

    private IEnumerator ShowHintPreview(
        int count,
        float duration)
    {
        hintPreviewCount = count;

        RefreshCircuitVisuals();

        yield return new WaitForSeconds(duration);

        hintPreviewCount = 0;

        if (!finalScreenVisible)
        {
            RefreshCircuitVisuals();

            SetStatus(
                "힌트 종료. 회로를 계속 연결하세요."
            );
        }
    }

    private void OnReset()
    {
        if (!isGameActive ||
            isProcessing ||
            powerConnected)
        {
            return;
        }

        StopAllCoroutines();

        RandomizeCircuit();

        SetStatus("회로를 다시 섞었습니다.");
    }

    private void OnStartClicked()
    {
        if (!isGameActive ||
            !powerConnected ||
            finalScreenVisible ||
            isProcessing)
        {
            return;
        }

        finalScreenVisible = true;
        isProcessing = true;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        // 완료 패널은 표시하지 않고 다음 미니게임으로 진행합니다.
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        Debug.Log("미니게임 20 START 클릭 - 다음 단계로 진행");
        Success();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    protected override void GiveHint()
    {
        if (statusText != null &&
            !isProcessing &&
            !powerConnected)
        {
            statusText.text =
                "힌트: POWER에서 연결되는 회로부터 확인하세요.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void ClearGeneratedObjects()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        generatedObjects.Clear();
    }

    private Image CreateImage(
        string objectName,
        RectTransform parent,
        Vector2 position,
        Vector2 size,
        Color color,
        bool raycast,
        Sprite sprite = null)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        generatedObjects.Add(obj);

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image =
            obj.GetComponent<Image>();

        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;

        return image;
    }

    private void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnReset);
        }

        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(OnCheck);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }
    }
}