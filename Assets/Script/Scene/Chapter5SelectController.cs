using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Reflection;

/// <summary>
/// 챕터 선택 화면(CheckProgressPanel)에 붙는 챕터5 연출.
///  1) 챕터1~4를 다 깨고 돌아오면: 강한 노이즈(회색 TV 지지직)가 지나가면서 챕터1~4 카드가 사라지고 챕터5 카드 하나만 남음 (한 번만)
///  2) 챕터5가 열린 뒤에는 챕터 선택 화면 전체가 계속 지지직 (UI 흔들림 / 글자 깨짐 / 가끔 화면 노이즈)
///  3) 챕터5 카드: 미니게임 수는 ?/? 로 표시, 아이콘은 ? 그림
///     챕터5에서 3번 이상 실패하고 돌아오면 ? 아이콘이 지지직거리다가 악마 아이콘으로 바뀜
///  4) 음악: 평소에는 A곡(bgm_chapter5_select_a) / 악마 아이콘으로 바뀌는 순간부터 B곡(bgm_chapter5_select_b)
///     → 챕터5 안에서 게임할 때도 같은 규칙 (GameBgm.Chapter5TrackName)
/// 챕터5 카드는 Chapter04 카드를 복사해서 실행할 때 자동으로 만든다 (Hierarchy에서 따로 만들 필요 없음).
///
/// [에디터 테스트 키 - 메인 씬 어디서나 (Game 창 클릭 후)]
///  F6 : 챕터1~4를 다 깬 상태로 만들기 → 챕터 선택 화면이 열리면서 챕터5 등장 연출 (시작 화면에서 눌러도 됨)
///  F7 : 챕터5 실패 횟수 +1 → 3번째부터 악마 아이콘 연출
/// </summary>
public class Chapter5SelectController : MonoBehaviour
{
    [Header("챕터5 아이콘 그림")]
    public Sprite questionIcon;   // CT5.png (?)
    public Sprite devilIcon;      // CT5 bug.png (악마)

    [Header("챕터5 화면 배경")]
    [Tooltip("챕터5만 남았을 때 챕터 선택 화면 배경 (설정배경2.png)")]
    public Sprite chapter5Background;

    [Header("데모 빌드")]
    [Tooltip("켜져 있으면 챕터5 카드를 눌러도 챕터5에 들어가지 않고 '데모 버전' 안내창이 뜸. 정식 버전 때 끄기")]
    public bool demoBuild = false;
    public string demoTitle = "DEMO VERSION";
    [TextArea] public string demoMessage = "현재는 데모 버전입니다.\nCHAPTER 5는 추후 제작 예정입니다.";
    public string demoHint = "- 클릭하면 닫힙니다 -";

    [Header("챕터5 카드 문구")]
    public string titleText = "> CHAPTER 5 <";
    public string subtitleText = "? ? ? ? ?";
    public string progressText = "- ?/? -";

    [Header("악마 아이콘")]
    [Tooltip("챕터5에서 이 횟수 이상 실패하면 악마 아이콘으로 바뀜")]
    public int devilAfterFails = 3;
    [Tooltip("? 아이콘을 보여 주는 시간 (그다음 지지직거리며 악마로 바뀜)")]
    public float questionHoldTime = 1.0f;
    [Tooltip("? → 악마로 바뀌는 지지직 시간")]
    public float transformDuration = 1.4f;

    [Header("챕터5가 처음 나타날 때 (강한 노이즈)")]
    [Tooltip("노이즈가 시작되기 전 평범한 화면을 보여 주는 시간")]
    public float revealDelay = 0.6f;
    [Tooltip("챕터4 고장 연출과 같은 강한 노이즈 시간")]
    public float revealNoiseDuration = 3f;

    [Header("챕터5 배경음악 (Resources/Sounds/BGM)")]
    [Tooltip("평소 챕터5 음악 (A)")]
    public string normalTrack = GameBgm.Chapter5NormalTrack;
    [Tooltip("3번 이상 실패해서 악마 아이콘이 된 뒤의 음악 (B)")]
    public string devilTrack = GameBgm.Chapter5DevilTrack;
    [Tooltip("챕터5 음악 크기 (메인 음악 크기 기준 배율)")]
    [Range(0f, 2f)] public float chapter5MusicGain = 1f;

    [Header("평소 지지직 (화면 노이즈 간격)")]
    public float burstIntervalMin = 3f;
    public float burstIntervalMax = 7f;

    [Header("챕터5 카드 (챕터1~4는 사라지고 이것 하나만 가운데)")]
    [Tooltip("카드(그림) 크기 - CT5 그림 비율(가로로 넓음)에 맞춤")]
    public Vector2 cardSize = new Vector2(520f, 468f);
    public float cardY = -55f;
    [Tooltip("박스 안 > CHAPTER 5 < 글자 높이 (가운데 기준)")]
    public float titleY = 145f;
    [Tooltip("박스 안 - ?/? - 글자 높이 (가운데 기준)")]
    public float progressY = -140f;
    public Color hoverColor = new Color32(254, 230, 35, 255);



    // 글자가 깨질 때 쓰는 문자 (폰트에 있는 기본 문자만)
    private const string GlitchChars = "#%&@$*!?/|_=+01X";

    private class Target
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 rest;
        public Vector2 offset;
        public float hold;
        public bool isChapter5;
    }

    private class Corrupted
    {
        public TMP_Text text;
        public string original;
        public float timer;
    }

    private bool initialized = false;
    private bool active = false;      // 챕터5가 열려서 지지직 중인지
    private bool revealing = false;
    private bool iconTransforming = false;
    private bool devilShown = false;
    private bool hovering = false;

    private float intensity = 0f;     // 0 평소 / 1 노이즈 순간 / 2 등장 연출
    private float chapter5Boost = 0f; // 챕터5 카드만 더 흔들기
    private float jitterTimer = 0f;

    private readonly List<RectTransform> baseCards = new List<RectTransform>();
    private readonly List<Vector2> basePos = new List<Vector2>();
    private readonly List<Vector3> baseScale = new List<Vector3>();
    private readonly List<Target> targets = new List<Target>();
    private readonly List<TMP_Text> allTexts = new List<TMP_Text>();
    private readonly List<Corrupted> corrupted = new List<Corrupted>();

    private GameObject card5;
    private Image card5Bg;
    private Outline card5Outline;
    private Image card5Icon;
    private TMP_Text card5Title;
    private Vector2 iconRest;

    private StartMenuController startMenu;
    private Image panelBg;
    private Sprite originalBackground;
    private CanvasGroup panelGroup;

    // CRT 노이즈
    private Canvas rootCanvas;
    private ChapterNoiseController noise;
    private Camera noiseCamera;
    private UniversalAdditionalCameraData cameraData;
    private int originalRendererIndex = 0;
    private bool heavyNoiseOn = false;
    private bool lightNoiseOn = false;
    private RenderMode savedRenderMode;
    private Camera savedCamera;
    private float savedPlaneDistance;
    private bool noiseSoundOn = false;

    // 아래 게이지(진행도 슬라이더)
    private Slider gauge;
    private Image gaugeFill;
    private Color gaugeFillColor = Color.white;
    private float gaugeValue;
    private float gaugeHold;
    private bool gaugeGlitching;
    private Vector3 gaugeScale = Vector3.one;


    // ==============================
    // 자동으로 붙이기
    // 씬(MainScene)에 미리 넣어 두지 않아도, 게임이 시작되면 챕터 선택 화면(CheckProgressPanel)에 자동으로 붙음
    // ==============================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToChapterSelect();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AttachToChapterSelect();
    }

    private static void AttachToChapterSelect()
    {
        StartMenuController menu = FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.chapterSelectPanel == null)
            return;

        if (menu.chapterSelectPanel.GetComponent<Chapter5SelectController>() == null)
            menu.chapterSelectPanel.AddComponent<Chapter5SelectController>();
        Debug.Log("[Chapter5] 챕터 선택 화면에 챕터5 연출을 붙였습니다.");

#if UNITY_EDITOR
        // 에디터 테스트 키 (F6/F7) - 어느 화면에서 눌러도 되게 늘 켜져 있는 오브젝트에 붙임
        if (menu.GetComponent<Chapter5TestKeys>() == null)
            menu.gameObject.AddComponent<Chapter5TestKeys>();
#endif
    }


    // ==============================
    // 준비
    // ==============================

    private void Init()
    {
        if (initialized)
            return;

        initialized = true;

        startMenu = FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);

        Canvas c = GetComponentInParent<Canvas>(true);
        if (c != null)
            rootCanvas = c.rootCanvas;

        SetupNoise();


        // Inspector에서 비워 두면 Resources/ChapterFxResources 에서 가져옴
        ChapterFxResources res = Resources.Load<ChapterFxResources>("ChapterFxResources");
        if (res != null)
        {
            if (questionIcon == null) questionIcon = res.chapter5QuestionIcon;
            if (devilIcon == null) devilIcon = res.chapter5DevilIcon;
            if (chapter5Background == null) chapter5Background = res.chapter5Background;
        }

#if UNITY_EDITOR
        // 그래도 없으면 에디터에서는 원래 위치에서 찾아 씀
        if (questionIcon == null)
            questionIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/1. Team Folder/Subin/CT5/CT5.png");
        if (devilIcon == null)
            devilIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/1. Team Folder/Subin/CT5/CT5 bug.png");
        if (chapter5Background == null)
            chapter5Background = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/1. Team Folder/Subin/NEW ART/설정배경2.png");
#endif

        // 기존 챕터 카드 1~4
        for (int i = 1; i <= 4; i++)
        {
            RectTransform rt = transform.Find("Chapter0" + i) as RectTransform;
            if (rt == null)
                continue;

            baseCards.Add(rt);
            basePos.Add(rt.anchoredPosition);
            baseScale.Add(rt.localScale);
        }

        Debug.Log($"[Chapter5] 준비 완료 (카드 {baseCards.Count}장 찾음, ?아이콘 {(questionIcon != null ? "OK" : "없음")}, 악마아이콘 {(devilIcon != null ? "OK" : "없음")})  테스트: F6 챕터5 열기 / F7 실패+1");

        if (baseCards.Count == 4)
            BuildChapter5Card(baseCards[3]);
        else
            Debug.LogWarning("[Chapter5] Chapter01~Chapter04 카드를 찾지 못해서 챕터5 카드를 만들 수 없습니다.");

        // 지지직 흔들릴 UI = 챕터 선택 화면 바로 아래의 모든 UI
        foreach (Transform child in transform)
        {
            RectTransform rt = child as RectTransform;
            if (rt == null)
                continue;

            CanvasGroup g = rt.GetComponent<CanvasGroup>();
            if (g == null)
                g = rt.gameObject.AddComponent<CanvasGroup>();

            targets.Add(new Target
            {
                rect = rt,
                group = g,
                rest = rt.anchoredPosition,
                isChapter5 = card5 != null && rt.gameObject == card5
            });
        }

        allTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));

        gauge = GetComponentInChildren<Slider>(true);
        if (gauge != null && gauge.fillRect != null)
        {
            gaugeFill = gauge.fillRect.GetComponent<Image>();
            if (gaugeFill != null) gaugeFillColor = gaugeFill.color;
        }
        if (gauge != null)
            gaugeScale = gauge.transform.localScale;

        panelBg = GetComponent<Image>();
        if (panelBg != null)
            originalBackground = panelBg.sprite;

        panelGroup = GetComponent<CanvasGroup>();
        if (panelGroup == null)
            panelGroup = gameObject.AddComponent<CanvasGroup>();
    }


    private void BuildChapter5Card(RectTransform template)
    {
        // 꺼진 상자 안에서 복사 → 복사본의 Awake(호버 스크립트 등)가 실행되기 전에 손볼 수 있음
        GameObject holder = new GameObject("C5_Holder");
        holder.SetActive(false);
        holder.transform.SetParent(transform, false);

        card5 = Instantiate(template.gameObject, holder.transform);
        card5.name = "Chapter05";

        // 챕터4용 호버 색 스크립트 제거 (챕터5는 아래에서 따로 처리)
        ButtonHoverColor hover = card5.GetComponent<ButtonHoverColor>();
        if (hover != null)
            DestroyImmediate(hover);

        // 클릭 → 챕터5 씬 (복사된 '챕터4로 이동' 연결은 지움)
        Button button = card5.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(OnClickChapter5);
        }

        // 카드 크기: CT5 그림 비율대로 가로로 넓게
        RectTransform cardRT = (RectTransform)card5.transform;
        cardRT.sizeDelta = cardSize;
        cardRT.localScale = Vector3.one;

        // 카드 배경은 투명 (클릭 영역만), 그림이 곧 카드
        card5Bg = card5.GetComponent<Image>();
        if (card5Bg != null)
        {
            card5Bg.sprite = null;
            card5Bg.color = new Color(0f, 0f, 0f, 0f);
        }

        card5Outline = card5.GetComponent<Outline>();
        if (card5Outline != null)
            card5Outline.enabled = false;

        // 글자: [0] 제목(그림 위)  [1] 부제목(숨김)  [2] 진행도 ?/?(그림 아래)  [3] 완료(숨김)
        TMP_Text[] texts = card5.GetComponentsInChildren<TMP_Text>(true);
        // 글자는 박스(그림) 안쪽: 제목은 위쪽, ?/? 는 아래쪽, 모두 가운데 정렬
        if (texts.Length > 0)
        {
            texts[0].text = titleText;
            texts[0].fontSize = 34;
            CenterText(texts[0], titleY);
            card5Title = texts[0];
        }
        if (texts.Length > 1) { texts[1].text = subtitleText; texts[1].gameObject.SetActive(false); }
        if (texts.Length > 2)
        {
            texts[2].text = progressText;
            texts[2].fontSize = 34;
            CenterText(texts[2], progressY);
        }
        if (texts.Length > 3) texts[3].gameObject.SetActive(false);

        // 가운데 아이콘 (? / 악마)
        GameObject iconGO = new GameObject("C5_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGO.layer = card5.layer;
        iconGO.transform.SetParent(card5.transform, false);
        iconGO.transform.SetSiblingIndex(0); // 글자 뒤에

        RectTransform iconRT = (RectTransform)iconGO.transform;
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        iconRest = Vector2.zero;

        card5Icon = iconGO.GetComponent<Image>();
        card5Icon.sprite = questionIcon;
        card5Icon.preserveAspect = true;
        card5Icon.raycastTarget = false;

        // 마우스 올렸을 때
        EventTrigger trigger = card5.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, () => SetHover(true));
        AddTrigger(trigger, EventTriggerType.PointerExit, () => SetHover(false));

        // 챕터 선택 화면으로 옮기기 (챕터4 바로 뒤)
        card5.SetActive(false);
        card5.transform.SetParent(transform, false);
        card5.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);
        Destroy(holder);
    }


    // 카드 가운데 기준으로 가로/세로 모두 가운데 정렬
    private void CenterText(TMP_Text t, float y)
    {
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(cardSize.x - 60f, 60f);
        rt.anchoredPosition = new Vector2(0f, y);
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.margin = Vector4.zero;
    }


    private static void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }


    // ==============================
    // 화면이 열릴 때 / 닫힐 때
    // ==============================

    private void OnEnable()
    {
        Init();
        Refresh();
    }


    private void OnDisable()
    {
        HideDemoPopup();
        StopAllCoroutines();
        CleanUp();

        // 챕터 선택 화면을 벗어나면 챕터5 음악 끄고 메인 음악으로
        StopChapter5Music(true);
    }


    private void OnDestroy()
    {
        StopAllNoiseNow();
        if (noise != null) Destroy(noise.gameObject);
    }


    private void Refresh()
    {
        StopAllCoroutines();
        CleanUp();

        if (card5 == null)
            return;

        if (!SessionProgress.IsChapter5Unlocked())
        {
            active = false;
            GameBgm.chapter5SelectMusicOwns = false;
            ApplyLayout(false);
            StopChapter5Music(true);
            return;
        }

        active = true;

        // 이제부터 이 화면이 음악 담당 → 메인 음악이 끼어들지 못하게
        GameBgm.chapter5SelectMusicOwns = true;

        if (!SessionProgress.chapter5Revealed)
        {
            StartCoroutine(RevealRoutine());
        }
        else
        {
            ApplyLayout(true);
            GameBgm.StopMusic();
            StartChapter5Music(true);
            StartLoops();
        }
    }


    private void StartLoops()
    {
        StartCoroutine(IconRoutine());
        StartCoroutine(BurstLoop());
    }


    // 흔들림/글자깨짐/노이즈를 전부 원래대로
    private void CleanUp()
    {
        RestoreTexts();
        RestoreGauge();
        StopAllNoiseNow();
        StopNoiseSound();

        intensity = 0f;
        chapter5Boost = 0f;
        musicGlitch = 0f;
        musicBasePitch = 1f;
        revealing = false;
        iconTransforming = false;

        foreach (Target t in targets)
        {
            if (t.rect == null) continue;
            t.offset = Vector2.zero;
            t.hold = 0f;
            t.rect.anchoredPosition = t.rest;
            if (t.group != null) t.group.alpha = 1f;
        }

        if (card5Icon != null)
        {
            card5Icon.color = Color.white;
            card5Icon.rectTransform.anchoredPosition = iconRest;
            card5Icon.rectTransform.localScale = Vector3.one;
        }

        if (panelGroup != null)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }
    }


    // only5 = true : 챕터1~4 카드는 사라지고 챕터5 카드 하나만 가운데
    //         false : 원래대로 챕터1~4
    private void ApplyLayout(bool only5)
    {
        for (int i = 0; i < baseCards.Count; i++)
        {
            baseCards[i].localScale = baseScale[i];
            SetRest(baseCards[i], basePos[i]);
            baseCards[i].gameObject.SetActive(!only5);
        }

        // 배경: 챕터5만 남으면 챕터5 배경으로
        if (panelBg != null)
            panelBg.sprite = (only5 && chapter5Background != null) ? chapter5Background : originalBackground;

        if (card5 != null)
        {
            card5.SetActive(only5);
            SetRest((RectTransform)card5.transform, new Vector2(0f, cardY));
        }
    }


    private void SetRest(RectTransform rt, Vector2 pos)
    {
        rt.anchoredPosition = pos;

        foreach (Target t in targets)
        {
            if (t.rect == rt)
                t.rest = pos;
        }
    }


    // ==============================
    // 1) 챕터5 처음 등장 (강한 노이즈)
    // ==============================

    private IEnumerator RevealRoutine()
    {
        revealing = true;
        ApplyLayout(false);

        // 연출 중에는 클릭 막기
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        // A) 잠깐 평범한 챕터 선택 화면 (살짝 떨림)
        intensity = 0.3f;
        yield return new WaitForSecondsRealtime(revealDelay);

        // B) 챕터4에서 게임이 고장 날 때와 같은 '강한 노이즈'가 점점 심해짐
        //    → 제일 심할 때 챕터1~4가 사라지고 챕터5만 남음
        if (noise != null)
        {
            heavyNoiseOn = true;
            UseChapterRenderer(true);
            StartCoroutine(noise.PlayHeavyNoise(revealNoiseDuration));
        }

        // 원래 음악은 테이프가 멈추듯 음이 쭉 내려가면서 꺼짐
        GameBgm.TapeStop(1.4f);

        float t = 0f;
        bool swapped = false;
        while (t < revealNoiseDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / revealNoiseDuration);
            intensity = Mathf.Lerp(0.8f, 2f, k);

            if (!swapped && k >= 0.6f)
            {
                swapped = true;
                ApplyLayout(true);
                if (card5Icon != null) card5Icon.sprite = questionIcon;

                // 챕터5가 나타나는 순간 챕터5 음악이 지지직 끊기며 들어옴
                StartChapter5Music(false);
            }

            // 음악 지지직: 처음엔 심하게 끊기다가 노이즈가 끝날수록 덜 끊김
            if (swapped)
                musicGlitch = Mathf.Lerp(1f, 0.5f, (k - 0.6f) / 0.4f);

            yield return null;
        }

        StopHeavyNoise();

        // C) 지지직이 가라앉으면서 챕터5 카드가 깜빡이며 자리 잡음
        const float settle = 0.6f;
        t = 0f;
        Target c5 = targets.Find(x => x.isChapter5);
        while (t < settle)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / settle);
            intensity = Mathf.Lerp(1.5f, 0f, k);
            musicGlitch = Mathf.Lerp(0.5f, 0f, k);

            if (c5 != null && c5.group != null)
                c5.group.alpha = Random.value < Mathf.Lerp(0.6f, 1f, k) ? 1f : Random.Range(0.3f, 0.7f);

            yield return null;
        }

        foreach (Target tg in targets)
            if (tg.group != null) tg.group.alpha = 1f;

        intensity = 0f;
        revealing = false;
        SessionProgress.chapter5Revealed = true;

        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;

        StartLoops();
    }


    // ==============================
    // 2) 아이콘 (? → 악마)
    // ==============================

    private IEnumerator IconRoutine()
    {
        devilShown = false;
        SetIcon(questionIcon);

        // 한두 번 실패: ? 그대로
        if (SessionProgress.chapter5FailCount < devilAfterFails)
            yield break;

        // 3번 이상: 처음엔 ?로 보이다가
        yield return new WaitForSecondsRealtime(questionHoldTime);

        // 지지직(챕터3·4 노이즈)이 연달아 나면서 악마로
        iconTransforming = true;

        float t = 0f;
        float swapTimer = 0f;
        float nextNoise = 0f;

        while (t < transformDuration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            swapTimer -= dt;

            float k = Mathf.Clamp01(t / transformDuration);
            chapter5Boost = Mathf.Lerp(1f, 3f, k);
            intensity = Mathf.Lerp(0.4f, 1f, k);
            musicGlitch = Mathf.Lerp(0.3f, 1f, k);
            musicBasePitch = Mathf.Lerp(1f, 0.85f, k); // 음이 점점 내려감

            if (t >= nextNoise && !lightNoiseOn)
            {
                nextNoise = t + 0.4f;
                StartCoroutine(LightNoise());
            }

            if (swapTimer <= 0f)
            {
                swapTimer = Random.Range(0.03f, 0.09f);

                // 시간이 갈수록 악마가 보이는 비율이 늘어남
                bool devil = Random.value < k * k * 1.2f + 0.08f;
                SetIcon(devil ? devilIcon : questionIcon);

                card5Icon.color = RandomGlitchColor();

                RectTransform irt = card5Icon.rectTransform;
                irt.anchoredPosition = iconRest + new Vector2(Random.Range(-14f, 14f), Random.Range(-5f, 5f)) * (0.5f + k);
                irt.localScale = new Vector3(Random.Range(0.85f, 1.2f), Random.Range(0.92f, 1.06f), 1f);
            }

            yield return null;
        }

        while (lightNoiseOn)
            yield return null;

        SetIcon(devilIcon);
        card5Icon.color = Color.white;
        card5Icon.rectTransform.anchoredPosition = iconRest;
        card5Icon.rectTransform.localScale = Vector3.one;

        // 악마로 바뀌는 순간 음악도 B곡으로 (지지직 끊기며 바뀜)
        SwitchChapter5Music(devilTrack);

        chapter5Boost = 0f;
        intensity = 0f;
        musicGlitch = 0f;
        musicBasePitch = 1f;
        iconTransforming = false;
        devilShown = true;
    }


    private void SetIcon(Sprite sprite)
    {
        if (card5Icon != null && sprite != null)
            card5Icon.sprite = sprite;
    }


    private Color RandomGlitchColor()
    {
        // 무섭지 않게: 빨강/검정 계열은 안 씀 (흰색 / 하늘 / 노랑 / 살짝 흐림)
        float r = Random.value;
        if (r < 0.5f) return Color.white;
        if (r < 0.7f) return new Color(0.55f, 0.95f, 1f, 1f);   // 하늘
        if (r < 0.88f) return new Color(1f, 0.95f, 0.55f, 1f);  // 노랑
        return new Color(1f, 1f, 1f, Random.Range(0.6f, 0.85f)); // 살짝 흐림
    }


    // ==============================
    // 3) 평소에 가끔 화면 노이즈 (챕터3·4와 같은 짧은 지지직)
    // ==============================

    private IEnumerator BurstLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(burstIntervalMin, burstIntervalMax));

            if (revealing || iconTransforming || lightNoiseOn)
                continue;

            intensity = 1f;
            musicGlitch = 0.7f;

            // 악마가 된 뒤에는 노이즈 순간에 잠깐 ?로 돌아갔다가 다시 악마
            bool flashQuestion = devilShown && Random.value < 0.4f;
            if (flashQuestion) SetIcon(questionIcon);

            // 가끔은 두 번 연달아 지지직
            int count = Random.value < 0.3f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                yield return StartCoroutine(LightNoise());
                if (i < count - 1)
                    yield return new WaitForSecondsRealtime(Random.Range(0.08f, 0.2f));
            }

            if (flashQuestion) SetIcon(devilIcon);

            intensity = 0f;
            musicGlitch = 0f;
        }
    }


    // ==============================
    // 매 프레임 UI 흔들기 / 글자 깨기
    // ==============================

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        MusicTick(dt);

        if (!active || card5 == null)
            return;

        // 설정에서 화면 효과(Screen FX)를 끄면 흔들림은 약하게
        float fx = SettingsController.ScreenEffectsOn ? 1f : 0.3f;

        jitterTimer -= dt;
        if (jitterTimer <= 0f)
        {
            jitterTimer = Random.Range(0.04f, 0.11f);
            JitterTick(fx);
        }

        foreach (Target t in targets)
        {
            if (t.rect == null)
                continue;

            t.hold -= dt;
            if (t.hold <= 0f)
            {
                t.offset = Vector2.zero;
                if (t.group != null && !(revealing && t.isChapter5)) t.group.alpha = 1f;
            }

            Vector2 extra = Vector2.zero;

            // 챕터5 카드는 늘 미세하게 떨림 (마우스 올리면 더)
            if (t.isChapter5)
            {
                float amp = (1.2f + chapter5Boost * 3f + (hovering ? 2.5f : 0f)) * fx;
                extra = new Vector2(Random.Range(-amp, amp), Random.Range(-amp, amp) * 0.4f);
            }

            t.rect.anchoredPosition = t.rest + t.offset + extra;
        }

        // 챕터5 아이콘 색이 가끔 번쩍 (변신 중이 아닐 때)
        if (!iconTransforming && card5Icon != null)
        {
            if (Random.value < 0.015f * fx + intensity * 0.1f)
                card5Icon.color = RandomGlitchColor();
            else if (Random.value < 0.3f)
                card5Icon.color = Color.white;
        }

        GaugeTick(dt, fx);

        // 깨진 글자 되돌리기
        for (int i = corrupted.Count - 1; i >= 0; i--)
        {
            corrupted[i].timer -= dt;
            if (corrupted[i].timer <= 0f)
            {
                if (corrupted[i].text != null)
                    corrupted[i].text.text = corrupted[i].original;
                corrupted.RemoveAt(i);
            }
        }
    }


    private void JitterTick(float fx)
    {
        float lvl = Mathf.Clamp(intensity, 0f, 2f);

        // 평소: 가끔 한두 개 / 노이즈: 대부분 / 등장 연출: 전부 크게
        float chance = Mathf.Lerp(0.05f, 0.7f, Mathf.Clamp01(lvl)) + (lvl > 1f ? 0.3f : 0f);
        float ampX = Mathf.Lerp(4f, 14f, Mathf.Clamp01(lvl)) + (lvl > 1f ? (lvl - 1f) * 22f : 0f);
        float ampY = ampX * 0.35f;
        float alphaChance = lvl <= 0.01f ? 0.01f : Mathf.Lerp(0.15f, 0.5f, lvl / 2f);

        foreach (Target t in targets)
        {
            if (t.rect == null || t.hold > 0f)
                continue;

            float c = chance + (t.isChapter5 ? 0.2f + chapter5Boost * 0.1f : 0f);
            if (Random.value >= c)
                continue;

            t.offset = new Vector2(Random.Range(-ampX, ampX), Random.Range(-ampY, ampY)) * fx;
            t.hold = Random.Range(0.03f, 0.12f);

            if (t.group != null && !(revealing && t.isChapter5) && Random.value < alphaChance * fx)
                t.group.alpha = Random.Range(0.6f, 0.9f);
        }

        // 글자 깨짐
        float textChance = Mathf.Lerp(0.06f, 0.6f, Mathf.Clamp01(lvl)) + (lvl > 1f ? 0.3f : 0f);
        if (Random.value < textChance * Mathf.Max(fx, 0.5f))
            CorruptRandomText(lvl > 0.5f ? 3 : 1);

        // 챕터5 카드 글자는 더 자주
        if (Random.value < 0.12f + chapter5Boost * 0.1f)
            CorruptRandomText(1, true);
    }


    private void CorruptRandomText(int count, bool chapter5Only = false)
    {
        for (int n = 0; n < count; n++)
        {
            List<TMP_Text> pool = chapter5Only
                ? allTexts.FindAll(x => x != null && card5 != null && x.transform.IsChildOf(card5.transform))
                : allTexts;

            if (pool.Count == 0)
                return;

            TMP_Text t = pool[Random.Range(0, pool.Count)];
            if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text))
                continue;
            if (corrupted.Exists(x => x.text == t))
                continue;

            string original = t.text;
            char[] chars = original.ToCharArray();
            int swaps = Random.Range(1, 4);

            for (int i = 0; i < swaps; i++)
            {
                int idx = Random.Range(0, chars.Length);
                if (char.IsWhiteSpace(chars[idx]) || chars[idx] == '<' || chars[idx] == '>')
                    continue;
                chars[idx] = GlitchChars[Random.Range(0, GlitchChars.Length)];
            }

            t.text = new string(chars);
            corrupted.Add(new Corrupted { text = t, original = original, timer = Random.Range(0.05f, 0.14f) });
        }
    }


    // 아래 게이지: 값이 갑자기 튀거나, 색이 바뀌거나, 줄었다 늘었다 하는 지지직
    private void GaugeTick(float dt, float fx)
    {
        if (gauge == null)
            return;

        if (!gaugeGlitching)
            gaugeValue = gauge.value; // 진짜 값 기억 (ChapterProgressUI가 넣어 준 값)

        gaugeHold -= dt;
        if (gaugeHold > 0f)
            return;

        float chance = (0.04f + Mathf.Clamp01(intensity) * 0.5f + (intensity > 1f ? 0.4f : 0f)) * fx;

        if (Random.value < chance)
        {
            gaugeGlitching = true;
            gaugeHold = Random.Range(0.03f, 0.14f);

            float r = Random.value;
            if (r < 0.45f) gauge.value = Random.value;                         // 값이 튐
            else if (r < 0.7f) gauge.value = Random.value < 0.5f ? 0f : 1f;   // 0 ↔ 꽉 참
            else gauge.value = Mathf.Clamp01(gaugeValue + Random.Range(-0.3f, 0.3f));

            if (gaugeFill != null)
                gaugeFill.color = Random.value < 0.5f ? RandomGlitchColor() : gaugeFillColor;

            // 게이지 자체도 옆으로 살짝 늘어남
            gauge.transform.localScale = Vector3.Scale(gaugeScale, new Vector3(Random.Range(0.92f, 1.12f), Random.Range(0.8f, 1.3f), 1f));
        }
        else if (gaugeGlitching)
        {
            RestoreGauge();
        }
    }


    private void RestoreGauge()
    {
        if (gauge == null || !gaugeGlitching)
            return;

        gaugeGlitching = false;
        gauge.value = gaugeValue;
        gauge.transform.localScale = gaugeScale;
        if (gaugeFill != null) gaugeFill.color = gaugeFillColor;
    }


    private void RestoreTexts()
    {
        foreach (Corrupted c in corrupted)
        {
            if (c.text != null)
                c.text.text = c.original;
        }
        corrupted.Clear();
    }


    // ==============================
    // 마우스 / 클릭
    // ==============================

    private void SetHover(bool on)
    {
        hovering = on;

        if (card5Title != null)
            card5Title.color = on ? hoverColor : Color.white;

        if (card5 != null)
            card5.transform.localScale = on ? Vector3.one * 1.04f : Vector3.one;
    }


    // ==============================
    // 데모 안내창
    // ==============================

    private GameObject demoPopup;

    private void ShowDemoPopup()
    {
        if (demoPopup == null)
            BuildDemoPopup();

        demoPopup.transform.SetAsLastSibling();
        demoPopup.SetActive(true);
    }


    private void HideDemoPopup()
    {
        if (demoPopup != null)
            demoPopup.SetActive(false);
    }


    private void BuildDemoPopup()
    {
        // 화면 전체를 어둡게 덮는 판 (아무 데나 누르면 닫힘)
        demoPopup = new GameObject("C5_DemoPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        demoPopup.layer = gameObject.layer;
        demoPopup.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)demoPopup.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image dim = demoPopup.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);

        Button close = demoPopup.GetComponent<Button>();
        close.transition = Selectable.Transition.None;
        close.onClick.AddListener(HideDemoPopup);

        // 가운데 상자 (검정 + 하늘색 테두리)
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        box.layer = gameObject.layer;
        box.transform.SetParent(demoPopup.transform, false);
        RectTransform boxRT = (RectTransform)box.transform;
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(980f, 420f);
        boxRT.anchoredPosition = new Vector2(0f, -20f);
        Image boxImg = box.GetComponent<Image>();
        boxImg.color = Color.black;
        boxImg.raycastTarget = false;
        Outline ol = box.GetComponent<Outline>();
        ol.effectColor = new Color32(25, 200, 240, 255);
        ol.effectDistance = new Vector2(4f, -4f);

        AddPopupText(box.transform, demoTitle, new Vector2(0f, 120f), 64, new Color32(254, 230, 35, 255));
        AddPopupText(box.transform, demoMessage, new Vector2(0f, 0f), 38, Color.white);
        AddPopupText(box.transform, demoHint, new Vector2(0f, -150f), 26, new Color(0.7f, 0.7f, 0.7f, 1f));

        demoPopup.SetActive(false);
    }


    private void AddPopupText(Transform parent, string text, Vector2 pos, float size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(920f, size * 2.6f);
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (card5Title != null)
            t.font = card5Title.font; // 다른 글자와 같은 폰트
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
    }


    private void OnClickChapter5()
    {
        if (revealing)
            return;

        // (데모 안내창은 이제 쓰지 않음 - 챕터5 바로 플레이)

        // 챕터5로 들어갈 때는 메인 음악으로 돌리지 않음 (챕터5 씬에서 GameBgm이 A/B곡을 이어서 틀어 줌)
        leavingToChapter5 = true;
        StopChapter5Music(false);

        if (startMenu != null)
            startMenu.OnClickChapter5();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Chapter5");
    }


    // ==============================
    // CRT 화면 노이즈 / 소리
    // ==============================

    // ------------------------------------------------------------
    // 챕터5 배경음악
    //  - 평소: A곡 반복 / 3번 이상 실패해서 악마 아이콘이 되면: B곡 반복
    //  - 노이즈가 날 때 음악도 끊기고(지지직) 음이 흔들림
    // ------------------------------------------------------------

    private AudioSource music;
    private bool musicOn = false;
    private bool leavingToChapter5 = false;
    private float musicFade = 0f;        // 0~1 서서히 커지기
    private float musicFadeSpeed = 1f;
    private float musicGlitch = 0f;      // 0 = 깨끗 / 1 = 심하게 끊김
    private float musicBasePitch = 1f;
    private float musicStutterTimer = 0f;
    private bool musicGateOpen = true;
    private float musicGlitchPitch = 1f;

    // 화면이 열릴 때 시작하는 곡: 늘 A곡 (3번 이상 실패했으면 아이콘이 악마로 바뀌는 순간 B곡으로 넘어감)
    private void StartChapter5Music(bool fadeIn)
    {
        leavingToChapter5 = false;

        if (music == null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.ignoreListenerPause = true;
        }

        if (musicOn && music.isPlaying)
            return;

        PlayTrack(normalTrack);

        musicOn = true;
        musicFade = fadeIn ? 0f : 1f;
        musicFadeSpeed = 1f / 1.5f;
    }

    // 다른 곡으로 바꾸기 (악마 아이콘이 될 때 A → B)
    private void SwitchChapter5Music(string trackName)
    {
        if (!musicOn || music == null)
            return;

        if (music.clip != null && music.clip.name == trackName && music.isPlaying)
            return;

        PlayTrack(trackName);
        musicFade = 0.35f;              // 살짝 작게 시작해서 다시 커짐
        musicFadeSpeed = 1f / 1.2f;
    }

    private void PlayTrack(string trackName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/BGM/" + trackName);
        if (clip == null)
        {
            Debug.LogWarning("[Chapter5] 음악 파일을 찾지 못했습니다: Resources/Sounds/BGM/" + trackName);
            return;
        }

        music.clip = clip;
        music.loop = true;
        music.time = 0f;
        music.Play();
    }

    // backToMain = true 면 메인 음악을 다시 틀어 줌
    private void StopChapter5Music(bool backToMain)
    {
        // 화면을 떠나면 메인 음악을 다시 틀 수 있게
        GameBgm.chapter5SelectMusicOwns = false;

        if (!musicOn)
            return;

        musicOn = false;
        if (music != null)
            music.Stop();

        if (backToMain && !leavingToChapter5 && gameObject.scene.isLoaded)
            GameBgm.PlayMain();
    }

    private void MusicTick(float dt)
    {
        if (!musicOn || music == null)
            return;

        musicFade = Mathf.MoveTowards(musicFade, 1f, musicFadeSpeed * dt);

        // 지지직: 짧게 끊기고, 음이 살짝 흔들리고, 가끔 조금 뒤로 튐
        if (musicGlitch > 0.01f)
        {
            musicStutterTimer -= dt;
            if (musicStutterTimer <= 0f)
            {
                musicStutterTimer = Random.Range(0.04f, 0.1f);
                musicGateOpen = Random.value > musicGlitch * 0.55f;
                musicGlitchPitch = 1f + Random.Range(-0.12f, 0.06f) * musicGlitch;

                if (musicGlitch > 0.5f && Random.value < 0.15f && music.time > 0.5f)
                    music.time = Mathf.Max(0f, music.time - Random.Range(0.08f, 0.2f));
            }
        }
        else
        {
            musicGateOpen = true;
            musicGlitchPitch = 1f;
        }

        music.pitch = musicBasePitch * musicGlitchPitch;

        float popupDuck = (demoPopup != null && demoPopup.activeSelf) ? 0.5f : 1f;
        float gate = musicGateOpen ? 1f : 0.12f;
        music.volume = GameBgm.CurrentVolume * chapter5MusicGain * musicFade * gate * popupDuck;
    }


    // ------------------------------------------------------------
    // 챕터3·4와 똑같은 노이즈 (ChapterNoiseController 그대로 사용)
    //  - 짧은 지지직 : PlayLightNoiseAndWait (0.25초)
    //  - 강한 노이즈 : PlayHeavyNoise (챕터4 고장 연출과 같은 것)
    // ------------------------------------------------------------

    private void SetupNoise()
    {
        if (noise != null || rootCanvas == null)
            return;

        // 노이즈 동안 캔버스를 카메라 모드로 바꾸므로 원래 상태를 기억
        savedRenderMode = rootCanvas.renderMode;
        savedCamera = rootCanvas.worldCamera;
        savedPlaneDistance = rootCanvas.planeDistance;

        Camera cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        // 메인 씬 카메라는 인트로용 렌더러(ExampleCRTFilter2DRenderer, 어두운 CRT)를 씀.
        // 챕터3·4와 똑같은 노이즈를 내려고, 노이즈 순간에만 카메라를 챕터 씬과 같은 렌더러(PC_Renderer)로 바꿈
        noiseCamera = cam;
        if (cam != null)
        {
            cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                FieldInfo field = typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    originalRendererIndex = (int)field.GetValue(cameraData);
            }
        }

        GameObject go = new GameObject("C5_NoiseController");
        noise = go.AddComponent<ChapterNoiseController>();
        noise.Setup(rootCanvas, cam);
    }


    private IEnumerator LightNoise()
    {
        if (noise == null)
            yield break;

        lightNoiseOn = true;
        UseChapterRenderer(true);
        yield return StartCoroutine(noise.PlayLightNoiseAndWait());
        UseChapterRenderer(false);
        lightNoiseOn = false;
    }


    private void StopHeavyNoise()
    {
        if (noise != null)
            noise.StopAllNoise();

        heavyNoiseOn = false;
        RestoreCanvas();
    }


    // 노이즈가 중간에 끊겨도 화면(캔버스)이 원래대로 돌아오게
    private void StopAllNoiseNow()
    {
        if (!heavyNoiseOn && !lightNoiseOn)
            return;

        if (noise != null)
            noise.StopAllNoise();

        heavyNoiseOn = false;
        lightNoiseOn = false;
        RestoreCanvas();
    }


    // true: 챕터 씬과 같은 렌더러(0번, CRTRendererFeature 들어 있음) / false: 원래 렌더러
    private void UseChapterRenderer(bool on)
    {
        if (cameraData == null || originalRendererIndex == 0)
            return;

        cameraData.SetRenderer(on ? 0 : originalRendererIndex);
    }


    private void RestoreCanvas()
    {
        UseChapterRenderer(false);

        if (rootCanvas == null || noise == null)
            return;

        rootCanvas.renderMode = savedRenderMode;
        rootCanvas.worldCamera = savedCamera;
        rootCanvas.planeDistance = savedPlaneDistance;
    }


    private void StartNoiseSound(float volume)
    {
        GameSfx.StartNoise(volume);
        noiseSoundOn = true;
    }


    private void StopNoiseSound()
    {
        if (!noiseSoundOn)
            return;

        GameSfx.StopNoise();
        noiseSoundOn = false;
    }


    // ==============================
    // 에디터 테스트 키
    // ==============================

    // 테스트 키(Chapter5TestKeys)에서 부름
    public void TestUnlock()
    {
        for (int i = 1; i <= 4; i++)
        {
            SessionProgress.SetProgress(i, 5);
            SessionProgress.CompleteChapter(i);
        }

        SessionProgress.chapter5Revealed = false;
        Debug.Log("[테스트] 챕터1~4 클리어 상태 → 챕터5 등장 연출");

        ChapterProgressUI ui = GetComponent<ChapterProgressUI>();
        if (ui != null) ui.RefreshProgress();

        if (isActiveAndEnabled) { Init(); Refresh(); }
    }

    public void TestAddFail()
    {
        SessionProgress.chapter5FailCount++;
        Debug.Log($"[테스트] 챕터5 실패 횟수: {SessionProgress.chapter5FailCount}");

        if (isActiveAndEnabled) { Init(); Refresh(); }
    }
}
