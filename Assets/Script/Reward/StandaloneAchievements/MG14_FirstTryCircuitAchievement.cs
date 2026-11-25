using UnityEngine;

// Achievement #15, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG14_FirstTryCircuitAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 15;

    // Counts the original circuit's WrongConnection() cases.
    protected override bool DidMeetCondition()
    {
        return !disqualified && Get<int>("errorCount") == 0;
    }

}
