
using UnityEngine;
using UnityEngine.UI;

// 업적 18 - 결말부터 보지 않는 사람
// 조건: 약관 끝에 도달하기 전에
// 숨겨진 START를 발견하고 클리어
// 기존 Minigame21_HiddenTerms.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG21_EarlyHiddenStartAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 18;

    private Minigame21_HiddenTerms original;

    private bool reachedBottom;
    private bool foundEarly;
    private bool sawRevealed;
    private bool scrollInitialized;

    private const float BottomThreshold = 0.02f;

    // =====================================
    // 게임 시작 시 초기화
    // =====================================

    protected override void ResetWatch()
    {
        original = game as Minigame21_HiddenTerms;

        reachedBottom = false;
        foundEarly = false;
        sawRevealed = false;
        scrollInitialized = false;

        Debug.Log(
            "[MG21 Achievement] 업적 감지 시작"
        );
    }

    // =====================================
    // 약관 스크롤 및 START 발견 감지
    // =====================================

    protected override void Poll()
    {
        if (original == null ||
            original.termsScrollRect == null)
        {
            return;
        }

        ScrollRect scroll = original.termsScrollRect;

        float position =
            scroll.verticalNormalizedPosition;

        // 원본 MG21은 시작 후 다음 프레임에
        // 약관을 맨 위로 초기화하므로
        // 그 시점까지 판정을 시작하지 않음
        if (!scrollInitialized)
        {
            if (position >= 0.95f)
            {
                scrollInitialized = true;
            }
            else
            {
                return;
            }
        }

        // 약관 끝까지 내려간 적이 있는지 기록
        // 1 = 맨 위, 0 = 맨 아래
        if (!reachedBottom &&
            position <= BottomThreshold)
        {
            reachedBottom = true;

            Debug.Log(
                "[MG21 Achievement] 약관 끝에 도달함"
            );
        }

        // 원본 미니게임에서 START 발견 여부 확인
        bool revealed = Get<bool>("startRevealed");

        if (revealed && !sawRevealed)
        {
            sawRevealed = true;

            // 끝에 도달하기 전에 발견했어야 성공
            foundEarly = !reachedBottom;

            Debug.Log(
                "[MG21 Achievement] 숨겨진 START 발견 | " +
                (foundEarly
                    ? "업적 조건 충족"
                    : "이미 약관 끝에 도달하여 조건 실패")
            );
        }
    }

    // =====================================
    // 최종 클리어 시 업적 판정
    // =====================================

    protected override bool DidMeetCondition()
    {
        bool success =
            !disqualified &&
            sawRevealed &&
            foundEarly;

        Debug.Log(
            "[MG21 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
