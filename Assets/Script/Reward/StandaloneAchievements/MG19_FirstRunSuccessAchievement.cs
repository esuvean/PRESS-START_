using UnityEngine;

// Achievement #17, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG19_FirstRunSuccessAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 17;

    // Any failed RUN in either round disqualifies this attempt.
    protected override bool DidMeetCondition()
    {
        return !disqualified && Get<int>("failureCount") == 0;
    }

}
