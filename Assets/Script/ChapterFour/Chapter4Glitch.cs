using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 챕터4 전용 연출 (씬에 따로 배치하지 않아도 챕터4가 시작되면 자동으로 생김)
///  1) 챕터3보다 자주 화면 노이즈 발생
///  2) MG18을 깬 직후 게임이 한 번 '튕김' → 챕터 선택 화면으로 돌아감 (진행도 3/5 유지)
///     → 챕터4를 다시 누르면 MG19부터 이어서 플레이
///  튕김은 한 번 실행할 때 한 번만 일어남.
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
    public float heavyNoiseDuration = 1.4f;
    public float blackScreenDuration = 2.2f;

    // 한 번 실행 중에 이미 튕겼는지
    private static bool crashDone = false;

    private const int ChapterNumber = 4;

    private ChapterManager chapterManager;
    private ChapterNoiseController noise;
    private Coroutine noiseLoop;
    private bool crashing = false;

    // 튕김 연출을 이미 봤는지 저장 (게임을 다시 켜도 또 튕기지 않게)
    private const string CrashDoneKey = "Save_Chapter4CrashDone";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        crashDone = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadCrashState()
    {
        crashDone = PlayerPrefs.GetInt(CrashDoneKey, 0) == 1;
    }

    // NewGame을 누르면 튕김 연출도 다시 한 번 나오게
    public static void ResetCrash()
    {
        crashDone = false;
        PlayerPrefs.DeleteKey(CrashDoneKey);
        PlayerPrefs.Save();
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

            GameObject go = new GameObject("NoiseController");
            noise = go.AddComponent<ChapterNoiseController>();
            noise.Setup(canvas, Camera.main);
        }

        noiseLoop = StartCoroutine(NoiseLoop());
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
        PlayerPrefs.SetInt(CrashDoneKey, 1);
        PlayerPrefs.Save();

        if (noiseLoop != null)
            StopCoroutine(noiseLoop);

        // 화면 클릭 막기
        GameObject blocker = CreateOverlay(new Color(0f, 0f, 0f, 0f), false);

        yield return new WaitForSecondsRealtime(0.3f);

        // 1) 점점 심해지는 노이즈
        if (noise != null)
            yield return StartCoroutine(noise.PlayHeavyNoise(heavyNoiseDuration));

        // 2) 화면이 꺼지듯 검은 화면 + 오류 문구
        if (noise != null)
            noise.StopAllNoise();

        Destroy(blocker);
        CreateOverlay(Color.black, true);

        yield return new WaitForSecondsRealtime(blackScreenDuration);

        // 3) 챕터 선택 화면으로 (진행도는 그대로 남아서 이어하기 가능)
        SessionProgress.openChapterSelectOnLoad = true;
        string mainScene = chapterManager != null ? chapterManager.mainSceneName : "MainScene";
        SceneManager.LoadScene(mainScene);
    }

    private GameObject CreateOverlay(Color color, bool showError)
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

        GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image img = bg.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        if (showError)
        {
            GameObject textGO = new GameObject("ErrorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(canvasGO.transform, false);
            RectTransform tRT = (RectTransform)textGO.transform;
            tRT.anchorMin = new Vector2(0f, 0f);
            tRT.anchorMax = new Vector2(0f, 0f);
            tRT.pivot = new Vector2(0f, 0f);
            tRT.anchoredPosition = new Vector2(60f, 60f);
            tRT.sizeDelta = new Vector2(1400f, 120f);

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            ChapterFxResources res = Resources.Load<ChapterFxResources>("ChapterFxResources");
            if (res != null && res.failFont != null)
                tmp.font = res.failFont;
#if UNITY_EDITOR
            if (res == null || res.failFont == null)
            {
                TMP_FontAsset f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11_Bold SDF.asset");
                if (f != null) tmp.font = f;
            }
#endif
            tmp.fontSize = 28;
            tmp.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.text = "PRESS_START.exe 이(가) 응답하지 않습니다.\nERROR 0x0000START";
            tmp.raycastTarget = false;
        }

        return canvasGO;
    }
}
