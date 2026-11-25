using UnityEngine;
// Attach ONLY to the ACTUAL final ending screen object, not the general UI canvas.
public sealed class Achievement06_FinalEndingPanel : MonoBehaviour
{
    private void OnEnable() { MarkEndingSeen(); }
    public void MarkEndingSeen()
    {
        if (Application.isPlaying && AchievementManager.Instance != null)
            AchievementManager.Instance.Unlock(6);
    }
}
