using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 챕터5 전용 규칙 (씬에 따로 배치하지 않아도 챕터5가 시작되면 자동으로 생김)
///  - 챕터5에서 실패하면 '처음부터': 실패 횟수 +1, 챕터5 진행도 0으로, 챕터 선택 화면으로 돌아감
///  - 실패로 치는 것: 한 미니게임에서 5번 틀림 (ReportMistake / Fail) / 제한 시간 초과
///    (다음 미니게임으로 넘어가면 틀린 횟수는 0부터 다시)
///  - 미니게임 순서: 지금은 MG21 → MG25 순서대로 (RandomOrder 를 true 로 바꾸면 랜덤 + 배지 5-1 ~ 5-5)
///  - 미니게임 스크립트에서 직접 실패시키고 싶으면  Chapter5Rules.FailChapter();  한 줄만 부르면 됨
///  [에디터 테스트] 챕터5 플레이 중 F8 = 바로 실패 처리
/// </summary>
public class Chapter5Rules : MonoBehaviour
{
    [Tooltip("실패 화면을 보여 준 뒤 챕터 선택 화면으로 돌아가기까지 시간(초)")]
    public float leaveDelay = 1.8f;

    public string failTitle = "FAILED";
    public string failMessage = "처음부터 다시 시작합니다";

    [Tooltip("한 미니게임에서 이만큼 틀리면 챕터5 처음부터")]
    public int maxMistakes = 5;

    private const int ChapterNumber = 5;

    // true 로 바꾸면 챕터5 미니게임이 랜덤 순서로 나오고, 왼쪽 위 배지가 나오는 순서(5-1 ~ 5-5)로 바뀜
    public const bool RandomOrder = false;

    // 이번 플레이에서 정한 챕터5 미니게임 순서 (처음부터 할 때마다 새로 섞음)
    private static List<GameObject> sessionOrder;

    private MinigameBase currentGame;
    private int mistakes;
    private TMP_Text mistakeText;
    private float mistakeFlashUntil;

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

        if (RandomOrder)
            ShuffleOrder(cm);

        GameObject go = new GameObject("Chapter5Rules");
        Chapter5Rules rules = go.AddComponent<Chapter5Rules>();
        rules.chapterManager = cm;
    }

    // 챕터5 미니게임 순서 섞기 (ChapterManager가 첫 미니게임을 만들기 전에 실행됨)
    private static void ShuffleOrder(ChapterManager cm)
    {
        if (cm.chapters == null || cm.chapters.Count == 0 || cm.chapters[0] == null ||
            cm.chapters[0].minigamePrefabs == null)
            return;

        List<GameObject> list = cm.chapters[0].minigamePrefabs;

        bool sameSet = sessionOrder != null && sessionOrder.Count == list.Count;
        if (sameSet)
        {
            foreach (GameObject g in list)
                if (!sessionOrder.Contains(g)) { sameSet = false; break; }
        }

        // 처음부터 시작할 때(진행도 0)는 새로 섞고, 중간에 이어 할 때는 같은 순서 유지
        if (!sameSet || SessionProgress.GetProgress(ChapterNumber) <= 0)
        {
            sessionOrder = new List<GameObject>(list);
            for (int i = sessionOrder.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                GameObject t = sessionOrder[i];
                sessionOrder[i] = sessionOrder[j];
                sessionOrder[j] = t;
            }
        }

        list.Clear();
        list.AddRange(sessionOrder);
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
        MinigameBase.OnMistake += HandleMistake;
        MinigameBase.OnGameFailure += HandleMistake;
        MinigameBase.OnTimeOut += HandleTimeOut;
    }

    private void OnDisable()
    {
        MinigameBase.OnMistake -= HandleMistake;
        MinigameBase.OnGameFailure -= HandleMistake;
        MinigameBase.OnTimeOut -= HandleTimeOut;
    }

    private void Start()
    {
        // 챕터5는 '5번 틀리면 처음부터' 규칙이 따로 있으므로 샷건 연출은 쓰지 않음
        ShotgunController shotgun = Object.FindAnyObjectByType<ShotgunController>(FindObjectsInactive.Include);
        if (shotgun != null)
        {
            shotgun.shotgunOnTimeOut = false;
            shotgun.enabled = false;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F8))
            StartFail();
#endif

        // 새 미니게임이 나오면: 틀린 횟수 0, 배지 번호 = 나온 순서
        MinigameBase game = Object.FindAnyObjectByType<MinigameBase>();
        if (game != currentGame)
        {
            currentGame = game;
            mistakes = 0;
            mistakeText = null;
            if (game != null)
                SetupMinigameUI(game);
        }

        if (mistakeText != null)
        {
            bool flash = Time.unscaledTime < mistakeFlashUntil;
            mistakeText.color = flash ? new Color(1f, 0.3f, 0.4f, 1f) : new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private void SetupMinigameUI(MinigameBase game)
    {
        int order = Mathf.Max(1, SessionProgress.GetProgress(ChapterNumber) + 1);

        // 왼쪽 위 MG 배지: 나오는 순서대로 5-1, 5-2, ...
        Transform badge = FindDeep(game.transform, "C5_MG");
        TMP_Text badgeText = badge != null ? badge.GetComponentInChildren<TMP_Text>(true) : null;
        if (RandomOrder && badgeText != null)
            badgeText.text = "5-" + order;

        // 틀린 횟수 표시 (오른쪽 위 타이머 아래)
        GameObject go = new GameObject("C5_MistakeCounter", typeof(RectTransform));
        go.layer = game.gameObject.layer;
        go.transform.SetParent(game.transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(690f, 400f);
        rt.sizeDelta = new Vector2(300f, 40f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (badgeText != null)
        {
            t.font = badgeText.font;
            t.fontSharedMaterial = badgeText.fontSharedMaterial;
        }
        t.fontSize = 24f;
        t.alignment = TextAlignmentOptions.Right;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        mistakeText = t;
        UpdateMistakeText();
    }

    private void UpdateMistakeText()
    {
        if (mistakeText != null)
            mistakeText.text = "ERROR " + mistakes + " / " + maxMistakes;
    }

    private void HandleMistake()
    {
        if (failing)
            return;

        mistakes++;
        mistakeFlashUntil = Time.unscaledTime + 0.6f;
        UpdateMistakeText();
        Debug.Log($"[Chapter5] 틀림 {mistakes}/{maxMistakes}");

        if (mistakes >= maxMistakes)
            StartFail();
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

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
