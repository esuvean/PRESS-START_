
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 업적 07 - 도망쳐 봤자야
// 조건: 20초 이내, Ctrl 키 사용 없이 클리어
[DisallowMultipleComponent]
public sealed class MG01_FastClearAchievement : AchievementStandaloneWatcher
{
    protected override int AchievementId => 7;

    private bool usedControl;

    protected override void ResetWatch()
    {
        usedControl = false;
    }

    protected override void Poll()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed)
            {
                usedControl = true;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            usedControl = true;
        }
#endif
    }

    protected override bool DidMeetCondition()
    {
        return !disqualified &&
               Seconds <= 20f &&
               !usedControl;
    }
}
