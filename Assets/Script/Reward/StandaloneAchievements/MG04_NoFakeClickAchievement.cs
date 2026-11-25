using UnityEngine;

// Achievement #09, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG04_NoFakeClickAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 9;

    protected override bool DidMeetCondition()
    {
        return Seconds <= 15f && Get<int>("failCount") == 0 && !disqualified;
    }

}
