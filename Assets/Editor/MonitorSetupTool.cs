using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메뉴: 상단 메뉴바 > PRESS START > 모니터 화면 자동 설정
// MainScene을 연 상태에서 한 번 누르면 모니터 연출에 필요한 설정을 모두 해 준다.
public static class MonitorSetupTool
{
    const string MonitorPngPath = "Assets/1. Team Folder/Youngwoo/art/moniter/moniter.png";

    // moniter.png(1920x1080)의 투명한 화면 영역: x 488~1434, y 95~704 (위에서부터)
    static readonly Vector2 HoleAnchorMin = new Vector2(488f / 1920f, 1f - 705f / 1080f);
    static readonly Vector2 HoleAnchorMax = new Vector2(1435f / 1920f, 1f - 95f / 1080f);

    static readonly string[] PanelNames =
    {
        "MainPanel", "CheckProgressPanel", "SettingsPanel",
        "NoiseBackgroundPanel", "SystemCheckPanel", "RebootPanel"
    };

    [MenuItem("PRESS START/모니터 화면 자동 설정")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "MainScene")
        {
            EditorUtility.DisplayDialog("모니터 설정", "MainScene을 먼저 열어 주세요.\n(Project 창 > Assets/Scenes/MainScene 더블클릭)", "확인");
            return;
        }

        // 1) 모니터 PNG를 UI용 Sprite로 설정
        TextureImporter ti = AssetImporter.GetAtPath(MonitorPngPath) as TextureImporter;
        if (ti == null)
        {
            EditorUtility.DisplayDialog("모니터 설정", "모니터 이미지를 찾지 못했습니다:\n" + MonitorPngPath, "확인");
            return;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
        Sprite monitorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonitorPngPath);

        // 2) 씬에서 필요한 오브젝트 찾기
        GameObject uiManager = Find(scene, "UIManager");
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("모니터 설정", "UIManager 오브젝트를 찾지 못했습니다.", "확인");
            return;
        }
        StartMenuController startMenu = uiManager.GetComponent<StartMenuController>();

        var panels = new List<RectTransform>();
        var missing = new List<string>();
        foreach (string n in PanelNames)
        {
            GameObject go = Find(scene, n);
            if (go != null) panels.Add(go.GetComponent<RectTransform>());
            else missing.Add(n);
        }

        // 3) 예전에 만든 모니터 Canvas가 있으면 지우고 새로 만들기
        GameObject old = Find(scene, "Canvas_MonitorFrame");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject canvasGO = new GameObject("Canvas_MonitorFrame", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Monitor Setup");
        SceneManager.MoveGameObjectToScene(canvasGO, scene);
        canvasGO.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // 기존 Canvas(0)보다 앞

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // GraphicRaycaster는 일부러 넣지 않음 -> 모니터 그림이 버튼 클릭을 막지 않음

        // 4) 책상+모니터 그림
        GameObject frameGO = new GameObject("MonitorFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameGO.layer = canvasGO.layer;
        frameGO.transform.SetParent(canvasGO.transform, false);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        Stretch(frameRT, Vector2.zero, Vector2.one);
        Image img = frameGO.GetComponent<Image>();
        img.sprite = monitorSprite;
        img.raycastTarget = false;
        img.preserveAspect = false;

        // 5) 모니터 화면(투명 영역) 위치 표시용
        GameObject holeGO = new GameObject("MonitorHole", typeof(RectTransform));
        holeGO.layer = canvasGO.layer;
        holeGO.transform.SetParent(frameGO.transform, false);
        Stretch(holeGO.GetComponent<RectTransform>(), HoleAnchorMin, HoleAnchorMax);

        // 6) UIManager에 MonitorViewController 붙이고 연결
        MonitorViewController view = uiManager.GetComponent<MonitorViewController>();
        if (view == null) view = Undo.AddComponent<MonitorViewController>(uiManager);
        Undo.RecordObject(view, "Monitor Setup");
        view.monitorFrame = frameRT;
        view.monitorHole = holeGO.GetComponent<RectTransform>();
        view.monitorFrameCanvas = canvasGO;
        view.screenPanels = panels;
        EditorUtility.SetDirty(view);

        if (startMenu != null)
        {
            Undo.RecordObject(startMenu, "Monitor Setup");
            startMenu.monitorView = view;
            EditorUtility.SetDirty(startMenu);
        }

        // 7) 카메라 배경을 검정으로 (모니터 안 여백이 하늘색으로 보이지 않게)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Monitor Setup");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            EditorUtility.SetDirty(cam);
        }

        // 8) 씬 저장
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasGO;

        string msg = "설정 완료! 이제 위쪽의 ▶(Play) 버튼을 눌러 확인해 보세요.";
        if (missing.Count > 0) msg += "\n\n찾지 못한 패널: " + string.Join(", ", missing);
        if (startMenu == null) msg += "\n\nUIManager에 StartMenuController가 없어 확대 연결은 하지 못했습니다.";
        EditorUtility.DisplayDialog("모니터 설정", msg, "확인");
    }

    [MenuItem("PRESS START/모니터 화면 설정 되돌리기")]
    public static void Remove()
    {
        Scene scene = SceneManager.GetActiveScene();

        GameObject canvasGO = Find(scene, "Canvas_MonitorFrame");
        if (canvasGO != null) Undo.DestroyObjectImmediate(canvasGO);

        GameObject uiManager = Find(scene, "UIManager");
        if (uiManager != null)
        {
            StartMenuController startMenu = uiManager.GetComponent<StartMenuController>();
            if (startMenu != null)
            {
                Undo.RecordObject(startMenu, "Monitor Remove");
                startMenu.monitorView = null;
                EditorUtility.SetDirty(startMenu);
            }
            MonitorViewController view = uiManager.GetComponent<MonitorViewController>();
            if (view != null) Undo.DestroyObjectImmediate(view);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Monitor Remove");
            cam.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(cam);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("모니터 설정", "모니터 설정을 지우고 원래 상태로 되돌렸습니다.", "확인");
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

    // 꺼져 있는 오브젝트까지 포함해서 이름으로 찾기 (앞뒤 공백 무시)
    static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Trim() == name)
                    return t.gameObject;
        return null;
    }
}
