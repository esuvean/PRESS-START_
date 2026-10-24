using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 모든 챕터 씬(ChapterManager가 있는 씬)에 게임을 시작할 때 자동으로
///  - 빨간 테두리 경고 (TimeWarningVignette)
///  - 게임오버 화면 (FailPanelController)
///  - 샷건 연출 (ShotgunController)
/// 을 붙여 준다. 씬에 이미 들어 있으면 그걸 그대로 쓰고, 없을 때만 새로 만든다.
/// → Hierarchy에서 따로 설정하지 않아도 1~4챕터 모두 똑같이 동작한다.
/// </summary>
public static class ChapterFxBootstrap
{
    // 자체 시간 규칙이 있어서 "시간 초과 = 게임오버"를 적용하지 않는 미니게임
    //  - Minigame19_StartLadder : 시간은 '도움말이 나오기까지 남은 시간'일 뿐 실패가 아님
    //  - Minigame20_FinalCircuit : 자체 제한시간 + 실패 후 재도전 규칙이 따로 있음 (빨간 테두리만 적용)
    private static readonly string[] OwnTimeRuleGames =
    {
        "Minigame19_StartLadder", "Minigame20_FinalCircuit"
    };

    // 빨간 테두리는 보여 주지 않을 미니게임
    private static readonly string[] NoWarningGames =
    {
        "Minigame19_StartLadder"
    };

    // 샷건 연출이 들어갈 모니터 화면 영역 (ShotgunSetupTool과 같은 값)
    private static readonly Vector2 HoleAnchorMin = new Vector2(494f / 1920f, 1f - 701f / 1080f);
    private static readonly Vector2 HoleAnchorMax = new Vector2(1429f / 1920f, 1f - 100f / 1080f);

    /// <summary>이 미니게임은 "시간 초과 → 게임오버 → 샷건"을 쓰지 않는지</summary>
    public static bool UsesOwnTimeRule(MinigameBase game)
    {
        return game != null && System.Array.IndexOf(OwnTimeRuleGames, game.GetType().Name) >= 0;
    }

    /// <summary>빨간 테두리를 보여 주지 않는 미니게임인지</summary>
    public static bool HideWarning(MinigameBase game)
    {
        return game != null && System.Array.IndexOf(NoWarningGames, game.GetType().Name) >= 0;
    }

    /// <summary>
    /// 빨간 테두리에 쓸 남은 시간.
    /// 대부분은 MinigameBase의 남은 시간, 자체 타이머가 있는 게임(예: 20번)은 그 게임의 remainingTime.
    /// </summary>
    public static float GetWarningTime(MinigameBase game)
    {
        if (game == null) return float.MaxValue;
        if (UsesOwnTimeRule(game))
        {
            FieldInfo f = game.GetType().GetField("remainingTime", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(game);
            return float.MaxValue;
        }
        return game.RemainingTime;
    }

    // ---------------- 자동으로 붙이기 ----------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup();
    }

    private static void Setup()
    {
        ChapterManager cm = Object.FindAnyObjectByType<ChapterManager>();
        if (cm == null) return; // 챕터 씬이 아니면 아무것도 안 함 (MainScene 등)

        // 1) 빨간 테두리
        if (TimeWarningVignette.Instance == null)
        {
            GameObject go = new GameObject("TimeWarningVignette");
            go.AddComponent<TimeWarningVignette>().chapterManager = cm;
        }

        ChapterFxResources res = LoadResources();

        // 2) 게임오버 화면
        FailPanelController fail = Object.FindAnyObjectByType<FailPanelController>(FindObjectsInactive.Include);
        if (fail == null)
        {
            GameObject go = new GameObject("FailPanelController");
            fail = go.AddComponent<FailPanelController>();
            fail.chapterManager = cm;
            if (res != null) fail.font = res.failFont;
        }
        else if (fail.chapterManager == null)
        {
            fail.chapterManager = cm;
        }

        // 3) 샷건
        ShotgunController shotgun = Object.FindAnyObjectByType<ShotgunController>(FindObjectsInactive.Include);
        if (shotgun == null)
        {
            shotgun = CreateShotgun(cm, res);
        }
        if (shotgun != null)
        {
            if (shotgun.failPanel == null) shotgun.failPanel = fail;
            shotgun.shotgunOnTimeOut = true;
        }
    }

    private static ChapterFxResources LoadResources()
    {
        ChapterFxResources res = Resources.Load<ChapterFxResources>("ChapterFxResources");

#if UNITY_EDITOR
        // 에디터에서는 Resources 파일이 없거나 비어 있어도 원래 위치에서 직접 찾아 쓴다
        if (res == null || res.shotgunFrames == null || res.shotgunFrames.Count == 0 || res.shotgunFrames[0] == null)
        {
            res = ScriptableObject.CreateInstance<ChapterFxResources>();
            for (int i = 0; i < 100; i++)
            {
                string path = $"Assets/1. Team Folder/Youngwoo/art/moniter/shotgun/shotgun_{i:00}.png";
                Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null) break;
                res.shotgunFrames.Add(s);
            }
            res.failFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Galmuri11_Bold SDF.asset");
        }
#endif
        return res;
    }

    private static ShotgunController CreateShotgun(ChapterManager cm, ChapterFxResources res)
    {
        if (res == null || res.shotgunFrames == null || res.shotgunFrames.Count == 0) return null;

        Canvas gameCanvas = null;
        if (cm.canvasTransform != null) gameCanvas = cm.canvasTransform.GetComponentInParent<Canvas>();
        if (gameCanvas == null) return null;
        gameCanvas = gameCanvas.rootCanvas;

        // 책상+모니터 Canvas (평소에는 투명)
        GameObject canvasGO = new GameObject("Canvas_MonitorFrame", typeof(RectTransform));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject frameGO = new GameObject("MonitorFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        frameGO.layer = canvasGO.layer;
        frameGO.transform.SetParent(canvasGO.transform, false);
        RectTransform frameRT = (RectTransform)frameGO.transform;
        Stretch(frameRT, Vector2.zero, Vector2.one);
        Image img = frameGO.GetComponent<Image>();
        img.sprite = res.shotgunFrames[0];
        img.raycastTarget = false;
        CanvasGroup cg = frameGO.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        GameObject holeGO = new GameObject("MonitorHole", typeof(RectTransform));
        holeGO.layer = canvasGO.layer;
        holeGO.transform.SetParent(frameGO.transform, false);
        Stretch((RectTransform)holeGO.transform, HoleAnchorMin, HoleAnchorMax);

        // 컨트롤러 (Awake 전에 연결을 채우려고 잠깐 꺼 둔 상태에서 만든다)
        GameObject ctrlGO = new GameObject("ShotgunController");
        ctrlGO.SetActive(false);
        ShotgunController ctrl = ctrlGO.AddComponent<ShotgunController>();
        AudioSource audio = ctrlGO.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        ctrl.gameCanvas = gameCanvas;
        ctrl.monitorFrameCanvas = canvasGO;
        ctrl.monitorFrame = frameRT;
        ctrl.monitorHole = (RectTransform)holeGO.transform;
        ctrl.shotgunFrames = new List<Sprite>(res.shotgunFrames);
        ctrl.audioSource = audio;
        ctrl.shotgunOnTimeOut = true;

        ctrlGO.SetActive(true);
        return ctrl;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
