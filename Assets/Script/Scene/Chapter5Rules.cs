using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 챕터5 전용 규칙 (씬에 따로 배치하지 않아도 챕터5가 시작되면 자동으로 생김)
///  - 챕터5에서 실패하면 '처음부터': 실패 횟수 +1, 챕터5 진행도 0으로, 챕터 선택 화면으로 돌아감
///  - 실패로 치는 것: 미니게임의 Fail() / 제한 시간 초과
///  - 미니게임 스크립트에서 직접 실패시키고 싶으면  Chapter5Rules.FailChapter();  한 줄만 부르면 됨
///  [에디터 테스트] 챕터5 플레이 중 F8 = 바로 실패 처리
/// </summary>
public class Chapter5Rules : MonoBehaviour
{
    [Tooltip("실패 화면을 보여 준 뒤 챕터 선택 화면으로 돌아가기까지 시간(초)")]
    public float leaveDelay = 1.8f;

    public string failTitle = "FAILED";
    public string failMessage = "처음부터 다시 시작합니다";

    private const int ChapterNumber = 5;

    private static Chapter5Rules instance;

    private ChapterManager chapterManager;
    private bool failing = false;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate();
    }

    private static void TryCreate()
    {
        ChapterManager cm = Object.FindAnyObjectByType<ChapterManager>();
        if (cm == null || cm.chapterNumber != ChapterNumber)
            return;

        if (Object.FindAnyObjectByType<Chapter5Rules>() != null)
            return;

        GameObject go = new GameObject("Chapter5Rules");
        Chapter5Rules rules = go.AddComponent<Chapter5Rules>();
        rules.chapterManager = cm;
    }


    /// <summary>챕터5를 실패 처리 (처음부터 → 챕터 선택 화면)</summary>
    public static void FailChapter()
    {
        if (instance != null)
        {
            instance.StartFail();
            return;
        }

        // 혹시 규칙 오브젝트가 없을 때도 동작하게
        SessionProgress.RegisterChapter5Failure();
        Time.timeScale = 1f;
        SessionProgress.openChapterSelectOnLoad = true;
        SceneManager.LoadScene("MainScene");
    }


    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnEnable()
    {
        MinigameBase.OnGameFailure += StartFail;
        MinigameBase.OnTimeOut += HandleTimeOut;
    }

    private void OnDisable()
    {
        MinigameBase.OnGameFailure -= StartFail;
        MinigameBase.OnTimeOut -= HandleTimeOut;
    }

    private void Start()
    {
        // 챕터5는 시간 초과 = 바로 처음부터 → 샷건 연출은 시간 초과에 안 씀 (실수 누적 샷건은 그대로)
        ShotgunController shotgun = Object.FindAnyObjectByType<ShotgunController>(FindObjectsInactive.Include);
        if (shotgun != null)
            shotgun.shotgunOnTimeOut = false;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            StartFail();
    }
#endif

    private void HandleTimeOut()
    {
        // 자체 시간 규칙이 있는 미니게임은 제외
        if (ChapterFxBootstrap.UsesOwnTimeRule(Object.FindAnyObjectByType<MinigameBase>()))
            return;

        StartFail();
    }

    private void StartFail()
    {
        if (failing)
            return;

        failing = true;

        SessionProgress.RegisterChapter5Failure();
        Debug.Log($"[Chapter5] 실패 → 처음부터 (실패 횟수 {SessionProgress.chapter5FailCount})");

        StartCoroutine(LeaveRoutine());
    }

    private IEnumerator LeaveRoutine()
    {
        // 실패 화면 (다른 화면보다 위, 클릭 막기)
        GameObject root = CreateOverlay();

        GameSfx.StartNoise(0.35f);
        yield return new WaitForSecondsRealtime(0.25f);
        GameSfx.StopNoise();

        yield return new WaitForSecondsRealtime(leaveDelay);

        Time.timeScale = 1f;
        SessionProgress.openChapterSelectOnLoad = true;

        string mainScene = chapterManager != null ? chapterManager.mainSceneName : "MainScene";
        SceneManager.LoadScene(mainScene);
    }


    // ===== 실패 화면 만들기 =====

    private GameObject CreateOverlay()
    {
        GameObject canvasGO = new GameObject("Chapter5FailOverlay", typeof(RectTransform));
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image img = bg.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        img.raycastTarget = true;

        AddText(canvasGO.transform, failTitle, new Vector2(0f, 60f), 120, new Color(1f, 0.3f, 0.45f, 1f));
        AddText(canvasGO.transform, failMessage, new Vector2(0f, -70f), 40, new Color(0.85f, 0.85f, 0.85f, 1f));

        return canvasGO;
    }

    private void AddText(Transform parent, string text, Vector2 pos, float size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1400f, size * 1.4f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetFont();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private TMP_FontAsset GetFont()
    {
        ChapterFxResources res = Resources.Load<ChapterFxResources>("ChapterFxResources");
        if (res != null && res.failFont != null)
            return res.failFont;
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11_Bold SDF.asset");
#else
        return null;
#endif
    }
}
