
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AchievementChapterBridge
{
    // 챕터의 마지막 미니게임까지
    // 모두 클리어했을 때만 호출
    public static void OnChapterFullyCompleted()
    {
        AchievementManager manager =
            AchievementManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                "[Achievement] AchievementManager가 없습니다."
            );
            return;
        }

        string sceneName =
            SceneManager.GetActiveScene().name;

        // Chapter1 ~ Chapter5 씬 이름으로 판별
        for (int chapter = 1; chapter <= 5; chapter++)
        {
            if (sceneName == "Chapter" + chapter)
            {
                manager.ReportChapterClear(chapter);

                Debug.Log(
                    "[Achievement] Chapter " +
                    chapter +
                    " 클리어 업적 확인"
                );

                return;
            }
        }

        Debug.LogWarning(
            "[Achievement] 챕터 씬이 아닙니다: " +
            sceneName
        );
    }

    // 마지막 엔딩을 실제로 확인했을 때 호출
    public static void OnFinalEndingShown()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.ReportFinalEnding();
        }
    }
}
