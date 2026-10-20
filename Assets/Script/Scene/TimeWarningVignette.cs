using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니게임 남은 시간이 10초 이하가 되면 화면 가장자리(테두리)가 점점 빨개지는 연출.
/// 화면 가운데는 그대로 보이고, 바깥 라인만 붉게 물든다. 0초에 가까울수록 진하고 빠르게 깜빡인다.
///
/// ▶ 따로 설정할 필요 없음: ChapterFxBootstrap이 모든 챕터 씬에 게임 시작 시 자동으로 붙인다.
/// ▶ 게임 Canvas 안에 들어가므로 샷건 연출 때 모니터 화면 안으로 같이 줄어든다.
/// </summary>
public class TimeWarningVignette : MonoBehaviour
{
    public static TimeWarningVignette Instance { get; private set; }

    [Header("언제부터")]
    [Tooltip("남은 시간이 이 값(초) 이하가 되면 빨개지기 시작")]
    public float warningTime = 10f;

    [Header("모양")]
    public Color color = new Color(1f, 0f, 0f, 1f);
    [Tooltip("0초일 때 가장 진한 정도 (0~1)")]
    [Range(0f, 1f)] public float maxAlpha = 0.85f;
    [Tooltip("빨간 테두리 두께 (화면 높이 대비 비율)")]
    [Range(0.05f, 0.5f)] public float edgeWidth = 0.18f;

    [Header("깜빡임")]
    [Tooltip("경고가 시작될 때 깜빡이는 속도 (초당 횟수)")]
    public float pulseSpeedStart = 1f;
    [Tooltip("0초일 때 깜빡이는 속도 (초당 횟수)")]
    public float pulseSpeedEnd = 3f;
    [Tooltip("깜빡임 세기 (0 = 깜빡임 없음)")]
    [Range(0f, 1f)] public float pulseAmount = 0.35f;

    private RawImage image;
    private Canvas hostCanvas;
    private MinigameBase current;
    private bool suppressed = false;
    private float pulsePhase = 0f;

    // ---------------- 본체 ----------------

    [HideInInspector] public ChapterManager chapterManager;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (image != null) Destroy(image.gameObject);
    }

    private void Start()
    {
        Build();
    }

    /// <summary>샷건 연출이 끝나면 호출: 다음 미니게임이 시작될 때까지 빨간 테두리를 끈다.</summary>
    public void ClearUntilNextGame()
    {
        suppressed = true;
        if (image != null) SetAlpha(0f);
    }

    private void Build()
    {
        // 미니게임이 들어가는 Canvas 안에 만든다 (샷건 때 같이 줄어들도록)
        if (chapterManager != null && chapterManager.canvasTransform != null)
            hostCanvas = chapterManager.canvasTransform.GetComponentInParent<Canvas>();
        if (hostCanvas == null) hostCanvas = FindAnyObjectByType<Canvas>();
        if (hostCanvas == null) return;
        hostCanvas = hostCanvas.rootCanvas;

        GameObject go = new GameObject("TimeWarningVignette_Image", typeof(RectTransform));
        go.layer = hostCanvas.gameObject.layer;
        go.transform.SetParent(hostCanvas.transform, false);

        // 미니게임·실패 화면(40)보다 위, 샷건 모니터(50)보다 아래에 그린다
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 45;

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        image = go.AddComponent<RawImage>();
        image.texture = MakeEdgeTexture(edgeWidth);
        image.raycastTarget = false; // 클릭 방해 안 함
        SetAlpha(0f);
    }

    private void Update()
    {
        if (image == null) return;

        // 지금 진행 중인 미니게임 찾기 (새로 시작/다시 시작하면 바뀜)
        if (current == null || !current.isActiveAndEnabled)
        {
            MinigameBase found = FindAnyObjectByType<MinigameBase>();
            if (found != current)
            {
                current = found;
                suppressed = false; // 새 미니게임 → 다시 동작
            }
        }

        if (current == null) { SetAlpha(0f); return; }

        if (ChapterFxBootstrap.HideWarning(current)) { SetAlpha(0f); return; }

        float t = ChapterFxBootstrap.GetWarningTime(current);

        // 다시 시작해서 시간이 넉넉해지면 원래대로
        if (suppressed && t > warningTime) suppressed = false;

        if (suppressed || t > warningTime || (!current.IsGameActive && t > 0f))
        {
            SetAlpha(0f);
            pulsePhase = 0f;
            return;
        }

        // 10초 → 0 , 0초 → 1
        float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, warningTime));
        float strength = k * k * 0.75f + k * 0.25f; // 처음엔 은은하게, 끝으로 갈수록 빠르게 진해짐

        // 게임이 멈춰도(실패 화면) 깜빡이도록 unscaled 시간 사용
        float speed = Mathf.Lerp(pulseSpeedStart, pulseSpeedEnd, k);
        pulsePhase += Time.unscaledDeltaTime * speed * Mathf.PI * 2f;
        float pulse = 1f - pulseAmount * (0.5f + 0.5f * Mathf.Sin(pulsePhase));

        SetAlpha(maxAlpha * strength * pulse);
    }

    private void SetAlpha(float a)
    {
        Color col = color;
        col.a = a;
        image.color = col;
        image.enabled = a > 0.001f;
    }

    // 가운데는 투명, 가장자리로 갈수록 진해지는 테두리 그림을 코드로 만든다
    private static Texture2D MakeEdgeTexture(float width)
    {
        const int W = 256, H = 144;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float aspect = (float)W / H;
        Color32[] px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                // 가장 가까운 화면 끝까지의 거리 (화면 높이 기준)
                float u = (x + 0.5f) / W;
                float v = (y + 0.5f) / H;
                float dx = Mathf.Min(u, 1f - u) * aspect;
                float dy = Mathf.Min(v, 1f - v);
                float d = Mathf.Min(dx, dy);

                float a = 1f - Mathf.Clamp01(d / width);
                a = a * a; // 끝쪽은 진하고 안쪽으로 부드럽게 사라짐
                px[y * W + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
