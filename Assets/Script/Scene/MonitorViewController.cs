using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기존 게임 화면(패널)의 부모를 바꾸지 않고,
/// 위치/크기만 조정해서 모니터 PNG의 투명 영역 안에 보이게 한다.
/// PlayZoom()이 호출되면 모니터 화면 쪽으로 확대한 뒤
/// 모든 패널을 원래 값으로 되돌리고 모니터 PNG를 숨긴다.
/// </summary>
public class MonitorViewController : MonoBehaviour
{
    [Header("모니터 PNG (Canvas_MonitorFrame 쪽)")]
    [Tooltip("책상+모니터 PNG가 들어 있는 Image의 RectTransform (MonitorFrame)")]
    public RectTransform monitorFrame;

    [Tooltip("모니터 투명 영역 위에 맞춰 둔 빈 RectTransform (MonitorHole)")]
    public RectTransform monitorHole;

    [Tooltip("Canvas_MonitorFrame 오브젝트 (확대가 끝나면 꺼짐)")]
    public GameObject monitorFrameCanvas;

    [Header("모니터 안에 넣을 기존 패널들 (부모는 바꾸지 않음)")]
    public List<RectTransform> screenPanels = new List<RectTransform>();

    public enum FitMode
    {
        Stretch,   // 모니터 화면에 딱 맞게 늘림 (여백 없음, 살짝 세로로 늘어남)
        FitInside, // 비율 유지, 위아래 검은 여백
        Fill       // 비율 유지, 꽉 채움 (양옆이 조금 잘림)
    }

    [Header("모니터 안 배치 방식")]
    public FitMode fitMode = FitMode.Stretch;

    [Header("확대 연출")]
    public float zoomDuration = 1.8f;

    [Tooltip("확대 끝에서 모니터 구멍이 화면보다 얼마나 더 커질지 (테두리가 확실히 밖으로 나가도록)")]
    public float zoomOvershoot = 1.05f;

    [Tooltip("확대 후반부에 모니터 PNG를 서서히 투명하게 할지")]
    public bool fadeFrameAtEnd = true;

    // 원래 값 저장용
    private class SavedRect
    {
        public RectTransform rect;
        public Canvas rootCanvas;
        public Vector3 localPosition;
        public Vector3 localScale;
    }

    private readonly List<SavedRect> saved = new List<SavedRect>();
    private Vector3 frameStartPos;
    private Vector3 frameStartScale;
    private CanvasGroup frameGroup;

    private bool monitorMode = false;
    private bool zooming = false;

    private void Awake()
    {
        // 패널들의 원래 위치/크기 저장
        foreach (RectTransform rt in screenPanels)
        {
            if (rt == null) continue;

            Canvas c = rt.GetComponentInParent<Canvas>(true);

            saved.Add(new SavedRect
            {
                rect = rt,
                rootCanvas = c != null ? c.rootCanvas : null,
                localPosition = rt.localPosition,
                localScale = rt.localScale
            });
        }

        if (monitorFrame != null)
        {
            frameStartPos = monitorFrame.localPosition;
            frameStartScale = monitorFrame.localScale;

            frameGroup = monitorFrame.GetComponent<CanvasGroup>();
            if (frameGroup == null)
                frameGroup = monitorFrame.gameObject.AddComponent<CanvasGroup>();

            // 모니터 PNG가 클릭을 막지 않도록
            frameGroup.blocksRaycasts = false;
            frameGroup.interactable = false;
            frameGroup.alpha = 1f;
        }

        // 챕터를 끝내고 돌아온 경우: 모니터 없이 바로 전체 화면
        // (StartMenuController.Start()가 이 값을 false로 바꾸기 전에 Awake에서 확인)
        if (SessionProgress.openChapterSelectOnLoad)
        {
            ExitMonitorMode();
            return;
        }

        if (monitorFrame == null || monitorHole == null)
        {
            Debug.LogWarning("[MonitorView] monitorFrame / monitorHole이 연결되지 않아 모니터 모드를 건너뜁니다.");
            ExitMonitorMode();
            return;
        }

        if (monitorFrameCanvas != null)
            monitorFrameCanvas.SetActive(true);

        monitorMode = true;
    }

    // 매 프레임 모니터 구멍 위치에 맞춰 패널을 배치
    // (해상도가 바뀌거나 확대 중이어도 항상 맞게 따라감)
    private void LateUpdate()
    {
        if (!monitorMode) return;
        FitPanelsToHole();
    }

    private void FitPanelsToHole()
    {
        // 모니터 구멍의 화면(픽셀) 좌표 구하기
        Canvas frameCanvas = monitorHole.GetComponentInParent<Canvas>();
        Camera frameCam = (frameCanvas != null &&
                           frameCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? frameCanvas.rootCanvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        monitorHole.GetWorldCorners(corners);
        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(frameCam, corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(frameCam, corners[2]);

        foreach (SavedRect s in saved)
        {
            if (s.rect == null || s.rootCanvas == null) continue;

            RectTransform canvasRT = s.rootCanvas.transform as RectTransform;
            Camera cam = s.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : s.rootCanvas.worldCamera;

            // 구멍 좌표를 이 Canvas의 로컬 좌표로 변환
            Vector2 localMin, localMax;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenMin, cam, out localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenMax, cam, out localMax);

            Vector2 holeSize = localMax - localMin;
            Vector2 holeCenter = (localMin + localMax) * 0.5f;
            Vector2 canvasSize = canvasRT.rect.size;

            if (canvasSize.x <= 0f || canvasSize.y <= 0f) continue;

            // 모니터 구멍에 맞는 배율
            float sx = holeSize.x / canvasSize.x;
            float sy = holeSize.y / canvasSize.y;
            if (fitMode == FitMode.FitInside) sx = sy = Mathf.Min(sx, sy);
            else if (fitMode == FitMode.Fill) sx = sy = Mathf.Max(sx, sy);
            sx = Mathf.Min(sx, 1f);
            sy = Mathf.Min(sy, 1f);

            // Canvas 중심 기준으로 축소한 뒤 구멍 중심으로 이동
            s.rect.localPosition = new Vector3(holeCenter.x + s.localPosition.x * sx,
                                               holeCenter.y + s.localPosition.y * sy,
                                               s.localPosition.z);
            s.rect.localScale = new Vector3(s.localScale.x * sx, s.localScale.y * sy, s.localScale.z);
        }
    }

    /// <summary>
    /// StartMenuController에서 정상화 연출이 끝난 직후 호출.
    /// </summary>
    public IEnumerator PlayZoom()
    {
        if (!monitorMode || zooming)
            yield break;

        zooming = true;

        RectTransform frameParent = monitorFrame.parent as RectTransform;

        // 시작 시점의 구멍 중심/크기 (MonitorFrame 부모 기준 로컬 좌표)
        Vector3[] corners = new Vector3[4];
        monitorHole.GetWorldCorners(corners);
        Vector3 min = frameParent.InverseTransformPoint(corners[0]);
        Vector3 max = frameParent.InverseTransformPoint(corners[2]);

        Vector2 holeCenter = (min + max) * 0.5f;
        Vector2 holeSize = max - min;
        Vector2 screenSize = frameParent.rect.size;
        Vector2 screenCenter = frameParent.rect.center;

        // 구멍이 화면을 완전히 덮을 때까지 필요한 배율
        float targetZoom = Mathf.Max(screenSize.x / holeSize.x, screenSize.y / holeSize.y) * zoomOvershoot;

        // 구멍 중심이 (프레임 피벗 기준으로) 어디 있는지
        Vector2 holeFromPivot = holeCenter - (Vector2)frameStartPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, zoomDuration);
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            // 지수 보간: 확대 속도가 일정하게 느껴짐
            float zoom = Mathf.Pow(targetZoom, u);

            // 구멍 중심이 화면 중앙으로 이동하도록 프레임 위치 계산
            Vector2 holeTarget = Vector2.Lerp(holeCenter, screenCenter, u);
            Vector2 framePos = holeTarget - holeFromPivot * zoom;

            monitorFrame.localScale = frameStartScale * zoom;
            monitorFrame.localPosition = new Vector3(framePos.x, framePos.y, frameStartPos.z);

            if (fadeFrameAtEnd && frameGroup != null)
                frameGroup.alpha = 1f - Mathf.InverseLerp(0.75f, 1f, u);

            yield return null; // LateUpdate에서 패널이 구멍을 따라감
        }

        ExitMonitorMode();
        zooming = false;
    }

    /// <summary>
    /// 패널을 원래대로 되돌리고 모니터 PNG를 숨김 (= 기존 전체 화면 게임)
    /// </summary>
    private void ExitMonitorMode()
    {
        monitorMode = false;

        foreach (SavedRect s in saved)
        {
            if (s.rect == null) continue;
            s.rect.localPosition = s.localPosition;
            s.rect.localScale = s.localScale;
        }

        if (monitorFrame != null)
        {
            monitorFrame.localPosition = frameStartPos;
            monitorFrame.localScale = frameStartScale;
        }

        if (monitorFrameCanvas != null)
            monitorFrameCanvas.SetActive(false);
    }

    // Play 중 Inspector의 ⋮ 메뉴에서 확대만 따로 테스트할 때 사용
    [ContextMenu("Test Zoom")]
    private void TestZoom()
    {
        if (Application.isPlaying)
            StartCoroutine(PlayZoom());
    }
}
