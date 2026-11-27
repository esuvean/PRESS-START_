
using UnityEngine;
using UnityEditor;

public static class ResetAchievementRecords
{
    [MenuItem("Tools/PRESS START/Reset Achievement Records")]
    public static void ResetRecords()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Play 모드를 먼저 종료하세요.");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "업적 초기화",
            "획득한 업적 30개를 모두 초기화할까요?",
            "초기화",
            "취소"
        );

        if (!confirm) return;

        for (int i = 1; i <= 30; i++)
        {
            PlayerPrefs.DeleteKey(
                "PRESS_START_ACHIEVEMENT_" + i
            );

            PlayerPrefs.DeleteKey(
                "PRESS_START_ACHIEVEMENT_" + i.ToString("00")
            );
        }

        PlayerPrefs.Save();

        Debug.Log("업적 30개 초기화 완료!");
    }
}
