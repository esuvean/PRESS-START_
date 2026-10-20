using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 챕터 씬에서 미니게임 실수가 일정 횟수 쌓이면
/// 전체 화면 → 책상으로 줌아웃 → 샷건(키보드 내리치기) → 다시 줌인 해서 게임을 이어간다.
/// 기존 미니게임 오브젝트의 부모는 바꾸지 않고, 연출하는 동안만 크기/위치를 조정한 뒤 원래대로 되돌린다.
/// </summary>
public class ShotgunController : MonoBehaviour
{
    [Header("발동 조건")]
    [Tooltip("처음 샷건이 나오는 실수 횟수")]
    public int firstThreshold = 5;
    [Tooltip("그 다음부터는 실수 몇 번마다 나올지")]
    public int repeatThreshold = 7;
    [Tooltip("미니게임을 깨면 실수 횟수를 0으로")]
    public bool resetOnSuccess = true;
    [Tooltip("제한 시간이 다 되면 바로 샷건")]
    public bool shotgunOnTimeOut = false;
    [Tooltip("제한 시간이 다 되면 실패 화면(게임오버) 먼저 보여 주고 그 다음 샷건")]
    public FailPanelController failPanel;
    [Tooltip("게임오버 화면을 보여 준 뒤 샷건이 시작되기까지 기다리는 시간(초)")]
    public float gameOverHoldBeforeShotgun = 1f;

    [Header("연결 (자동 설정 메뉴가 채워 줌)")]
    public Canvas gameCanvas;                 // 미니게임들이 들어가는 기존 Canvas
    public GameObject monitorFrameCanvas;     // Canvas_MonitorFrame
    public RectTransform monitorFrame;        // 책상+모니터 Image
    public RectTransform monitorHole;         // 모니터 화면 영역
    public List<Sprite> shotgunFrames = new List<Sprite>();

    [Header("연출 시간")]
    public float zoomOutDuration = 0.8f;
    public float holdBeforeSlam = 0.3f;
    public float framesPerSecond = 12f;
    [Tooltip("주먹이 키보드에 닿는 프레임 번호 (0부터)")]
    public int impactFrame = 7;
    public float holdAfterSlam = 0.4f;
    public float zoomInDuration = 0.8f;
    public float shakeStrength = 25f;
    public float shakeDuration = 0.35f;

    [Header("사운드 (선택)")]
    public AudioSource audioSource;
    public AudioClip slamSound;

    // 내부 상태
    private int mistakeCount = 0;
    private int timesTriggered = 0;
    private bool playing = false;

    private Image frameImage;
    private CanvasGroup frameGroup;
    private CanvasGroup gameGroup;
    private Vector3 frameRestPos;
    private Vector3 frameRestScale;
    private Vector2 shakeOffset;

    private class Saved
    {
        public RectTransform rect;
        public Vector3 pos;
        public Vector3 scale;
    }
    private readonly List<Saved> saved = new List<Saved>();

    private void Awake()
    {
        if (monitorFrame != null)
        {
            frameImage = monitorFrame.GetComponent<Image>();
            frameGroup = monitorFrame.GetComponent<CanvasGroup>();
            if (frameGroup == null) frameGroup = monitorFrame.gameObject.AddComponent<CanvasGroup>();
            frameGroup.blocksRaycasts = false;
            frameGroup.interactable = false;
            frameRestPos = monitorFrame.localPosition;
            frameRestScale = monitorFrame.localScale;
            frameGroup.alpha = 0f; // 평소에는 안 보임 = 전체 화면 게임
        }

        if (monitorFrameCanvas != null)
            monitorFrameCanvas.SetActive(true);
    }

    private void OnEnable()
    {
        MinigameBase.OnMistake += HandleMistake;
        MinigameBase.OnGameSuccess += HandleSuccess;
        MinigameBase.OnTimeOut += HandleTimeOut;
    }

    private void OnDisable()
    {
        MinigameBase.OnMistake -= HandleMistake;
        MinigameBase.OnGameSuccess -= HandleSuccess;
        MinigameBase.OnTimeOut -= HandleTimeOut;
    }

    private void HandleTimeOut()
    {
        if (playing) return;
        if (failPanel != null && failPanel.IsShowing) return;
        if (!shotgunOnTimeOut && failPanel == null) return;
        // 자체 시간 규칙이 있는 미니게임(예: 안 누르고 기다리기, 사다리, 마지막 회로)은 제외
        if (ChapterFxBootstrap.UsesOwnTimeRule(FindAnyObjectByType<MinigameBase>())) return;
        StartCoroutine(TimeOutRoutine());
    }

    // 시간 초과: 게임오버 화면 먼저 → 잠깐 보여 준 뒤 샷건 → 다시 원래 화면(게임오버 화면은 그대로 남음)
    private IEnumerator TimeOutRoutine()
    {
        if (failPanel != null)
        {
            failPanel.Show();
            if (shotgunOnTimeOut) failPanel.SetButtonsEnabled(false); // 샷건 끝날 때까지 버튼 잠금
            yield return new WaitForSecondsRealtime(gameOverHoldBeforeShotgun);
        }

        if (shotgunOnTimeOut)
            yield return StartCoroutine(PlayShotgun());

        if (failPanel != null)
            failPanel.SetButtonsEnabled(true);

        // 샷건이 끝나면 빨간 테두리도 꺼서 화면을 원래대로
        if (TimeWarningVignette.Instance != null)
            TimeWarningVignette.Instance.ClearUntilNextGame();
    }

    private void HandleSuccess()
    {
        if (resetOnSuccess) mistakeCount = 0;
    }

    private void HandleMistake()
    {
        if (playing) return;

        mistakeCount++;
        int need = timesTriggered == 0 ? firstThreshold : repeatThreshold;

        if (mistakeCount >= need)
        {
            mistakeCount = 0;
            timesTriggered++;
            StartCoroutine(PlayShotgun());
        }
    }

    // Play 중 Inspector ⋮ 메뉴에서 바로 테스트
    [ContextMenu("Test Shotgun")]
    public void TestShotgun()
    {
        if (Application.isPlaying && !playing)
            StartCoroutine(PlayShotgun());
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 에디터 테스트용: F9 키로 바로 샷건
        if (Input.GetKeyDown(KeyCode.F9)) TestShotgun();
        // 에디터 테스트용: F10 키로 시간 초과 흉내
        if (Input.GetKeyDown(KeyCode.F10) && !playing && (failPanel == null || !failPanel.IsShowing))
            StartCoroutine(TimeOutRoutine());
    }
#endif

    public IEnumerator PlayShotgun()
    {
        if (playing || gameCanvas == null || monitorFrame == null || monitorHole == null || monitorFrameCanvas == null)
            yield break;

        playing = true;

        // 게임 멈추고 클릭 막기
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        gameGroup = gameCanvas.GetComponent<CanvasGroup>();
        bool addedGroup = false;
        if (gameGroup == null) { gameGroup = gameCanvas.gameObject.AddComponent<CanvasGroup>(); addedGroup = true; }
        bool oldBlocks = gameGroup.blocksRaycasts;
        gameGroup.blocksRaycasts = false;

        // 실패 메시지를 잠깐 보여 준 뒤 시작
        yield return new WaitForSecondsRealtime(0.3f);

        // 지금 Canvas 안에 있는 것들(현재 미니게임 등)의 원래 값 저장
        saved.Clear();
        foreach (Transform child in gameCanvas.transform)
        {
            RectTransform rt = child as RectTransform;
            if (rt == null) continue;
            saved.Add(new Saved { rect = rt, pos = rt.localPosition, scale = rt.localScale });
        }

        if (frameImage != null && shotgunFrames.Count > 0)
            frameImage.sprite = shotgunFrames[0];

        shakeOffset = Vector2.zero;
        ApplyFrame(1f);
        frameGroup.alpha = 1f;

        // 확대된 상태(구멍이 화면을 꽉 채운 상태)에서 시작해서 줌아웃
        yield return Zoom(1f, 0f, zoomOutDuration);

        yield return new WaitForSecondsRealtime(holdBeforeSlam);

        // 샷건 애니메이션
        float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
        for (int i = 0; i < shotgunFrames.Count; i++)
        {
            if (frameImage != null) frameImage.sprite = shotgunFrames[i];

            if (i == impactFrame)
            {
                if (audioSource != null && slamSound != null) audioSource.PlayOneShot(slamSound);
                StartCoroutine(Shake());
            }

            float t = 0f;
            while (t < frameTime)
            {
                t += Time.unscaledDeltaTime;
                ApplyFrame(0f);
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(holdAfterSlam);

        if (frameImage != null && shotgunFrames.Count > 0)
            frameImage.sprite = shotgunFrames[0];

        // 다시 줌인
        yield return Zoom(0f, 1f, zoomInDuration);

        // 원래대로 복구
        frameGroup.alpha = 0f;
        RestoreAll();

        gameGroup.blocksRaycasts = oldBlocks;
        if (addedGroup) Destroy(gameGroup);

        // 게임오버 화면이 떠 있으면 게임은 계속 멈춘 상태로 둔다 (RETRY/EXIT 누를 때까지)
        if (failPanel != null && failPanel.IsShowing)
            Time.timeScale = 0f;
        else
            Time.timeScale = oldTimeScale == 0f ? 1f : oldTimeScale;
        playing = false;
    }

    // u = 0 : 책상 전체가 보이는 상태 / u = 1 : 모니터 화면이 전체 화면을 꽉 채운 상태
    private IEnumerator Zoom(float from, float to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float u = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            ApplyFrame(u);
            yield return null;
        }
        ApplyFrame(to);
    }

    private float currentU = 0f;

    private void ApplyFrame(float u)
    {
        currentU = u;
        RectTransform parent = monitorFrame.parent as RectTransform;

        // 책상 상태(rest)에서의 구멍 위치
        monitorFrame.localPosition = frameRestPos;
        monitorFrame.localScale = frameRestScale;

        Vector3[] c = new Vector3[4];
        monitorHole.GetWorldCorners(c);
        Vector2 min = parent.InverseTransformPoint(c[0]);
        Vector2 max = parent.InverseTransformPoint(c[2]);
        Vector2 holeCenter = (min + max) * 0.5f;
        Vector2 holeSize = max - min;
        Vector2 screen = parent.rect.size;
        if (holeSize.x <= 0f || holeSize.y <= 0f) return;

        float targetZoom = Mathf.Max(screen.x / holeSize.x, screen.y / holeSize.y) * 1.05f;
        float zoom = Mathf.Pow(targetZoom, u);
        Vector2 holeFromPivot = holeCenter - (Vector2)frameRestPos;
        Vector2 holeTarget = Vector2.Lerp(holeCenter, parent.rect.center, u);
        Vector2 framePos = holeTarget - holeFromPivot * zoom + shakeOffset;

        monitorFrame.localScale = frameRestScale * zoom;
        monitorFrame.localPosition = new Vector3(framePos.x, framePos.y, frameRestPos.z);

        FitGameToHole();
    }

    // 기존 미니게임 화면을 모니터 구멍 안에 맞추기 (늘려서 꽉 채움)
    private void FitGameToHole()
    {
        Vector3[] c = new Vector3[4];
        monitorHole.GetWorldCorners(c);
        Canvas frameCanvas = monitorHole.GetComponentInParent<Canvas>().rootCanvas;
        Camera fcam = frameCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : frameCanvas.worldCamera;
        Vector2 sMin = RectTransformUtility.WorldToScreenPoint(fcam, c[0]);
        Vector2 sMax = RectTransformUtility.WorldToScreenPoint(fcam, c[2]);

        Canvas root = gameCanvas.rootCanvas;
        RectTransform canvasRT = root.transform as RectTransform;
        Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        Vector2 lMin, lMax;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sMin, cam, out lMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sMax, cam, out lMax);

        Vector2 size = canvasRT.rect.size;
        if (size.x <= 0f || size.y <= 0f) return;

        float sx = Mathf.Min((lMax.x - lMin.x) / size.x, 1f);
        float sy = Mathf.Min((lMax.y - lMin.y) / size.y, 1f);
        Vector2 center = (lMin + lMax) * 0.5f;

        foreach (Saved s in saved)
        {
            if (s.rect == null) continue;
            s.rect.localPosition = new Vector3(center.x + s.pos.x * sx, center.y + s.pos.y * sy, s.pos.z);
            s.rect.localScale = new Vector3(s.scale.x * sx, s.scale.y * sy, s.scale.z);
        }
    }

    private IEnumerator Shake()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float power = shakeStrength * (1f - t / shakeDuration);
            shakeOffset = Random.insideUnitCircle * power;
            yield return null;
        }
        shakeOffset = Vector2.zero;
    }

    private void RestoreAll()
    {
        foreach (Saved s in saved)
        {
            if (s.rect == null) continue;
            s.rect.localPosition = s.pos;
            s.rect.localScale = s.scale;
        }
        saved.Clear();

        monitorFrame.localPosition = frameRestPos;
        monitorFrame.localScale = frameRestScale;
    }

    private void OnDestroy()
    {
        // 연출 중에 씬이 바뀌어도 게임이 멈춘 채로 남지 않게
        if (playing) Time.timeScale = 1f;
    }
}
