
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 업적 16 - 마우스와 절교
// 조건: 10초 동안 마우스 이동 및 클릭 없이 클리어
// 기존 DontPress.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG16_NoMouseAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 16;

    private DontPress original;

    private Vector2 lastPointer;
    private bool sampled;

    // =====================================
    // 미니게임 시작 시 초기화
    // =====================================

    protected override void ResetWatch()
    {
        original = game as DontPress;

        lastPointer = MousePosition();
        sampled = true;

        Debug.Log(
            "[MG16 Achievement] 마우스 감지 시작"
        );
    }

    // =====================================
    // 마우스 이동 및 클릭 감지
    // =====================================

    protected override void Poll()
    {
        Vector2 currentPointer = MousePosition();

        // 마우스 이동 검사
        // 아주 작은 수치 오차만 허용
        if (sampled &&
            (currentPointer - lastPointer).sqrMagnitude > 0.0001f)
        {
            disqualified = true;
        }

        // 왼쪽 / 오른쪽 클릭 검사
        if (MouseClicked())
        {
            disqualified = true;
        }

        // 가운데 클릭 검사
        if (MiddleMouseClicked())
        {
            disqualified = true;
        }

        lastPointer = currentPointer;
        sampled = true;
    }

    private bool MiddleMouseClicked()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.middleButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(2);
#else
        return false;
#endif
    }

    // =====================================
    // 미니게임 클리어 시 업적 판정
    // =====================================

    protected override bool DidMeetCondition()
    {
        if (disqualified || original == null)
        {
            Debug.Log(
                "[MG16 Achievement] 업적 실패"
            );

            return false;
        }

        // 원본 미니게임의 실제 10초 타이머 확인
        bool waitedTenSeconds =
            original.waitTime >= 10f &&
            original.RemainingWaitTime <= 0f;

        Debug.Log(
            "[MG16 Achievement] 최종 판정: " +
            (waitedTenSeconds ? "업적 달성" : "업적 미달성")
        );

        return waitedTenSeconds;
    }
}
