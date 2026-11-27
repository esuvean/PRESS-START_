using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 챕터5 미니게임(MG26~MG30) 공통 UI 도구
///  - 화면 틀(MG 배지, 제목, 설명, 별, 아이콘, 설정 버튼)은 에디터 도구(Chapter5SkinTool2630)가 프리팹에 넣어 둔다.
///  - 가운데 게임 화면(네온 판, 문자 슬롯, 버튼, 연출)은 각 미니게임 스크립트가 이 도구로 만든다.
///  - 만든 오브젝트 이름은 모두 RT_ 로 시작 (다시 시작할 때 통째로 지우고 새로 만듦)
/// </summary>
public static class C5Kit
{
    // ---------- 색 (챕터5 네온) ----------
    public static readonly Color White = Color.white;
    public static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
    public static readonly Color Pink = new Color32(255, 45, 149, 255);
    public static readonly Color Cyan = new Color32(25, 220, 245, 255);
    public static readonly Color Yellow = new Color32(255, 232, 26, 255);
    public static readonly Color Green = new Color32(70, 235, 110, 255);
    public static readonly Color Navy = new Color32(11, 26, 58, 255);
    public static readonly Color DeepNavy = new Color32(6, 12, 32, 235);
    public static readonly Color Pale = new Color32(169, 188, 224, 255);

    public const string YellowHex = "#FFE81A";
    public const string PinkHex = "#FF2D95";
    public const string CyanHex = "#19DCF5";
    public const string GreenHex = "#46EB6E";

    // 기존 미니게임 아래쪽 안내 문구 자리 (MG07 StatusText)
    public static readonly Vector2 BottomTextPos = new Vector2(0f, -430f);

    // =====================================================
    // 글꼴
    // =====================================================
    public static TMP_FontAsset ResolveFont(Transform root, TMP_FontAsset preferred)
    {
        if (preferred != null)
            return preferred;

        // 에디터 도구가 MG07에서 복사해 온 제목 글자(C5_)의 글꼴
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
            if (t.name.StartsWith("C5_") && t.font != null)
                return t.font;

        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (f != null && f.name == "Galmuri14_Regular SDF")
                return f;

        return TMP_Settings.defaultFontAsset;
    }

    // =====================================================
    // 예전(회색 임시) UI 숨기기 / 새 화면 루트 만들기
    // =====================================================

    /// <summary>프리팹에 남아 있는 예전 임시 UI를 숨김 (BG, C5_ 화면 틀, RT_ 새 화면은 그대로)</summary>
    public static void HideLegacy(Transform root)
    {
        foreach (Transform c in root)
        {
            string n = c.name;
            if (n == "BG" || n.StartsWith("C5_") || n.StartsWith("RT_"))
                continue;
            c.gameObject.SetActive(false);
        }
    }

    /// <summary>게임 화면 루트(화면 전체 크기)를 새로 만듦. 같은 이름이 있으면 지우고 다시 만듦</summary>
    public static RectTransform NewScreen(Transform root, string name)
    {
        Transform old = root.Find(name);
        if (old != null)
        {
            old.name = name + "_old";
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        RectTransform rt = Rect(root, name, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 배경 바로 위 (화면 틀 글자/설정 버튼보다 아래)
        Transform bg = root.Find("BG");
        rt.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
        return rt;
    }

    // =====================================================
    // 기본 오브젝트 만들기
    // =====================================================
    public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size,
        bool preserveAspect = false)
    {
        RectTransform rt = Rect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Txt(Transform parent, string name, string text, float fontSize, Color color,
        TextAlignmentOptions align, Vector2 pos, Vector2 box, TMP_FontAsset font)
    {
        RectTransform rt = Rect(parent, name, pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.richText = true;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    /// <summary>이미지를 클릭 가능한 버튼으로 (마우스 올리면 살짝 커짐)</summary>
    public static Button MakeButton(Image img, UnityAction onClick, bool hover = true)
    {
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        b.colors = cb;
        Navigation nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        if (onClick != null)
            b.onClick.AddListener(onClick);
        if (hover)
            img.gameObject.AddComponent<C5Hover>();
        return b;
    }

    /// <summary>기존 미니게임 START 버튼 모양 (현민님 normal_button + 같은 글꼴)</summary>
    public static Button StartButton(Transform parent, Sprite sprite, TMP_FontAsset font, Vector2 pos, Vector2 size,
        UnityAction onClick, string name = "RT_FinalStartButton")
    {
        Image img = Img(parent, name, sprite, sprite != null ? White : Pink, pos, size);
        Button b = MakeButton(img, onClick);
        Txt(img.transform, "Label", "START", size.y * 0.46f, White, TextAlignmentOptions.Center,
            Vector2.zero, size, font);
        return b;
    }

    /// <summary>판 위쪽에 걸리는 제목 띠 (현민님 26_upline)</summary>
    public static TextMeshProUGUI Plate(Transform parent, Sprite sprite, TMP_FontAsset font, Vector2 pos,
        Vector2 size, string text, float fontSize = 34f)
    {
        Image plate = Img(parent, "RT_Plate", sprite, sprite != null ? White : DeepNavy, pos, size);
        if (sprite == null)
            AddOutline(plate, Pink, 3f);
        TextMeshProUGUI t = Txt(plate.transform, "RT_PlateText", text, fontSize, White,
            TextAlignmentOptions.Center, new Vector2(0f, 2f), size, font);
        return t;
    }

    /// <summary>아래쪽 안내 문구 (기존 미니게임과 같은 자리)</summary>
    public static TextMeshProUGUI BottomText(Transform parent, TMP_FontAsset font, string text)
    {
        TextMeshProUGUI t = Txt(parent, "RT_BottomText", text, 32f, White, TextAlignmentOptions.Center,
            BottomTextPos, new Vector2(1500f, 60f), font);
        return t;
    }

    /// <summary>네온 점무늬 게임 판 (현민님 23_line_back)</summary>
    public static Image Panel(Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        Image panel = Img(parent, "RT_Panel", sprite, sprite != null ? White : Navy, pos, size);
        if (sprite == null)
            AddOutline(panel, Cyan, 4f);
        return panel;
    }

    public static void AddOutline(Graphic g, Color color, float dist)
    {
        Outline o = g.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(dist, -dist);
        o.useGraphicAlpha = false;
    }

    // =====================================================
    // 가운데 큰 알림 (ERROR / SYSTEM READY 등)
    // =====================================================
    public static CanvasGroup Banner(Transform parent, TMP_FontAsset font, Vector2 pos, Vector2 size,
        out TextMeshProUGUI text)
    {
        Image back = Img(parent, "RT_Banner", null, DeepNavy, pos, size);
        AddOutline(back, Pink, 4f);
        CanvasGroup cg = back.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        text = Txt(back.transform, "RT_BannerText", "", 46f, Pink, TextAlignmentOptions.Center,
            Vector2.zero, size, font);
        text.lineSpacing = -10f;
        return cg;
    }

    public static void SetBanner(CanvasGroup cg, TextMeshProUGUI text, string message, Color color)
    {
        if (cg == null)
            return;
        text.text = message;
        text.color = color;
        Outline o = cg.GetComponent<Outline>();
        if (o != null)
            o.effectColor = color;
        cg.transform.SetAsLastSibling();
        cg.alpha = 1f;
    }

    public static void HideBanner(CanvasGroup cg)
    {
        if (cg != null)
            cg.alpha = 0f;
    }

    // =====================================================
    // 연출
    // =====================================================

    public static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    public static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>톡 튀어 오르는 크기 연출</summary>
    public static IEnumerator Pop(Transform t, float peak = 1.2f, float duration = 0.24f)
    {
        if (t == null)
            yield break;
        Vector3 baseScale = Vector3.one;
        float e = 0f;
        while (e < duration)
        {
            if (t == null)
                yield break;
            e += Time.deltaTime;
            float k = e / duration;
            float s = k < 0.4f
                ? Mathf.Lerp(1f, peak, k / 0.4f)
                : Mathf.Lerp(peak, 1f, (k - 0.4f) / 0.6f);
            t.localScale = baseScale * s;
            yield return null;
        }
        if (t != null)
            t.localScale = baseScale;
    }

    /// <summary>흔들림 (anchoredPosition 기준, 끝나면 제자리)</summary>
    public static IEnumerator Shake(RectTransform rt, float duration, float magnitude)
    {
        if (rt == null)
            yield break;
        Vector2 origin = rt.anchoredPosition;
        float e = 0f;
        while (e < duration)
        {
            if (rt == null)
                yield break;
            e += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(e / duration);
            rt.anchoredPosition = origin + Random.insideUnitCircle * magnitude * fade;
            yield return null;
        }
        if (rt != null)
            rt.anchoredPosition = origin;
    }

    /// <summary>월드 좌표로 날아가기 (다른 부모 사이를 이동할 때)</summary>
    public static IEnumerator FlyTo(Transform t, Vector3 worldTarget, float duration, float endScale = 1f)
    {
        if (t == null)
            yield break;
        Vector3 from = t.position;
        Vector3 fromScale = t.localScale;
        float e = 0f;
        while (e < duration)
        {
            if (t == null)
                yield break;
            e += Time.deltaTime;
            float k = EaseInOut(e / duration);
            t.position = Vector3.Lerp(from, worldTarget, k);
            // 살짝 위로 휘어지게
            t.position += Vector3.up * Mathf.Sin(k * Mathf.PI) * 40f * t.lossyScale.y;
            t.localScale = Vector3.Lerp(fromScale, Vector3.one * endScale, k);
            yield return null;
        }
        if (t != null)
        {
            t.position = worldTarget;
            t.localScale = Vector3.one * endScale;
        }
    }

    public static IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;
        float e = 0f;
        while (e < duration)
        {
            if (cg == null)
                yield break;
            e += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, e / duration);
            yield return null;
        }
        if (cg != null)
            cg.alpha = to;
    }

    /// <summary>
    /// 글리치: 기존 챕터에서 쓰던 CRT 노이즈(ChapterNoiseController, 지지직 소리 포함) + 내용이 살짝 좌우로 튐
    /// area: (예전 호환용, 지금은 안 씀) / jitter: 좌우로 튈 오브젝트(없으면 안 튐)
    /// sound: false 면 화면 노이즈 없이 튀기만 함 (자주 나오는 작은 연출용)
    /// </summary>
    public static IEnumerator Glitch(RectTransform area, RectTransform jitter, float duration, float strength = 1f,
        bool sound = true)
    {
        if (sound)
            PlayChapterNoise();

        Vector2 origin = jitter != null ? jitter.anchoredPosition : Vector2.zero;
        float e = 0f;
        float next = 0f;
        while (e < duration)
        {
            if (jitter != null && e >= next)
            {
                next = e + 0.05f;
                jitter.anchoredPosition = origin + new Vector2(Random.Range(-8f, 8f) * strength,
                    Random.Range(-2f, 2f) * strength);
            }
            e += Time.deltaTime;
            yield return null;
        }

        if (jitter != null)
            jitter.anchoredPosition = origin;
    }

    /// <summary>
    /// S · T · A · R · T → START 결합 연출
    /// from: 글자가 출발할 위치(슬롯 등, 없으면 가운데에서 바로 시작)
    /// 끝나면 글자는 사라지고, 호출한 쪽에서 START 버튼을 보여 주면 됨
    /// </summary>
    public static IEnumerator MergeLetters(RectTransform parent, Sprite[] sprites, TMP_FontAsset font,
        Vector2 center, Vector2 letterSize, Transform[] from = null)
    {
        const string Word = "START";
        RectTransform[] letters = new RectTransform[5];
        float spread = letterSize.x * 1.55f;

        for (int i = 0; i < 5; i++)
        {
            Sprite s = sprites != null && i < sprites.Length ? sprites[i] : null;
            Image img = Img(parent, "RT_Merge" + i, s, s != null ? White : Clear, center, letterSize, true);
            if (s == null)
                Txt(img.transform, "L", Word[i].ToString(), letterSize.y * 0.8f, White,
                    TextAlignmentOptions.Center, Vector2.zero, letterSize, font);
            letters[i] = img.rectTransform;
            if (from != null && i < from.Length && from[i] != null)
                letters[i].position = from[i].position;
            else
                letters[i].anchoredPosition = center + new Vector2((i - 2) * spread, 0f);
        }

        // 1) 한 줄로 벌려 서기  S · T · A · R · T
        Vector2[] start = new Vector2[5];
        for (int i = 0; i < 5; i++)
            start[i] = letters[i].anchoredPosition;

        float e = 0f;
        const float LineUp = 0.45f;
        while (e < LineUp)
        {
            e += Time.deltaTime;
            float k = EaseOut(e / LineUp);
            for (int i = 0; i < 5; i++)
                if (letters[i] != null)
                    letters[i].anchoredPosition = Vector2.Lerp(start[i], center + new Vector2((i - 2) * spread, 0f), k);
            yield return null;
        }

        GameSfx.Play("correct_ding", 0.55f);
        yield return new WaitForSeconds(0.25f);

        // 2) 가운데로 모이며 하나로
        e = 0f;
        const float Join = 0.28f;
        while (e < Join)
        {
            e += Time.deltaTime;
            float k = EaseInOut(e / Join);
            for (int i = 0; i < 5; i++)
            {
                if (letters[i] == null) continue;
                letters[i].anchoredPosition = Vector2.Lerp(center + new Vector2((i - 2) * spread, 0f),
                    center + new Vector2((i - 2) * letterSize.x * 0.55f, 0f), k);
                letters[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, k);
            }
            yield return null;
        }

        // 3) 번쩍
        Image flash = Img(parent, "RT_MergeFlash", null, new Color(1f, 1f, 1f, 0.9f), center,
            new Vector2(letterSize.x * 4.2f, letterSize.y * 1.3f));
        for (int i = 0; i < 5; i++)
            if (letters[i] != null)
                Object.Destroy(letters[i].gameObject);

        e = 0f;
        while (e < 0.22f)
        {
            e += Time.deltaTime;
            if (flash != null)
                flash.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, e / 0.22f));
            yield return null;
        }
        if (flash != null)
            Object.Destroy(flash.gameObject);
    }

    // =====================================================
    // 기존 챕터 노이즈 (ChapterNoiseController: CRT 필터 지지직 0.25초 + 소리)
    // =====================================================
    private static ChapterNoiseController noise;
    private static float lastNoiseTime = -10f;

    /// <summary>미니게임이 시작할 때 불러 둠 (노이즈 장치가 없으면 챕터4처럼 하나 만들어 둠)</summary>
    public static void PrepareNoise(Transform minigameRoot)
    {
        if (noise == null)
            noise = Object.FindAnyObjectByType<ChapterNoiseController>();
        if (noise != null || minigameRoot == null)
            return;

        Canvas canvas = minigameRoot.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas = canvas.rootCanvas;
        if (canvas == null || Camera.main == null)
            return;

        GameObject go = new GameObject("NoiseController");
        noise = go.AddComponent<ChapterNoiseController>();
        noise.Setup(canvas, Camera.main);
    }

    /// <summary>기존 노이즈 한 번 (겹쳐서 부르면 무시 - 캔버스 설정이 꼬이지 않게)</summary>
    public static void PlayChapterNoise()
    {
        if (Time.unscaledTime - lastNoiseTime < 0.45f)
            return;
        if (noise == null)
            noise = Object.FindAnyObjectByType<ChapterNoiseController>();
        if (noise == null || !noise.isActiveAndEnabled)
            return;

        lastNoiseTime = Time.unscaledTime;
        noise.StartCoroutine(noise.PlayLightNoiseAndWait());
    }

    /// <summary>(예전 이름) 기존 노이즈 한 번</summary>
    public static void TryCrtNoise(MonoBehaviour host)
    {
        PlayChapterNoise();
    }

    // =====================================================
    // 좌표
    // =====================================================

    /// <summary>target 의 사각형을 space 의 로컬 좌표로</summary>
    public static UnityEngine.Rect RectIn(RectTransform space, RectTransform target)
    {
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector3 l = space.InverseTransformPoint(c[i]);
            minX = Mathf.Min(minX, l.x);
            minY = Mathf.Min(minY, l.y);
            maxX = Mathf.Max(maxX, l.x);
            maxY = Mathf.Max(maxY, l.y);
        }
        return UnityEngine.Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    public static Camera UiCamera(Component c)
    {
        Canvas canvas = c.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    public static bool TryMouse(out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            position = mouse.position.ReadValue();
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        position = Input.mousePosition;
        return true;
#else
        position = Vector2.zero;
        return false;
#endif
    }

    public static bool MouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
            return mouse.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    public static bool MousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
            return mouse.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    /// <summary>방향키/WASD (-1~1)</summary>
    public static Vector2 KeyAxis()
    {
        float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k != null)
        {
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1f;
            if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1f;
            if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1f;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1f;
#endif
        return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
    }

    public static bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k != null)
            return k.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }
}

/// <summary>
/// 아래쪽 S-T-A-R-T 문자 슬롯 줄 (현민님 26_miniline + 26_blueblank + 26_pink_X + 26_arrow)
/// </summary>
public class C5SlotRow
{
    public RectTransform root;
    private readonly Image[] slots = new Image[5];
    private readonly TextMeshProUGUI[] fallbackLetters = new TextMeshProUGUI[5];
    private readonly Sprite emptySprite;
    private readonly Sprite[] filledSprites;
    private const string Word = "START";

    public const float Spacing = 107f;

    public C5SlotRow(Transform parent, Vector2 pos, Sprite frame, Sprite empty, Sprite[] filled,
        Sprite arrowLeft, Sprite arrowRight, TMP_FontAsset font)
    {
        emptySprite = empty;
        filledSprites = filled;

        root = C5Kit.Rect(parent, "RT_SlotRow", pos, new Vector2(900f, 110f));

        Image frameImg = C5Kit.Img(root, "Frame", frame, frame != null ? C5Kit.White : C5Kit.DeepNavy,
            Vector2.zero, new Vector2(640f, 112f));
        if (frame == null)
            C5Kit.AddOutline(frameImg, C5Kit.Cyan, 3f);

        if (arrowLeft != null)
            C5Kit.Img(root, "ArrowL", arrowLeft, C5Kit.White, new Vector2(-400f, 0f), new Vector2(72f, 56f), true);
        if (arrowRight != null)
            C5Kit.Img(root, "ArrowR", arrowRight, C5Kit.White, new Vector2(400f, 0f), new Vector2(72f, 56f), true);

        for (int i = 0; i < 5; i++)
        {
            Vector2 p = new Vector2((i - 2) * Spacing, 0f);
            slots[i] = C5Kit.Img(root, "Slot" + i, empty, empty != null ? C5Kit.White : C5Kit.Navy, p,
                new Vector2(88f, 81f), true);
            if (empty == null)
                C5Kit.AddOutline(slots[i], C5Kit.Cyan, 3f);
            fallbackLetters[i] = C5Kit.Txt(slots[i].transform, "Letter", "", 44f, C5Kit.White,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(88f, 81f), font);
        }
    }

    public void SetCount(int count)
    {
        for (int i = 0; i < 5; i++)
            SetSlot(i, i < count);
    }

    public void SetSlot(int i, bool filled)
    {
        Sprite s = filled && filledSprites != null && i < filledSprites.Length ? filledSprites[i] : null;
        if (filled && s != null)
        {
            slots[i].sprite = s;
            fallbackLetters[i].text = "";
        }
        else
        {
            slots[i].sprite = emptySprite;
            fallbackLetters[i].text = filled ? Word[i].ToString() : "";
        }
    }

    public Transform Slot(int i)
    {
        return slots[i].transform;
    }
}

