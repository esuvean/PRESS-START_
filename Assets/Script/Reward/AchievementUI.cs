
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUI : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject achievementPanel;
    public RectTransform content;

    [Header("Progress UI")]
    public TMP_Text progressText;
    public Image progressBarFill;

    [Header("Buttons")]
    public Button closeButton;
    public Button allButton;
    public Button basicButton;
    public Button challengeButton;
    public Button hiddenButton;

    [Header("ART - List Row Sprites")]
    public Sprite unlockedRowSprite;
    public Sprite lockedRowSprite;

    [Header("Optional Font")]
    public TMP_FontAsset listFont;

    [Header("List Settings")]
    public float rowHeight = 100f;
    public float titleFontSize = 27f;
    public float descriptionFontSize = 21f;

    private enum ListFilter
    {
        All,
        Basic,
        Challenge,
        Hidden
    }

    private ListFilter currentFilter = ListFilter.All;

    private AchievementManager subscribedManager;

    private readonly List<GameObject> generatedRows =
        new List<GameObject>();

    private readonly Color normalButtonColor =
        new Color32(28, 42, 69, 255);

    private readonly Color selectedButtonColor =
        new Color32(27, 100, 119, 255);

    private readonly Color unlockedColor =
        new Color32(101, 226, 230, 255);

    private readonly Color lockedColor =
        new Color32(151, 168, 191, 255);

    private void Awake()
    {
        SetupButtons();
    }

    private void Start()
    {
        ConnectManager();

        if (achievementPanel != null &&
            achievementPanel.activeSelf)
        {
            Refresh();
        }
    }

    private void OnEnable()
    {
        ConnectManager();
    }

    private void OnDisable()
    {
        DisconnectManager();
    }

    private void OnDestroy()
    {
        DisconnectManager();
    }

    // =====================================
    // AchievementManager 이벤트 연결
    // =====================================

    private void ConnectManager()
    {
        AchievementManager manager =
            AchievementManager.Instance;

        if (subscribedManager == manager)
            return;

        DisconnectManager();

        if (manager == null)
            return;

        subscribedManager = manager;

        subscribedManager.OnAchievementUnlocked +=
            OnAchievementUnlocked;
    }

    private void DisconnectManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnAchievementUnlocked -=
            OnAchievementUnlocked;

        subscribedManager = null;
    }

    private void OnAchievementUnlocked(
        AchievementInfo achievement)
    {
        if (achievementPanel != null &&
            achievementPanel.activeInHierarchy)
        {
            Refresh();
        }
    }

    // =====================================
    // 버튼 설정
    // =====================================

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (allButton != null)
        {
            allButton.onClick.AddListener(
                () => SetFilter(ListFilter.All)
            );
        }

        if (basicButton != null)
        {
            basicButton.onClick.AddListener(
                () => SetFilter(ListFilter.Basic)
            );
        }

        if (challengeButton != null)
        {
            challengeButton.onClick.AddListener(
                () => SetFilter(ListFilter.Challenge)
            );
        }

        if (hiddenButton != null)
        {
            hiddenButton.onClick.AddListener(
                () => SetFilter(ListFilter.Hidden)
            );
        }
    }

    // =====================================
    // 업적창 열기 / 닫기
    // =====================================

    public void Open()
    {
        if (achievementPanel == null)
        {
            Debug.LogError(
                "[AchievementUI] Achievement Panel이 연결되지 않았습니다.",
                this
            );

            return;
        }

        ConnectManager();

        achievementPanel.SetActive(true);

        // 다른 메뉴 패널보다 앞에 표시
        achievementPanel.transform.SetAsLastSibling();

        SetFilter(ListFilter.All);
    }

    public void Close()
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(false);
        }
    }

    // =====================================
    // 카테고리 선택
    // =====================================

    private void SetFilter(ListFilter filter)
    {
        currentFilter = filter;

        UpdateFilterButtons();
        Refresh();
    }

    private void UpdateFilterButtons()
    {
        SetButtonColor(
            allButton,
            currentFilter == ListFilter.All
        );

        SetButtonColor(
            basicButton,
            currentFilter == ListFilter.Basic
        );

        SetButtonColor(
            challengeButton,
            currentFilter == ListFilter.Challenge
        );

        SetButtonColor(
            hiddenButton,
            currentFilter == ListFilter.Hidden
        );
    }

    private void SetButtonColor(
        Button button,
        bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = selected
                ? selectedButtonColor
                : normalButtonColor;
        }
    }

    // =====================================
    // 업적창 새로고침
    // =====================================

    public void Refresh()
    {
        ConnectManager();

        ClearRows();

        AchievementManager manager =
            AchievementManager.Instance;

        if (manager == null)
        {
            if (progressText != null)
            {
                progressText.text = "0 / 30";
            }

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
            }

            Debug.LogWarning(
                "[AchievementUI] AchievementManager를 찾지 못했습니다."
            );

            return;
        }

        int unlocked = manager.GetUnlockedCount();
        int total = manager.Achievements.Count;

        // 전체 업적 진행도
        if (progressText != null)
        {
            progressText.text =
                unlocked + " / " + total;
        }

        if (progressBarFill != null)
        {
            progressBarFill.type =
                Image.Type.Filled;

            progressBarFill.fillMethod =
                Image.FillMethod.Horizontal;

            progressBarFill.fillOrigin = 0;

            progressBarFill.fillAmount =
                total > 0
                    ? (float)unlocked / total
                    : 0f;
        }

        if (content == null)
        {
            Debug.LogError(
                "[AchievementUI] Content가 연결되지 않았습니다.",
                this
            );

            return;
        }

        foreach (AchievementInfo info in manager.Achievements)
        {
            if (!ShouldDisplay(info))
                continue;

            CreateAchievementRow(
                manager,
                info
            );
        }

        Canvas.ForceUpdateCanvases();
    }

    // =====================================
    // 필터 조건
    // =====================================

    private bool ShouldDisplay(AchievementInfo info)
    {
        switch (currentFilter)
        {
            case ListFilter.Basic:
                return info.category ==
                    AchievementCategory.Basic;

            case ListFilter.Challenge:
                return info.category ==
                    AchievementCategory.Challenge;

            case ListFilter.Hidden:
                return info.category ==
                    AchievementCategory.Hidden;

            default:
                return true;
        }
    }

    // =====================================
    // 이전 목록 제거
    // =====================================

    private void ClearRows()
    {
        foreach (GameObject row in generatedRows)
        {
            if (row == null)
                continue;

            row.SetActive(false);
            Destroy(row);
        }

        generatedRows.Clear();
    }

    // =====================================
    // 업적 한 줄 자동 생성
    // =====================================

    private void CreateAchievementRow(
        AchievementManager manager,
        AchievementInfo info)
    {
        bool unlocked =
            manager.IsUnlocked(info.id);

        GameObject row = new GameObject(
            "Achievement_" + info.id.ToString("00"),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement)
        );

        row.transform.SetParent(content, false);

        RectTransform rowRect =
            row.GetComponent<RectTransform>();

        rowRect.sizeDelta =
            new Vector2(0f, rowHeight);

        LayoutElement layout =
            row.GetComponent<LayoutElement>();

        layout.minHeight = rowHeight;
        layout.preferredHeight = rowHeight;
        layout.flexibleHeight = 0f;

        Image background =
            row.GetComponent<Image>();

        Sprite rowSprite = unlocked
            ? unlockedRowSprite
            : lockedRowSprite;

        if (rowSprite != null)
        {
            background.sprite = rowSprite;
            background.type = Image.Type.Simple;
            background.color = Color.white;
        }
        else
        {
            background.color = unlocked
                ? new Color32(27, 55, 74, 255)
                : new Color32(29, 45, 72, 255);
        }

        background.raycastTarget = false;

        // 업적 아이콘
        CreateAchievementIcon(
            row.transform,
            manager,
            info,
            unlocked
        );

        bool isHiddenLocked =
            info.category == AchievementCategory.Hidden &&
            !unlocked;

        string title = isHiddenLocked
            ? "???"
            : info.title;

        string description = isHiddenLocked
            ? "획득 조건 달성 후 해금"
            : info.condition;

        // 업적 이름
        CreateText(
            "AchievementTitle",
            row.transform,
            title,
            new Vector2(115f, 18f),
            new Vector2(570f, 40f),
            titleFontSize,
            unlocked ? Color.white : lockedColor
        );

        // 업적 조건
        CreateText(
            "AchievementCondition",
            row.transform,
            description,
            new Vector2(115f, -20f),
            new Vector2(620f, 39f),
            descriptionFontSize,
            new Color32(168, 193, 213, 255)
        );

        // 획득 여부
        TMP_Text status = CreateText(
            "AchievementStatus",
            row.transform,
            unlocked ? "?" : "LOCK",
            new Vector2(-20f, 0f),
            new Vector2(110f, 50f),
            24f,
            unlocked ? unlockedColor : lockedColor
        );

        RectTransform statusRect =
            status.GetComponent<RectTransform>();

        statusRect.anchorMin =
            new Vector2(1f, 0.5f);

        statusRect.anchorMax =
            new Vector2(1f, 0.5f);

        statusRect.pivot =
            new Vector2(1f, 0.5f);

        status.alignment =
            TextAlignmentOptions.Center;

        generatedRows.Add(row);
    }

    // =====================================
    // 아이콘 자동 생성
    // =====================================

    private void CreateAchievementIcon(
        Transform parent,
        AchievementManager manager,
        AchievementInfo info,
        bool unlocked)
    {
        GameObject iconObject = new GameObject(
            "AchievementIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        iconObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            iconObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0.5f);

        rect.anchorMax =
            new Vector2(0f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            new Vector2(55f, 0f);

        rect.sizeDelta =
            new Vector2(74f, 74f);

        Image image =
            iconObject.GetComponent<Image>();

        Sprite sprite = unlocked
            ? manager.GetIcon(info.id)
            : manager.lockedIcon;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            image.color =
                new Color32(38, 61, 92, 255);

            // 아트가 없는 동안 표시할 기본 아이콘
            TMP_Text fallback = CreateText(
                "FallbackIcon",
                iconObject.transform,
                unlocked
                    ? info.id.ToString("00")
                    : "?",
                Vector2.zero,
                new Vector2(70f, 68f),
                32f,
                unlocked ? unlockedColor : lockedColor
            );

            RectTransform fallbackRect =
                fallback.GetComponent<RectTransform>();

            fallbackRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            fallbackRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            fallbackRect.pivot =
                new Vector2(0.5f, 0.5f);

            fallback.alignment =
                TextAlignmentOptions.Center;
        }

        image.raycastTarget = false;
    }

    // =====================================
    // TMP 생성 공통 함수
    // =====================================

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color)
    {
        GameObject go = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        go.transform.SetParent(parent, false);

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0.5f);

        rect.anchorMax =
            new Vector2(0f, 0.5f);

        rect.pivot =
            new Vector2(0f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text =
            go.GetComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment =
            TextAlignmentOptions.Left;

        text.enableWordWrapping = false;
        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget = false;

        if (listFont != null)
        {
            text.font = listFont;
        }
        else if (progressText != null &&
                 progressText.font != null)
        {
            text.font = progressText.font;
        }

        return text;
    }
}
