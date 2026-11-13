using UnityEngine;

public static class SessionProgress
{
    // 챕터5까지 (챕터5는 1~4를 다 깨야 나타남)
    public const int TotalChapters = 5;

    // 챕터5 전용 기록 (게임이 켜져 있는 동안만 기억)
    // - 챕터5에서 실패한 횟수 (3번 이상이면 챕터 선택 화면의 ? 아이콘이 악마 아이콘으로 바뀜)
    public static int chapter5FailCount = 0;
    // - 챕터5가 처음 나타나는 '강한 노이즈' 연출을 이미 봤는지
    public static bool chapter5Revealed = false;

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

        chapter5FailCount = 0;
        chapter5Revealed = false;
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

        chapter5FailCount = 0;
        chapter5Revealed = false;

        Save();
    }


    // 챕터1~4를 모두 깼는지 (= 챕터5가 열렸는지)
    public static bool IsChapter5Unlocked()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (!IsChapterCompleted(i))
                return false;
        }

        return true;
    }


    // 챕터5 실패: 실패 횟수 +1, 챕터5 진행도는 처음(0)으로
    public static void RegisterChapter5Failure()
    {
        chapter5FailCount++;
        ResetChapterProgress(5);
    }


    // 한 챕터의 진행도를 처음으로 되돌림 (SetProgress는 줄어들지 않으므로 따로 만듦)
    public static void ResetChapterProgress(int chapterNumber)
    {
        int index = chapterNumber - 1;

        if (index < 0 || index >= chapterProgress.Length)
            return;

        chapterProgress[index] = 0;
        chapterCompleted[index] = false;
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