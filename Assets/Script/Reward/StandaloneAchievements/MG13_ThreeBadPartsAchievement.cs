using UnityEngine;

// Achievement #23, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG13_ThreeBadPartsAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 23;

    protected override void Poll()
    {
        if (Get<int>("errorCount") >= 3) Grant();
    }

}
