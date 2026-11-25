
using UnityEngine;

// 업적 17 - 미래를 보는 자
// 조건: MG19에서 RUN 버튼 첫 실행에 성공
// 두 라운드 모두 첫 RUN에 성공해야 획득
// 기존 미니게임 스크립트 수정 불필요

[DisallowMultipleComponent]
public sealed class MG19_FirstRunSuccessAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 17;

    private bool failedRun;

    // 미니게임 시작 시 초기화
    protected override void ResetWatch()
    {
        failedRun = false;

        Debug.Log(
            "[MG19 Achievement] 첫 RUN 성공 감지 시작"
        );
    }

    // RUN 실패 여부 감지
    protected override void Poll()
    {
        if (failedRun)
            return;

        // 원본 MG19는 RUN 실행 실패 시
        // failureCount를 증가시킴
        if (Get<int>("failureCount") > 0)
        {
            failedRun = true;
            disqualified = true;

            Debug.Log(
                "[MG19 Achievement] RUN 실패! " +
                "업적 획득 불가"
            );
        }
    }

    // 미니게임 클리어 시 최종 판정
    protected override bool DidMeetCondition()
    {
        bool success =
            !disqualified &&
            !failedRun &&
            Get<int>("failureCount") == 0;

        Debug.Log(
            "[MG19 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
