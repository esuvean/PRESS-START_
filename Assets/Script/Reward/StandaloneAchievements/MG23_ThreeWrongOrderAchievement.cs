using UnityEngine;

// Achievement #26, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG23_ThreeWrongOrderAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 26;

    private float previousUntil;
    private int wrong;
    protected override void ResetWatch() { previousUntil = 0f; wrong = 0; }
    protected override void Poll()
    {
        float until = Get<float>("wrongNoticeUntil");
        if (until > previousUntil + 0.01f && until > Time.unscaledTime)
        {
            wrong++;
            if (wrong >= 3) Grant();
        }
        previousUntil = until;
    }

}
