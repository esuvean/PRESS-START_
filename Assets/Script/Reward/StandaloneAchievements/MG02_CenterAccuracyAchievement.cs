
using UnityEngine;
using UnityEngine.EventSystems;

// 업적 08 - 인간 자
// 조건: 3라운드 모두 목표 크기 ±5% 이내로 성공
// 기존 Minigame2_HoldToMatch.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG02_CenterAccuracyAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 8;

    private Minigame2_HoldToMatch original;

    private EventTrigger buttonTrigger;
    private EventTrigger.Entry releaseEntry;

    private readonly bool[] accurateRounds = new bool[3];

    // =========================================
    // 미니게임 시작 시 초기화
    // =========================================

    protected override void ResetWatch()
    {
        original = game as Minigame2_HoldToMatch;

        for (int i = 0; i < accurateRounds.Length; i++)
        {
            accurateRounds[i] = false;
        }

        if (original == null)
        {
            Debug.LogWarning(
                "[MG02 Achievement] 원본 미니게임을 찾지 못했습니다."
            );

            return;
        }

        Debug.Log("[MG02 Achievement] 업적 감지 시작");
    }

    // =========================================
    // 버튼 이벤트 자동 연결
    // =========================================

    protected override void Poll()
    {
        if (original == null)
        {
            original = game as Minigame2_HoldToMatch;
        }

        if (original == null ||
            original.resizableButton == null)
        {
            return;
        }

        EventTrigger trigger =
            original.resizableButton.GetComponent<EventTrigger>();

        if (trigger == null)
            return;

        buttonTrigger = trigger;

        if (releaseEntry == null)
        {
            releaseEntry = new EventTrigger.Entry();

            releaseEntry.eventID =
                EventTriggerType.PointerUp;

            releaseEntry.callback.AddListener(
                OnButtonReleased
            );
        }

        // 원본 게임의 PointerUp보다 먼저 실행되도록
        // 이벤트 목록 맨 앞에 등록
        if (!buttonTrigger.triggers.Contains(releaseEntry))
        {
            buttonTrigger.triggers.Insert(0, releaseEntry);
        }
    }

    // =========================================
    // 버튼을 놓는 순간 실제 크기 검사
    // =========================================

    private void OnButtonReleased(BaseEventData eventData)
    {
        if (original == null ||
            !original.IsGameActive)
        {
            return;
        }

        // 실제로 버튼을 누르고 있었을 때만 판정
        if (!Get<bool>("isHolding"))
            return;

        int round = Get<int>("currentRound");

        if (round < 0 || round >= 3)
            return;

        if (original.targetScales == null ||
            round >= original.targetScales.Length)
        {
            return;
        }

        float currentScale =
            Get<float>("currentScale", 1f);

        float targetScale =
            original.targetScales[round];

        if (targetScale <= 0f)
            return;

        // 목표 크기 대비 오차 계산
        float error =
            Mathf.Abs(currentScale - targetScale);

        float tolerance =
            targetScale * 0.05f;

        // 목표 크기 ±5% 판정
        bool accurate =
            error <= tolerance;

        accurateRounds[round] = accurate;

        Debug.Log(
            $"[MG02 Achievement] ROUND {round + 1} | " +
            $"현재 크기: {currentScale:F3} | " +
            $"목표 크기: {targetScale:F3} | " +
            $"오차: {error:F3} | " +
            $"결과: {(accurate ? "정확" : "범위 초과")}"
        );
    }

    // =========================================
    // 최종 업적 획득 조건
    // =========================================

    protected override bool DidMeetCondition()
    {
        bool completedThreeRounds =
            Get<int>("currentRound") >= 3;

        bool allAccurate =
            accurateRounds[0] &&
            accurateRounds[1] &&
            accurateRounds[2];

        bool success =
            !disqualified &&
            completedThreeRounds &&
            allAccurate;

        Debug.Log(
            "[MG02 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
