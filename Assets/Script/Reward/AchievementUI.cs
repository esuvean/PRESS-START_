
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PRESS START - 업적 목록 UI
// 기존 Inspector 연결을 유지하면서 카드 배치를 수정한 버전

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
    public float rowHeight = 125f;
    public float titleFontSize = 35f;
    public float descriptionFontSize = 25f;

    [Header("Optional - Neon Status Art")]
    public Sprite unlockedStatusSprite;
    public Sprite lockedStatusSprite;

    [Header("Optional - New Sort Buttons")]
    public Button latestSortButton;
    public Button earnedSortButton;

    [Header("Optional - Category Dropdown")]
    public TMP_Dropdown categoryDropdown;

    private enum ListFilter
    {
        All,
        Basic,
        Challenge,
        Hidden
    }

    private enum SortMode
    {
        Default,
        Latest,
        Earned
    }

    private ListFilter currentFilter = ListFilter.All;
    private SortMode currentSort = SortMode.Default;

    private AchievementManager subscribedManager;

    private readonly List<GameObject> generatedRows =
        new List<GameObject>();

    private readonly Color accent =
        new Color32(0, 232, 245, 255);

    private readonly Color muted =
        new Color32(150, 171, 201, 255);

    private readonly Color cardColor =
        new Color32(5, 20, 41, 255);

    private readonly Color descriptionColor =
        new Color32(187, 205, 230, 255);

    private readonly Color normalButtonColor =
        new Color32(14, 37, 58, 255);

    private readonly Color selectedButtonColor =
        new Color32(12, 102, 121, 255);

    // =====================================
    // 초기화
    // =====================================

    private void Awake()
    {
        SetupButtons();
    }

    private void Start()
    {
        ConnectManager();

        if (achievementPanel != null &&
            achievementPanel.activeInHierarchy)
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
    // 업적 매니저 연결
    // =====================================

    private void ConnectManager()
    {
        AchievementManager manager =
            AchievementManager.Instance;

        if (manager == subscribedManager)
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
    // 버튼 이벤트
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

        if (latestSortButton != null)
        {
            latestSortButton.onClick.AddListener(
                () => SetSort(SortMode.Latest)
            );
        }

        if (earnedSortButton != null)
        {
            earnedSortButton.onClick.AddListener(
                () => SetSort(SortMode.Earned)
            );
        }

        if (categoryDropdown != null)
        {
            categoryDropdown.onValueChanged.AddListener(
                OnCategoryChanged
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
                "[AchievementUI] Achievement Panel 연결 필요!",
                this
            );

            return;
        }

        ConnectManager();

        achievementPanel.SetActive(true);
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
    // 필터
    // =====================================

    private void SetFilter(ListFilter filter)
    {
        currentFilter = filter;

        UpdateFilterButtons();

        if (categoryDropdown != null)
        {
            categoryDropdown.SetValueWithoutNotify(
                (int)filter
            );
        }

        Refresh();
    }

    private void OnCategoryChanged(int value)
    {
        if (value < 0 || value > 3)
            return;

        currentFilter = (ListFilter)value;

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
    // 정렬
    // =====================================

    private void SetSort(SortMode sort)
    {
        currentSort = sort;
        Refresh();
    }

    // =====================================
    // 업적 목록 새로고침
    // =====================================

    public void Refresh()
    {
        ConnectManager();

        AchievementManager manager =
            AchievementManager.Instance;

        if (manager == null)
        {
            if (progressText != null)
                progressText.text = "0 / 30";

            if (progressBarFill != null)
                progressBarFill.fillAmount = 0f;

            ClearRows();
            return;
        }

        int unlocked = manager.GetUnlockedCount();
        int total = manager.Achievements.Count;

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
                "[AchievementUI] Content 연결 필요!",
                this
            );

            return;
        }

        // 업적 카드가 좁게 표시되는 문제 해결
        ConfigureContentAndScrollView();

        ClearRows();

        List<AchievementInfo> list =
            new List<AchievementInfo>();

        foreach (AchievementInfo info
                 in manager.Achievements)
        {
            if (ShouldDisplay(info))
                list.Add(info);
        }

        if (currentSort == SortMode.Latest)
        {
            // 기존 코드와 동일한 ID 역순
            list.Sort(
                (a, b) => b.id.CompareTo(a.id)
            );
        }
        else if (currentSort == SortMode.Earned)
        {
            // 획득한 업적 먼저
            list.Sort((a, b) =>
            {
                bool aUnlocked =
                    manager.IsUnlocked(a.id);

                bool bUnlocked =
                    manager.IsUnlocked(b.id);

                if (aUnlocked != bUnlocked)
                    return aUnlocked ? -1 : 1;

                return a.id.CompareTo(b.id);
            });
        }
        else
        {
            list.Sort(
                (a, b) => a.id.CompareTo(b.id)
            );
        }

        foreach (AchievementInfo info in list)
        {
            CreateAchievementRow(manager, info);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            content
        );

        Canvas.ForceUpdateCanvases();
    }

    private bool ShouldDisplay(
        AchievementInfo info)
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
    // Scroll View / Content 크기 보정
    // =====================================

    private void ConfigureContentAndScrollView()
    {
        // Content를 Viewport 가로 전체로 확장
        content.anchorMin =
            new Vector2(0f, 1f);

        content.anchorMax =
            new Vector2(1f, 1f);

        content.pivot =
            new Vector2(0.5f, 1f);

        content.sizeDelta =
            new Vector2(
                0f,
                content.sizeDelta.y
            );

        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout =
            content.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout =
                content.gameObject
                    .AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = 14f;

        layout.padding =
            new RectOffset(12, 12, 10, 10);

        layout.childAlignment =
            TextAnchor.UpperCenter;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            content.GetComponent<ContentSizeFitter>();

        if (fitter == null)
        {
            fitter =
                content.gameObject
                    .AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll =
            content.GetComponentInParent<ScrollRect>();

        if (scroll == null)
            return;

        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;

        // 기본 회색 Scroll View 배경만 변경
        Image scrollBackground =
            scroll.GetComponent<Image>();

        if (scrollBackground != null &&
            (scrollBackground.sprite == null ||
             scrollBackground.sprite.name == "Background"))
        {
            scrollBackground.color =
                new Color32(5, 16, 35, 255);
        }

        RectTransform viewport =
            scroll.viewport;

        if (viewport == null)
        {
            viewport =
                content.parent as RectTransform;
        }

        if (viewport != null)
        {
            scroll.viewport = viewport;

            Image viewportImage =
                viewport.GetComponent<Image>();

            if (viewportImage != null &&
                (viewportImage.sprite == null ||
                 viewportImage.sprite.name == "UISprite" ||
                 viewportImage.sprite.name == "Background"))
            {
                viewportImage.color = Color.clear;
            }
        }
    }

    // =====================================
    // 이전 카드 제거
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
    // 업적 카드 생성
    // =====================================

    private void CreateAchievementRow(
        AchievementManager manager,
        AchievementInfo info)
    {
        bool unlocked =
            manager.IsUnlocked(info.id);

        float height =
            Mathf.Max(110f, rowHeight);

        GameObject row = NewUIObject(
            "Achievement_" + info.id.ToString("00"),
            content,
            typeof(Image),
            typeof(LayoutElement)
        );

        RectTransform rowRect =
            row.GetComponent<RectTransform>();

        rowRect.anchorMin =
            new Vector2(0f, 1f);

        rowRect.anchorMax =
            new Vector2(1f, 1f);

        rowRect.pivot =
            new Vector2(0.5f, 0.5f);

        rowRect.sizeDelta =
            new Vector2(0f, height);

        LayoutElement element =
            row.GetComponent<LayoutElement>();

        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        element.flexibleWidth = 1f;

        Image bg = row.GetComponent<Image>();

        Sprite sprite = unlocked
            ? unlockedRowSprite
            : lockedRowSprite;

        if (sprite != null)
        {
            bg.sprite = sprite;
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
        }
        else
        {
            bg.color = cardColor;

            ApplyOutline(
                row,
                unlocked ? accent : muted,
                3f
            );
        }

        bg.raycastTarget = false;

        // =============================
        // 왼쪽 업적 아이콘
        // =============================

        Image iconFrame = MakeImage(
            "IconFrame",
            row.transform,
            new Vector2(0f, 0.5f),
            new Vector2(66f, 0f),
            new Vector2(84f, 84f),
            new Color32(9, 29, 53, 255)
        );

        ApplyOutline(
            iconFrame.gameObject,
            unlocked ? Color.white : muted,
            2f
        );

        Sprite iconSprite = unlocked
            ? manager.GetIcon(info.id)
            : manager.lockedIcon;

        if (iconSprite != null)
        {
            Image art = MakeImage(
                "AchievementIcon",
                iconFrame.transform,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(70f, 70f),
                Color.white
            );

            art.sprite = iconSprite;
            art.preserveAspect = true;
        }
        else
        {
            TMP_Text fallback = MakeText(
                "FallbackIcon",
                iconFrame.transform,
                unlocked
                    ? info.id.ToString("00")
                    : "?",
                37f,
                unlocked ? Color.white : muted,
                new Vector2(70f, 70f)
            );

            fallback.alignment =
                TextAlignmentOptions.Center;
        }

        // =============================
        // 아이콘과 글씨 사이 구분선
        // =============================

        MakeImage(
            "Divider",
            row.transform,
            new Vector2(0f, 0.5f),
            new Vector2(132f, 0f),
            new Vector2(3f, height - 26f),
            unlocked ? accent : muted
        );

        // =============================
        // 업적 이름과 획득 조건
        // =============================

        bool hideHidden =
            info.category ==
                AchievementCategory.Hidden &&
            !unlocked;

        string title = hideHidden
            ? "???"
            : info.title;

        string description = hideHidden
            ? "획득 조건: 알 수 없음"
            : info.condition;

        TMP_Text titleText = MakeStretchText(
            "AchievementTitle",
            row.transform,
            title,
            Mathf.Max(15f, titleFontSize),
            unlocked
                ? Color.white
                : new Color32(228, 236, 248, 255),
            163f,
            120f,
            10f,
            49f
        );

        titleText.fontStyle = FontStyles.Bold;

        MakeStretchText(
            "AchievementCondition",
            row.transform,
            description,
            Mathf.Max(12f, descriptionFontSize),
            descriptionColor,
            163f,
            120f,
            -47f,
            -9f
        );

        // =============================
        // 오른쪽 획득 / 잠금 아이콘
        // =============================

        CreateStatus(
            row.transform,
            unlocked
        );

        generatedRows.Add(row);
    }

    // =====================================
    // 업적 획득 상태 이미지
    // =====================================

    private void CreateStatus(
        Transform parent,
        bool unlocked)
    {
        Image frame = MakeImage(
            "AchievementStatus",
            parent,
            new Vector2(1f, 0.5f),
            new Vector2(-58f, 0f),
            new Vector2(64f, 64f),
            new Color32(3, 17, 34, 255)
        );

        ApplyOutline(
            frame.gameObject,
            unlocked ? accent : muted,
            2f
        );

        Sprite statusSprite = unlocked
            ? unlockedStatusSprite
            : lockedStatusSprite;

        // 아트팀 이미지가 연결되어 있다면 표시
        if (statusSprite != null)
        {
            Image art = MakeImage(
                "StatusIcon",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(52f, 52f),
                Color.white
            );

            art.sprite = statusSprite;
            art.preserveAspect = true;

            return;
        }

        // 이미지가 없는 동안 사용하는 임시 표시
        if (unlocked)
        {
            Image shortLine = MakeImage(
                "CheckShort",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(-10f, -4f),
                new Vector2(19f, 7f),
                accent
            );

            shortLine.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, -45f);

            Image longLine = MakeImage(
                "CheckLong",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(8f, 2f),
                new Vector2(33f, 7f),
                accent
            );

            longLine.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, 45f);
        }
        else
        {
            MakeImage(
                "LockTop",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f),
                new Vector2(31f, 27f),
                muted
            );

            MakeImage(
                "LockTopHole",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 12f),
                new Vector2(17f, 17f),
                new Color32(3, 17, 34, 255)
            );

            MakeImage(
                "LockBottom",
                frame.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -11f),
                new Vector2(43f, 29f),
                muted
            );
        }
    }

    // =====================================
    // UI GameObject 생성
    // =====================================

    private GameObject NewUIObject(
        string name,
        Transform parent,
        params System.Type[] extra)
    {
        System.Type[] types =
            new System.Type[extra.Length + 2];

        types[0] = typeof(RectTransform);
        types[1] = typeof(CanvasRenderer);

        for (int i = 0; i < extra.Length; i++)
        {
            types[i + 2] = extra[i];
        }

        GameObject go =
            new GameObject(name, types);

        go.transform.SetParent(parent, false);

        return go;
    }

    // =====================================
    // Image 생성
    // =====================================

    private Image MakeImage(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 offset,
        Vector2 size,
        Color color)
    {
        GameObject go = NewUIObject(
            name,
            parent,
            typeof(Image)
        );

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;

        rt.pivot =
            new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = offset;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    // =====================================
    // TMP 텍스트 생성
    // =====================================

    private TMP_Text MakeText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        Vector2 size)
    {
        GameObject go = NewUIObject(
            name,
            parent,
            typeof(TextMeshProUGUI)
        );

        RectTransform rt =
            go.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(0.5f, 0.5f);

        rt.anchorMax =
            new Vector2(0.5f, 0.5f);

        rt.pivot =
            new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        TextMeshProUGUI text =
            go.GetComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;

        text.alignment =
            TextAlignmentOptions.MidlineLeft;

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

    // =====================================
    // 카드 너비에 따라 늘어나는 텍스트
    // =====================================

    private TMP_Text MakeStretchText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        float left,
        float right,
        float bottom,
        float top)
    {
        TMP_Text text = MakeText(
            name,
            parent,
            value,
            fontSize,
            color,
            Vector2.zero
        );

        RectTransform rt =
            text.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(0f, 0.5f);

        rt.anchorMax =
            new Vector2(1f, 0.5f);

        rt.pivot =
            new Vector2(0.5f, 0.5f);

        rt.offsetMin =
            new Vector2(left, bottom);

        rt.offsetMax =
            new Vector2(-right, top);

        text.alignment =
            TextAlignmentOptions.MidlineLeft;

        return text;
    }

    // =====================================
    // 임시 네온 테두리
    // =====================================

    private void ApplyOutline(
        GameObject go,
        Color color,
        float thickness)
    {
        Outline outline =
            go.GetComponent<Outline>();

        if (outline == null)
        {
            outline = go.AddComponent<Outline>();
        }

        outline.effectColor = color;

        outline.effectDistance =
            new Vector2(
                thickness,
                -thickness
            );

        outline.useGraphicAlpha = false;
    }
}
