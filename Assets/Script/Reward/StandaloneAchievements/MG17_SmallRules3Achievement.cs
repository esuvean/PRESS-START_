
using UnityEngine;

// 업적 24 - 작은 글씨는 장식이 아닙니다
// 조건: MG17에서 작은 글씨 지시를 3회 위반
// 기존 Minigame17_SmallText.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG17_SmallRules3Achievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 24;

    private Minigame17_SmallText original;

    private int wrongChoices;
    private bool previousWrongFeedback;

    // =====================================
    // 미니게임 시작 시 초기화
    // =====================================

    protected override void ResetWatch()
    {
        original = game as Minigame17_SmallText;

        wrongChoices = 0;
        previousWrongFeedback = false;

        Debug.Log(
            "[MG17 Achievement] 작은 글씨 위반 감지 시작"
        );
    }

    // =====================================
    // 원본 게임의 오답 결과 감지
    // =====================================

    protected override void Poll()
    {
        if (original == null)
            return;

        if (original.feedbackText == null)
            return;

        string feedback = original.feedbackText.text;

        bool isWrongFeedback =
            !string.IsNullOrEmpty(feedback) &&
            feedback.Contains("틀렸습니다!");

        // 새로운 오답이 발생한 순간에만 1회 기록
        if (isWrongFeedback && !previousWrongFeedback)
        {
            wrongChoices++;

            Debug.Log(
                "[MG17 Achievement] 작은 글씨 위반: " +
                wrongChoices + "/3회"
            );

            if (wrongChoices >= 3)
            {
                Grant();

                Debug.Log(
                    "[MG17 Achievement] " +
                    "히든 업적 24번 달성!"
                );
            }
        }

        previousWrongFeedback = isWrongFeedback;
    }
}


