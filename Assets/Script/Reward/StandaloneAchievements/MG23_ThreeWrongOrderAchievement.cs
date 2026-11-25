
using UnityEngine;

// 업적 26 - 성격이 급하시군요
// 조건: MG23에서 잘못된 순서의 문자 3회 클릭

[DisallowMultipleComponent]
public sealed class MG23_ThreeWrongOrderAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 26;

    private float previousUntil;
    private int wrongCount;

    protected override void ResetWatch()
    {
        previousUntil = 0f;
        wrongCount = 0;
    }

    protected override void Poll()
    {
        float until = Get<float>("wrongNoticeUntil");

        // 잘못된 문자를 클릭하면
        // 원본 게임의 wrongNoticeUntil 값이 갱신됨
        if (until > previousUntil &&
            until > Time.unscaledTime)
        {
            wrongCount++;

            Debug.Log(
                $"[MG23 Achievement] 잘못된 순서 클릭: {wrongCount}/3"
            );

            if (wrongCount >= 3)
            {
                Grant();
            }
        }

        previousUntil = until;
    }
}
