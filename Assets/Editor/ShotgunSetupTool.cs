using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메뉴: PRESS START > 샷건 연출 설정 (Chapter2)
public static class ShotgunSetupTool
{
    const string FramesFolder = "Assets/1. Team Folder/Youngwoo/art/moniter/shotgun";

    // MonitorSetupTool과 같은 모니터 화면 영역
    static readonly Vector2 HoleAnchorMin = new Vector2(494f / 1920f, 1f - 701f / 1080f);
    static readonly Vector2 HoleAnchorMax = new Vector2(1429f / 1920f, 1f - 100f / 1080f);

    [MenuItem("PRESS START/샷건 연출 설정 (Chapter2)")]
    public static void SetupChapter2() { Setup("Chapter2", false); }

    [MenuItem("PRESS START/샷건 연출 설정 (Chapter3)")]
    public static void SetupChapter3() { Setup("Chapter3", true); }

    [MenuItem("PRESS START/샷건 연출 지우기 (Chapter2)")]
    public static void RemoveChapter2() { Remove("Chapter2"); }

    [MenuItem("PRESS START/샷건 연출 지우기 (Chapter3)")]
    public static void RemoveChapter3() { Remove("Chapter3"); }

    static void Setup(string sceneName, bool onTimeOut)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);

        // 1) 샷건 프레임 이미지 설정 (shotgun_00 ~ shotgun_15)
        var frames = new List<Sprite>();
        for (int i = 0; i < 100; i++)
        {
            string path = $"{FramesFolder}/shotgun_{i:00}.png";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) break;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
            frames.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
        }
        if (frames.Count == 0)
        {
            EditorUtility.DisplayDialog("샷건 설정", "샷건 이미지를 찾지 못했습니다:\n" + FramesFolder, "확인");
            return;
        }

        // 2) 미니게임이 들어가는 Canvas 찾기
        Canvas gameCanvas = null;
        ChapterManager cm = Object.FindAnyObjectByType<ChapterManager>();
        if (cm != null && cm.canvasTransform != null) gameCanvas = cm.canvasTransform.GetComponentInParent<Canvas>();
        if (gameCanvas == null)
        {
            GameObject c = Find(scene, "Canvas");
            if (c != null) gameCanvas = c.GetComponent<Canvas>();
        }
        if (gameCanvas == null)
        {
            EditorUtility.DisplayDialog("샷건 설정", "게임 Canvas를 찾지 못했습니다.", "확인");
            return;
        }
        gameCanvas = gameCanvas.rootCanvas;

        // 3) 예전 설정 지우기
        GameObject old = Find(scene, "Canvas_MonitorFrame");
        if (old != null) Undo.DestroyObjectImmediate(old);
        GameObject oldCtrl = Find(scene, "ShotgunController");
        if (oldCtrl != null) Undo.DestroyObjectImmediate(oldCtrl);
        GameObject oldFail = Find(scene, "FailPanelController");
        if (oldFail != null) Undo.DestroyObjectImmediate(oldFail);

        // 4) 책상+모니터 Canvas (평소에는 투명)
        GameObject canvasGO = new GameObject("Canvas_MonitorFrame", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Shotgun Setup");
        SceneManager.MoveGameObjectToScene(canvasGO, scene);
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
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        Stretch(frameRT, Vector2.zero, Vector2.one);
        Image img = frameGO.GetComponent<Image>();
        img.sprite = frames[0];
        img.raycastTarget = false;
        CanvasGroup cg = frameGO.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        GameObject holeGO = new GameObject("MonitorHole", typeof(RectTransform));
        holeGO.layer = canvasGO.layer;
        holeGO.transform.SetParent(frameGO.transform, false);
        Stretch(holeGO.GetComponent<RectTransform>(), HoleAnchorMin, HoleAnchorMax);

        // 5) 샷건 컨트롤러
        GameObject ctrlGO = new GameObject("ShotgunController");
        Undo.RegisterCreatedObjectUndo(ctrlGO, "Shotgun Setup");
        SceneManager.MoveGameObjectToScene(ctrlGO, scene);
        ShotgunController ctrl = ctrlGO.AddComponent<ShotgunController>();
        AudioSource audio = ctrlGO.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        ctrl.gameCanvas = gameCanvas;
        ctrl.monitorFrameCanvas = canvasGO;
        ctrl.monitorFrame = frameRT;
        ctrl.monitorHole = holeGO.GetComponent<RectTransform>();
        ctrl.shotgunFrames = frames;
        ctrl.audioSource = audio;
        ctrl.shotgunOnTimeOut = onTimeOut;

        // 시간 초과 실패 화면 (TIME OVER / RETRY / EXIT)
        if (onTimeOut)
        {
            GameObject failGO = new GameObject("FailPanelController");
            Undo.RegisterCreatedObjectUndo(failGO, "Shotgun Setup");
            SceneManager.MoveGameObjectToScene(failGO, scene);
            FailPanelController fail = failGO.AddComponent<FailPanelController>();
            fail.chapterManager = cm;
            fail.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Galmuri11_Bold SDF.asset");
            EditorUtility.SetDirty(fail);
            ctrl.failPanel = fail;
        }
        EditorUtility.SetDirty(ctrl);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = ctrlGO;

        EditorUtility.DisplayDialog("샷건 설정",
            $"{sceneName}에 샷건 연출을 넣었습니다!\n\n" +
            "▶ Play 후 키보드 F9를 누르면 바로 테스트할 수 있어요.\n" +
            "실제 게임에서는 실수 5번 → 샷건, 그 다음부터 7번마다 샷건이 나옵니다." +
            (onTimeOut ? "\n제한 시간이 다 되면 샷건 후 실패 화면(RETRY / EXIT)이 나옵니다." : ""), "확인");
    }

    static void Remove(string sceneName)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);
        GameObject a = Find(scene, "Canvas_MonitorFrame");
        if (a != null) Undo.DestroyObjectImmediate(a);
        GameObject b = Find(scene, "ShotgunController");
        if (b != null) Undo.DestroyObjectImmediate(b);
        GameObject c = Find(scene, "FailPanelController");
        if (c != null) Undo.DestroyObjectImmediate(c);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("샷건 설정", $"{sceneName}에서 샷건 연출을 지웠습니다.", "확인");
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Trim() == name)
                    return t.gameObject;
        return null;
    }
}
