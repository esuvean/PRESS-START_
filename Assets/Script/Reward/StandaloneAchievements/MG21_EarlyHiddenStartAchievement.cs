using UnityEngine;

// Achievement #18, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG21_EarlyHiddenStartAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 18;

    private bool foundEarly, sawRevealed;
    protected override void ResetWatch() { foundEarly = sawRevealed = false; }
    protected override void Poll()
    {
        if (sawRevealed || !Get<bool>("startRevealed")) return;
        sawRevealed = true;
        var original = game as Minigame21_HiddenTerms;
        if (original && original.termsScrollRect != null)
            foundEarly = original.termsScrollRect.verticalNormalizedPosition > 0.02f;
    }
    protected override bool DidMeetCondition() { return foundEarly && !disqualified; }

}
