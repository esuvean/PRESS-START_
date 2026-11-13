#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// 빌드했을 때 게임 이름과 아이콘을 자동으로 맞춰 줌 (에디터 전용)
///  - 게임 이름(exe 이름 / 창 제목): PRESS START_
///  - 아이콘: Assets/1. Team Folder/Subin/Icon/PRESS_START_Icon.png
/// 유니티가 코드를 읽을 때 자동으로 적용됨. 메뉴 PRESS START → 빌드 이름·아이콘 적용 으로도 가능.
/// </summary>
[InitializeOnLoad]
public static class BuildIdentitySetup
{
    private const string GameName = "PRESS START_";
    private const string IconPath = "Assets/1. Team Folder/Subin/Icon/PRESS_START_Icon.png";

    static BuildIdentitySetup()
    {
        EditorApplication.delayCall += () => Apply(false);
    }

    [MenuItem("PRESS START/빌드 이름·아이콘 적용")]
    private static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool fromMenu)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool changed = false;

        if (PlayerSettings.productName != GameName)
        {
            PlayerSettings.productName = GameName;
            changed = true;
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            if (fromMenu)
                Debug.LogWarning("[빌드 설정] 아이콘 그림을 찾지 못했습니다: " + IconPath);
        }
        else
        {
            Texture2D[] current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            if (current == null || current.Length == 0 || current[0] != icon)
            {
                // 기본 아이콘 (모든 플랫폼 공통)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                changed = true;
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[빌드 설정] 게임 이름 'PRESS START_' 와 아이콘을 적용했습니다. 다음 빌드부터 반영됩니다.");
        }
        else if (fromMenu)
        {
            Debug.Log("[빌드 설정] 이미 적용되어 있습니다.");
        }
    }
}
#endif
