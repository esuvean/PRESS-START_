#if UNITY_EDITOR
using CRTFilter;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 노이즈 안전장치 (에디터 전용)
/// 노이즈(CRT 필터)가 켜진 순간에 Play를 멈추면 필터가 켜진 채로 남아서
/// Scene 창이나 다음 Play 화면이 계속 지지직거리는 문제가 생김.
/// → Play를 끝낼 때 / 시작할 때 / 유니티가 코드를 다시 읽을 때 CRT 필터를 자동으로 끔.
/// </summary>
[InitializeOnLoad]
public static class CrtFilterSafety
{
    static CrtFilterSafety()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        // 지금 켜져 있는 것도 바로 끔 (Play 중이 아닐 때만)
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                TurnOff();
        };
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.ExitingPlayMode ||
            state == PlayModeStateChange.EnteredEditMode)
        {
            TurnOff();
        }
    }

    [MenuItem("PRESS START/노이즈(CRT 필터) 끄기")]
    public static void TurnOff()
    {
        foreach (CRTRendererFeature f in Resources.FindObjectsOfTypeAll<CRTRendererFeature>())
        {
            if (f == null) continue;
            f.SetActive(false);
        }

        SceneView.RepaintAll();
    }
}
#endif
