
using System;
using System.Collections.Generic;
using UnityEngine;

public enum AchievementCategory
{
    Basic,
    Challenge,
    Hidden
}

public class AchievementInfo
{
    public int id;
    public string title;
    public string condition;
    public AchievementCategory category;
    public int minigameId;

    public AchievementInfo(
        int id,
        string title,
        string condition,
        AchievementCategory category,
        int minigameId = 0)
    {
        this.id = id;
        this.title = title;
        this.condition = condition;
        this.category = category;
        this.minigameId = minigameId;
    }
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance
    {
        get;
        private set;
    }

    [Header("ART - Achievement Icons (ID 01-30)")]
    [Tooltip("0번 칸은 업적 01, 29번 칸은 업적 30입니다.")]
    public Sprite[] achievementIcons = new Sprite[30];

    [Header("ART - Locked Icon")]
    public Sprite lockedIcon;

    public event Action<AchievementInfo> OnAchievementUnlocked;

    private readonly List<AchievementInfo> achievements =
        new List<AchievementInfo>();

    private readonly Dictionary<int, int> actionCounts =
        new Dictionary<int, int>();

    private int chapter5FailureCount;

    private const string SavePrefix =
        "PRESS_START_ACHIEVEMENT_";

    public IReadOnlyList<AchievementInfo> Achievements
    {
        get { return achievements; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        BuildAchievementDatabase();

        Debug.Log(
            "[Achievement] 업적 시스템 준비 완료: " +
            achievements.Count + "개"
        );
    }

    // =========================================
    // 업적 30개 데이터
    // =========================================

    private void BuildAchievementDatabase()
    {
        achievements.Clear();

        // 기본 업적 6개
        Add(1, "마우스는 정상입니다",
            "Chapter 1 완료", AchievementCategory.Basic);

        Add(2, "눈은 정상입니다",
            "Chapter 2 완료", AchievementCategory.Basic);

        Add(3, "시스템은 정상입니다",
            "Chapter 3 완료", AchievementCategory.Basic);

        Add(4, "당신은 정상입니까?",
            "Chapter 4 완료", AchievementCategory.Basic);

        Add(5, "오류는 계속된다",
            "Chapter 5 완료", AchievementCategory.Basic);

        Add(6, "게임을 시작했습니다",
            "최종 엔딩 확인", AchievementCategory.Basic);

        // 도전 업적 14개
        Add(7, "도망쳐 봤자야",
            "20초 이내 체크포인트 없이 클리어",
            AchievementCategory.Challenge, 1);

        Add(8, "인간 자",
            "3라운드 모두 목표 중앙 ±5% 이내 성공",
            AchievementCategory.Challenge, 2);

        Add(9, "의심의 여지가 없군요",
            "15초 이내 가짜 버튼 클릭 없이 클리어",
            AchievementCategory.Challenge, 4);

        Add(10, "휠은 장식입니다",
            "마우스 휠 없이 스크롤바 드래그로만 클리어",
            AchievementCategory.Challenge, 7);

        Add(11, "흔들림 없는 손",
            "수락 없이 클리어",
            AchievementCategory.Challenge, 8);

        Add(12, "선택적 수신",
            "오답 수신과 정답 중복 없이 클리어",
            AchievementCategory.Challenge, 9);

        Add(13, "미니멀리스트",
            "최소 블록 이동 횟수로 클리어",
            AchievementCategory.Challenge, 11);

        Add(14, "공 하나면 충분해",
            "공을 한 번도 잃지 않고 클리어",
            AchievementCategory.Challenge, 12);

        Add(15, "전기기사 1급",
            "잘못된 연결 및 수정 없이 회로 완성",
            AchievementCategory.Challenge, 14);

        Add(16, "마우스와 절교",
            "10초 동안 마우스 이동 및 클릭 없이 클리어",
            AchievementCategory.Challenge, 16);

        Add(17, "미래를 보는 자",
            "RUN 버튼 첫 실행에 성공",
            AchievementCategory.Challenge, 19);

        Add(18, "겉만부터 보지 않는 사람",
            "약관 끝에 도달하기 전에 숨겨진 START 발견",
            AchievementCategory.Challenge, 21);

        Add(19, "기억력 테스트는 끝났습니다",
            "힌트 없이 90초 이내 클리어",
            AchievementCategory.Challenge, 22);

        Add(20, "한 문자도 놓치지 않아",
            "목표 문자 첫 등장에 모두 수집하고 오답 없이 클리어",
            AchievementCategory.Challenge, 26);

        // 히든 업적 10개
        Add(21, "그거 아닌데요?",
            "조각을 잘못된 슬롯에 3회 배치",
            AchievementCategory.Hidden, 3);

        Add(22, "창 좀 그만 열어!",
            "팝업 생성 함정 3회 작동",
            AchievementCategory.Hidden, 6);

        Add(23, "불량품 수집가",
            "불량 부품 3회 선택",
            AchievementCategory.Hidden, 13);

        Add(24, "작은 글씨는 장식이 아닙니다",
            "작은 글씨 지시 위반 3회",
            AchievementCategory.Hidden, 17);

        Add(25, "연결 환장입니다",
            "MG18 연결 관련 히든 업적",
            AchievementCategory.Hidden, 18);

        Add(26, "성격이 급하시군요",
            "잘못된 순서의 문자 3회 클릭",
            AchievementCategory.Hidden, 23);

        Add(27, "손가락 안 아프세요?",
            "클릭 20회 연타",
            AchievementCategory.Hidden, 24);

        Add(28, "팝업 중독자",
            "팝업창 1개라도 남기고 성공",
            AchievementCategory.Hidden, 25);

        Add(29, "속으셨군요?",
            "잘못된 회로로 가짜 성공 연출 확인",
            AchievementCategory.Hidden, 27);

        Add(30, "악마와의 계약",
            "Chapter 5에서 3회 실패 후 악마 엔딩 변종 확인",
            AchievementCategory.Hidden);
    }

    private void Add(
        int id,
        string title,
        string condition,
        AchievementCategory category,
        int minigameId = 0)
    {
        achievements.Add(
            new AchievementInfo(
                id,
                title,
                condition,
                category,
                minigameId
            )
        );
    }

    // =========================================
    // 업적 정보 조회
    // =========================================

    public AchievementInfo GetAchievement(int id)
    {
        return achievements.Find(a => a.id == id);
    }

    public bool IsUnlocked(int id)
    {
        return PlayerPrefs.GetInt(
            SavePrefix + id,
            0
        ) == 1;
    }

    public int GetUnlockedCount()
    {
        int count = 0;

        foreach (AchievementInfo info in achievements)
        {
            if (IsUnlocked(info.id))
                count++;
        }

        return count;
    }

    public int GetUnlockedCount(
        AchievementCategory category)
    {
        int count = 0;

        foreach (AchievementInfo info in achievements)
        {
            if (info.category == category &&
                IsUnlocked(info.id))
            {
                count++;
            }
        }

        return count;
    }

    public Sprite GetIcon(int id)
    {
        if (id < 1 ||
            id > 30 ||
            achievementIcons == null ||
            achievementIcons.Length < id)
        {
            return null;
        }

        return achievementIcons[id - 1];
    }

    // =========================================
    // 업적 획득 - 핵심 함수
    // =========================================

    public bool Unlock(int id)
    {
        AchievementInfo info = GetAchievement(id);

        if (info == null)
        {
            Debug.LogWarning(
                "[Achievement] 없는 업적 ID: " + id
            );

            return false;
        }

        // 이미 획득했다면 중복 지급하지 않음
        if (IsUnlocked(id))
            return false;

        PlayerPrefs.SetInt(
            SavePrefix + id,
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[Achievement] UNLOCKED! " +
            id.ToString("00") +
            " - " + info.title
        );

        OnAchievementUnlocked?.Invoke(info);

        return true;
    }

    // =========================================
    // 기본 업적: 챕터 클리어
    // =========================================

    public void ReportChapterClear(int chapterNumber)
    {
        if (chapterNumber < 1 || chapterNumber > 5)
            return;

        Unlock(chapterNumber);
    }

    public void ReportFinalEnding()
    {
        Unlock(6);
    }

    // =========================================
    // 도전 업적
    //
    // 실제 미니게임에서 모든 조건을 검증한 뒤
    // 이 함수를 호출해야 함
    // =========================================

    public void ReportChallengeClear(
        int achievementId,
        bool conditionMet)
    {
        if (!conditionMet)
            return;

        AchievementInfo info =
            GetAchievement(achievementId);

        if (info == null ||
            info.category != AchievementCategory.Challenge)
        {
            return;
        }

        Unlock(achievementId);
    }

    // =========================================
    // 미니게임 시작 시 임시 카운트 초기화
    // =========================================

    public void BeginMinigame(int minigameId)
    {
        switch (minigameId)
        {
            case 3: actionCounts.Remove(21); break;
            case 6: actionCounts.Remove(22); break;
            case 13: actionCounts.Remove(23); break;
            case 17: actionCounts.Remove(24); break;
            case 23: actionCounts.Remove(26); break;
            case 24: actionCounts.Remove(27); break;
        }
    }

    // =========================================
    // 히든 업적 행동 횟수 기록
    // =========================================

    public void RecordHiddenAction(int achievementId)
    {
        int requiredCount = GetHiddenRequiredCount(
            achievementId
        );

        if (requiredCount <= 0 ||
            IsUnlocked(achievementId))
        {
            return;
        }

        int current = 0;

        actionCounts.TryGetValue(
            achievementId,
            out current
        );

        current++;

        actionCounts[achievementId] = current;

        Debug.Log(
            "[Achievement] Hidden " +
            achievementId +
            " 진행: " +
            current + "/" + requiredCount
        );

        if (current >= requiredCount)
            Unlock(achievementId);
    }

    private int GetHiddenRequiredCount(int id)
    {
        switch (id)
        {
            case 21:
            case 22:
            case 23:
            case 24:
            case 26:
                return 3;

            case 27:
                return 20;
        }

        return 0;
    }

    // =========================================
    // 특정 이벤트형 히든 업적
    // =========================================

    public void ReportMinigameClear(
        int minigameId,
        bool popupRemaining = false)
    {
        // MG18은 기획서 메모에 따라
        // 별도 오답 판정 없이 지급
        if (minigameId == 18)
            Unlock(25);

        if (minigameId == 25 && popupRemaining)
            Unlock(28);
    }

    public void ReportMG27FakeReadySeen()
    {
        Unlock(29);
    }

    // =========================================
    // Chapter 5 특수 히든 업적
    // =========================================

    public void BeginChapter5()
    {
        chapter5FailureCount = 0;
    }

    public void ReportChapter5Failure()
    {
        chapter5FailureCount++;

        Debug.Log(
            "[Achievement] Chapter 5 실패: " +
            chapter5FailureCount
        );
    }

    public void ReportDevilEndingVariantSeen()
    {
        if (chapter5FailureCount >= 3)
        {
            Unlock(30);
        }
    }
}
