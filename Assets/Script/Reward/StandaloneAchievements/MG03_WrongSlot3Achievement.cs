using UnityEngine;

// Achievement #21, original minigame scripts remain unchanged.
[DisallowMultipleComponent]
public sealed class MG03_WrongSlot3Achievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 21;

    protected override void Poll()
    {
        if (Get<int>("failCount") >= 3) Grant();
    }

}
