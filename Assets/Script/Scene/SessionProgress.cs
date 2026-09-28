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



    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        chapterProgress =
            new int[TotalChapters];

        chapterCompleted =
            new bool[TotalChapters];

        openChapterSelectOnLoad = false;
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