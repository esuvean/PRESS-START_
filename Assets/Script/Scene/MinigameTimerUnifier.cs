using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 미니게임의 타이머를 미니게임1 타이머와 똑같은 모양으로 맞춘다.
///  - 모양: "TIME 00:00", Galmuri14 글꼴, 크기 36, 흰색, 오른쪽 위 (미니게임1 TimerText 위치)
///  - 시간: 각 미니게임이 원래 쓰던 남은 시간 그대로
/// 챕터 씬이 열리면 자동으로 생기므로 씬/프리팹을 따로 고칠 필요 없음.
/// </summary>
public class MinigameTimerUnifier : MonoBehaviour
{
    // 미니게임1 TimerText 설정값
    private const string FontName = "Galmuri14_Regular SDF";
    private const float FontSize = 36f;
    private static readonly Vector2 Position = new Vector2(670f, 449f);
    private static readonly Vector2 Size = new Vector2(200f, 50f);
    private const string Format = "TIME {0:00}:{1:00}";

    // 미니게임 안에서 타이머로 쓰이는 글자 오브젝트 이름
    private static readonly string[] TimerNames = { "TimerText", "TimeText", "Timer", "Ttime" };

    private MinigameBase current;
    private TMP_Text timerText;
    private TMP_FontAsset font;

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
        if (Object.FindAnyObjectByType<ChapterManager>() == null) return;
        if (Object.FindAnyObjectByType<MinigameTimerUnifier>() != null) return;

        new GameObject("MinigameTimerUnifier").AddComponent<MinigameTimerUnifier>();
    }

    private void Start()
    {
        // 예전 타이머 통일 스크립트가 씬에 있으면 겹치지 않게 끔
        foreach (ChapterTimerUI old in FindObjectsByType<ChapterTimerUI>(FindObjectsSortMode.None))
            old.enabled = false;
    }

    private void LateUpdate()
    {
        if (current == null || timerText == null)
        {
            current = FindAnyObjectByType<MinigameBase>();
            timerText = null;
            if (current == null) return;
            timerText = FindOrCreateTimer(current.transform);
        }

        float t = Mathf.Max(0f, GetRemainingTime(current));
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timerText.text = string.Format(Format, minutes, seconds);
    }

    // 각 미니게임이 원래 쓰던 남은 시간
    private static float GetRemainingTime(MinigameBase game)
    {
        System.Type type = game.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 19번: 도움말이 나오기까지 남은 시간을 타이머로 보여 주던 게임
        if (type.Name == "Minigame19_StartLadder")
        {
            FieldInfo help = type.GetField("autoHelpTime", flags);
            FieldInfo elapsed = type.GetField("elapsedTime", flags);
            if (help != null && elapsed != null)
                return (float)help.GetValue(game) - (float)elapsed.GetValue(game);
        }

        // 16번(DontPress)의 remainingTime은 '누르지 말고 기다리기' 시간이라 제외
        if (type.Name != "DontPress")
        {
            FieldInfo own = type.GetField("remainingTime", flags);
            if (own != null && own.FieldType == typeof(float))
                return (float)own.GetValue(game);
        }

        return game.RemainingTime;
    }

    private TMP_Text FindOrCreateTimer(Transform root)
    {
        TMP_Text t = null;

        foreach (TMP_Text child in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string n = child.name.Trim();
            if (System.Array.IndexOf(TimerNames, n) >= 0) { t = child; break; }
        }

        if (t == null)
        {
            GameObject go = new GameObject("TimerText", typeof(RectTransform));
            go.layer = root.gameObject.layer;
            t = go.AddComponent<TextMeshProUGUI>();
        }

        if (font == null)
        {
            foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (f.name == FontName) { font = f; break; }
        }

        t.transform.SetParent(root, false);
        t.gameObject.SetActive(true);

        if (font != null)
        {
            t.font = font;
            t.fontSharedMaterial = font.material;
        }

        t.fontSize = FontSize;
        t.enableAutoSizing = false;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.margin = Vector4.zero;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Position;
        rt.sizeDelta = Size;
        rt.SetAsLastSibling();

        return t;
    }
}
