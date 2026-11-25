using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Achievement #20, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG26_FirstCatchPerfectAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 20;

    private readonly char[] target = { 'S', 'T', 'A', 'R', 'T' };
    private int lastProgress;
    private GameObject firstTargetObject;
    private bool firstSeen;
    private bool allFive;
    protected override void ResetWatch()
    {
        lastProgress = 0; firstTargetObject = null;
        firstSeen = false; allFive = false;
    }
    protected override void Poll()
    {
        int progress = Get<int>("collectedCount");
        if (progress < lastProgress) disqualified = true; // Wrong receipt resets the catch sequence.
        if (progress > lastProgress)
        {
            if (!firstSeen || firstTargetObject != null && firstTargetObject.activeSelf)
                disqualified = true;
            if (progress >= 5) allFive = true;
            firstSeen = false;
            firstTargetObject = null;
        }
        lastProgress = progress;
        if (progress >= 5) return;
        var items = Field("activeLetters") as IEnumerable;
        if (items == null) return;
        bool stillInList = false;
        foreach (object item in items)
        {
            char letter = StandaloneFieldUtil.ReadChar(item, "character");
            GameObject obj = StandaloneFieldUtil.ReadGameObject(item, "obj");
            if (firstSeen && obj == firstTargetObject) stillInList = true;
            if (!firstSeen && letter == target[progress] && obj != null)
            {
                firstSeen = true;
                firstTargetObject = obj;
                stillInList = true;
            }
        }
        if (firstSeen && !stillInList && firstTargetObject == null) disqualified = true;
    }
    protected override bool DidMeetCondition() { return allFive && !disqualified; }

}
