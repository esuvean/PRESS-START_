
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

    [Header("Settings")]
    public float autoHelpTime = 90f;
    public float movementDuration = 0.22f;

    private readonly List<GameObject> generatedObjects =
        new List<GameObject>();

    private readonly Dictionary<int, Connector> connectors =
        new Dictionary<int, Connector>();

    private readonly List<Connector> movableConnectors =
        new List<Connector>();

    private Image[,] ghostSlots;
    private TMP_Text[] destinationLabels;

    private int currentRound;
    private int rungCount;
    private int failureCount;

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
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        StopAllCoroutines();

        gameName = "START 사다리";
        instruction = "가로 연결선을 배치해 START 순서를 만드세요.";

        base.StartMinigame();

        if (ladderArea == null || connectorTray == null)
        {
            Debug.LogError(
                "LadderArea 또는 ConnectorTray가 연결되지 않았습니다."
            );

            isGameActive = false;
            return;
        }

        currentRound = 0;
        failureCount = 0;
        elapsedTime = 0f;

        isAnimating = false;
        isClearing = false;
        autoHelpUsed = false;

        if (titleText != null)
        {
            titleText.text = gameName;
        }

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
        {
            clearPanel.SetActive(false);
        }

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

        if (!autoHelpUsed &&
            elapsedTime >= autoHelpTime &&
            !isAnimating)
        {
            autoHelpUsed = true;
            AutoPlaceOneConnector();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        float remainingTime =
            Mathf.Max(0f, autoHelpTime - elapsedTime);

        timerText.text =
            $"남은 시간: {Mathf.CeilToInt(remainingTime)}초";
    }

    private void BuildRound()
    {
        ClearGeneratedObjects();

        connectors.Clear();
        movableConnectors.Clear();

        failureCount = 0;
        isAnimating = false;

        rungCount = currentRound == 0 ? 6 : 8;

        railSpacing = Mathf.Min(
            190f,
            (ladderArea.rect.width - 100f) / 4f
        );

        topY = ladderArea.rect.height * 0.5f - 38f;
        bottomY = -ladderArea.rect.height * 0.5f + 38f;

        ghostSlots = new Image[rungCount, 4];
        destinationLabels = new TMP_Text[5];

        if (roundText != null)
        {
            roundText.text =
                $"ROUND {currentRound + 1} / 2";
        }

        if (instructionText != null)
        {
            instructionText.text =
                "가로선을 드래그해 배치한 뒤 경로를 실행하세요.";
        }

        if (trayLabelText != null)
        {
            trayLabelText.text = currentRound == 0
                ? "이동할 가로선 4개"
                : "이동할 가로선 5개";
        }

        if (statusText != null)
        {
            statusText.text = "";
        }

        if (executeButton != null)
        {
            executeButton.interactable = true;
        }

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
        float step =
            (topY - bottomY) / (rungCount + 1f);

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
                new Vector2(7f, topY - bottomY),
                railColor,
                false
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
                new Vector2(
                    LaneX(0) - 55f,
                    RowY(row)
                ),
                new Vector2(35f, 35f),
                20f,
                railColor
            );

            for (int gap = 0; gap < 4; gap++)
            {
                Image slot = CreateImage(
                    $"Slot_R{row + 1}_G{gap + 1}",
                    ladderArea,
                    new Vector2(
                        GapX(gap),
                        RowY(row)
                    ),
                    new Vector2(
                        railSpacing + 5f,
                        6f
                    ),
                    ghostColor,
                    false
                );

                ghostSlots[row, gap] = slot;
            }
        }
    }

    private void CreateLettersAndDestinations()
    {
        for (int i = 0; i < 5; i++)
        {
            CreateText(
                $"TopLetter_{i + 1}",
                ladderArea,
                startLetters[i].ToString(),
                new Vector2(
                    LaneX(i),
                    topY + 34f
                ),
                new Vector2(75f, 55f),
                42f,
                letterColor
            );

            destinationLabels[i] = CreateText(
                $"Destination_{i + 1}",
                ladderArea,
                $"{i + 1}",
                new Vector2(
                    LaneX(i),
                    bottomY - 30f
                ),
                new Vector2(75f, 50f),
                36f,
                letterColor
            );
        }
    }

    private void CreateFixedConnector(int row, int gap)
    {
        Image image = CreateImage(
            $"FixedConnector_{row + 1}",
            ladderArea,
            new Vector2(
                GapX(gap),
                RowY(row)
            ),
            new Vector2(
                railSpacing + 10f,
                12f
            ),
            fixedColor,
            false
        );

        Connector connector = new Connector
        {
            row = row,
            gap = gap,
            targetGap = gap,
            isFixed = true,
            rect = image.rectTransform,
            barImage = image
        };

        connectors[row] = connector;
    }

    private void CreateMovableConnector(
        int row,
        int targetGap,
        int trayIndex,
        int trayCount)
    {
        float spacing =
            trayCount == 4 ? 175f : 155f;

        float trayX =
            (trayIndex - (trayCount - 1) * 0.5f)
            * spacing;

        Image handle = CreateImage(
            $"MovableConnector_{row + 1}",
            connectorTray,
            new Vector2(trayX, 0f),
            new Vector2(140f, 42f),
            new Color(1f, 1f, 1f, 0.02f),
            true
        );

        Image bar = CreateImage(
            "VisibleBar",
            handle.rectTransform,
            Vector2.zero,
            new Vector2(140f, 11f),
            movableColor,
            false
        );

        RectTransform barRect =
            bar.rectTransform;

        barRect.anchorMin =
            new Vector2(0f, 0.5f);

        barRect.anchorMax =
            new Vector2(1f, 0.5f);

        barRect.sizeDelta =
            new Vector2(0f, 11f);

        TMP_Text number = CreateText(
            "ConnectorNumber",
            handle.rectTransform,
            $"{row + 1}",
            Vector2.zero,
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
            barImage = bar,
            numberLabel = number
        };

        connectors[row] = connector;
        movableConnectors.Add(connector);

        LadderConnectorDrag drag =
            handle.gameObject
                .AddComponent<LadderConnectorDrag>();

        RectTransform dragLayer =
            GetComponent<RectTransform>();

        drag.Initialize(
            this,
            row,
            dragLayer
        );
    }

    public bool CanDrag(int row)
    {
        if (!isGameActive ||
            isAnimating ||
            isClearing)
        {
            return false;
        }

        return connectors.ContainsKey(row) &&
               !connectors[row].isFixed;
    }

    public bool TryDropConnector(
        int row,
        RectTransform rect,
        PointerEventData eventData)
    {
        if (!CanDrag(row))
            return false;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                ladderArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPosition))
        {
            return false;
        }

        Rect bounds = ladderArea.rect;

        if (localPosition.x < bounds.xMin - 25f ||
            localPosition.x > bounds.xMax + 25f ||
            localPosition.y < bounds.yMin - 25f ||
            localPosition.y > bounds.yMax + 25f)
        {
            return false;
        }

        int closestGap = 0;
        float closestDistance = float.MaxValue;

        for (int gap = 0; gap < 4; gap++)
        {
            float distance = Mathf.Abs(
                localPosition.x - GapX(gap)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGap = gap;
            }
        }

        PlaceConnector(
            connectors[row],
            closestGap
        );

        return true;
    }

    private void PlaceConnector(
        Connector connector,
        int gap)
    {
        if (connector == null ||
            connector.isFixed)
        {
            return;
        }

        connector.gap = gap;

        connector.rect.SetParent(
            ladderArea,
            false
        );

        connector.rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        connector.rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        connector.rect.pivot =
            new Vector2(0.5f, 0.5f);

        connector.rect.sizeDelta =
            new Vector2(
                railSpacing + 10f,
                42f
            );

        connector.rect.anchoredPosition =
            new Vector2(
                GapX(gap),
                RowY(connector.row)
            );

        connector.barImage.color =
            movableColor;

        if (connector.numberLabel != null)
        {
            connector.numberLabel
                .gameObject.SetActive(false);
        }
    }

    private void OnExecuteClicked()
    {
        if (!isGameActive ||
            isAnimating ||
            isClearing)
        {
            return;
        }

        StartCoroutine(
            ExecutePathRoutine()
        );
    }

    private IEnumerator ExecutePathRoutine()
    {
        isAnimating = true;

        if (executeButton != null)
        {
            executeButton.interactable = false;
        }

        if (statusText != null)
        {
            statusText.text = "경로 검사 중...";
        }

        TMP_Text[] movingLetters =
            new TMP_Text[5];

        for (int i = 0; i < 5; i++)
        {
            movingLetters[i] = CreateText(
                $"MovingLetter_{i + 1}",
                ladderArea,
                startLetters[i].ToString(),
                new Vector2(
                    LaneX(i),
                    topY - 10f
                ),
                new Vector2(75f, 55f),
                38f,
                letterColor
            );
        }

        yield return new WaitForSeconds(0.2f);

        for (int row = 0; row < rungCount; row++)
        {
            yield return MoveLettersDown(
                movingLetters,
                RowY(row),
                movementDuration
            );

            if (!connectors.TryGetValue(
                row,
                out Connector connector))
            {
                continue;
            }

            if (connector.gap < 0)
                continue;

            int gap = connector.gap;

            TMP_Text leftLetter =
                movingLetters[gap];

            TMP_Text rightLetter =
                movingLetters[gap + 1];

            yield return SwapLetters(
                leftLetter,
                rightLetter,
                movementDuration
            );

            movingLetters[gap] =
                rightLetter;

            movingLetters[gap + 1] =
                leftLetter;
        }

        yield return MoveLettersDown(
            movingLetters,
            bottomY + 12f,
            movementDuration
        );

        string result = "";

        for (int i = 0; i < 5; i++)
        {
            result += movingLetters[i].text;
        }

        bool correct =
            result == "START";

        if (correct)
        {
            if (statusText != null)
            {
                statusText.text =
                    "도착 결과: START ? 정답입니다!";
            }
        }
        else
        {
            HandleWrongResult(result);
        }

        yield return new WaitForSeconds(1.2f);

        for (int i = 0; i < movingLetters.Length; i++)
        {
            if (movingLetters[i] != null)
            {
                Destroy(
                    movingLetters[i].gameObject
                );
            }
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
                StartCoroutine(
                    ShowClearRoutine()
                );
            }
        }
        else
        {
            isAnimating = false;

            if (executeButton != null)
            {
                executeButton.interactable = true;
            }
        }
    }

    private IEnumerator MoveLettersDown(
        TMP_Text[] letters,
        float targetY,
        float duration)
    {
        float startY =
            letters[0].rectTransform
                .anchoredPosition.y;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            for (int i = 0; i < letters.Length; i++)
            {
                RectTransform rect =
                    letters[i].rectTransform;

                Vector2 pos =
                    rect.anchoredPosition;

                pos.y = Mathf.Lerp(
                    startY,
                    targetY,
                    t
                );

                rect.anchoredPosition = pos;
            }

            yield return null;
        }

        for (int i = 0; i < letters.Length; i++)
        {
            RectTransform rect =
                letters[i].rectTransform;

            Vector2 pos =
                rect.anchoredPosition;

            pos.y = targetY;
            rect.anchoredPosition = pos;
        }
    }

    private IEnumerator SwapLetters(
        TMP_Text left,
        TMP_Text right,
        float duration)
    {
        RectTransform leftRect =
            left.rectTransform;

        RectTransform rightRect =
            right.rectTransform;

        Vector2 leftStart =
            leftRect.anchoredPosition;

        Vector2 rightStart =
            rightRect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            leftRect.anchoredPosition =
                Vector2.Lerp(
                    leftStart,
                    rightStart,
                    t
                );

            rightRect.anchoredPosition =
                Vector2.Lerp(
                    rightStart,
                    leftStart,
                    t
                );

            yield return null;
        }

        leftRect.anchoredPosition =
            rightStart;

        rightRect.anchoredPosition =
            leftStart;
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

        if (wrongIndex >= 0 &&
            destinationLabels[wrongIndex] != null)
        {
            destinationLabels[wrongIndex].color =
                Color.red;
        }

        if (statusText != null)
        {
            statusText.text =
                $"도착: {result} ? 실행 문자 순서가 올바르지 않습니다.";
        }

        if (failureCount == 1)
        {
            if (statusText != null)
            {
                statusText.text +=
                    "\n빨간색으로 표시된 도착 칸을 확인하세요.";
            }
        }
        else if (failureCount == 2)
        {
            if (ghostSlots.GetLength(0) > 1)
            {
                ghostSlots[1, 0].color =
                    hintColor;
            }

            if (connectors.TryGetValue(
                0,
                out Connector fixedConnector))
            {
                fixedConnector.barImage.color =
                    hintColor;
            }

            if (statusText != null)
            {
                statusText.text +=
                    "\n힌트: S가 지나갈 첫 번째 경로를 확인하세요.";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text +=
                    "\n힌트: 정답 연결 위치가 깜빡입니다.";
            }

            StartCoroutine(
                FlashCorrectSlot()
            );
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

        Image slot = ghostSlots[
            target.row,
            target.targetGap
        ];

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
            if (connector.gap ==
                connector.targetGap)
            {
                continue;
            }

            PlaceConnector(
                connector,
                connector.targetGap
            );

            if (statusText != null)
            {
                statusText.text =
                    "90초 힌트: 연결선 하나가 자동으로 설치되었습니다.";
            }

            return;
        }
    }

    private IEnumerator ShowClearRoutine()
    {
        isClearing = true;

        if (clearPanel == null)
        {
            Success();
            yield break;
        }

        clearPanel.SetActive(true);

        if (clearStartButton != null)
        {
            clearStartButton.interactable = false;
        }

        if (clearTitleText != null)
        {
            clearTitleText.text = "START";

            float elapsed = 0f;
            float duration = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                clearTitleText.characterSpacing =
                    Mathf.Lerp(60f, 0f, t);

                yield return null;
            }

            clearTitleText.characterSpacing = 0f;
        }

        if (clearStartButton != null)
        {
            clearStartButton.interactable = true;
        }
    }

    private void OnClearStartClicked()
    {
        if (!isGameActive || !isClearing)
            return;

        Success();
    }

    protected override void GiveHint()
    {
        if (statusText != null &&
            !isAnimating)
        {
            statusText.text =
                "힌트: 연결선은 두 글자의 이동 경로를 서로 바꿉니다.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void ClearGeneratedObjects()
    {
        for (
            int i = generatedObjects.Count - 1;
            i >= 0;
            i--
        )
        {
            if (generatedObjects[i] != null)
            {
                Destroy(
                    generatedObjects[i]
                );
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
        bool raycast)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        obj.transform.SetParent(
            parent,
            false
        );

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

        obj.transform.SetParent(
            parent,
            false
        );

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

        TextMeshProUGUI text =
            obj.GetComponent<TextMeshProUGUI>();

        if (titleText != null &&
            titleText.font != null)
        {
            text.font = titleText.font;
        }
        else
        {
            text.font =
                TMP_Settings.defaultFontAsset;
        }

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;

        text.alignment =
            TextAlignmentOptions.Center;

        text.enableAutoSizing = false;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes.Overflow;

        text.raycastTarget = false;

        return text;
    }

    private void OnDestroy()
    {
        if (executeButton != null)
        {
            executeButton.onClick.RemoveListener(
                OnExecuteClicked
            );
        }

        if (clearStartButton != null)
        {
            clearStartButton.onClick.RemoveListener(
                OnClearStartClicked
            );
        }
    }
}
