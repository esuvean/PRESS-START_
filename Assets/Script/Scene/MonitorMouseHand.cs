using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 메인 화면 책상 그림의 '마우스 잡은 손'이 실제 마우스를 따라 살짝 움직이게 함.
/// 씬에 따로 붙일 필요 없음 (MonitorViewController가 있는 씬이 열리면 자동으로 생김)
///
/// 그림 파일: Assets/Resources/MainMonitor/
///   moniter_bg   = 손을 지운 책상+모니터 그림
///   moniter_hand = 손+마우스만 잘라낸 그림 (같은 1920x1080 크기라 위치가 딱 맞음)
/// </summary>
public class MonitorMouseHand : MonoBehaviour
{
    [Tooltip("손이 움직이는 최대 거리 (그림 픽셀 기준, 가로/세로)")]
    public Vector2 range = new Vector2(10f, 6f);

    [Tooltip("따라오는 속도 (클수록 바로바로 따라옴)")]
    public float followSpeed = 10f;

    [Tooltip("클릭할 때 손이 눌리는 거리 (그림 픽셀)")]
    public float clickPress = 3f;

    private const float ImageWidth = 1920f;

    private RectTransform rect;
    private RectTransform frame;
    private Vector2 current;
    private float press;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate();
    }

    private static void TryCreate()
    {
        MonitorViewController view =
            Object.FindAnyObjectByType<MonitorViewController>(FindObjectsInactive.Include);

        if (view == null || view.monitorFrame == null)
            return;

        if (view.monitorFrame.Find("MouseHand") != null)
            return;

        Sprite bg = Resources.Load<Sprite>("MainMonitor/moniter_bg");
        Sprite hand = Resources.Load<Sprite>("MainMonitor/moniter_hand");

        if (bg == null || hand == null)
        {
            Debug.LogWarning("[MonitorMouseHand] Resources/MainMonitor 그림을 찾지 못했습니다.");
            return;
        }

        // 책상 그림을 '손 없는 그림'으로 바꾸고
        Image frameImage = view.monitorFrame.GetComponent<Image>();
        if (frameImage != null)
            frameImage.sprite = bg;

        // 그 위에 손 그림을 같은 크기로 겹쳐 놓음
        GameObject go = new GameObject(
            "MouseHand",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        go.layer = view.monitorFrame.gameObject.layer;
        go.transform.SetParent(view.monitorFrame, false);
        go.transform.SetAsFirstSibling();

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = hand;
        img.raycastTarget = false;
        if (frameImage != null)
            img.preserveAspect = frameImage.preserveAspect;

        MonitorMouseHand follow = go.AddComponent<MonitorMouseHand>();
        follow.rect = rt;
        follow.frame = view.monitorFrame;
    }

    private void Update()
    {
        if (rect == null || frame == null)
            return;

        // 화면 가운데 = 0, 끝 = ±1
        Vector2 pointer = PointerPosition();
        float nx = Mathf.Clamp(pointer.x / Mathf.Max(1f, Screen.width) * 2f - 1f, -1f, 1f);
        float ny = Mathf.Clamp(pointer.y / Mathf.Max(1f, Screen.height) * 2f - 1f, -1f, 1f);

        Vector2 target = new Vector2(nx * range.x, ny * range.y);

        float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        current = Vector2.Lerp(current, target, k);

        // 클릭하면 손이 살짝 눌림
        float pressTarget = MouseHeld() ? clickPress : 0f;
        press = Mathf.Lerp(press, pressTarget, 1f - Mathf.Exp(-30f * Time.unscaledDeltaTime));

        // 그림 픽셀 → 화면(Canvas) 단위
        float unit = frame.rect.width / ImageWidth;

        rect.anchoredPosition = new Vector2(current.x, current.y - press) * unit;
    }

    private static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null) return Pointer.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#endif
    }

    private static bool MouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }
}
