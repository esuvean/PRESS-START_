#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Optional one-click installer. Adds ONLY new watcher components to original prefab assets.
// Existing sprites, buttons, MonoBehaviours and Inspector values are not replaced.
public static class AchievementStandalonePrefabInstaller
{
    [MenuItem("Tools/PRESS START/Install 21 Standalone Achievements")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before installing achievements.");
            return;
        }
        var watcherByGame = new Dictionary<Type, Type>
        {
            { typeof(Minigame1_FleeingButton), typeof(MG01_FastClearAchievement) },
            { typeof(Minigame2_HoldToMatch), typeof(MG02_CenterAccuracyAchievement) },
            { typeof(Minigame4_TargetClick), typeof(MG04_NoFakeClickAchievement) },
            { typeof(Minigame7_TermsScroll), typeof(MG07_DragOnlyAchievement) },
            { typeof(Minigame8_TiltBoard), typeof(MG08_NoFallAchievement) },
            { typeof(Minigame9_FallingStart), typeof(MG09_PerfectReceiptAchievement) },
            { typeof(Minigame11_PathPuzzle), typeof(MG11_MinimumMovesAchievement) },
            { typeof(Minigame12_Breakout), typeof(MG12_NoBallLostAchievement) },
            { typeof(Minigame14_PowerCircuit), typeof(MG14_FirstTryCircuitAchievement) },
            { typeof(DontPress), typeof(MG16_NoMouseAchievement) },
            { typeof(Minigame19_StartLadder), typeof(MG19_FirstRunSuccessAchievement) },
            { typeof(Minigame21_HiddenTerms), typeof(MG21_EarlyHiddenStartAchievement) },
            { typeof(Minigame22_ScatteredStart), typeof(MG22_NoHintsUnder90Achievement) },
            { typeof(Minigame26_FallingLetters), typeof(MG26_FirstCatchPerfectAchievement) },
            { typeof(Minigame3_MashButton), typeof(MG03_WrongSlot3Achievement) },
            { typeof(Minigame6_PopupStart), typeof(MG06_TrapPopup3Achievement) },
            { typeof(Minigame13_ButtonFactory), typeof(MG13_ThreeBadPartsAchievement) },
            { typeof(Minigame17_SmallText), typeof(MG17_SmallRules3Achievement) },
            { typeof(Minigame18_StartInterview), typeof(MG18_ConnectionAchievement) },
            { typeof(Minigame23_WildStart), typeof(MG23_ThreeWrongOrderAchievement) },
            { typeof(Minigame24_WaitAt99), typeof(MG24_TwentyRapidClicksAchievement) },
        };
        int changedPrefabs = 0;
        int addedComponents = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject loaded = null;
            try
            {
                loaded = PrefabUtility.LoadPrefabContents(path);
                bool modified = false;
                foreach (MinigameBase game in loaded.GetComponentsInChildren<MinigameBase>(true))
                {
                    if (!watcherByGame.TryGetValue(game.GetType(), out Type watcherType))
                        continue;
                    if (game.GetComponent(watcherType) != null)
                        continue;
                    Component watcher = game.gameObject.AddComponent(watcherType);
                    var baseWatcher = watcher as AchievementStandaloneWatcher;
                    if (baseWatcher != null) baseWatcher.game = game;
                    modified = true;
                    addedComponents++;
                }
                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(loaded, path);
                    changedPrefabs++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not inspect prefab " + path + ": " + ex.Message);
            }
            finally
            {
                if (loaded != null) PrefabUtility.UnloadPrefabContents(loaded);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[PRESS START] Standalone achievement installation complete. Prefabs: " + changedPrefabs + ", components: " + addedComponents);
        EditorUtility.DisplayDialog("PRESS START achievements", "Updated prefab assets: " + changedPrefabs + "\nAdded watcher components: " + addedComponents + "\nFinal ending achievements #06 and #30 still need their actual ending screen panels.", "OK");
    }
}
#endif
