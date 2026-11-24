using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementPopupUI : MonoBehaviour
{
    public static AchievementPopupUI Instance { get; private set; }

    [Header("Popup UI")]
    public RectTransform popupPanel;
    public Image popupBackground;
    public Image popupIcon;

    public TMP_Text popupHeaderText;
    public TMP_Text popupCategoryText;
    public TMP_Text popupNameText;
    public TMP_Text popupDescriptionText;

    [Header("ART - Popup Backgrounds")]
    public Sprite basicPopupSprite;
    public Sprite challengePopupSprite;
    public Sprite hiddenPopupSprite;

    [Header("ART - Optional Fallback Icon")]
    public Sprite fallbackIcon;

    [Header("Animation")]
    public float displaySeconds = 3f;
    public float fadeSeconds = 0.25f;
    public float slideDistance = 55f;

    private CanvasGroup popupGroup;
    private AchievementManager subscribedManager;

    private readonly Queue<AchievementInfo> pendingPopups =
        new Queue<AchievementInfo>();

    private Coroutine popupRoutine;
    private Vector2 normalPosition;
    private Sprite originalIconSprite;

    private void Awake()
    {
        // 다른 씬에서 중복 생성되지 않도록 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 반드시 최상위 Canvas 오브젝트에 연결
        DontDestroyOnLoad(gameObject);

        Canvas canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
        }

        if (popupPanel == null)
        {
            Debug.LogError(
                "[AchievementPopupUI] Popup Panel 연결 필요",
                this
            );

            return;
        }

        popupGroup = popupPanel.GetComponent<CanvasGroup>();

        if (popupGroup == null)
        {
            popupGroup =
                popupPanel.gameObject.AddComponent<CanvasGroup>();
        }

        normalPosition = popupPanel.anchoredPosition;

        if (popupIcon != null)
            originalIconSprite = popupIcon.sprite;

        popupGroup.alpha = 0f;
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;

        popupPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (Instance == this)
            ConnectAchievementManager();
    }

    private void Start()
    {
        ConnectAchievementManager();
    }

    private void Update()
    {
        if (Instance != this)
            return;

        // AchievementManager가 나중에 생성되어도 연결
        if (subscribedManager != AchievementManager.Instance)
        {
            ConnectAchievementManager();
        }
    }

    private void OnDisable()
    {
        DisconnectAchievementManager();
    }

    private void OnDestroy()
    {
        DisconnectAchievementManager();

        if (Instance == this)
            Instance = null;
    }

    // =====================================
    // AchievementManager 이벤트 연결
    // =====================================

    private void ConnectAchievementManager()
    {
        AchievementManager manager =
            AchievementManager.Instance;

        if (subscribedManager == manager)
            return;

        DisconnectAchievementManager();

        if (manager == null)
            return;

        subscribedManager = manager;

        subscribedManager.OnAchievementUnlocked +=
            OnAchievementUnlocked;

        Debug.Log(
            "[AchievementPopupUI] 업적 이벤트 연결 완료"
        );
    }

    private void DisconnectAchievementManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnAchievementUnlocked -=
            OnAchievementUnlocked;

        subscribedManager = null;
    }

    // =====================================
    // 업적 획득 시 자동 실행
    // =====================================

    private void OnAchievementUnlocked(
        AchievementInfo achievement)
    {
        if (achievement == null)
            return;

        pendingPopups.Enqueue(achievement);

        // 여러 업적을 동시에 획득해도
        // 알림이 하나씩 순서대로 나타남
        if (popupRoutine == null && isActiveAndEnabled)
        {
            popupRoutine = StartCoroutine(
                ShowPopupQueue()
            );
        }
    }

    // =====================================
    // 업적 종류별 팝업 디자인
    // =====================================

    private void ApplyPopupContent(AchievementInfo achievement)
    {
        Sprite backgroundSprite = null;

        Color backgroundColor =
            new Color32(16, 45, 80, 255);

        Color accentColor =
            new Color32(101, 226, 230, 255);

        string categoryLabel = "BASIC";

        switch (achievement.category)
        {
            case AchievementCategory.Basic:
                backgroundSprite = basicPopupSprite;

                backgroundColor =
                    new Color32(16, 45, 80, 255);

                accentColor =
                    new Color32(101, 226, 230, 255);

                categoryLabel = "BASIC";
                break;

            case AchievementCategory.Challenge:
                backgroundSprite = challengePopupSprite;

                backgroundColor =
                    new Color32(15, 53, 62, 255);

                accentColor =
                    new Color32(97, 235, 188, 255);

                categoryLabel = "CHALLENGE";
                break;

            case AchievementCategory.Hidden:
                backgroundSprite = hiddenPopupSprite;

                backgroundColor =
                    new Color32(55, 24, 65, 255);

                accentColor =
                    new Color32(255, 144, 207, 255);

                categoryLabel = "HIDDEN";
                break;
        }

        // 팝업 배경
        if (popupBackground != null)
        {
            popupBackground.sprite = backgroundSprite;
            popupBackground.type = Image.Type.Simple;

            popupBackground.color =
                backgroundSprite != null
                    ? Color.white
                    : backgroundColor;
        }

        // 업적 아이콘
        if (popupIcon != null)
        {
            Sprite icon = null;

            if (AchievementManager.Instance != null)
            {
                icon = AchievementManager.Instance.GetIcon(
                    achievement.id
                );
            }

            if (icon == null)
                icon = fallbackIcon;

            if (icon == null)
                icon = originalIconSprite;

            popupIcon.sprite = icon;
            popupIcon.preserveAspect = true;

            popupIcon.color =
                icon != null
                    ? Color.white
                    : accentColor;
        }

        // 텍스트 설정
        if (popupHeaderText != null)
        {
            popupHeaderText.text =
                "ACHIEVEMENT UNLOCKED!";

            popupHeaderText.color = accentColor;
        }

        if (popupCategoryText != null)
        {
            popupCategoryText.text = categoryLabel;
            popupCategoryText.color = accentColor;
        }

        if (popupNameText != null)
        {
            popupNameText.text = achievement.title;
            popupNameText.color = Color.white;
        }

        if (popupDescriptionText != null)
        {
            popupDescriptionText.text =
                achievement.condition;

            popupDescriptionText.color =
                new Color32(184, 208, 231, 255);
        }
    }

    // =====================================
    // 알림 순서대로 표시
    // =====================================

    private IEnumerator ShowPopupQueue()
    {
        if (popupPanel == null || popupGroup == null)
        {
            popupRoutine = null;
            yield break;
        }

        while (pendingPopups.Count > 0)
        {
            AchievementInfo achievement =
                pendingPopups.Dequeue();

            ApplyPopupContent(achievement);

            popupPanel.gameObject.SetActive(true);

            popupGroup.alpha = 0f;
            popupGroup.blocksRaycasts = false;
            popupGroup.interactable = false;

            popupPanel.anchoredPosition =
                normalPosition +
                new Vector2(slideDistance, 0f);

            // 등장 애니메이션
            yield return StartCoroutine(
                AnimatePopup(true)
            );

            // 표시 유지
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.1f, displaySeconds)
            );

            // 사라지는 애니메이션
            yield return StartCoroutine(
                AnimatePopup(false)
            );

            popupPanel.gameObject.SetActive(false);

            if (pendingPopups.Count > 0)
            {
                yield return new WaitForSecondsRealtime(
                    0.15f
                );
            }
        }

        popupRoutine = null;
    }

    // =====================================
    // 페이드 + 슬라이드 애니메이션
    // =====================================

    private IEnumerator AnimatePopup(bool appearing)
    {
        float duration =
            Mathf.Max(0.01f, fadeSeconds);

        float elapsed = 0f;

        Vector2 hiddenPosition =
            normalPosition +
            new Vector2(slideDistance, 0f);

        Vector2 startPosition = appearing
            ? hiddenPosition
            : normalPosition;

        Vector2 endPosition = appearing
            ? normalPosition
            : hiddenPosition;

        float startAlpha = appearing ? 0f : 1f;
        float endAlpha = appearing ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // 부드러운 이동
            float smoothT =
                t * t * (3f - 2f * t);

            popupPanel.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    endPosition,
                    smoothT
                );

            popupGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    smoothT
                );

            yield return null;
        }

        popupPanel.anchoredPosition = endPosition;
        popupGroup.alpha = endAlpha;
    }

    // =====================================
    // 테스트 전용
    // 실제 업적 획득 기록은 변경하지 않음
    // =====================================

    [ContextMenu("TEST - Basic Achievement Popup")]
    public void TestBasicPopup()
    {
        TestPopup(1, AchievementCategory.Basic);
    }

    [ContextMenu("TEST - Challenge Achievement Popup")]
    public void TestChallengePopup()
    {
        TestPopup(7, AchievementCategory.Challenge);
    }

    [ContextMenu("TEST - Hidden Achievement Popup")]
    public void TestHiddenPopup()
    {
        TestPopup(29, AchievementCategory.Hidden);
    }

    private void TestPopup(
        int achievementId,
        AchievementCategory category)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "[AchievementPopupUI] Play 모드에서 테스트하세요."
            );

            return;
        }

        AchievementInfo info = null;

        if (AchievementManager.Instance != null)
        {
            info = AchievementManager.Instance
                .GetAchievement(achievementId);
        }

        if (info == null)
        {
            info = new AchievementInfo(
                achievementId,
                "TEST ACHIEVEMENT",
                "팝업 표시 테스트",
                category
            );
        }

        OnAchievementUnlocked(info);
    }
}