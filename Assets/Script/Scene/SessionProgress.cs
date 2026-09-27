using UnityEngine;

public static class SessionProgress
{
    public const int TotalChapters = 4;

    
    public static int[] chapterProgress =
        new int[TotalChapters];

    public static bool[] chapterCompleted =
        new bool[TotalChapters];

  
    public static bool[] chapterUnlocked =
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

        chapterUnlocked =
            new bool[TotalChapters];

        // Chapter 1은 처음부터 해금
        chapterUnlocked[0] = true;

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


        // 다음 챕터 해금
        int nextIndex =
            index + 1;

        if (nextIndex <
            chapterUnlocked.Length)
        {
            chapterUnlocked[nextIndex] = true;
        }
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


    public static bool IsChapterUnlocked(
        int chapterNumber)
    {
        int index =
            chapterNumber - 1;

        if (index < 0 ||
            index >= chapterUnlocked.Length)
        {
            return false;
        }

        return chapterUnlocked[index];
    }
}