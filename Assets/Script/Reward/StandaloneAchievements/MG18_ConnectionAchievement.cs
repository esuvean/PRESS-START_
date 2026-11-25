using UnityEngine;

// Achievement #25, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG18_ConnectionAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 25;

    // Design-sheet implementation note: there is no connection-error action in MG18.
    // Therefore the hidden achievement is awarded on clearing this game.
    protected override bool DidMeetCondition() { return !disqualified; }

}
