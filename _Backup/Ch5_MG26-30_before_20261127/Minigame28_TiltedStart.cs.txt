
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame28_TiltedStart : MinigameBase
{
    [Header("Main UI")]
    public RectTransform gameArea;
    public RectTransform tiltBoard;
    public RectTransform mazeWallsLayer;
    public RectTransform letterLayer;
    public RectTransform goalZone;

    public TMP_Text roundText;
    public TMP_Text collectedText;
    public TMP_Text stageStatusText;
    public TMP_Text failText;
    public TMP_Text resultText;

    public Button finalStartButton;
    public Image noiseOverlay;

    [Header("ART - Letter Sprites")]
    public Sprite letterS;
    public Sprite letterT;
    public Sprite letterA;
    public Sprite letterR;

    [Header("ART - Other Sprites")]
    public Sprite boardSprite;
    public Sprite wallSprite;
    public Sprite goalSprite;
    public Sprite noiseSprite;
    public Sprite finalStartSprite;

    [Header("ART - Options")]
    public bool hideFinalTextWhenSpriteExists = true;

    [Header("Tilt Movement")]
    public float acceleration = 1400f;
    public float maxMoveSpeed = 340f;
    public float friction = 4f;
    public float maxTiltAngle = 9f;
    public float tiltSmoothness = 7f;

    [Header("Letter Settings")]
    public Vector2 letterSize = new Vector2(46f, 46f);
    public float collisionPadding = 2f;
    public float collisionStep = 3f;

    [Header("Maze Settings")]
    public float wallThickness = 9f;
    public Color wallColor =
        new Color32(105, 174, 219, 255);

    [Header("Timing")]
    public float failDisplayTime = 1f;
    public float roundClearDelay = 0.8f;

    private const string Word = "START";

    // 라운드가 진행될수록 미로가 복잡해짐
    private readonly int[] gridColumns =
    {
        6, 7, 8, 10, 12
    };

    private readonly int[] gridRows =
    {
        3, 4, 4, 5, 6
    };

    // 추가 통로의 비율.
    // 값이 낮을수록 막힌 길이 많아짐.
    private readonly float[] extraPassageChance =
    {
        0.75f, 0.52f, 0.32f, 0.16f, 0.04f
    };

    private readonly string[] roundDescriptions =
    {
        "1판 - S | 기본 장애물과 벽",
        "2판 - T | 더 좁아진 길",
        "3판 - A | 증가한 장애물",
        "4판 - R | 복잡한 굴곡과 좁은 통로",
        "5판 - T | 최종 미로와 CRT 노이즈"
    };

    private enum Phase
    {
        Playing,
        Failed,
        RoundClear,
        StartReady,
        Completed
    }

    private Phase phase;

    private int currentRound;
    private int collectedCount;

    private int columns;
    private int rows;

    private float cellWidth;
    private float cellHeight;

    private bool[,,] passages;

    private Vector2 startPosition;
    private Vector2 velocity;

    private RectTransform movingLetter;

    private readonly List<GameObject> generatedWalls =
        new List<GameObject>();

    private readonly List<Rect> wallBounds =
        new List<Rect>();

    private readonly List<GameObject> generatedNoiseLines =
        new List<GameObject>();

    private Coroutine noiseRoutine;

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1)
    };

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    // =========================================
    // 게임 시작
    // =========================================

    public override void StartMinigame()
    {
        gameName = "기울어진 START";

        instruction =
            "게임판을 기울여 문자를 GOAL까지 이동시키십시오.";

        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();
        noiseRoutine = null;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG28: 필요한 UI가 없습니다. " +
                "Hierarchy 이름 또는 Inspector 연결을 확인하세요.",
                this
            );

            isGameActive = false;
            return;
        }

        ClearGeneratedObjects();

        currentRound = 0;
        collectedCount = 0;

        ApplyArtwork();
        SetupButtons();

        resultText.gameObject.SetActive(false);
        finalStartButton.gameObject.SetActive(false);

        roundText.gameObject.SetActive(true);
        collectedText.gameObject.SetActive(true);

        BeginRound();
    }

    // =========================================
    // UI 자동 검색
    // =========================================

    private Transform FindDeep(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        if (parent.name == objectName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found =
                FindDeep(child, objectName);

            if (found != null)
                return found;
        }

        return null;
    }

    private T FindUI<T>(string objectName)
        where T : Component
    {
        Transform found =
            FindDeep(transform, objectName);

        return found == null
            ? null
            : found.GetComponent<T>();
    }

    private bool FindReferences()
    {
        if (gameArea == null)
            gameArea = FindUI<RectTransform>("GameArea");

        if (tiltBoard == null)
            tiltBoard = FindUI<RectTransform>("TiltBoard");

        if (mazeWallsLayer == null)
            mazeWallsLayer =
                FindUI<RectTransform>("MazeWallsLayer");

        if (letterLayer == null)
            letterLayer =
                FindUI<RectTransform>("LetterLayer");

        if (goalZone == null)
            goalZone = FindUI<RectTransform>("GoalZone");

        if (roundText == null)
            roundText = FindUI<TMP_Text>("RoundText");

        if (collectedText == null)
            collectedText = FindUI<TMP_Text>("CollectedText");

        if (stageStatusText == null)
            stageStatusText =
                FindUI<TMP_Text>("StageStatusText");

        if (failText == null)
            failText = FindUI<TMP_Text>("FailText");

        if (resultText == null)
            resultText = FindUI<TMP_Text>("ResultText");

        if (finalStartButton == null)
            finalStartButton =
                FindUI<Button>("FinalStartButton");

        if (noiseOverlay == null)
            noiseOverlay = FindUI<Image>("NoiseOverlay");

        return
            gameArea != null &&
            tiltBoard != null &&
            mazeWallsLayer != null &&
            letterLayer != null &&
            goalZone != null &&
            roundText != null &&
            collectedText != null &&
            stageStatusText != null &&
            failText != null &&
            resultText != null &&
            finalStartButton != null &&
            noiseOverlay != null;
    }

    // =========================================
    // 아트 이미지 적용
    // =========================================

    private void ApplyArtwork()
    {
        ApplySprite(
            tiltBoard.GetComponent<Image>(),
            boardSprite,
            false
        );

        ApplySprite(
            goalZone.GetComponent<Image>(),
            goalSprite,
            true
        );

        ApplySprite(
            noiseOverlay,
            noiseSprite,
            false
        );

        ApplySprite(
            finalStartButton.GetComponent<Image>(),
            finalStartSprite,
            true
        );

        TMP_Text[] buttonLabels =
            finalStartButton.GetComponentsInChildren<
                TMP_Text>(true);

        foreach (TMP_Text label in buttonLabels)
        {
            bool hide =
                finalStartSprite != null &&
                hideFinalTextWhenSpriteExists;

            label.gameObject.SetActive(!hide);
            label.raycastTarget = false;
        }

        noiseOverlay.raycastTarget = false;
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

    // =========================================
    // 버튼 연결
    // =========================================

    private void SetupButtons()
    {
        finalStartButton.onClick.RemoveAllListeners();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
    }

    // =========================================
    // 라운드 시작
    // =========================================

    private void BeginRound()
    {
        phase = Phase.Playing;

        StopNoiseEffect();
        ClearGeneratedObjects();

        gameArea.gameObject.SetActive(true);
        failText.gameObject.SetActive(false);

        tiltBoard.localRotation = Quaternion.identity;

        velocity = Vector2.zero;

        Canvas.ForceUpdateCanvases();

        columns = gridColumns[currentRound];
        rows = gridRows[currentRound];

        cellWidth = tiltBoard.rect.width / columns;
        cellHeight = tiltBoard.rect.height / rows;

        GenerateMaze();

        startPosition = CellCenter(0, rows / 2);

        goalZone.anchoredPosition =
            CellCenter(columns - 1, rows / 2);

        CreateMovingLetter();

        UpdateProgressTexts();

        stageStatusText.text =
            roundDescriptions[currentRound] +
            "\nWASD 또는 방향키로 게임판을 기울이세요.";

        if (currentRound == 4)
        {
            CreateNoiseLines();

            noiseRoutine = StartCoroutine(
                CRTNoiseRoutine()
            );
        }
        else
        {
            noiseOverlay.gameObject.SetActive(false);
        }

        Debug.Log(
            "MG28: " + (currentRound + 1) +
            "판 시작 - " + Word[currentRound]
        );
    }

    // =========================================
    // 진행도 표시
    // =========================================

    private void UpdateProgressTexts()
    {
        roundText.text =
            "ROUND " + (currentRound + 1) +
            " / 5 : " + Word[currentRound];

        string result = "";

        for (int i = 0; i < Word.Length; i++)
        {
            result += i < collectedCount
                ? Word[i].ToString()
                : "_";

            if (i < Word.Length - 1)
                result += "  ";
        }

        collectedText.text =
            "COLLECTED : " + result;
    }

    // =========================================
    // 셀 중앙 좌표
    // =========================================

    private Vector2 CellCenter(int x, int y)
    {
        return new Vector2(
            -tiltBoard.rect.width * 0.5f +
            (x + 0.5f) * cellWidth,

            -tiltBoard.rect.height * 0.5f +
            (y + 0.5f) * cellHeight
        );
    }

    // =========================================
    // 미로 생성
    // 모든 칸이 연결되므로 클리어 가능한 길 존재
    // =========================================

    private void GenerateMaze()
    {
        passages = new bool[columns, rows, 4];

        bool[,] visited =
            new bool[columns, rows];

        Stack<Vector2Int> stack =
            new Stack<Vector2Int>();

        System.Random random =
            new System.Random(2800 + currentRound * 521);

        Vector2Int first =
            new Vector2Int(0, rows / 2);

        visited[first.x, first.y] = true;
        stack.Push(first);

        while (stack.Count > 0)
        {
            Vector2Int cell = stack.Peek();

            List<int> available =
                new List<int>();

            for (int dir = 0; dir < 4; dir++)
            {
                Vector2Int next =
                    cell + directions[dir];

                if (next.x >= 0 &&
                    next.x < columns &&
                    next.y >= 0 &&
                    next.y < rows &&
                    !visited[next.x, next.y])
                {
                    available.Add(dir);
                }
            }

            if (available.Count == 0)
            {
                stack.Pop();
                continue;
            }

            int chosen =
                available[random.Next(available.Count)];

            Vector2Int destination =
                cell + directions[chosen];

            passages[
                cell.x,
                cell.y,
                chosen
            ] = true;

            passages[
                destination.x,
                destination.y,
                (chosen + 2) % 4
            ] = true;

            visited[
                destination.x,
                destination.y
            ] = true;

            stack.Push(destination);
        }

        // 초반 라운드에서는 길을 더 많이 개방
        // 후반 라운드에서는 좁고 복잡하게 유지
        float chance =
            extraPassageChance[currentRound];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (x < columns - 1 &&
                    !passages[x, y, 0] &&
                    random.NextDouble() < chance)
                {
                    passages[x, y, 0] = true;
                    passages[x + 1, y, 2] = true;
                }

                if (y < rows - 1 &&
                    !passages[x, y, 1] &&
                    random.NextDouble() < chance)
                {
                    passages[x, y, 1] = true;
                    passages[x, y + 1, 3] = true;
                }
            }
        }

        float thickness =
            Mathf.Max(3f, wallThickness);

        // 막힌 셀 경계에만 벽 생성
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 center = CellCenter(x, y);

                // 오른쪽 방향 막힘
                if (x < columns - 1 &&
                    !passages[x, y, 0])
                {
                    CreateWall(
                        "Wall_V",
                        new Vector2(
                            center.x + cellWidth * 0.5f,
                            center.y
                        ),
                        new Vector2(
                            thickness,
                            cellHeight + thickness
                        )
                    );
                }

                // 위쪽 방향 막힘
                if (y < rows - 1 &&
                    !passages[x, y, 1])
                {
                    CreateWall(
                        "Wall_H",
                        new Vector2(
                            center.x,
                            center.y + cellHeight * 0.5f
                        ),
                        new Vector2(
                            cellWidth + thickness,
                            thickness
                        )
                    );
                }
            }
        }
    }

    // =========================================
    // 미로 벽 생성 및 충돌 영역 등록
    // =========================================

    private void CreateWall(
        string objectName,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(
            mazeWallsLayer,
            false
        );

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();

        if (wallSprite != null)
        {
            image.sprite = wallSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.color = wallColor;
        }

        image.raycastTarget = false;

        generatedWalls.Add(go);

        wallBounds.Add(
            new Rect(
                position.x - size.x * 0.5f,
                position.y - size.y * 0.5f,
                size.x,
                size.y
            )
        );
    }

    // =========================================
    // 현재 라운드 문자 생성
    // =========================================

    private void CreateMovingLetter()
    {
        char letter = Word[currentRound];

        GameObject go = new GameObject(
            "MovingLetter_" + letter,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(
            letterLayer,
            false
        );

        movingLetter =
            go.GetComponent<RectTransform>();

        movingLetter.anchorMin =
            new Vector2(0.5f, 0.5f);

        movingLetter.anchorMax =
            new Vector2(0.5f, 0.5f);

        movingLetter.pivot =
            new Vector2(0.5f, 0.5f);

        movingLetter.sizeDelta = letterSize;
        movingLetter.anchoredPosition = startPosition;

        Image image = go.GetComponent<Image>();

        Sprite artwork = GetLetterSprite(letter);

        if (artwork != null)
        {
            image.sprite = artwork;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        else
        {
            image.color =
                new Color32(22, 160, 133, 255);

            GameObject labelObject = new GameObject(
                "LetterText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            labelObject.transform.SetParent(
                go.transform,
                false
            );

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();

            label.text = letter.ToString();
            label.fontSize = 32f;
            label.fontStyle = FontStyles.Bold;
            label.alignment =
                TextAlignmentOptions.Center;

            label.color = Color.white;
            label.raycastTarget = false;
        }

        image.raycastTarget = false;
    }

    // =========================================
    // 메인 업데이트
    // =========================================

    private void LateUpdate()
    {
        if (!isGameActive ||
            phase != Phase.Playing ||
            movingLetter == null)
        {
            return;
        }

        float dt = Mathf.Min(
            Time.deltaTime,
            0.05f
        );

        if (dt <= 0f)
            return;

        Vector2 input = ReadTiltInput();

        UpdateBoardTilt(input, dt);
        UpdateLetterMovement(input, dt);
    }

    // =========================================
    // WASD / 방향키
    // =========================================

    private Vector2 ReadTiltInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
            input.x -= 1f;

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
            input.x += 1f;

        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
            input.y += 1f;

        if (Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow))
            input.y -= 1f;
#endif

#if ENABLE_INPUT_SYSTEM
        var keyboard =
            UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed ||
                keyboard.leftArrowKey.isPressed)
                input.x -= 1f;

            if (keyboard.dKey.isPressed ||
                keyboard.rightArrowKey.isPressed)
                input.x += 1f;

            if (keyboard.wKey.isPressed ||
                keyboard.upArrowKey.isPressed)
                input.y += 1f;

            if (keyboard.sKey.isPressed ||
                keyboard.downArrowKey.isPressed)
                input.y -= 1f;
        }
#endif

        // Active Input Handling = Both인 경우에도
        // 입력이 과하게 중복되지 않도록 제한
        return Vector2.ClampMagnitude(input, 1f);
    }

    // =========================================
    // 게임판 기울이기 연출
    // =========================================

    private void UpdateBoardTilt(
        Vector2 input,
        float dt)
    {
        Quaternion targetRotation =
            Quaternion.Euler(
                -input.y * maxTiltAngle * 0.45f,
                 input.x * maxTiltAngle * 0.45f,
                -input.x * maxTiltAngle * 0.75f
            );

        float blend =
            1f - Mathf.Exp(
                -tiltSmoothness * dt
            );

        tiltBoard.localRotation =
            Quaternion.Slerp(
                tiltBoard.localRotation,
                targetRotation,
                blend
            );
    }

    // =========================================
    // 문자 가속 / 마찰 / 이동
    // =========================================

    private void UpdateLetterMovement(
        Vector2 input,
        float dt)
    {
        velocity += input * acceleration * dt;

        velocity = Vector2.ClampMagnitude(
            velocity,
            maxMoveSpeed
        );

        velocity *= 1f /
            (1f + Mathf.Max(0f, friction) * dt);

        Vector2 movement = velocity * dt;

        // 빠르게 이동해도 벽을 통과하지 못하게
        // 짧은 거리로 나누어 충돌 검사
        float step = Mathf.Clamp(
            collisionStep,
            1f,
            8f
        );

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(
                movement.magnitude / step
            )
        );

        Vector2 portion = movement / steps;

        for (int i = 0; i < steps; i++)
        {
            if (phase != Phase.Playing)
                return;

            Vector2 current =
                movingLetter.anchoredPosition;

            // X축 이동 검사
            Vector2 nextX = new Vector2(
                current.x + portion.x,
                current.y
            );

            if (IsOutsideBoard(nextX))
            {
                TriggerRoundFailure();
                return;
            }

            if (!TouchesWall(nextX))
            {
                current = nextX;
            }
            else
            {
                velocity.x = 0f;
            }

            // Y축 이동 검사
            Vector2 nextY = new Vector2(
                current.x,
                current.y + portion.y
            );

            if (IsOutsideBoard(nextY))
            {
                TriggerRoundFailure();
                return;
            }

            if (!TouchesWall(nextY))
            {
                current = nextY;
            }
            else
            {
                velocity.y = 0f;
            }

            movingLetter.anchoredPosition = current;

            if (HasReachedGoal(current))
            {
                CompleteCurrentRound();
                return;
            }
        }
    }

    // =========================================
    // 문자 크기 반영
    // =========================================

    private float LetterRadius()
    {
        return Mathf.Min(
            letterSize.x,
            letterSize.y
        ) * 0.5f +
        Mathf.Max(0f, collisionPadding);
    }

    // =========================================
    // 게임판 밖 이탈 검사
    // =========================================

    private bool IsOutsideBoard(Vector2 position)
    {
        float radius = LetterRadius();

        float limitX =
            tiltBoard.rect.width * 0.5f;

        float limitY =
            tiltBoard.rect.height * 0.5f;

        return
            position.x - radius < -limitX ||
            position.x + radius > limitX ||
            position.y - radius < -limitY ||
            position.y + radius > limitY;
    }

    // =========================================
    // 문자와 미로 벽 충돌
    // 사각형 벽 대 원형 근사 충돌
    // =========================================

    private bool TouchesWall(Vector2 position)
    {
        float radius = LetterRadius();
        float radiusSquared = radius * radius;

        foreach (Rect wall in wallBounds)
        {
            float nearestX = Mathf.Clamp(
                position.x,
                wall.xMin,
                wall.xMax
            );

            float nearestY = Mathf.Clamp(
                position.y,
                wall.yMin,
                wall.yMax
            );

            float dx = position.x - nearestX;
            float dy = position.y - nearestY;

            if (dx * dx + dy * dy <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================
    // GOAL 도착 확인
    // =========================================

    private bool HasReachedGoal(Vector2 position)
    {
        Vector2 goal =
            goalZone.anchoredPosition;

        float allowedX = Mathf.Max(
            20f,
            goalZone.rect.width * 0.3f
        );

        float allowedY = Mathf.Max(
            20f,
            goalZone.rect.height * 0.3f
        );

        return
            Mathf.Abs(position.x - goal.x) <= allowedX &&
            Mathf.Abs(position.y - goal.y) <= allowedY;
    }

    // =========================================
    // 라운드 실패
    // =========================================

    private void TriggerRoundFailure()
    {
        if (phase != Phase.Playing)
            return;

        phase = Phase.Failed;
        velocity = Vector2.zero;

        StartCoroutine(
            RoundFailureRoutine()
        );
    }

    private IEnumerator RoundFailureRoutine()
    {
        failText.text =
            "문자가 떨어졌습니다!\n현재 라운드를 다시 시작합니다.";

        failText.gameObject.SetActive(true);

        stageStatusText.text =
            "실패! 이전에 수집한 문자는 유지됩니다.";

        Debug.Log(
            "MG28: " + (currentRound + 1) +
            "판 실패 - 현재 판 재시작"
        );

        yield return new WaitForSecondsRealtime(
            failDisplayTime
        );

        if (!isGameActive)
            yield break;

        // currentRound와 collectedCount는 유지
        BeginRound();
    }

    // =========================================
    // 현재 라운드 성공
    // =========================================

    private void CompleteCurrentRound()
    {
        if (phase != Phase.Playing)
            return;

        phase = Phase.RoundClear;
        velocity = Vector2.zero;

        collectedCount++;

        UpdateProgressTexts();

        stageStatusText.text =
            Word[currentRound] +
            " 획득 성공!";

        Debug.Log(
            "MG28: 문자 " +
            Word[currentRound] +
            " 획득 (" +
            collectedCount + "/5)"
        );

        StartCoroutine(
            RoundClearRoutine()
        );
    }

    private IEnumerator RoundClearRoutine()
    {
        yield return new WaitForSecondsRealtime(
            roundClearDelay
        );

        if (!isGameActive)
            yield break;

        if (collectedCount >= Word.Length)
        {
            ShowFinalResult();
        }
        else
        {
            currentRound++;
            BeginRound();
        }
    }

    // =========================================
    // CRT 노이즈용 가로줄 자동 생성
    // =========================================

    private void CreateNoiseLines()
    {
        ClearNoiseLines();

        RectTransform overlayRect =
            noiseOverlay.GetComponent<RectTransform>();

        for (int i = 0; i < 9; i++)
        {
            GameObject go = new GameObject(
                "CRT_Scanline_" + i,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            go.transform.SetParent(
                noiseOverlay.transform,
                false
            );

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.sizeDelta = new Vector2(
                overlayRect.rect.width,
                Random.Range(2f, 6f)
            );

            rect.anchoredPosition = new Vector2(
                0f,
                Random.Range(
                    -overlayRect.rect.height * 0.5f,
                     overlayRect.rect.height * 0.5f
                )
            );

            Image image = go.GetComponent<Image>();

            image.color = new Color(
                1f,
                1f,
                1f,
                Random.Range(0.15f, 0.4f)
            );

            image.raycastTarget = false;

            generatedNoiseLines.Add(go);
        }
    }

    // =========================================
    // 5판 CRT 노이즈 반복
    // =========================================

    private IEnumerator CRTNoiseRoutine()
    {
        noiseOverlay.gameObject.SetActive(false);

        while (isGameActive &&
               phase == Phase.Playing &&
               currentRound == 4)
        {
            yield return new WaitForSecondsRealtime(
                Random.Range(0.8f, 2.1f)
            );

            if (!isGameActive ||
                phase != Phase.Playing ||
                currentRound != 4)
            {
                break;
            }

            noiseOverlay.gameObject.SetActive(true);

            noiseOverlay.color = new Color(
                1f, 1f, 1f,
                noiseSprite != null ? 0.3f : 0.05f
            );

            foreach (GameObject line in generatedNoiseLines)
            {
                if (line == null)
                    continue;

                RectTransform rect =
                    line.GetComponent<RectTransform>();

                rect.anchoredPosition = new Vector2(
                    0f,
                    Random.Range(
                        -tiltBoard.rect.height * 0.5f,
                         tiltBoard.rect.height * 0.5f
                    )
                );
            }

            yield return new WaitForSecondsRealtime(
                Random.Range(0.07f, 0.17f)
            );

            noiseOverlay.gameObject.SetActive(false);
        }

        noiseOverlay.gameObject.SetActive(false);
        noiseRoutine = null;
    }

    private void StopNoiseEffect()
    {
        if (noiseRoutine != null)
        {
            StopCoroutine(noiseRoutine);
            noiseRoutine = null;
        }

        if (noiseOverlay != null)
        {
            noiseOverlay.gameObject.SetActive(false);
        }
    }

    private void ClearNoiseLines()
    {
        foreach (GameObject go in generatedNoiseLines)
        {
            if (go != null)
            {
                go.SetActive(false);
                Destroy(go);
            }
        }

        generatedNoiseLines.Clear();
    }

    // =========================================
    // 자동 생성 오브젝트 정리
    // =========================================

    private void ClearGeneratedObjects()
    {
        if (movingLetter != null)
        {
            movingLetter.gameObject.SetActive(false);
            Destroy(movingLetter.gameObject);

            movingLetter = null;
        }

        foreach (GameObject wall in generatedWalls)
        {
            if (wall != null)
            {
                wall.SetActive(false);
                Destroy(wall);
            }
        }

        generatedWalls.Clear();
        wallBounds.Clear();

        ClearNoiseLines();
    }

    // =========================================
    // 5글자 획득 후 최종 화면
    // =========================================

    private void ShowFinalResult()
    {
        phase = Phase.StartReady;

        StopNoiseEffect();

        gameArea.gameObject.SetActive(false);

        roundText.gameObject.SetActive(false);
        collectedText.gameObject.SetActive(false);

        resultText.text = "S - T - A - R - T";
        resultText.gameObject.SetActive(true);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();

        Debug.Log(
            "MG28: START 완성! 최종 버튼 활성화"
        );
    }

    // =========================================
    // 최종 클리어
    // =========================================

    private void OnFinalStartClicked()
    {
        if (!isGameActive ||
            phase != Phase.StartReady)
        {
            return;
        }

        phase = Phase.Completed;

        Debug.Log("MG28: 검사 완료");

        Success();
    }

    protected override void GiveHint()
    {
        if (phase == Phase.Playing)
        {
            stageStatusText.text =
                "힌트: WASD 또는 방향키로 기울이고, " +
                "벽을 따라 통로를 찾아보세요.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
