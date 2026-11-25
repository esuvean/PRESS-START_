using UnityEngine;

// Achievement #19, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG22_NoHintsUnder90Achievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 19;

    private bool hintAppeared;
    protected override void ResetWatch() { hintAppeared = false; }
    protected override void Poll()
    {
        var original = game as Minigame22_ScatteredStart;
        if (original && original.hintText != null &&
            original.hintText.text != null && original.hintText.text.StartsWith("힌트:"))
            hintAppeared = true;
    }
    protected override bool DidMeetCondition()
    {
        return !disqualified && Get<float>("elapsed", 999f) <= 90f && !hintAppeared;
    }

}
