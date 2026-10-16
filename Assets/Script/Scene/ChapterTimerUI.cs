using TMPro;
using UnityEngine;

/// <summary>
/// 지금 진행 중인 미니게임의 남은 시간을 오른쪽 위에 "TIME 00:00" 으로 보여 준다.
/// 미니게임 프리팹을 고치지 않고, 미니게임이 생길 때마다 타이머 글자를 붙여 준다.
/// 글꼴/크기/위치는 챕터 3 타이머(Minigame_11Panel의 TimerText)와 똑같이 맞춘다.
/// </summary>
public class ChapterTimerUI : MonoBehaviour
{
    [Header("모양 (자동 설정 메뉴가 챕터 3 타이머에서 복사)")]
    public TMP_FontAsset font;
    public Material fontMaterial;
    public float fontSize = 36f;
    public Color color = Color.white;
    public TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft;
    public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
    public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
    public Vector2 anchoredPosition = new Vector2(692f, 446f);
    public Vector2 sizeDelta = new Vector2(200f, 50f);
    public Vector2 pivot = new Vector2(0.5f, 0.5f);
    public string format = "TIME {0:00}:{1:00}";

    private MinigameBase current;
    private TMP_Text timerText;

    private void LateUpdate()
    {
        if (current == null)
        {
            current = FindAnyObjectByType<MinigameBase>();
            timerText = null;
            if (current == null) return;
            timerText = CreateOrReuseText(current.transform);
        }

        if (timerText == null) return;

        float t = Mathf.Max(0f, current.RemainingTime);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timerText.text = string.Format(format, minutes, seconds);
    }

    private TMP_Text CreateOrReuseText(Transform minigameRoot)
    {
        TMP_Text t = null;

        // 프리팹에 이미 TimerText가 있으면 그걸 사용 (예: 10번)
        foreach (TMP_Text child in minigameRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            string n = child.name.Trim();
            if (n == "TimerText" || n == "TimeText") { t = child; break; }
        }

        if (t == null)
        {
            GameObject go = new GameObject("TimerText", typeof(RectTransform));
            go.layer = minigameRoot.gameObject.layer;
            go.transform.SetParent(minigameRoot, false);
            t = go.AddComponent<TextMeshProUGUI>();
        }

        t.gameObject.SetActive(true);
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = alignment;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        rt.SetAsLastSibling(); // 다른 그림에 가려지지 않게
        return t;
    }
}
