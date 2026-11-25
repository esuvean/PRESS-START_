
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 업적 10 - 휠은 장식입니다
// 조건: 마우스 휠 사용 없이
// Scroll View를 마우스로 드래그하여 클리어

[DisallowMultipleComponent]
public sealed class MG07_DragOnlyAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 10;

    private Minigame7_TermsScroll original;
    private ScrollRect scrollRect;
    private RectTransform dragArea;

    private Camera uiCamera;

    private bool startedOnScrollView;
    private bool usedDrag;

    private Vector2 mouseDownPosition;
    private float previousScrollPosition;

    // ====================================
    // 미니게임 시작 시 초기화
    // ====================================

    protected override void ResetWatch()
    {
        original = game as Minigame7_TermsScroll;

        startedOnScrollView = false;
        usedDrag = false;

        if (original == null)
        {
            Debug.LogWarning(
                "[MG07 Achievement] 원본 게임을 찾지 못했습니다."
            );
            return;
        }

        scrollRect = original.termsScroll;

        if (scrollRect == null)
        {
            Debug.LogWarning(
                "[MG07 Achievement] Scroll View 연결이 없습니다."
            );
            return;
        }

        // 기존 Scroll View의 Viewport를 감지 영역으로 사용
        dragArea = scrollRect.viewport;

        if (dragArea == null)
        {
            dragArea = scrollRect.GetComponent<RectTransform>();
        }

        Canvas canvas = scrollRect.GetComponentInParent<Canvas>();

        uiCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        previousScrollPosition =
            scrollRect.verticalNormalizedPosition;

        Debug.Log("[MG07 Achievement] 드래그 감지 시작");
    }

    // ====================================
    // 마우스 조작 감지
    // ====================================

    protected override void Poll()
    {
        if (scrollRect == null || dragArea == null)
            return;

        // 마우스 휠을 사용하면 업적 실패
        if (MouseWheelMoved())
        {
            disqualified = true;

            Debug.Log(
                "[MG07 Achievement] 마우스 휠 사용 - 업적 실패"
            );
        }

        bool mouseDown = false;
        bool mouseHeld = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseDown =
                Mouse.current.leftButton.wasPressedThisFrame;

            mouseHeld =
                Mouse.current.leftButton.isPressed;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        mouseDown = Input.GetMouseButtonDown(0);
        mouseHeld = Input.GetMouseButton(0);
#endif

        Vector2 mousePosition = MousePosition();

        // Scroll View 안에서 마우스를 누른 경우
        if (mouseDown)
        {
            startedOnScrollView =
                RectTransformUtility.RectangleContainsScreenPoint(
                    dragArea,
                    mousePosition,
                    uiCamera
                );

            mouseDownPosition = mousePosition;
        }

        float currentScrollPosition =
            scrollRect.verticalNormalizedPosition;

        // 마우스를 누른 상태로 실제 스크롤 이동 확인
        if (startedOnScrollView && mouseHeld)
        {
            float mouseMovement =
                Vector2.Distance(
                    mouseDownPosition,
                    mousePosition
                );

            float scrollMovement =
                Mathf.Abs(
                    currentScrollPosition -
                    previousScrollPosition
                );

            if (mouseMovement >= 8f &&
                scrollMovement > 0.0005f)
            {
                usedDrag = true;
            }
        }

        if (!mouseHeld)
        {
            startedOnScrollView = false;
        }

        previousScrollPosition = currentScrollPosition;
    }

    // ====================================
    // 업적 최종 판정
    // ====================================

    protected override bool DidMeetCondition()
    {
        bool success = !disqualified && usedDrag;

        Debug.Log(
            "[MG07 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
