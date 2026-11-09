using UnityEngine;

public static class SessionProgress
{
    public const int TotalChapters = 4;

    // 챕터별 진행도
    public static int[] chapterProgress =
        new int[TotalChapters];

    // 챕터 완료 여부
    public static bool[] chapterCompleted =
        new bool[TotalChapters];

   
    public static bool openChapterSelectOnLoad = false;

    // NewGame으로 메인 씬을 다시 열 때: 모니터 화면/지지직 인트로 없이 바로 전체 화면 시작 화면
    public static bool skipIntroOnLoad = false;



    // ---------------------------------------------
    // 저장 안 함
    // 진행도는 게임이 켜져 있는 동안만 기억함 (챕터 중간에 나가도 이어하기 가능)
    // 게임을 끄고 다시 켜면 처음부터 시작
    // ---------------------------------------------
    private const string ProgressKey = "Save_ChapterProgress_";   // + 챕터번호
    private const string CompletedKey = "Save_ChapterCompleted_"; // + 챕터번호


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        chapterProgress =
            new int[TotalChapters];

        chapterCompleted =
            new bool[TotalChapters];

        openChapterSelectOnLoad = false;
        skipIntroOnLoad = false;
    }


    // 게임을 켤 때(첫 씬이 열리기 직전) 저장된 진행도 불러오기
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadOnBoot()
    {
        // 예전에 PlayerPrefs에 저장해 둔 진행도가 남아 있으면 지움
        bool removed = false;

        for (int i = 0; i < TotalChapters; i++)
        {
            if (PlayerPrefs.HasKey(ProgressKey + (i + 1)) ||
                PlayerPrefs.HasKey(CompletedKey + (i + 1)))
            {
                PlayerPrefs.DeleteKey(ProgressKey + (i + 1));
                PlayerPrefs.DeleteKey(CompletedKey + (i + 1));
                removed = true;
            }
        }

        if (removed)
            PlayerPrefs.Save();
    }


    // (저장 기능 없음) 다른 스크립트에서 불러도 문제없게 남겨 둠
    public static void Load()
    {
    }


    // (저장 기능 없음) 진행도는 메모리에만 남음
    public static void Save()
    {
    }


    // 저장된 진행도가 하나라도 있는지 (CONTINUE 가능 여부)
    public static bool HasSave()
    {
        for (int i = 0; i < TotalChapters; i++)
        {
            if (chapterProgress[i] > 0 || chapterCompleted[i])
                return true;
        }

        return false;
    }



    // NewGame: 모든 진행도를 처음 상태로 (저장 파일도 지움)
    public static void ResetAll()
    {
        chapterProgress =
            new int[TotalChapters];

        chapterCompleted =
            new bool[TotalChapters];

        openChapterSelectOnLoad = false;

        Save();
    }

    public static void SetProgress(
        int chapterNumber,
        int progress)
    {
        int index =
            chapterNumber - 1;

        if (index < 0 ||
            index >= chapterProgress.Length)
        {
            return;
        }

        chapterProgress[index] =
            Mathf.Max(
                chapterProgress[index],
                progress
            );

        Save();
    }


    public static int GetProgress(
        int chapterNumber)
    {
        int index =
            chapterNumber - 1;

        if (index < 0 ||
            index >= chapterProgress.Length)
        {
            return 0;
        }

        return chapterProgress[index];
    }

    public static void CompleteChapter(
        int chapterNumber)
    {
        int index =
            chapterNumber - 1;

        if (index < 0 ||
            index >= chapterCompleted.Length)
        {
            return;
        }

        chapterCompleted[index] = true;

        Save();
    }



    public static bool IsChapterCompleted(
        int chapterNumber)
    {
        int index =
            chapterNumber - 1;

        if (index < 0 ||
            index >= chapterCompleted.Length)
        {
            return false;
        }

        return chapterCompleted[index];
    }
}