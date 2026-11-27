
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame26_FallingLetters : MinigameBase
{
    [Header("Stage 1 - Catch")]
    public RectTransform dropArea;
    public RectTransform fallingLettersLayer;
    public RectTransform catchPaddle;
    public TMP_Text currentText;
    public TMP_Text collectedText;
    public TMP_Text errorText;

    [Header("Stage 2 - Stack")]
    public RectTransform stackArea;
    public RectTransform stackBase;
    public TMP_Text stackStatusText;
    public Button finalStartButton;

    [Header("ART - Falling Letter Sprites")]
    public Sprite fallingS;
    public Sprite fallingT;
    public Sprite fallingA;
    public Sprite fallingR;
    public Sprite fallingE;
    public Sprite fallingN;
    public Sprite fallingD;
    public Sprite fallingX;
    public Sprite fallingI;
    public Sprite fallingP;
    public Sprite fallingL;
    public Sprite fallingY;

    [Header("ART - Stack Block Sprites")]
    public Sprite blockS;
    public Sprite blockT;
    public Sprite blockA;
    public Sprite blockR;

    [Header("ART - Other Sprites")]
    public Sprite paddleSprite;
    public Sprite stackBaseSprite;
    public Sprite finalStartSprite;

    [Tooltip("최종 버튼 이미지에 START 글자가 포함되어 있으면 체크")]
    public bool hideFinalTextWhenUsingSprite = true;

    [Header("Falling Letter Settings")]
    public Vector2 fallingLetterSize =
        new Vector2(78f, 82f);

    public float spawnInterval = 0.8f;
    public float minFallSpeed = 190f;
    public float maxFallSpeed = 270f;
    public float paddleMoveSpeed = 850f;
    public float errorDisplaySeconds = 1.2f;

    [Header("Stack Settings")]
    public Vector2 blockSize =
        new Vector2(150f, 54f);

    public float stackGap = 4f;
    public float snapDistanceX = 65f;
    public float snapDistanceY = 65f;
    public float stableSeconds = 3f;

    private const string TargetWord = "START";
    private const string LetterPool = "STARTENDXITPLAY";

    private enum Phase
    {
        Catching,
        Error,
        Stacking,
        Stability,
        StartReady,
        Completed
    }

    private Phase phase;

    private int collectedCount;
    private int stackedCount;

    private float spawnTimer;
    private float stabilityTimer;

    private bool finished;

    private readonly List<FallingLetter> activeLetters =
        new List<FallingLetter>();

    private readonly List<GameObject> createdBlocks =
        new List<GameObject>();

    private readonly List<Vector2> blockHomePositions =
        new List<Vector2>();

    private RectTransform stackGuide;

    private readonly Vector3[] corners = new Vector3[4];

    private class FallingLetter
    {
        public GameObject obj;
        public RectTransform rect;
        public char character;
        public float speed;
    }

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "떨어지는 문자 복구";
        instruction =
            "S-T-A-R-T를 순서대로 수집하고 탑을 쌓으십시오.";

        timeLimit = 999999f;

        base.StartMinigame();
        StopAllCoroutines();

        finished = false;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG26: UI 연결이 누락되었습니다.",
                this
            );

            isGameActive = false;
            return;
        }

        ClearGeneratedObjects();

        Canvas.ForceUpdateCanvases();

        ApplyStaticArtwork();
        SetupFinalButton();
        BeginCatchStage();
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
            Transform result =
                FindDeep(child, objectName);

            if (result != null)
                return result;
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
        if (dropArea == null)
            dropArea =
                FindUI<RectTransform>("DropArea");

        if (fallingLettersLayer == null)
            fallingLettersLayer =
                FindUI<RectTransform>("FallingLettersLayer");

        if (catchPaddle == null)
            catchPaddle =
                FindUI<RectTransform>("CatchPaddle");

        if (currentText == null)
            currentText = FindUI<TMP_Text>("CurrentText");

        if (collectedText == null)
            collectedText = FindUI<TMP_Text>("CollectedText");

        if (errorText == null)
            errorText = FindUI<TMP_Text>("ErrorText");

        if (stackArea == null)
            stackArea =
                FindUI<RectTransform>("StackArea");

        if (stackBase == null)
            stackBase =
                FindUI<RectTransform>("StackBase");

        if (stackStatusText == null)
            stackStatusText =
                FindUI<TMP_Text>("StackStatusText");

        if (finalStartButton == null)
            finalStartButton =
                FindUI<Button>("FinalStartButton");

        return
            dropArea != null &&
            fallingLettersLayer != null &&
            catchPaddle != null &&
            currentText != null &&
            collectedText != null &&
            errorText != null &&
            stackArea != null &&
            stackBase != null &&
            stackStatusText != null &&
            finalStartButton != null;
    }

    // =========================================
    // 문자별 이미지 선택
    // =========================================

    private Sprite GetFallingSprite(char c)
    {
        switch (c)
        {
            case 'S': return fallingS;
            case 'T': return fallingT;
            case 'A': return fallingA;
            case 'R': return fallingR;
            case 'E': return fallingE;
            case 'N': return fallingN;
            case 'D': return fallingD;
            case 'X': return fallingX;
            case 'I': return fallingI;
            case 'P': return fallingP;
            case 'L': return fallingL;
            case 'Y': return fallingY;
        }

        return null;
    }

    private Sprite GetBlockSprite(char c)
    {
        switch (c)
        {
            case 'S': return blockS;
            case 'T': return blockT;
            case 'A': return blockA;
            case 'R': return blockR;
        }

        return null;
    }

    // =========================================
    // 기존 UI에 아트 이미지 적용
    // =========================================

    private void ApplyStaticArtwork()
    {
        ApplySpriteToUI(catchPaddle, paddleSprite);
        ApplySpriteToUI(stackBase, stackBaseSprite);

        ApplySpriteToUI(
            finalStartButton.GetComponent<RectTransform>(),
            finalStartSprite
        );

        if (finalStartSprite != null &&
            hideFinalTextWhenUsingSprite)
        {
            TMP_Text[] labels =
                finalStartButton.GetComponentsInChildren<
                    TMP_Text>(true);

            foreach (TMP_Text label in labels)
                label.gameObject.SetActive(false);
        }
        else
        {
            TMP_Text[] labels =
                finalStartButton.GetComponentsInChildren<
                    TMP_Text>(true);

            foreach (TMP_Text label in labels)
                label.gameObject.SetActive(true);
        }
    }

    private void ApplySpriteToUI(
        RectTransform target,
        Sprite sprite)
    {
        if (target == null || sprite == null)
            return;

        Image image = target.GetComponent<Image>();

        if (image == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    // =========================================
    // 문자 이미지 또는 기본 TMP 자동 생성
    // =========================================

    private void CreateLetterVisual(
        Transform parent,
        char character,
        Sprite sprite,
        float fontSize)
    {
        if (sprite != null)
        {
            GameObject imageObject = new GameObject(
                "LetterArt_" + character,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            imageObject.transform.SetParent(
                parent,
                false
            );

            RectTransform imageRect =
                imageObject.GetComponent<RectTransform>();

            StretchToParent(imageRect);

            Image image =
                imageObject.GetComponent<Image>();

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        else
        {
            GameObject textObject = new GameObject(
                "FallbackText_" + character,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            textObject.transform.SetParent(
                parent,
                false
            );

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            StretchToParent(textRect);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();

            text.text = character.ToString();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment =
                TextAlignmentOptions.Center;

            text.color = Color.white;
            text.raycastTarget = false;
        }
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // =========================================
    // 생성된 오브젝트 정리
    // =========================================

    private void ClearGeneratedObjects()
    {
        ClearFallingLetters();

        foreach (GameObject block in createdBlocks)
        {
            if (block != null)
            {
                block.SetActive(false);
                Destroy(block);
            }
        }

        createdBlocks.Clear();
        blockHomePositions.Clear();

        if (stackGuide != null)
        {
            stackGuide.gameObject.SetActive(false);
            Destroy(stackGuide.gameObject);
            stackGuide = null;
        }
    }

    private void ClearFallingLetters()
    {
        foreach (FallingLetter item in activeLetters)
        {
            if (item.obj != null)
            {
                item.obj.SetActive(false);
                Destroy(item.obj);
            }
        }

        activeLetters.Clear();
    }

    // =========================================
    // 최종 버튼 설정
    // =========================================

    private void SetupFinalButton()
    {
        finalStartButton.onClick.RemoveAllListeners();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
    }

    // =========================================
    // 메인 업데이트
    // =========================================

    private void LateUpdate()
    {
        if (!isGameActive || finished)
            return;

        if (phase == Phase.Catching)
        {
            UpdatePaddle();
            UpdateFallingLetters();

            if (phase == Phase.Catching)
                UpdateSpawning();
        }
        else if (phase == Phase.Stability)
        {
            UpdateStability();
        }
    }

    // =========================================
    // 1단계 시작
    // =========================================

    private void BeginCatchStage()
    {
        phase = Phase.Catching;

        collectedCount = 0;
        stackedCount = 0;

        spawnTimer = 0f;
        stabilityTimer = 0f;

        ClearFallingLetters();

        dropArea.gameObject.SetActive(true);
        stackArea.gameObject.SetActive(false);

        currentText.gameObject.SetActive(true);
        collectedText.gameObject.SetActive(true);
        errorText.gameObject.SetActive(false);

        finalStartButton.gameObject.SetActive(false);

        catchPaddle.anchoredPosition =
            new Vector2(
                0f,
                catchPaddle.anchoredPosition.y
            );

        Image paddleImage =
            catchPaddle.GetComponent<Image>();

        if (paddleImage != null)
            paddleImage.raycastTarget = false;

        UpdateCatchTexts();

        SpawnLetter(true);
    }

    // =========================================
    // 받침대 이동 - 마우스 / 키보드
    // =========================================

    private void UpdatePaddle()
    {
        bool moveLeft = false;
        bool moveRight = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        moveLeft |=
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow);

        moveRight |=
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow);
#endif

#if ENABLE_INPUT_SYSTEM
        var keyboard =
            UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null)
        {
            moveLeft |=
                keyboard.aKey.isPressed ||
                keyboard.leftArrowKey.isPressed;

            moveRight |=
                keyboard.dKey.isPressed ||
                keyboard.rightArrowKey.isPressed;
        }
#endif

        float direction =
            (moveRight ? 1f : 0f) -
            (moveLeft ? 1f : 0f);

        float nextX = catchPaddle.anchoredPosition.x;

        if (Mathf.Abs(direction) > 0.01f)
        {
            nextX += direction *
                paddleMoveSpeed * Time.deltaTime;
        }
        else
        {
            Vector2 mousePosition;

            if (TryGetMousePosition(out mousePosition))
            {
                Canvas canvas =
                    dropArea.GetComponentInParent<Canvas>();

                Camera camera = null;

                if (canvas != null &&
                    canvas.renderMode !=
                    RenderMode.ScreenSpaceOverlay)
                {
                    camera = canvas.worldCamera;
                }

                if (RectTransformUtility
                    .RectangleContainsScreenPoint(
                        dropArea,
                        mousePosition,
                        camera
                    ))
                {
                    Vector2 local;

                    if (RectTransformUtility
                        .ScreenPointToLocalPointInRectangle(
                            dropArea,
                            mousePosition,
                            camera,
                            out local
                        ))
                    {
                        nextX = local.x;
                    }
                }
            }
        }

        float halfWidth =
            catchPaddle.rect.width *
            Mathf.Abs(catchPaddle.localScale.x) * 0.5f;

        float limit =
            dropArea.rect.width * 0.5f -
            halfWidth - 12f;

        nextX = Mathf.Clamp(nextX, -limit, limit);

        catchPaddle.anchoredPosition =
            new Vector2(
                nextX,
                catchPaddle.anchoredPosition.y
            );
    }

    private bool TryGetMousePosition(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;

        if (mouse != null)
        {
            position = mouse.position.ReadValue();
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
    // 문자 낙하 생성
    // =========================================

    private void UpdateSpawning()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;

        if (activeLetters.Count < 16)
            SpawnLetter(false);
    }

    private void SpawnLetter(bool forceTarget)
    {
        if (collectedCount >= TargetWord.Length)
            return;

        char target = TargetWord[collectedCount];
        char letter = target;

        if (!forceTarget && Random.value > 0.45f)
        {
            do
            {
                letter = LetterPool[
                    Random.Range(0, LetterPool.Length)
                ];
            }
            while (letter == target);
        }

        GameObject go = new GameObject(
            "FallingLetter_" + letter,
            typeof(RectTransform)
        );

        go.transform.SetParent(
            fallingLettersLayer,
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

        rect.sizeDelta = fallingLetterSize;

        float limitX =
            dropArea.rect.width * 0.5f - 60f;

        float spawnX = forceTarget
            ? 0f
            : Random.Range(-limitX, limitX);

        float spawnY =
            dropArea.rect.height * 0.5f - 35f;

        rect.anchoredPosition =
            new Vector2(spawnX, spawnY);

        // ★ 이미지가 있으면 Sprite, 없으면 TMP
        Sprite artwork = GetFallingSprite(letter);

        CreateLetterVisual(
            go.transform,
            letter,
            artwork,
            64f
        );

        FallingLetter data = new FallingLetter();

        data.obj = go;
        data.rect = rect;
        data.character = letter;
        data.speed = Random.Range(
            minFallSpeed,
            maxFallSpeed
        );

        activeLetters.Add(data);
    }

    // =========================================
    // UI 사각형 충돌 범위 계산
    // =========================================

    private Rect GetBounds(RectTransform element)
    {
        element.GetWorldCorners(corners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector3 local =
                dropArea.InverseTransformPoint(corners[i]);

            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);

            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }

        return Rect.MinMaxRect(
            minX, minY, maxX, maxY
        );
    }

    // =========================================
    // 낙하 이동 및 받기 판정
    // =========================================

    private void UpdateFallingLetters()
    {
        Rect paddleBounds = GetBounds(catchPaddle);

        for (int i = activeLetters.Count - 1;
             i >= 0; i--)
        {
            FallingLetter item = activeLetters[i];

            Vector2 position =
                item.rect.anchoredPosition;

            position.y -=
                item.speed * Time.deltaTime;

            item.rect.anchoredPosition = position;

            Rect letterBounds = GetBounds(item.rect);

            if (paddleBounds.Overlaps(letterBounds))
            {
                char received = item.character;

                activeLetters.RemoveAt(i);

                item.obj.SetActive(false);
                Destroy(item.obj);

                ReceiveLetter(received);

                if (phase != Phase.Catching)
                    return;

                continue;
            }

            if (position.y <
                -dropArea.rect.height * 0.5f - 90f)
            {
                activeLetters.RemoveAt(i);

                item.obj.SetActive(false);
                Destroy(item.obj);
            }
        }
    }

    // =========================================
    // 순서대로 글자 받기
    // =========================================

    private void ReceiveLetter(char letter)
    {
        char target = TargetWord[collectedCount];

        if (letter != target)
        {
            StartCoroutine(WrongLetterRoutine());
            return;
        }

        collectedCount++;

        Debug.Log(
            "MG26: " + letter +
            " 획득 (" + collectedCount + "/5)"
        );

        if (collectedCount >= TargetWord.Length)
        {
            EnterStackStage();
            return;
        }

        UpdateCatchTexts();
    }

    private void UpdateCatchTexts()
    {
        if (collectedCount < TargetWord.Length)
        {
            currentText.text =
                "CURRENT : " + TargetWord[collectedCount];
        }
        else
        {
            currentText.text = "CURRENT : COMPLETE";
        }

        string result = "";

        for (int i = 0; i < TargetWord.Length; i++)
        {
            result += i < collectedCount
                ? TargetWord[i].ToString()
                : "_";

            if (i < TargetWord.Length - 1)
                result += "  ";
        }

        collectedText.text =
            "COLLECTED : " + result;
    }

    // =========================================
    // 잘못된 문자 -> 초기화
    // =========================================

    private IEnumerator WrongLetterRoutine()
    {
        phase = Phase.Error;

        ClearFallingLetters();

        errorText.text =
            "ERROR\nINVALID CHARACTER";

        errorText.gameObject.SetActive(true);

        currentText.text = "CURRENT : ERROR";

        yield return new WaitForSeconds(
            errorDisplaySeconds
        );

        if (!isGameActive || finished)
            yield break;

        BeginCatchStage();
    }

    // =========================================
    // 2단계 진입
    // =========================================

    private void EnterStackStage()
    {
        phase = Phase.Stacking;

        ClearFallingLetters();

        dropArea.gameObject.SetActive(false);

        currentText.gameObject.SetActive(false);
        collectedText.gameObject.SetActive(false);
        errorText.gameObject.SetActive(false);

        stackArea.gameObject.SetActive(true);
        finalStartButton.gameObject.SetActive(false);

        Canvas.ForceUpdateCanvases();

        CreateStackBlocks();

        stackedCount = 0;
        stabilityTimer = 0f;

        UpdateStackGuide();

        stackStatusText.text =
            "S → T → A → R → T 순서로 블록을 쌓으십시오.";
    }

    // =========================================
    // 문자 블록 5개 생성
    // =========================================

    private void CreateStackBlocks()
    {
        float areaWidth = stackArea.rect.width;

        for (int i = 0; i < TargetWord.Length; i++)
        {
            char letter = TargetWord[i];

            GameObject go = new GameObject(
                "StackBlock_" + letter + "_" + i,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            go.transform.SetParent(
                stackArea,
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

            rect.sizeDelta = blockSize;

            float spacing =
                areaWidth / TargetWord.Length;

            float homeX =
                -areaWidth * 0.5f +
                spacing * (i + 0.5f);

            float homeY =
                -stackArea.rect.height * 0.5f + 55f;

            Vector2 home =
                new Vector2(homeX, homeY);

            rect.anchoredPosition = home;

            Image image = go.GetComponent<Image>();

            Sprite artwork = GetBlockSprite(letter);

            if (artwork != null)
            {
                // ★ 아트팀의 완성 블록 이미지 적용
                image.sprite = artwork;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
            else
            {
                // 이미지가 없으면 기존 블록 유지
                image.color = i % 2 == 0
                    ? new Color32(35, 122, 155, 255)
                    : new Color32(40, 107, 145, 255);

                CreateLetterVisual(
                    go.transform,
                    letter,
                    null,
                    35f
                );
            }

            image.raycastTarget = true;

            MG26StackBlockDrag drag =
                go.AddComponent<MG26StackBlockDrag>();

            drag.Configure(
                this,
                i,
                rect,
                stackArea
            );

            createdBlocks.Add(go);
            blockHomePositions.Add(home);
        }

        CreateStackGuide();
    }

    // =========================================
    // 탑 쌓기 목표 위치 표시
    // =========================================

    private void CreateStackGuide()
    {
        GameObject go = new GameObject(
            "StackTargetGuide",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(
            stackArea,
            false
        );

        stackGuide =
            go.GetComponent<RectTransform>();

        stackGuide.anchorMin =
            new Vector2(0.5f, 0.5f);

        stackGuide.anchorMax =
            new Vector2(0.5f, 0.5f);

        stackGuide.pivot =
            new Vector2(0.5f, 0.5f);

        stackGuide.sizeDelta = blockSize;

        Image image = go.GetComponent<Image>();

        image.color =
            new Color(0.3f, 0.95f, 0.95f, 0.23f);

        image.raycastTarget = false;

        stackGuide.SetAsFirstSibling();
    }

    private Vector2 GetStackTargetPosition(int index)
    {
        float baseTop =
            stackBase.anchoredPosition.y +
            stackBase.rect.height * 0.5f;

        float y =
            baseTop +
            4f +
            blockSize.y * 0.5f +
            index * (blockSize.y + stackGap);

        return new Vector2(
            stackBase.anchoredPosition.x,
            y
        );
    }

    private void UpdateStackGuide()
    {
        if (stackGuide == null)
            return;

        if (stackedCount >= TargetWord.Length)
        {
            stackGuide.gameObject.SetActive(false);
            return;
        }

        stackGuide.gameObject.SetActive(true);

        stackGuide.anchoredPosition =
            GetStackTargetPosition(stackedCount);
    }

    // =========================================
    // 블록 드래그 및 배치
    // =========================================

    public bool CanDragBlock(int index)
    {
        return
            isGameActive &&
            !finished &&
            phase == Phase.Stacking &&
            index == stackedCount;
    }

    public void TryPlaceBlock(
        int index,
        RectTransform block)
    {
        if (!CanDragBlock(index))
            return;

        Vector2 target =
            GetStackTargetPosition(index);

        Vector2 actual = block.anchoredPosition;

        bool correct =
            Mathf.Abs(actual.x - target.x) <=
                snapDistanceX &&
            Mathf.Abs(actual.y - target.y) <=
                snapDistanceY;

        if (!correct)
        {
            block.anchoredPosition =
                blockHomePositions[index];

            stackStatusText.text =
                "하늘색 표시 위치에 블록을 놓으십시오.";

            return;
        }

        block.anchoredPosition = target;

        MG26StackBlockDrag drag =
            block.GetComponent<MG26StackBlockDrag>();

        if (drag != null)
            drag.SetPlaced(true);

        stackedCount++;

        UpdateStackGuide();

        if (stackedCount >= TargetWord.Length)
        {
            BeginStabilityCheck();
        }
        else
        {
            stackStatusText.text =
                "다음 블록 : " +
                TargetWord[stackedCount];
        }
    }

    // =========================================
    // 3초 안정성 검사
    // =========================================

    private void BeginStabilityCheck()
    {
        phase = Phase.Stability;
        stabilityTimer = 0f;

        stackStatusText.text =
            "탑 안정성 검사 중... 3.0초";
    }

    private void UpdateStability()
    {
        stabilityTimer += Time.deltaTime;

        float remaining = Mathf.Max(
            0f,
            stableSeconds - stabilityTimer
        );

        stackStatusText.text =
            "탑 안정성 검사 중... " +
            remaining.ToString("F1") + "초";

        if (stabilityTimer >= stableSeconds)
            ShowFinalStartButton();
    }

    // =========================================
    // 마지막 START 버튼
    // =========================================

    private void ShowFinalStartButton()
    {
        phase = Phase.StartReady;

        stackStatusText.text =
            "문자 복구 완료! START를 누르십시오.";

        RectTransform rect =
            finalStartButton.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            new Vector2(0f, 155f);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
    }

    private void OnFinalStartClicked()
    {
        if (!isGameActive ||
            finished ||
            phase != Phase.StartReady)
        {
            return;
        }

        finished = true;
        phase = Phase.Completed;

        Debug.Log("MG26: 검사 완료");

        Success();
    }

    protected override void GiveHint()
    {
        // 기획서에 별도 힌트 없음
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}

// =========================================
// 문자 블록 드래그
// 이 클래스도 같은 파일에 넣으세요.
// =========================================

public class MG26StackBlockDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Minigame26_FallingLetters manager;

    private int blockIndex;

    private RectTransform blockRect;
    private RectTransform parentArea;

    private Vector2 pointerOffset;

    private bool dragging;
    private bool placed;

    public void Configure(
        Minigame26_FallingLetters owner,
        int index,
        RectTransform rect,
        RectTransform area)
    {
        manager = owner;
        blockIndex = index;
        blockRect = rect;
        parentArea = area;

        dragging = false;
        placed = false;
    }

    public void SetPlaced(bool value)
    {
        placed = value;
        dragging = false;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (placed ||
            manager == null ||
            !manager.CanDragBlock(blockIndex))
        {
            return;
        }

        Vector2 local;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentArea,
                eventData.position,
                eventData.pressEventCamera,
                out local
            ))
        {
            return;
        }

        pointerOffset =
            blockRect.anchoredPosition - local;

        dragging = true;
        blockRect.SetAsLastSibling();
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging || placed)
            return;

        Vector2 local;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentArea,
                eventData.position,
                eventData.pressEventCamera,
                out local
            ))
        {
            return;
        }

        Vector2 next = local + pointerOffset;

        float halfWidth =
            blockRect.rect.width * 0.5f;

        float halfHeight =
            blockRect.rect.height * 0.5f;

        float limitX =
            parentArea.rect.width * 0.5f -
            halfWidth;

        float limitY =
            parentArea.rect.height * 0.5f -
            halfHeight;

        next.x = Mathf.Clamp(
            next.x,
            -limitX,
            limitX
        );

        next.y = Mathf.Clamp(
            next.y,
            -limitY,
            limitY
        );

        blockRect.anchoredPosition = next;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;

        if (!placed && manager != null)
        {
            manager.TryPlaceBlock(
                blockIndex,
                blockRect
            );
        }
    }
}
