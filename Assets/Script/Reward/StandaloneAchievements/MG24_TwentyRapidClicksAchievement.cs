using UnityEngine;
using UnityEngine.UI;

// Achievement #27, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG24_TwentyRapidClicksAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 27;

    [Header("Maximum gap allowed between rapid clicks")]
    public float maxGapSeconds = 0.8f;
    private bool hooked;
    private float lastClick;
    private int streak;
    protected override void ResetWatch() { hooked = false; lastClick = -100f; streak = 0; }
    protected override void Poll()
    {
        if (hooked) return;
        var original = game as Minigame24_WaitAt99;
        if (!original || !original.completeButton) return;
        original.completeButton.onClick.AddListener(OnPress);
        hooked = true;
    }
    private void OnPress()
    {
        if (!game || !game.IsGameActive) return;
        // Only clicks made during the 99% interaction count.
        object state = Field("currentState");
        if (state == null || !state.ToString().Equals("WaitingAt99")) return;
        if (Time.unscaledTime - lastClick > maxGapSeconds) streak = 0;
        streak++;
        lastClick = Time.unscaledTime;
        if (streak >= 20) Grant();
    }

}
