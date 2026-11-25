
using UnityEngine;

// 업적 19 - 기억력 테스트는 끝났습니다
// 조건: 힌트 없이 90초 이내 클리어

[DisallowMultipleComponent]
public sealed class MG22_NoHintsUnder90Achievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 19;

    private bool hintAppeared;

    protected override void ResetWatch()
    {
        hintAppeared = false;
    }

    protected override void Poll()
    {
        CheckHint();
    }

    private void CheckHint()
    {
        Minigame22_ScatteredStart original =
            game as Minigame22_ScatteredStart;

        if (original == null ||
            original.hintText == null)
            return;

        string message = original.hintText.text;

        if (!string.IsNullOrEmpty(message) &&
            message.TrimStart().StartsWith("힌트:"))
        {
            hintAppeared = true;
        }
    }

    protected override bool DidMeetCondition()
    {
        // 클리어 직전 힌트도 확인
        CheckHint();

        float elapsed = Get<float>("elapsed", -1f);

        bool success =
            !disqualified &&
            !hintAppeared &&
            elapsed >= 0f &&
            elapsed <= 90f;

        Debug.Log(
            "[MG22 Achievement] 클리어 시간: " +
            elapsed.ToString("F1") + "초 | " +
            "힌트 표시: " + hintAppeared + " | " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
