
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame30_SequenceBreakout : MinigameBase
{
    [Header("Main UI")]
    public RectTransform gameArea;
    public RectTransform brickLayer;
    public RectTransform ballLayer;
    public RectTransform paddle;

    public TMP_Text targetText;
    public TMP_Text progressText;
    public TMP_Text errorText;

    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button finalStartButton;

    [Header("ART - Correct Letter Sprites")]
    public Sprite letterS;
    public Sprite letterT;
    public Sprite letterA;
    public Sprite letterR;

    [Header("ART - Other Letter Sprites")]
    public Sprite letterE;
    public Sprite letterN;
    public Sprite letterD;
    public Sprite letterX;
    public Sprite letterI;
    public Sprite letterP;
    public Sprite letterL;
    public Sprite letterY;

    [Header("ART - Other Sprites")]
    public Sprite normalBrickSprite;
    public Sprite ballSprite;
    public Sprite paddleSprite;
    public Sprite backgroundSprite;
    public Sprite finalStartSprite;

    [Header("ART - Options")]
    public bool hideLetterTextWhenSpriteExists = true;
    public bool hideFinalTextWhenSpriteExists = true;

    [Header("Brick Settings")]
    public Vector2 targetBrickSize = new Vector2(150f, 54f);
    public Vector2 obstacleBrickSize = new Vector2(96f, 44f);
    public Vector2 normalBrickSize = new Vector2(145f, 38f);

    [Header("Ball Settings")]
    public float ballSpeed = 620f;
    public float ballSize = 28f;
    public float paddleBounceAngle = 65f;
    public float collisionStep = 4f;

    [Header("Paddle Settings")]
    public float paddleMoveSpeed = 1100f;

    [Header("Error Settings")]
    public float errorDisplayTime = 1.1f;

    private const string TargetWord = "START";

    // 방해 문자: END, EXIT, PLAY
    private const string ObstacleWord = "ENDEXITPLAY";

    private enum GamePhase
    {
        WaitingLaunch,
        Playing,
        SequenceError,
        StartReady,
        Completed
    }

    private GamePhase phase;

    private class BrickData
    {
        public GameObject obj;
        public RectTransform rect;

        public char character;
        public int targetIndex;

        public bool isTarget;
        public bool isNormal;
        public bool broken;
    }

    private readonly List<BrickData> bricks =
        new List<BrickData>();

    private RectTransform ballRect;
    private Vector2 ballVelocity;

    private int currentTargetIndex;
    private int ballSerial;

    private bool finished;

    private Texture2D defaultBallTexture;
    private Sprite defaultBallSprite;

    private readonly Vector3[] corners = new Vector3[4];

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    // ====================================
    // 게임 시작
    // ====================================

    public override void StartMinigame()
    {
        gameName = "순서대로 깨는 START";

        instruction =
            "S-T-A-R-T 순서대로 문자 벽돌을 깨십시오.";

        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        finished = false;
        currentTargetIndex = 0;
        ballSerial = 0;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG30: UI 연결이 누락되었습니다.",
                this
            );

            isGameActive = false;
            return;
        }

        ClearGeneratedObjects();

        gameArea.gameObject.SetActive(true);
        resultPanel.SetActive(false);
        errorText.gameObject.SetActive(false);

        Canvas.ForceUpdateCanvases();

        ApplyArtwork();
        SetupFinalButton();

        CreateAllBricks();

        UpdateHUD();
        RefreshTargetHighlights();

        SpawnBall();

        Debug.Log("MG30: 순서대로 깨는 START 시작");
    }

    // ====================================
    // 오브젝트 자동 검색
    // ====================================

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

    private GameObject FindObject(string objectName)
    {
        Transform found =
            FindDeep(transform, objectName);

        return found == null
            ? null
            : found.gameObject;
    }

    private bool FindReferences()
    {
        if (gameArea == null)
            gameArea = FindUI<RectTransform>("GameArea");

        if (brickLayer == null)
            brickLayer = FindUI<RectTransform>("BrickLayer");

        if (ballLayer == null)
            ballLayer = FindUI<RectTransform>("BallLayer");

        if (paddle == null)
            paddle = FindUI<RectTransform>("Paddle");

        if (targetText == null)
            targetText = FindUI<TMP_Text>("TargetText");

        if (progressText == null)
            progressText = FindUI<TMP_Text>("ProgressText");

        if (errorText == null)
            errorText = FindUI<TMP_Text>("ErrorText");

        if (resultPanel == null)
            resultPanel = FindObject("ResultPanel");

        if (resultText == null)
            resultText = FindUI<TMP_Text>("ResultText");

        if (finalStartButton == null)
            finalStartButton =
                FindUI<Button>("FinalStartButton");

        bool valid =
            gameArea != null &&
            brickLayer != null &&
            ballLayer != null &&
            paddle != null &&
            targetText != null &&
            progressText != null &&
            errorText != null &&
            resultPanel != null &&
            resultText != null &&
            finalStartButton != null;

        if (!valid)
        {
            Debug.LogError(
                "MG30: GameArea, BrickLayer, BallLayer, " +
                "Paddle, TargetText, ProgressText, ErrorText, " +
                "ResultPanel, ResultText, FinalStartButton를 " +
                "확인하세요.",
                this
            );
        }

        return valid;
    }

    // ====================================
    // 아트 이미지 적용
    // ====================================

    private void ApplyArtwork()
    {
        ApplySprite(
            gameArea.GetComponent<Image>(),
            backgroundSprite,
            false
        );

        ApplySprite(
            paddle.GetComponent<Image>(),
            paddleSprite,
            true
        );

        ApplySprite(
            finalStartButton.GetComponent<Image>(),
            finalStartSprite,
            true
        );

        foreach (TMP_Text label in
            finalStartButton.GetComponentsInChildren<
                TMP_Text>(true))
        {
            bool hide =
                finalStartSprite != null &&
                hideFinalTextWhenSpriteExists;

            label.gameObject.SetActive(!hide);
            label.raycastTarget = false;
        }

        Image paddleImage = paddle.GetComponent<Image>();

        if (paddleImage != null)
            paddleImage.raycastTarget = false;
    }

    private void ApplySprite(
        Image image,
        Sprite sprite,
        bool preserveAspect)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
    }

    private Sprite GetLetterSprite(char character)
    {
        switch (character)
        {
            case 'S': return letterS;
            case 'T': return letterT;
            case 'A': return letterA;
            case 'R': return letterR;

            case 'E': return letterE;
            case 'N': return letterN;
            case 'D': return letterD;
            case 'X': return letterX;
            case 'I': return letterI;
            case 'P': return letterP;
            case 'L': return letterL;
            case 'Y': return letterY;
        }

        return null;
    }

    // ====================================
    // 생성된 게임 오브젝트 정리
    // ====================================

    private void ClearGeneratedObjects()
    {
        ClearBall();

        foreach (BrickData brick in bricks)
        {
            if (brick.obj != null)
            {
                brick.obj.SetActive(false);
                Destroy(brick.obj);
            }
        }

        bricks.Clear();
    }

    // ====================================
    // 문자 벽돌 생성
    // ====================================

    private void CreateAllBricks()
    {
        float areaWidth = gameArea.rect.width;

        // 1. 일반 벽돌
        // 가장 아래에 배치
        for (int i = 0; i < 8; i++)
        {
            float x =
                (i - 3.5f) * (areaWidth * 0.11f);

            CreateBrick(
                "NormalBrick_" + i,
                ' ',
                -1,
                false,
                true,
                new Vector2(x, 65f),
                normalBrickSize
            );
        }

        // 2. START 목표 문자 5개
        // T는 첫 번째와 마지막을 별개로 관리
        float targetSpacing =
            Mathf.Min(260f, areaWidth * 0.18f);

        for (int i = 0; i < TargetWord.Length; i++)
        {
            char character = TargetWord[i];

            float x = (i - 2) * targetSpacing;

            CreateBrick(
                "Target_" + character + "_" + i,
                character,
                i,
                true,
                false,
                new Vector2(x, 155f),
                targetBrickSize
            );
        }

        // 3. 방해 문자 11개
        float obstacleSpacing =
            Mathf.Min(110f, areaWidth * 0.075f);

        float centerIndex =
            (ObstacleWord.Length - 1) * 0.5f;

        for (int i = 0; i < ObstacleWord.Length; i++)
        {
            char character = ObstacleWord[i];

            float x =
                (i - centerIndex) * obstacleSpacing;

            CreateBrick(
                "Obstacle_" + character + "_" + i,
                character,
                -1,
                false,
                false,
                new Vector2(x, 246f),
                obstacleBrickSize
            );
        }
    }

    private void CreateBrick(
        string objectName,
        char character,
        int targetIndex,
        bool isTarget,
        bool isNormal,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(brickLayer, false);

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

        Sprite sprite = isNormal
            ? normalBrickSprite
            : GetLetterSprite(character);

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
        else
        {
            image.color = isNormal
                ? new Color32(67, 109, 137, 255)
                : isTarget
                    ? new Color32(35, 125, 154, 255)
                    : new Color32(85, 96, 128, 255);
        }

        image.raycastTarget = false;

        bool showText =
            !isNormal &&
            (sprite == null ||
             !hideLetterTextWhenSpriteExists);

        if (showText)
        {
            CreateBrickLetter(
                go.transform,
                character
            );
        }

        BrickData data = new BrickData();

        data.obj = go;
        data.rect = rect;
        data.character = character;
        data.targetIndex = targetIndex;
        data.isTarget = isTarget;
        data.isNormal = isNormal;
        data.broken = false;

        bricks.Add(data);
    }

    // ====================================
    // 기본 문자 TMP 생성
    // ====================================

    private void CreateBrickLetter(
        Transform parent,
        char character)
    {
        GameObject go = new GameObject(
            "LetterText",
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

        text.text = character.ToString();
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;

        text.color = Color.white;

        text.alignment =
            TextAlignmentOptions.Center;

        text.raycastTarget = false;
    }

    // ====================================
    // 현재 목표 강조
    // ====================================

    private void RefreshTargetHighlights()
    {
        foreach (BrickData brick in bricks)
        {
            if (!brick.isTarget || brick.broken)
                continue;

            Image image =
                brick.obj.GetComponent<Image>();

            if (image == null)
                continue;

            if (brick.targetIndex == currentTargetIndex)
            {
                image.color = new Color32(
                    255, 211, 105, 255
                );
            }
            else if (image.sprite != null)
            {
                image.color = Color.white;
            }
            else
            {
                image.color = new Color32(
                    35, 125, 154, 255
                );
            }
        }
    }

    // ====================================
    // 기본 공 이미지 생성
    // ====================================

    private Sprite GetBallVisual()
    {
        if (ballSprite != null)
            return ballSprite;

        if (defaultBallSprite != null)
            return defaultBallSprite;

        const int size = 48;

        defaultBallTexture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        defaultBallTexture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        float radius = size * 0.46f;
        Vector2 center = new Vector2(
            (size - 1) * 0.5f,
            (size - 1) * 0.5f
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    center
                );

                float alpha = Mathf.Clamp01(
                    radius - distance + 0.7f
                );

                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        defaultBallTexture.SetPixels(pixels);
        defaultBallTexture.Apply();

        defaultBallSprite = Sprite.Create(
            defaultBallTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );

        return defaultBallSprite;
    }

    private void OnDestroy()
    {
        if (defaultBallSprite != null)
            Destroy(defaultBallSprite);

        if (defaultBallTexture != null)
            Destroy(defaultBallTexture);
    }

    // ====================================
    // 공 생성 / 삭제
    // ====================================

    private void ClearBall()
    {
        if (ballRect != null)
        {
            ballRect.gameObject.SetActive(false);
            Destroy(ballRect.gameObject);
            ballRect = null;
        }
    }

    private void SpawnBall()
    {
        ClearBall();

        GameObject go = new GameObject(
            "Ball_" + (++ballSerial),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(ballLayer, false);

        ballRect = go.GetComponent<RectTransform>();

        ballRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        ballRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        ballRect.pivot =
            new Vector2(0.5f, 0.5f);

        ballRect.sizeDelta =
            new Vector2(ballSize, ballSize);

        Image image = go.GetComponent<Image>();

        image.sprite = GetBallVisual();
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;

        ballVelocity = Vector2.zero;

        phase = GamePhase.WaitingLaunch;

        AttachBallToPaddle();
    }

    private void AttachBallToPaddle()
    {
        if (ballRect == null)
            return;

        Rect paddleBounds = GetAreaBounds(paddle);

        Vector2 position = new Vector2(
            paddleBounds.center.x,
            paddleBounds.yMax +
            BallRadius() + 10f
        );

        ballRect.anchoredPosition = position;
    }

    private float BallRadius()
    {
        return Mathf.Max(5f, ballSize * 0.5f);
    }

    // ====================================
    // 공 발사
    // ====================================

    private void LaunchBall()
    {
        if (phase != GamePhase.WaitingLaunch)
            return;

        float horizontal = Random.Range(
            -0.28f,
            0.28f
        );

        ballVelocity = new Vector2(
            horizontal,
            1f
        ).normalized * ballSpeed;

        phase = GamePhase.Playing;
    }

    // ====================================
    // 마우스 / 키보드 입력
    // ====================================

    private bool ReadInput(out Vector2 mousePosition,
                           out bool mouseClicked,
                           out bool launchPressed,
                           out float horizontal)
    {
        mousePosition = Vector2.zero;
        mouseClicked = false;
        launchPressed = false;
        horizontal = 0f;

        bool mouseAvailable = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        mousePosition = Input.mousePosition;
        mouseAvailable = true;

        mouseClicked |= Input.GetMouseButtonDown(0);

        launchPressed |=
            Input.GetKeyDown(KeyCode.Space);

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;
#endif

#if ENABLE_INPUT_SYSTEM
        var mouse =
            UnityEngine.InputSystem.Mouse.current;

        if (mouse != null)
        {
            mousePosition =
                mouse.position.ReadValue();

            mouseAvailable = true;

            mouseClicked |=
                mouse.leftButton.wasPressedThisFrame;
        }

        var keyboard =
            UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null)
        {
            launchPressed |=
                keyboard.spaceKey.wasPressedThisFrame;

            if (horizontal == 0f)
            {
                if (keyboard.aKey.isPressed ||
                    keyboard.leftArrowKey.isPressed)
                    horizontal -= 1f;

                if (keyboard.dKey.isPressed ||
                    keyboard.rightArrowKey.isPressed)
                    horizontal += 1f;
            }
        }
#endif

        horizontal =
            Mathf.Clamp(horizontal, -1f, 1f);

        return mouseAvailable;
    }

    // ====================================
    // 게임 업데이트
    // ====================================

    private void LateUpdate()
    {
        if (!isGameActive || finished)
            return;

        if (phase != GamePhase.WaitingLaunch &&
            phase != GamePhase.Playing)
            return;

        Vector2 mousePosition;
        bool mouseClicked;
        bool launchPressed;
        float horizontal;

        bool mouseAvailable = ReadInput(
            out mousePosition,
            out mouseClicked,
            out launchPressed,
            out horizontal
        );

        UpdatePaddle(
            mouseAvailable,
            mousePosition,
            horizontal
        );

        if (phase == GamePhase.WaitingLaunch)
        {
            AttachBallToPaddle();

            if (launchPressed ||
                (mouseClicked &&
                 IsMouseInsideArea(mousePosition)))
            {
                LaunchBall();
            }

            return;
        }

        UpdateBall();
    }

    private Camera GetUICamera()
    {
        Canvas canvas =
            gameArea.GetComponentInParent<Canvas>();

        if (canvas == null ||
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private bool IsMouseInsideArea(Vector2 screenPoint)
    {
        return RectTransformUtility
            .RectangleContainsScreenPoint(
                gameArea,
                screenPoint,
                GetUICamera()
            );
    }

    // ====================================
    // 패들 이동
    // ====================================

    private void UpdatePaddle(
        bool mouseAvailable,
        Vector2 mousePosition,
        float horizontal)
    {
        float x = paddle.anchoredPosition.x;

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            x += horizontal *
                paddleMoveSpeed * Time.deltaTime;
        }
        else if (mouseAvailable &&
                 IsMouseInsideArea(mousePosition))
        {
            Vector2 local;

            if (RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    gameArea,
                    mousePosition,
                    GetUICamera(),
                    out local
                ))
            {
                x = local.x;
            }
        }

        float halfPaddle =
            paddle.rect.width * 0.5f;

        float maxX =
            gameArea.rect.width * 0.5f -
            halfPaddle - 8f;

        x = Mathf.Clamp(x, -maxX, maxX);

        paddle.anchoredPosition = new Vector2(
            x,
            paddle.anchoredPosition.y
        );
    }

    // ====================================
    // UI 사각형을 GameArea 좌표로 계산
    // ====================================

    private Rect GetAreaBounds(RectTransform rect)
    {
        rect.GetWorldCorners(corners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector3 point =
                gameArea.InverseTransformPoint(corners[i]);

            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);

            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        return Rect.MinMaxRect(
            minX,
            minY,
            maxX,
            maxY
        );
    }

    // ====================================
    // 공 vs 사각형 충돌
    // ====================================

    private bool CircleTouchesRect(
        Vector2 circleCenter,
        float radius,
        Rect rectangle)
    {
        float nearestX = Mathf.Clamp(
            circleCenter.x,
            rectangle.xMin,
            rectangle.xMax
        );

        float nearestY = Mathf.Clamp(
            circleCenter.y,
            rectangle.yMin,
            rectangle.yMax
        );

        float dx = circleCenter.x - nearestX;
        float dy = circleCenter.y - nearestY;

        return dx * dx + dy * dy <= radius * radius;
    }

    // ====================================
    // 공 이동
    // ====================================

    private void UpdateBall()
    {
        if (ballRect == null)
            return;

        float dt = Mathf.Min(
            Time.deltaTime,
            0.05f
        );

        if (dt <= 0f)
            return;

        float speed =
            Mathf.Max(100f, ballSpeed);

        ballVelocity =
            ballVelocity.normalized * speed;

        float distance =
            speed * dt;

        float stepSize =
            Mathf.Clamp(collisionStep, 1f, 8f);

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / stepSize)
        );

        float stepDt = dt / steps;

        float radius = BallRadius();

        float left =
            -gameArea.rect.width * 0.5f + radius;

        float right =
            gameArea.rect.width * 0.5f - radius;

        float top =
            gameArea.rect.height * 0.5f - radius;

        float bottom =
            -gameArea.rect.height * 0.5f - radius;

        Vector2 position =
            ballRect.anchoredPosition;

        for (int step = 0; step < steps; step++)
        {
            if (phase != GamePhase.Playing)
                return;

            Vector2 previous = position;

            Vector2 next =
                position + ballVelocity * stepDt;

            // 왼쪽 / 오른쪽 벽
            if (next.x <= left)
            {
                next.x = left;
                ballVelocity.x =
                    Mathf.Abs(ballVelocity.x);
            }
            else if (next.x >= right)
            {
                next.x = right;
                ballVelocity.x =
                    -Mathf.Abs(ballVelocity.x);
            }

            // 위쪽 벽
            if (next.y >= top)
            {
                next.y = top;
                ballVelocity.y =
                    -Mathf.Abs(ballVelocity.y);
            }

            // 아래로 떨어지면 공만 재생성
            if (next.y < bottom)
            {
                Debug.Log(
                    "MG30: 공 낙하 - 진행도 유지"
                );

                SpawnBall();
                UpdateHUD();
                return;
            }

            // 패들 충돌
            if (ballVelocity.y < 0f)
            {
                Rect paddleBounds =
                    GetAreaBounds(paddle);

                if (CircleTouchesRect(
                    next,
                    radius,
                    paddleBounds))
                {
                    next.y =
                        paddleBounds.yMax + radius + 1f;

                    float relativeX =
                        (next.x - paddleBounds.center.x) /
                        Mathf.Max(
                            1f,
                            paddleBounds.width * 0.5f
                        );

                    relativeX = Mathf.Clamp(
                        relativeX, -1f, 1f
                    );

                    float angle =
                        relativeX *
                        paddleBounceAngle *
                        Mathf.Deg2Rad;

                    ballVelocity = new Vector2(
                        Mathf.Sin(angle),
                        Mathf.Cos(angle)
                    ).normalized * speed;
                }
            }

            // 벽돌 충돌
            bool hitBrick = false;

            foreach (BrickData brick in bricks)
            {
                if (brick.broken ||
                    brick.obj == null)
                    continue;

                Rect bounds =
                    GetAreaBounds(brick.rect);

                if (!CircleTouchesRect(
                    next,
                    radius,
                    bounds))
                    continue;

                HandleBrickHit(brick);

                if (phase != GamePhase.Playing)
                    return;

                // 충돌 방향 판정
                Vector2 normal =
                    GetCollisionNormal(
                        previous,
                        next,
                        bounds,
                        radius
                    );

                ballVelocity =
                    Vector2.Reflect(
                        ballVelocity,
                        normal
                    ).normalized * speed;

                // 같은 벽돌에 다시 끼지 않도록
                // 충돌 이전 위치로 되돌림
                next = previous;

                hitBrick = true;
                break;
            }

            position = next;

            if (hitBrick)
            {
                // 다음 작은 이동 구간에서
                // 반사된 방향으로 진행
            }
        }

        if (ballRect != null)
        {
            ballRect.anchoredPosition =
                position;
        }
    }

    // ====================================
    // 벽돌 충돌 방향 계산
    // ====================================

    private Vector2 GetCollisionNormal(
        Vector2 previous,
        Vector2 hitPosition,
        Rect bounds,
        float radius)
    {
        // 이전 위치가 위 / 아래 / 좌 / 우 중
        // 어느 쪽에 있었는지 먼저 확인

        if (previous.y + radius <= bounds.yMin)
            return Vector2.down;

        if (previous.y - radius >= bounds.yMax)
            return Vector2.up;

        if (previous.x + radius <= bounds.xMin)
            return Vector2.left;

        if (previous.x - radius >= bounds.xMax)
            return Vector2.right;

        // 모서리 부근 충돌
        Vector2 nearest = new Vector2(
            Mathf.Clamp(
                hitPosition.x,
                bounds.xMin,
                bounds.xMax
            ),
            Mathf.Clamp(
                hitPosition.y,
                bounds.yMin,
                bounds.yMax
            )
        );

        Vector2 difference =
            hitPosition - nearest;

        if (difference.sqrMagnitude > 0.001f)
            return difference.normalized;

        // 벽돌 안쪽까지 들어간 경우
        // 주된 이동 방향의 반대쪽으로 반사
        if (Mathf.Abs(ballVelocity.x) >
            Mathf.Abs(ballVelocity.y))
        {
            return ballVelocity.x > 0f
                ? Vector2.left
                : Vector2.right;
        }

        return ballVelocity.y > 0f
            ? Vector2.down
            : Vector2.up;
    }

    // ====================================
    // 벽돌에 맞았을 때 처리
    // ====================================

    private void HandleBrickHit(BrickData brick)
    {
        if (brick.isNormal)
        {
            // 일반 벽돌은 순서와 무관하게 제거
            BreakBrick(brick);

            Debug.Log("MG30: 일반 벽돌 제거");
            return;
        }

        if (brick.isTarget &&
            brick.targetIndex == currentTargetIndex)
        {
            // 현재 정답 문자
            BreakBrick(brick);

            currentTargetIndex++;

            UpdateHUD();
            RefreshTargetHighlights();

            Debug.Log(
                "MG30: 정답 획득 " +
                brick.character +
                " (" + currentTargetIndex + "/5)"
            );

            if (currentTargetIndex >= TargetWord.Length)
            {
                CompleteAllLetters();
            }

            return;
        }

        // 방해 문자 또는 순서가 틀린 START 문자
        TriggerSequenceError();
    }

    private void BreakBrick(BrickData brick)
    {
        brick.broken = true;
        brick.obj.SetActive(false);
    }

    // ====================================
    // 현재 목표와 진행도 표시
    // ====================================

    private void UpdateHUD()
    {
        if (currentTargetIndex >= TargetWord.Length)
        {
            targetText.text = "TARGET : COMPLETE";
        }
        else
        {
            string extra = "";

            if (currentTargetIndex == 1)
                extra = " (FIRST T)";
            else if (currentTargetIndex == 4)
                extra = " (LAST T)";

            targetText.text =
                "TARGET : " +
                TargetWord[currentTargetIndex] +
                extra;
        }

        string progress = "";

        for (int i = 0; i < TargetWord.Length; i++)
        {
            progress += i < currentTargetIndex
                ? TargetWord[i].ToString()
                : "_";

            if (i < TargetWord.Length - 1)
                progress += "  ";
        }

        progressText.text =
            "PROGRESS : " + progress;
    }

    // ====================================
    // 순서 오류
    // ====================================

    private void TriggerSequenceError()
    {
        if (phase != GamePhase.Playing)
            return;

        phase = GamePhase.SequenceError;

        ClearBall();

        StartCoroutine(
            SequenceErrorRoutine()
        );
    }

    private IEnumerator SequenceErrorRoutine()
    {
        errorText.text =
            "SEQUENCE ERROR\n문자 순서가 잘못되었습니다.";

        errorText.gameObject.SetActive(true);

        Debug.Log(
            "MG30: SEQUENCE ERROR"
        );

        yield return new WaitForSecondsRealtime(
            errorDisplayTime
        );

        if (!isGameActive || finished)
            yield break;

        errorText.gameObject.SetActive(false);

        // 정답 문자 다섯 개만 다시 생성된 상태로 복구
        foreach (BrickData brick in bricks)
        {
            if (!brick.isTarget)
                continue;

            brick.broken = false;
            brick.obj.SetActive(true);
        }

        // 일반 벽돌과 방해 문자의 상태는 유지
        currentTargetIndex = 0;

        UpdateHUD();
        RefreshTargetHighlights();

        SpawnBall();
    }

    // ====================================
    // 다섯 글자 수집 성공
    // ====================================

    private void CompleteAllLetters()
    {
        if (finished)
            return;

        phase = GamePhase.StartReady;

        ClearBall();

        gameArea.gameObject.SetActive(false);
        errorText.gameObject.SetActive(false);

        resultPanel.SetActive(true);

        resultText.text = "S - T - A - R - T";

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();

        Debug.Log(
            "MG30: START 문자 5개 제거 완료!"
        );
    }

    // ====================================
    // 최종 START 버튼
    // ====================================

    private void SetupFinalButton()
    {
        finalStartButton.onClick.RemoveAllListeners();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
    }

    private void OnFinalStartClicked()
    {
        if (!isGameActive ||
            finished ||
            phase != GamePhase.StartReady)
        {
            return;
        }

        finished = true;
        phase = GamePhase.Completed;

        Debug.Log(
            "MG30: 최종 START 클릭 - 검사 완료"
        );

        Success();
    }

    // ====================================
    // 힌트 및 재시작
    // ====================================

    protected override void GiveHint()
    {
        if (phase == GamePhase.Playing ||
            phase == GamePhase.WaitingLaunch)
        {
            if (currentTargetIndex < TargetWord.Length)
            {
                targetText.text =
                    "TARGET : " +
                    TargetWord[currentTargetIndex] +
                    "  |  다른 문자를 깨면 초기화됩니다.";
            }
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
