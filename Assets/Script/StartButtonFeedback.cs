using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 모든 챕터 미니게임에서 "진짜 START"를 눌러 성공했을 때 나오는 연출.
///
/// MinigameBase.Success()가 자동으로 불러주기 때문에
/// 씬이나 프리팹에 따로 붙일 필요가 없습니다. (처음 한 번 실행될 때 스스로 만들어짐)
///
/// 연출 내용
///  1) 눌린 START 버튼이 꾹 눌렸다가 통! 튀어오름
///  2) 누른 자리에서 노란 링이 퍼지고, 픽셀 조각이 사방으로 튐
///  3) "START!" 글자가 튀어나와 위로 떠오름
///  4) 화면이 아주 짧게 하얗게 번쩍
///  5) 소리: 딸깍 + 8비트 성공음 "띠-리-링↑"
///
/// 소리 크기는 설정 화면의 볼륨(AudioListener.volume)을 그대로 따라갑니다.
/// </summary>
public class StartButtonFeedback : MonoBehaviour
{
    // ==============================
    // 조절하고 싶으면 여기 숫자만 바꾸면 됩니다
    // ==============================

    /// <summary>성공 연출을 보여주고 다음 게임으로 넘어가기까지 기다리는 시간(초)</summary>
    public const float SuccessDelay = 0.65f;

    private const float SoundVolume = 0.6f;
    private const string PopupMessage = "START!";

    private static readonly Color Yellow = new Color32(254, 230, 35, 255); // #FEE623 (버튼 hover 색과 같음)
    private static readonly Color White = Color.white;

    // ==============================

    private static StartButtonFeedback instance;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Image inputBlocker;
    private Image screenFlash;
    private AudioSource audioSource;
    private AudioClip clickClip;
    private AudioClip successClip;
    private Sprite ringSprite;
    private Sprite pixelSprite;

    private static StartButtonFeedback Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[StartButtonFeedback]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<StartButtonFeedback>();
                instance.Build();
            }
            return instance;
        }
    }

    /// <summary>
    /// 성공 연출을 재생하고, SuccessDelay 초 뒤에 onFinished를 실행합니다.
    /// </summary>
    public static void PlaySuccess(Transform minigameRoot, Action onFinished)
    {
        Instance.StartCoroutine(Instance.SuccessRoutine(minigameRoot, onFinished));
    }

    // ==============================
    // 연출 순서
    // ==============================

    private IEnumerator SuccessRoutine(Transform minigameRoot, Action onFinished)
    {
        Vector2 screenPos = GetPointerPosition();
        RectTransform pressedButton = FindPressedButton(minigameRoot, screenPos);

        // 마우스가 버튼 위에 없으면(예: 기다려서 성공하는 게임) 버튼 자리에서 터지게 함
        if (pressedButton != null && !IsPointerOver(pressedButton, screenPos))
            screenPos = RectTransformUtility.WorldToScreenPoint(GetCam(pressedButton), pressedButton.position);

        // 연출 중에는 다른 걸 클릭하지 못하게 막기
        inputBlocker.gameObject.SetActive(true);

        PlaySound(clickClip, 0.8f);
        PlaySound(successClip, 1f);

        if (pressedButton != null)
            StartCoroutine(PunchButton(pressedButton));

        Vector2 localPos = ScreenToLocal(screenPos);
        StartCoroutine(ScreenFlash());
        StartCoroutine(Ring(localPos, 0f));
        StartCoroutine(Ring(localPos, 0.08f));
        SpawnSparks(localPos);
        StartCoroutine(PopupText(localPos));

        yield return new WaitForSecondsRealtime(SuccessDelay);

        inputBlocker.gameObject.SetActive(false);

        onFinished?.Invoke();
    }

    // ==============================
    // 1) 눌린 버튼 꾹 → 통!
    // ==============================

    private IEnumerator PunchButton(RectTransform target)
    {
        Vector3 baseScale = target.localScale;

        yield return ScaleTo(target, baseScale, baseScale * 0.85f, 0.06f);
        if (target == null) yield break;
        yield return ScaleTo(target, target.localScale, baseScale * 1.15f, 0.09f);
        if (target == null) yield break;
        yield return ScaleTo(target, target.localScale, baseScale, 0.12f);
    }

    private IEnumerator ScaleTo(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (target == null) yield break;
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.LerpUnclamped(from, to, EaseOut(t / duration));
            yield return null;
        }
        if (target != null)
            target.localScale = to;
    }

    // ==============================
    // 2) 링 + 픽셀 조각
    // ==============================

    private IEnumerator Ring(Vector2 pos, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        Image ring = CreateImage("Ring", ringSprite, Yellow);
        ring.rectTransform.anchoredPosition = pos;

        float duration = 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = EaseOut(t / duration);
            float size = Mathf.Lerp(40f, 260f, k);
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            ring.color = new Color(Yellow.r, Yellow.g, Yellow.b, 1f - k);
            yield return null;
        }
        Destroy(ring.gameObject);
    }

    private void SpawnSparks(Vector2 pos)
    {
        const int count = 14;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + UnityEngine.Random.Range(-10f, 10f);
            float distance = UnityEngine.Random.Range(110f, 190f);
            float size = UnityEngine.Random.Range(10f, 18f);
            Color color = (i % 3 == 0) ? White : Yellow;
            StartCoroutine(Spark(pos, angle, distance, size, color));
        }
    }

    private IEnumerator Spark(Vector2 pos, float angle, float distance, float size, Color color)
    {
        Image spark = CreateImage("Spark", pixelSprite, color);
        spark.rectTransform.sizeDelta = new Vector2(size, size);

        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        float duration = 0.45f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = EaseOut(t / duration);
            // 픽셀 느낌이 나도록 위치를 4픽셀 단위로 맞춤
            Vector2 p = pos + dir * distance * k;
            p = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);
            spark.rectTransform.anchoredPosition = p;
            float s = size * (1f - k * 0.7f);
            spark.rectTransform.sizeDelta = new Vector2(s, s);
            spark.color = new Color(color.r, color.g, color.b, 1f - k * k);
            yield return null;
        }
        Destroy(spark.gameObject);
    }

    // ==============================
    // 3) "START!" 글자
    // ==============================

    private IEnumerator PopupText(Vector2 pos)
    {
        GameObject go = new GameObject("PopupText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.text = PopupMessage;
        text.fontSize = 56f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Yellow;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);

        RectTransform rt = text.rectTransform;
        rt.sizeDelta = new Vector2(400f, 100f);

        float duration = SuccessDelay;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            // 처음엔 0 → 1.25배로 튀어나왔다가 1배로 안착
            float pop = k < 0.25f
                ? Mathf.Lerp(0.2f, 1.25f, EaseOut(k / 0.25f))
                : Mathf.Lerp(1.25f, 1f, EaseOut((k - 0.25f) / 0.25f));
            rt.localScale = Vector3.one * pop;
            rt.anchoredPosition = pos + new Vector2(0f, 70f + 40f * EaseOut(k));

            float alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            text.alpha = alpha;
            yield return null;
        }
        Destroy(go);
    }

    // ==============================
    // 4) 화면 번쩍
    // ==============================

    private IEnumerator ScreenFlash()
    {
        screenFlash.gameObject.SetActive(true);
        float duration = 0.15f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            screenFlash.color = new Color(1f, 1f, 1f, 0.35f * (1f - t / duration));
            yield return null;
        }
        screenFlash.gameObject.SetActive(false);
    }

    // ==============================
    // 5) 소리
    // ==============================

    private void PlaySound(AudioClip clip, float scale)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, SoundVolume * scale);
    }

    private const int SampleRate = 44100;

    // 짧은 "딸깍"
    private static AudioClip CreateClickClip()
    {
        int length = (int)(SampleRate * 0.03f);
        float[] data = new float[length];
        System.Random rand = new System.Random(7);

        for (int i = 0; i < length; i++)
        {
            float time = (float)i / SampleRate;
            float env = Mathf.Exp(-time * 180f);
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1400f * time));
            float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            data[i] = (square * 0.5f + noise * 0.5f) * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create("StartFeedback_Click", length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 8비트 성공음 "띠-리-링↑" (도-미-솔-높은도)
    private static AudioClip CreateSuccessClip()
    {
        float[] freqs = { 1046.5f, 1318.5f, 1568.0f, 2093.0f };
        float[] lengths = { 0.06f, 0.06f, 0.06f, 0.22f };

        int total = 0;
        foreach (float l in lengths)
            total += (int)(SampleRate * l);

        float[] data = new float[total];
        int index = 0;

        for (int n = 0; n < freqs.Length; n++)
        {
            int noteLen = (int)(SampleRate * lengths[n]);
            for (int i = 0; i < noteLen; i++, index++)
            {
                float time = (float)i / SampleRate;
                float phase = (time * freqs[n]) % 1f;
                float square = phase < 0.5f ? 1f : -1f;

                float attack = Mathf.Clamp01(time / 0.003f);
                float release = Mathf.Clamp01((lengths[n] - time) / 0.025f);
                data[index] = square * 0.22f * attack * release;
            }
        }

        AudioClip clip = AudioClip.Create("StartFeedback_Success", total, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ==============================
    // 준비 (처음 한 번만)
    // ==============================

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // 모든 UI보다 위
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        canvasRect = (RectTransform)transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        clickClip = CreateClickClip();
        successClip = CreateSuccessClip();
        ringSprite = CreateRingSprite();
        pixelSprite = CreatePixelSprite();

        screenFlash = CreateFullScreen("ScreenFlash", false);
        inputBlocker = CreateFullScreen("InputBlocker", true);
        inputBlocker.color = new Color(0f, 0f, 0f, 0f);
    }

    private Image CreateFullScreen(string name, bool blockInput)
    {
        Image img = CreateImage(name, pixelSprite, new Color(1f, 1f, 1f, 0f));
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.raycastTarget = blockInput;
        img.gameObject.SetActive(false);
        return img;
    }

    private Image CreateImage(string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    // 도트 느낌의 링(가운데가 빈 원)
    private static Sprite CreateRingSprite()
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool on = d <= center && d >= center - 2.5f;
                tex.SetPixel(x, y, on ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ==============================
    // 도우미 함수
    // ==============================

    private static float EaseOut(float k)
    {
        k = Mathf.Clamp01(k);
        return 1f - (1f - k) * (1f - k);
    }

    private static Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null)
            return Pointer.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(Screen.width / 2f, Screen.height / 2f);
#endif
    }

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 local);
        return local;
    }

    private static Camera GetCam(RectTransform rt)
    {
        Canvas c = rt.GetComponentInParent<Canvas>();
        if (c == null) return null;
        c = c.rootCanvas;
        return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }

    private static bool IsPointerOver(RectTransform rt, Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, GetCam(rt));
    }

    // 마우스 아래에 있는 버튼 찾기 → 없으면 이름에 START가 들어간 버튼 찾기
    private static RectTransform FindPressedButton(Transform minigameRoot, Vector2 screenPos)
    {
        if (EventSystem.current != null)
        {
            PointerEventData data = new PointerEventData(EventSystem.current) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);

            foreach (RaycastResult r in results)
            {
                if (r.gameObject == null) continue;
                Selectable s = r.gameObject.GetComponentInParent<Selectable>();
                if (s != null && !(s is Scrollbar) && !(s is Slider) &&
                    (minigameRoot == null || s.transform.IsChildOf(minigameRoot)))
                    return (RectTransform)s.transform;
            }
        }

        if (minigameRoot == null)
            return null;

        foreach (Button b in minigameRoot.GetComponentsInChildren<Button>(false))
        {
            if (b.name.IndexOf("start", StringComparison.OrdinalIgnoreCase) >= 0)
                return (RectTransform)b.transform;
        }

        return null;
    }
}
