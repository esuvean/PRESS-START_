using UnityEngine;

// Achievement #14, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG12_NoBallLostAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 14;

    protected override bool DidMeetCondition()
    {
        return !disqualified && Get<int>("missCount") == 0;
    }

}
