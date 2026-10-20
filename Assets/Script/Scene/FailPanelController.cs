using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제한 시간이 끝났을 때 나오는 실패 화면.
/// 반투명 어두운 배경 + TIME OVER / FAILED + RETRY / EXIT 버튼.
/// 화면은 실행할 때 코드로 만들어지므로 Hierarchy에서 따로 꾸밀 필요 없음.
/// </summary>
public class FailPanelController : MonoBehaviour
{
    [Header("연결 (자동 설정 메뉴가 채워 줌)")]
    public ChapterManager chapterManager;
    public TMP_FontAsset font;

    [Header("문구")]
    public string titleText = "TIME OVER";
    public string subText = "FAILED";
    public string retryText = "RETRY";
    public string exitText = "EXIT";

    [Header("색")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
    public Color titleColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color subColor = new Color(1f, 0.3f, 0.45f, 1f);

    private GameObject root;
    private bool showing = false;
    private float oldTimeScale = 1f;

    public bool IsShowing => showing;

    public void Show()
    {
        if (showing) return;
        if (root == null) Build();

        showing = true;
        oldTimeScale = Time.timeScale == 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f; // 뒤에서 게임이 계속 진행되지 않게
        root.SetActive(true);
    }

    /// <summary>샷건 연출 중에는 RETRY/EXIT 버튼을 못 누르게 막는다.</summary>
    public void SetButtonsEnabled(bool on)
    {
        if (root == null) return;
        CanvasGroup g = root.GetComponent<CanvasGroup>();
        if (g == null) g = root.AddComponent<CanvasGroup>();
        g.interactable = on;
        g.blocksRaycasts = true; // 뒤쪽 게임 클릭은 계속 막기
    }

    private void Hide()
    {
        showing = false;
        if (root != null) root.SetActive(false);
        Time.timeScale = oldTimeScale;
    }

    private void OnRetry()
    {
        Hide();
        if (chapterManager != null) chapterManager.RestartCurrentMinigame();
    }

    private void OnExit()
    {
        Time.timeScale = 1f;
        showing = false;
        if (chapterManager != null) chapterManager.ExitToMainScene();
    }

    private void OnDestroy()
    {
        if (showing) Time.timeScale = 1f;
        if (root != null) Destroy(root);
    }

    // ---------------- 화면 만들기 ----------------

    private void Build()
    {
        root = new GameObject("FailPanel", typeof(RectTransform));

        // 미니게임이 들어가는 Canvas 안에 만든다
        // → 샷건 연출 때 실패 화면도 모니터 화면 안으로 같이 줄어들었다가 돌아온다
        Canvas host = null;
        if (chapterManager != null && chapterManager.canvasTransform != null)
            host = chapterManager.canvasTransform.GetComponentInParent<Canvas>();
        Canvas canvas;
        if (host != null)
        {
            host = host.rootCanvas;
            root.layer = host.gameObject.layer;
            root.transform.SetParent(host.transform, false);
            Stretch((RectTransform)root.transform);
            canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
        }
        else
        {
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        canvas.sortingOrder = 40; // 게임보다 위, 빨간 테두리(45)·샷건 모니터(50)보다 아래
        root.AddComponent<GraphicRaycaster>();

        // 반투명 어두운 배경 (뒤쪽 클릭 막기)
        Image bg = NewUI<Image>("Dim", root.transform);
        Stretch(bg.rectTransform);
        bg.color = backgroundColor;
        bg.raycastTarget = true;

        NewText("Title", root.transform, titleText, 130, titleColor, new Vector2(0, 170));
        NewText("Sub", root.transform, subText, 64, subColor, new Vector2(0, 40));

        NewButton("RetryButton", root.transform, retryText, new Vector2(0, -110)).onClick.AddListener(OnRetry);
        NewButton("ExitButton", root.transform, exitText, new Vector2(0, -230)).onClick.AddListener(OnExit);

        root.SetActive(false);
    }

    private T NewUI<T>(string name, Transform parent) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, Color color, Vector2 pos)
    {
        TextMeshProUGUI t = NewUI<TextMeshProUGUI>(name, parent);
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.sizeDelta = new Vector2(1400, size * 1.4f);
        rt.anchoredPosition = pos;
        return t;
    }

    private Button NewButton(string name, Transform parent, string label, Vector2 pos)
    {
        Image img = NewUI<Image>(name, parent);
        img.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        RectTransform rt = img.rectTransform;
        rt.sizeDelta = new Vector2(380, 90);
        rt.anchoredPosition = pos;

        Outline outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);

        Button b = img.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.55f, 0.55f, 0.7f, 1f);
        cb.pressedColor = new Color(0.4f, 0.4f, 0.5f, 1f);
        b.colors = cb;

        TMP_Text t = NewText("Text", img.transform, label, 44, Color.white, Vector2.zero);
        t.rectTransform.sizeDelta = rt.sizeDelta;
        return b;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
