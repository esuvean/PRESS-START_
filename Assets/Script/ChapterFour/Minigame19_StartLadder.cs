using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame19_StartLadder : MinigameBase
{
    private class Connector
    {
        public int row;
        public int gap = -1;
        public int targetGap;
        public bool isFixed;
        public RectTransform rect;
        public Image handleImage;
        public Image barImage;
        public TMP_Text numberLabel;
    }

    [Header("Game Areas")]
    public RectTransform ladderArea;
    public RectTransform connectorTray;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text roundText;
    public TMP_Text instructionText;
    public TMP_Text trayLabelText;
    public TMP_Text statusText;
    public TMP_Text timerText;
    public Button executeButton;

    [Header("Clear UI")]
    public GameObject clearPanel;
    public TMP_Text clearTitleText;
    public Button clearStartButton;

    [Header("Colors")]
    public Color railColor = new Color(0.8f, 0.88f, 1f);
    public Color fixedColor = new Color(0.3f, 0.85f, 1f);
    public Color movableColor = new Color(1f, 0.76f, 0.3f);
    public Color ghostColor = new Color(0.8f, 0.9f, 1f, 0.13f);
    public Color hintColor = new Color(0.4f, 1f, 0.45f);
    public Color letterColor = Color.white;
    public Color selectionColor = new Color(1f, 0.35f, 0.7f);

    [Header("Art (다원)")]
    [Tooltip("위쪽 글자 박스 5개: S, T, A, R, T 순서")]
    public Sprite[] letterSprites;
    public Sprite numberBoxSprite;
    public Sprite railSprite;
    public Sprite connectorSprite;
    public Sprite slotSprite;
    public float letterScale = 0.85f;
    public float connectorHeight = 22f;
    public float maxRailSpacing = 190f;

    [Header("Settings")]
    public float autoHelpTime = 90f;
    public float movementDuration = 0.22f;
    public float trayLineSpacing = 65f;

    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private readonly Dictionary<int, Connector> connectors = new Dictionary<int, Connector>();
    private readonly List<Connector> movableConnectors = new List<Connector>();

    private Image[,] ghostSlots;
    private TMP_Text[] destinationLabels;

    private int currentRound;
    private int rungCount;
    private int failureCount;
    private int selectedRow = -1;

    private float railSpacing;
    private float topY;
    private float bottomY;
    private float elapsedTime;

    private bool isAnimating;
    private bool isClearing;
    private bool autoHelpUsed;

    private readonly string startLetters = "START";

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        StopAllCoroutines();

        gameName = "실행 문자 경로 검사";
        instruction = "사다리의 가로 연결선을 조정하여 S, T, A, R, T 글자가 아래의 1, 2, 3, 4, 5 칸에 순서대로 도착하게 만든다.";

        base.StartMinigame();

        if (ladderArea == null || connectorTray == null)
        {
            Debug.LogError("LadderArea 또는 ConnectorTray가 연결되지 않았습니다.");
            isGameActive = false;
            return;
        }

        currentRound = 0;
        failureCount = 0;
        elapsedTime = 0f;
        isAnimating = false;
        isClearing = false;
        autoHelpUsed = false;
        selectedRow = -1;

        if (titleText != null)
            titleText.text = gameName;

        if (executeButton != null)
        {
            executeButton.onClick.RemoveListener(OnExecuteClicked);
            executeButton.onClick.AddListener(OnExecuteClicked);
        }

        if (clearStartButton != null)
        {
            clearStartButton.onClick.RemoveListener(OnClearStartClicked);
            clearStartButton.onClick.AddListener(OnClearStartClicked);
        }

        if (clearPanel != null)
            clearPanel.SetActive(false);

        BuildRound();
        UpdateTimerText();
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || isClearing)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();

        if (!autoHelpUsed && elapsedTime >= autoHelpTime && !isAnimating)
        {
            autoHelpUsed = true;
            AutoPlaceOneConnector();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        float remaining = Mathf.Max(0f, autoHelpTime - elapsedTime);
        timerText.text = $"남은 시간: {Mathf.CeilToInt(remaining)}초";
    }

    private void BuildRound()
    {
        ClearGeneratedObjects();
        connectors.Clear();
        movableConnectors.Clear();

        selectedRow = -1;
        failureCount = 0;
        isAnimating = false;

        rungCount = currentRound == 0 ? 6 : 8;
        railSpacing = Mathf.Min(maxRailSpacing, (ladderArea.rect.width - 100f) / 4f);
        topY = ladderArea.rect.height * 0.5f - 38f;
        bottomY = -ladderArea.rect.height * 0.5f + 38f;

        ghostSlots = new Image[rungCount, 4];
        destinationLabels = new TMP_Text[5];

        if (roundText != null)
            roundText.text = $"ROUND {currentRound + 1} / 2";

        if (instructionText != null)
            instructionText.text = "오른쪽 연결선 클릭 → 사다리의 점선 칸 클릭 → 경로 실행";

        if (trayLabelText != null)
            trayLabelText.text = currentRound == 0 ? "연결선 4개" : "연결선 5개";

        if (statusText != null)
            statusText.text = "오른쪽 연결선 하나를 클릭하세요.";

        if (executeButton != null)
            executeButton.interactable = true;

        CreateRails();
        CreateGhostSlots();
        CreateLettersAndDestinations();

        if (currentRound == 0)
        {
            CreateFixedConnector(0, 0);
            CreateFixedConnector(4, 2);

            CreateMovableConnector(1, 0, 0, 4);
            CreateMovableConnector(2, 1, 1, 4);
            CreateMovableConnector(3, 1, 2, 4);
            CreateMovableConnector(5, 2, 3, 4);
        }
        else
        {
            CreateFixedConnector(0, 0);
            CreateFixedConnector(4, 2);
            CreateFixedConnector(6, 3);

            CreateMovableConnector(1, 0, 0, 5);
            CreateMovableConnector(2, 1, 1, 5);
            CreateMovableConnector(3, 1, 2, 5);
            CreateMovableConnector(5, 2, 3, 5);
            CreateMovableConnector(7, 3, 4, 5);
        }

        RefreshSelectionVisuals();
        UpdateTimerText();
    }

    private float LaneX(int lane)
    {
        return (lane - 2) * railSpacing;
    }

    private float GapX(int gap)
    {
        return (LaneX(gap) + LaneX(gap + 1)) * 0.5f;
    }

    private float RowY(int row)
    {
        float step = (topY - bottomY) / (rungCount + 1f);
        return topY - step * (row + 1);
    }

    private void CreateRails()
    {
        for (int i = 0; i < 5; i++)
        {
            CreateImage(
                $"VerticalRail_{i + 1}",
                ladderArea,
                new Vector2(LaneX(i), 0f),
                new Vector2(railSprite != null ? 16f : 7f, topY - bottomY),
                railSprite != null ? Color.white : railColor,
                false,
                railSprite
            );
        }
    }

    private void CreateGhostSlots()
    {
        for (int row = 0; row < rungCount; row++)
        {
            CreateText(
                $"RowNumber_{row + 1}",
                ladderArea,
                $"{row + 1}",
                new Vector2(LaneX(0) - 55f, RowY(row)),
                new Vector2(35f, 35f),
                20f,
                railColor
            );

            for (int gap = 0; gap < 4; gap++)
            {
                Image slot = CreateImage(
                    $"Slot_R{row + 1}_G{gap + 1}",
                    ladderArea,
                    new Vector2(GapX(gap), RowY(row)),
                    slotSprite != null
                        ? new Vector2(railSpacing - 26f, 36f)
                        : new Vector2(railSpacing + 5f, 32f),
                    ghostColor,
                    true,
                    slotSprite
                );

                ghostSlots[row, gap] = slot;

                Button slotButton = slot.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slot;
                slotButton.transition = Selectable.Transition.None;

                int capturedRow = row;
                int capturedGap = gap;
                slotButton.onClick.AddListener(
                    () => OnSlotClicked(capturedRow, capturedGap)
                );
            }
        }
    }

    private void CreateLettersAndDestinations()
    {
        for (int i = 0; i < 5; i++)
        {
            Sprite letterSprite = GetLetterSprite(i);

            if (letterSprite != null)
            {
                // 글자 박스 이미지 (오른쪽 화살표 부분만큼 중심을 옮겨서 박스가 세로줄 위에 오게 함)
                float boxWidth = GetLetterBoxWidth();
                Vector2 spriteSize = letterSprite.rect.size * letterScale;
                float arrowOffset = (letterSprite.rect.width - boxWidth) * 0.5f * letterScale;

                CreateImage(
                    $"TopLetterBox_{i + 1}",
                    ladderArea,
                    new Vector2(LaneX(i) + arrowOffset, topY + 34f),
                    spriteSize,
                    Color.white,
                    false,
                    letterSprite
                );
            }
            else
            {
            CreateText(
                $"TopLetter_{i + 1}",
                ladderArea,
                startLetters[i].ToString(),
                new Vector2(LaneX(i), topY + 34f),
                new Vector2(75f, 55f),
                42f,
                letterColor
            );
            }

            if (numberBoxSprite != null)
            {
                CreateImage(
                    $"DestinationBox_{i + 1}",
                    ladderArea,
                    new Vector2(LaneX(i), bottomY - 30f),
                    numberBoxSprite.rect.size * letterScale,
                    Color.white,
                    false,
                    numberBoxSprite
                );
            }

            destinationLabels[i] = CreateText(
                $"Destination_{i + 1}",
                ladderArea,
                $"{i + 1}",
                new Vector2(LaneX(i), bottomY - 30f),
                new Vector2(75f, 50f),
                36f,
                letterColor
            );
        }
    }

    private Sprite GetLetterSprite(int index)
    {
        if (letterSprites == null || index < 0 || index >= letterSprites.Length)
            return null;

        return letterSprites[index];
    }

    private float GetLetterBoxWidth()
    {
        float width = float.MaxValue;

        if (letterSprites != null)
        {
            foreach (Sprite sprite in letterSprites)
            {
                if (sprite != null)
                    width = Mathf.Min(width, sprite.rect.width);
            }
        }

        return width == float.MaxValue ? 0f : width;
    }

    private void CreateFixedConnector(int row, int gap)
    {
        Image image = CreateImage(
            $"FixedConnector_{row + 1}",
            ladderArea,
            new Vector2(GapX(gap), RowY(row)),
            new Vector2(railSpacing + 10f, connectorSprite != null ? connectorHeight : 12f),
            fixedColor,
            false,
            connectorSprite
        );

        connectors[row] = new Connector
        {
            row = row,
            gap = gap,
            targetGap = gap,
            isFixed = true,
            rect = image.rectTransform,
            barImage = image
        };
    }

    private void CreateMovableConnector(int row, int targetGap, int trayIndex, int trayCount)
    {
        float trayY = ((trayCount - 1) * 0.5f - trayIndex) * trayLineSpacing;

        Image handle = CreateImage(
            $"SelectableConnector_{row + 1}",
            connectorTray,
            new Vector2(0f, trayY),
            new Vector2(connectorSprite != null ? 240f : 170f, 48f),
            new Color(1f, 1f, 1f, 0.08f),
            true
        );

        Image bar = CreateImage(
            "VisibleBar",
            handle.rectTransform,
            Vector2.zero,
            new Vector2(140f, 11f),
            movableColor,
            false,
            connectorSprite
        );

        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(1f, 0.5f);

        float barHalf = GetConnectorHalfHeight();

        if (connectorSprite != null)
        {
            // 번호는 왼쪽, 연결선 이미지는 오른쪽에 배치
            barRect.offsetMin = new Vector2(48f, -barHalf);
            barRect.offsetMax = new Vector2(-10f, barHalf);
        }
        else
        {
            barRect.offsetMin = new Vector2(14f, -barHalf);
            barRect.offsetMax = new Vector2(-14f, barHalf);
        }

        TMP_Text number = CreateText(
            "ConnectorNumber",
            handle.rectTransform,
            $"{row + 1}",
            new Vector2(connectorSprite != null ? -95f : 0f, 0f),
            new Vector2(55f, 38f),
            24f,
            Color.white
        );

        Connector connector = new Connector
        {
            row = row,
            gap = -1,
            targetGap = targetGap,
            isFixed = false,
            rect = handle.rectTransform,
            handleImage = handle,
            barImage = bar,
            numberLabel = number
        };

        connectors[row] = connector;
        movableConnectors.Add(connector);

        Button selectButton = handle.gameObject.AddComponent<Button>();
        selectButton.targetGraphic = handle;
        selectButton.transition = Selectable.Transition.None;

        int capturedRow = row;
        selectButton.onClick.AddListener(() => SelectConnector(capturedRow));
    }

    private float GetConnectorHalfHeight()
    {
        return connectorSprite != null ? connectorHeight * 0.5f : 5.5f;
    }

    private void SelectConnector(int row)
    {
        if (!isGameActive || isAnimating || isClearing)
            return;

        if (!connectors.TryGetValue(row, out Connector connector) || connector.isFixed)
            return;

        selectedRow = selectedRow == row ? -1 : row;
        RefreshSelectionVisuals();

        if (statusText != null)
        {
            statusText.text = selectedRow < 0
                ? "선택을 취소했습니다. 다른 연결선을 클릭하세요."
                : $"{row + 1}번 선 선택! 사다리의 {row + 1}번째 가로줄에서 점선 연결 칸을 클릭하세요.";
        }
    }

    private void OnSlotClicked(int row, int gap)
    {
        if (!isGameActive || isAnimating || isClearing)
            return;

        if (selectedRow < 0)
        {
            if (statusText != null)
                statusText.text = "먼저 오른쪽의 연결선을 클릭하세요.";
            return;
        }

        if (selectedRow != row)
        {
            if (statusText != null)
                statusText.text = $"선택한 선은 {selectedRow + 1}번째 가로줄에만 설치할 수 있어요.";
            return;
        }

        Connector connector = connectors[selectedRow];
        PlaceConnector(connector, gap);
        int installedRow = selectedRow;
        selectedRow = -1;
        RefreshSelectionVisuals();

        if (statusText != null)
            statusText.text = $"{installedRow + 1}번 가로선을 {gap + 1}번과 {gap + 2}번 세로줄 사이에 설치했습니다.";
    }

    private void RefreshSelectionVisuals()
    {
        if (ghostSlots != null)
        {
            for (int row = 0; row < rungCount; row++)
            {
                for (int gap = 0; gap < 4; gap++)
                {
                    if (ghostSlots[row, gap] != null)
                    {
                        ghostSlots[row, gap].color = row == selectedRow
                            ? new Color(selectionColor.r, selectionColor.g, selectionColor.b, slotSprite != null ? 1f : 0.34f)
                            : ghostColor;
                    }
                }
            }
        }

        foreach (Connector connector in movableConnectors)
        {
            if (connector.handleImage != null)
            {
                connector.handleImage.color = connector.row == selectedRow
                    ? new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.3f)
                    : new Color(1f, 1f, 1f, 0.08f);
            }

            if (connector.barImage != null)
            {
                connector.barImage.color = connector.row == selectedRow
                    ? selectionColor
                    : movableColor;
            }
        }
    }

    private void PlaceConnector(Connector connector, int gap)
    {
        if (connector == null || connector.isFixed)
            return;

        connector.gap = gap;
        connector.rect.SetParent(ladderArea, false);
        connector.rect.anchorMin = new Vector2(0.5f, 0.5f);
        connector.rect.anchorMax = new Vector2(0.5f, 0.5f);
        connector.rect.pivot = new Vector2(0.5f, 0.5f);
        connector.rect.sizeDelta = new Vector2(railSpacing + 10f, 42f);
        connector.rect.anchoredPosition = new Vector2(GapX(gap), RowY(connector.row));
        connector.rect.SetAsLastSibling();

        if (connector.barImage != null)
        {
            RectTransform barRect = connector.barImage.rectTransform;
            float barHalf = GetConnectorHalfHeight();
            barRect.offsetMin = new Vector2(-5f, -barHalf);
            barRect.offsetMax = new Vector2(5f, barHalf);
        }

        if (connector.numberLabel != null)
            connector.numberLabel.gameObject.SetActive(false);
    }

    public bool CanDrag(int row)
    {
        return false;
    }

    public bool TryDropConnector(int row, RectTransform rect, PointerEventData eventData)
    {
        return false;
    }

    private void OnExecuteClicked()
    {
        if (!isGameActive || isAnimating || isClearing)
            return;

        StartCoroutine(ExecutePathRoutine());
    }

    private IEnumerator ExecutePathRoutine()
    {
        isAnimating = true;
        selectedRow = -1;
        RefreshSelectionVisuals();

        if (executeButton != null)
            executeButton.interactable = false;

        if (statusText != null)
            statusText.text = "경로 검사 중...";

        TMP_Text[] movingLetters = new TMP_Text[5];

        for (int i = 0; i < 5; i++)
        {
            movingLetters[i] = CreateText(
                $"MovingLetter_{i + 1}",
                ladderArea,
                startLetters[i].ToString(),
                new Vector2(LaneX(i), topY - 10f),
                new Vector2(75f, 55f),
                38f,
                letterColor
            );
        }

        yield return new WaitForSeconds(0.2f);

        for (int row = 0; row < rungCount; row++)
        {
            yield return MoveLettersDown(movingLetters, RowY(row), movementDuration);

            if (!connectors.TryGetValue(row, out Connector connector) || connector.gap < 0)
                continue;

            int gap = connector.gap;
            TMP_Text leftLetter = movingLetters[gap];
            TMP_Text rightLetter = movingLetters[gap + 1];

            yield return SwapLetters(leftLetter, rightLetter, movementDuration);
            movingLetters[gap] = rightLetter;
            movingLetters[gap + 1] = leftLetter;
        }

        yield return MoveLettersDown(movingLetters, bottomY + 12f, movementDuration);

        string result = "";
        for (int i = 0; i < 5; i++)
            result += movingLetters[i].text;

        bool correct = result == "START";

        if (correct)
        {
            if (statusText != null)
                statusText.text = "도착 결과: START ? 정답입니다!";
        }
        else
        {
            HandleWrongResult(result);
        }

        yield return new WaitForSeconds(1.2f);

        foreach (TMP_Text letter in movingLetters)
        {
            if (letter != null)
                Destroy(letter.gameObject);
        }

        if (correct)
        {
            if (currentRound == 0)
            {
                currentRound = 1;
                BuildRound();
            }
            else
            {
                StartCoroutine(ShowClearRoutine());
            }
        }
        else
        {
            isAnimating = false;
            if (executeButton != null)
                executeButton.interactable = true;
        }
    }

    private IEnumerator MoveLettersDown(TMP_Text[] letters, float targetY, float duration)
    {
        float startY = letters[0].rectTransform.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            foreach (TMP_Text letter in letters)
            {
                RectTransform rect = letter.rectTransform;
                Vector2 pos = rect.anchoredPosition;
                pos.y = Mathf.Lerp(startY, targetY, t);
                rect.anchoredPosition = pos;
            }

            yield return null;
        }

        foreach (TMP_Text letter in letters)
        {
            RectTransform rect = letter.rectTransform;
            Vector2 pos = rect.anchoredPosition;
            pos.y = targetY;
            rect.anchoredPosition = pos;
        }
    }

    private IEnumerator SwapLetters(TMP_Text left, TMP_Text right, float duration)
    {
        RectTransform leftRect = left.rectTransform;
        RectTransform rightRect = right.rectTransform;
        Vector2 leftStart = leftRect.anchoredPosition;
        Vector2 rightStart = rightRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            leftRect.anchoredPosition = Vector2.Lerp(leftStart, rightStart, t);
            rightRect.anchoredPosition = Vector2.Lerp(rightStart, leftStart, t);
            yield return null;
        }

        leftRect.anchoredPosition = rightStart;
        rightRect.anchoredPosition = leftStart;
    }

    private void HandleWrongResult(string result)
    {
        failureCount++;
        int wrongIndex = -1;

        for (int i = 0; i < 5; i++)
        {
            if (result[i] != startLetters[i])
            {
                wrongIndex = i;
                break;
            }
        }

        if (wrongIndex >= 0 && destinationLabels[wrongIndex] != null)
            destinationLabels[wrongIndex].color = Color.red;

        if (statusText != null)
            statusText.text = $"도착: {result} ? 실행 문자 순서가 올바르지 않습니다.";

        if (failureCount == 1)
        {
            if (statusText != null)
                statusText.text += "\n빨간색 도착 칸을 확인하세요.";
        }
        else if (failureCount == 2)
        {
            if (ghostSlots.GetLength(0) > 1 && ghostSlots[1, 0] != null)
                ghostSlots[1, 0].color = hintColor;

            if (connectors.TryGetValue(0, out Connector fixedConnector))
                fixedConnector.barImage.color = hintColor;

            if (statusText != null)
                statusText.text += "\n힌트: S가 지나갈 첫 번째 경로를 확인하세요.";
        }
        else
        {
            if (statusText != null)
                statusText.text += "\n힌트: 정답 연결 위치가 깜빡입니다.";

            StartCoroutine(FlashCorrectSlot());
        }
    }

    private IEnumerator FlashCorrectSlot()
    {
        Connector target = null;

        foreach (Connector connector in movableConnectors)
        {
            if (connector.gap != connector.targetGap)
            {
                target = connector;
                break;
            }
        }

        if (target == null)
            yield break;

        Image slot = ghostSlots[target.row, target.targetGap];
        if (slot == null)
            yield break;

        for (int i = 0; i < 6; i++)
        {
            slot.color = hintColor;
            yield return new WaitForSeconds(0.25f);
            slot.color = ghostColor;
            yield return new WaitForSeconds(0.25f);
        }

        slot.color = hintColor;
    }

    private void AutoPlaceOneConnector()
    {
        foreach (Connector connector in movableConnectors)
        {
            if (connector.gap == connector.targetGap)
                continue;

            PlaceConnector(connector, connector.targetGap);
            selectedRow = -1;
            RefreshSelectionVisuals();

            if (statusText != null)
                statusText.text = "90초 힌트: 연결선 하나가 자동으로 설치되었습니다.";

            return;
        }
    }

    [Header("Clear")]
    [Tooltip("마지막 라운드 성공 후 자동으로 클리어되기까지 기다리는 시간")]
    public float autoClearDelay = 0.8f;

    private IEnumerator ShowClearRoutine()
    {
        isClearing = true;

        // 패널/START 버튼 없이 자연스럽게 자동 클리어
        if (clearPanel != null)
            clearPanel.SetActive(false);

        if (executeButton != null)
            executeButton.interactable = false;

        if (statusText != null)
            statusText.text = "실행 문자 START 도착 완료!";

        yield return new WaitForSeconds(autoClearDelay);

        Success();
    }

    private void OnClearStartClicked()
    {
        if (!isGameActive || !isClearing)
            return;

        Success();
    }

    protected override void GiveHint()
    {
        if (statusText != null && !isAnimating)
            statusText.text = "힌트: 연결선을 클릭한 다음 해당 줄의 점선 칸을 클릭하세요.";
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void ClearGeneratedObjects()
    {
        for (int i = generatedObjects.Count - 1; i >= 0; i--)
        {
            if (generatedObjects[i] != null)
                Destroy(generatedObjects[i]);
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

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        RectTransform parent,
        string content,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(parent, false);
        generatedObjects.Add(obj);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.font = titleText != null && titleText.font != null
            ? titleText.font
            : TMP_Settings.defaultFontAsset;

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private void OnDestroy()
    {
        if (executeButton != null)
            executeButton.onClick.RemoveListener(OnExecuteClicked);

        if (clearStartButton != null)
            clearStartButton.onClick.RemoveListener(OnClearStartClicked);
    }
}
