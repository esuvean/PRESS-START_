using UnityEngine;
// Attach ONLY to the ACTUAL devil ending variant root. A devil CHAPTER CARD is not an ending.
public sealed class Achievement30_DevilEndingPanel : MonoBehaviour
{
    private void OnEnable() { MarkDevilEndingSeen(); }
    public void MarkDevilEndingSeen()
    {
        if (!Application.isPlaying || SessionProgress.chapter5FailCount < 3) return;
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.Unlock(30);
    }
}
