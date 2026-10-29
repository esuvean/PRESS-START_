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
    // 저장 (PlayerPrefs)
    // 게임을 껐다 켜도 챕터 진행도가 남아 있도록 저장함
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
        Load();
    }


    // 저장된 진행도 불러오기
    public static void Load()
    {
        for (int i = 0; i < TotalChapters; i++)
        {
            chapterProgress[i] =
                PlayerPrefs.GetInt(ProgressKey + (i + 1), 0);

            chapterCompleted[i] =
                PlayerPrefs.GetInt(CompletedKey + (i + 1), 0) == 1;
        }
    }


    // 지금 진행도 저장하기
    public static void Save()
    {
        for (int i = 0; i < TotalChapters; i++)
        {
            PlayerPrefs.SetInt(ProgressKey + (i + 1), chapterProgress[i]);
            PlayerPrefs.SetInt(CompletedKey + (i + 1), chapterCompleted[i] ? 1 : 0);
        }

        PlayerPrefs.Save();
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