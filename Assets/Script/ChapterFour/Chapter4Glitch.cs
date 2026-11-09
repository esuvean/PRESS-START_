using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 챕터4 전용 연출 (씬에 따로 배치하지 않아도 챕터4가 시작되면 자동으로 생김)
///  1) 챕터3보다 자주 화면 노이즈 발생
///  2) MG18을 깬 직후 게임이 진짜 고장난 것처럼 멈춤
///     짧은 노이즈 → 검은 화면 '응답 없음' → 재부팅 (전체 약 4초)
///     → 챕터 밖으로 튕기지 않고 그 자리에서 MG19로 이어짐
///  연출 동안은 시간이 멈춰서 다음 게임 타이머가 줄지 않음. 한 번만 일어남.
/// </summary>
public class Chapter4Glitch : MonoBehaviour
{
    [Header("노이즈 간격 (초)")]
    public float firstNoiseMin = 12f;
    public float firstNoiseMax = 20f;
    public float noiseIntervalMin = 10f;
    public float noiseIntervalMax = 20f;

    [Header("튕김")]
    [Tooltip("이 개수만큼 깼을 때 튕김 (3 = MG18 클리어 직후)")]
    public int crashAfterClearedCount = 3;
    public float heavyNoiseDuration = 0.9f;
    [Tooltip("'응답하지 않습니다' 검은 화면 시간")]
    public float blackScreenDuration = 1.3f;
    [Tooltip("재부팅 화면 시간")]
    public float rebootDuration = 1.1f;

    // 한 번 실행 중에 이미 튕겼는지
    private static bool crashDone = false;

    private const int ChapterNumber = 4;

    private ChapterManager chapterManager;
    private ChapterNoiseController noise;
    private Coroutine noiseLoop;
    private bool crashing = false;

    // 노이즈가 캔버스 모드를 바꾸므로 원래 상태를 기억했다가 되돌림
    private Canvas gameCanvas;
    private RenderMode savedRenderMode;
    private Camera savedCamera;
    private float savedPlaneDistance;
    private float savedTimeScale = 1f;
    private bool timeFrozen = false;

    // 예전 저장 키 (이제 저장 안 함, 남아 있으면 지우기만 함)
    private const string CrashDoneKey = "Save_Chapter4CrashDone";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        crashDone = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadCrashState()
    {
        // 저장 안 함: 게임을 다시 켜면 고장 연출도 다시 나옴
        crashDone = false;

        if (PlayerPrefs.HasKey(CrashDoneKey))
        {
            PlayerPrefs.DeleteKey(CrashDoneKey);
            PlayerPrefs.Save();
        }
    }

    // NewGame을 누르면 튕김 연출도 다시 한 번 나오게
    public static void ResetCrash()
    {
        crashDone = false;
    }

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

        if (Object.FindAnyObjectByType<Chapter4Glitch>() != null)
            return;

        GameObject go = new GameObject("Chapter4Glitch");
        Chapter4Glitch glitch = go.AddComponent<Chapter4Glitch>();
        glitch.chapterManager = cm;
    }

    private void OnEnable()
    {
        MinigameBase.OnGameSuccess += HandleSuccess;
    }

    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -= HandleSuccess;
    }

    private void Start()
    {
        noise = Object.FindAnyObjectByType<ChapterNoiseController>();

        if (noise == null)
        {
            Canvas canvas = null;
            if (chapterManager != null && chapterManager.canvasTransform != null)
                canvas = chapterManager.canvasTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas = canvas.rootCanvas;

            gameCanvas = canvas;

            GameObject go = new GameObject("NoiseController");
            noise = go.AddComponent<ChapterNoiseController>();
            noise.Setup(canvas, Camera.main);
        }

        if (gameCanvas == null && chapterManager != null && chapterManager.canvasTransform != null)
        {
            Canvas c = chapterManager.canvasTransform.GetComponentInParent<Canvas>();
            if (c != null) gameCanvas = c.rootCanvas;
        }

        if (gameCanvas != null)
        {
            savedRenderMode = gameCanvas.renderMode;
            savedCamera = gameCanvas.worldCamera;
            savedPlaneDistance = gameCanvas.planeDistance;
        }

        noiseLoop = StartCoroutine(NoiseLoop());
    }

    private void OnDestroy()
    {
        // 연출 도중 씬이 바뀌어도 시간이 멈춘 채로 남지 않게
        if (timeFrozen)
            Time.timeScale = savedTimeScale;
    }

    private IEnumerator NoiseLoop()
    {
        yield return new WaitForSeconds(Random.Range(firstNoiseMin, firstNoiseMax));

        while (!crashing)
        {
            yield return StartCoroutine(noise.PlayLightNoiseAndWait());
            yield return new WaitForSeconds(Random.Range(noiseIntervalMin, noiseIntervalMax));
        }
    }

    private void HandleSuccess()
    {
        if (crashDone || crashing)
            return;

        StartCoroutine(CheckCrashNextFrame());
    }

    private IEnumerator CheckCrashNextFrame()
    {
        // ChapterManager가 진행도를 저장한 다음에 확인
        yield return null;

        if (crashDone || crashing)
            yield break;

        if (SessionProgress.GetProgress(ChapterNumber) >= crashAfterClearedCount)
            StartCoroutine(CrashRoutine());
    }

    private IEnumerator CrashRoutine()
    {
        crashing = true;
        crashDone = true;

        if (noiseLoop != null)
            StopCoroutine(noiseLoop);

        // 게임 시간 멈춤 (다음 미니게임 로딩/타이머가 연출 뒤로 밀림)
        savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        timeFrozen = true;

        // 화면 클릭 막기 + 연출용 화면
        GameObject root = CreateOverlayCanvas();
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        Image bg = AddFullImage(root.transform, new Color(0f, 0f, 0f, 0f));

        yield return new WaitForSecondsRealtime(0.15f);

        // 1) 짧고 강한 노이즈
        if (noise != null)
            yield return StartCoroutine(noise.PlayHeavyNoise(heavyNoiseDuration));

        // 2) 뚝 끊기듯 검은 화면 + 응답 없음
        if (noise != null)
            noise.StopAllNoise();
        RestoreCanvas();

        bg.color = Color.black;

        TextMeshProUGUI hang = AddText(root.transform,
            "PRESS_START.exe 이(가) 응답하지 않습니다.\nERROR 0x0000START",
            new Vector2(0f, 0f), new Vector2(60f, 60f), new Vector2(1400f, 120f),
            28, new Color(0.75f, 0.75f, 0.75f, 1f), TextAlignmentOptions.BottomLeft);

        float t = 0f;
        while (t < blackScreenDuration)
        {
            // 커서가 깜빡이는 느낌
            bool cursor = Mathf.FloorToInt(t * 3f) % 2 == 0;
            hang.text = "PRESS_START.exe 이(가) 응답하지 않습니다.\nERROR 0x0000START" + (cursor ? " _" : "");
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) 재부팅
        hang.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.15f);

        TextMeshProUGUI boot = AddText(root.transform, "다시 시작하는 중",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 80f),
            34, new Color(0.85f, 0.85f, 0.85f, 1f), TextAlignmentOptions.Center);

        GameSfx.PlayGameStart();

        t = 0f;
        while (t < rebootDuration)
        {
            int dots = Mathf.FloorToInt(t * 5f) % 4;
            boot.text = "다시 시작하는 중" + new string('.', dots);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        boot.gameObject.SetActive(false);

        // 4) 화면이 다시 켜지듯 사라지고 게임 계속
        Time.timeScale = savedTimeScale;
        timeFrozen = false;

        t = 0f;
        const float fade = 0.3f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fade);
            yield return null;
        }

        Destroy(root);

        crashing = false;
        noiseLoop = StartCoroutine(NoiseLoop());
    }

    private void RestoreCanvas()
    {
        if (gameCanvas == null)
            return;

        gameCanvas.renderMode = savedRenderMode;
        gameCanvas.worldCamera = savedCamera;
        gameCanvas.planeDistance = savedPlaneDistance;
    }

    // ===== 연출용 UI 만들기 =====

    private GameObject CreateOverlayCanvas()
    {
        GameObject canvasGO = new GameObject("CrashOverlay", typeof(RectTransform));
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvasGO;
    }

    private Image AddFullImage(Transform parent, Color color)
    {
        GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(parent, false);
        RectTransform bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image img = bg.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    private TextMeshProUGUI AddText(Transform parent, string text,
        Vector2 anchor, Vector2 position, Vector2 size,
        float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)textGO.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetFont();
        if (font != null)
            tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
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
