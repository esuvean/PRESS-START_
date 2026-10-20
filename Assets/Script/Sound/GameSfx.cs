using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 게임 전체 효과음 담당. 씬에 따로 붙일 필요 없음 (게임이 시작되면 스스로 만들어짐)
///
/// 소리 파일 위치: Assets/Resources/Sounds/
///   ui_click.wav      - 아무 데나 클릭할 때 아주 작은 "딸깍"
///   button_press.wav  - 메뉴 버튼(챕터, 설정, 종료 등)을 누를 때
///   game_start.wav    - 메인화면 START 버튼을 누를 때
///   typing_1~5.wav    - 연출 타이핑 소리 (키보드 타자 소리, 번갈아 사용)
///   static_noise.wav  - 지지직 노이즈 (반복 재생)
/// → 같은 이름의 파일로 바꿔 넣으면 소리만 쉽게 교체할 수 있음
///
/// 전체 소리 크기는 설정 화면의 볼륨(AudioListener.volume)을 그대로 따라감
/// </summary>
public class GameSfx : MonoBehaviour
{
    // ==============================
    // 소리 크기 (0 ~ 1) — 여기 숫자만 바꾸면 됨
    // ==============================
    private const float ClickVolume  = 0.15f;  // 딸깍 (신경 안 쓰이게 아주 작게)
    private const float ButtonVolume = 0.5f;   // 메뉴 버튼
    private const float StartVolume  = 0.7f;   // 게임 시작 버튼
    private const float TypingVolume = 0.5f;   // 타이핑

    private static GameSfx instance;

    // ==============================
    // 효과음 볼륨 (설정 화면의 SFX 바) — 0 ~ 1
    // ==============================
    public const string SfxVolumeKey = "SfxVolumePercent";
    private static float sfxScale = 1f;

    /// <summary>설정에서 정한 효과음 크기 (0 ~ 1). 다른 스크립트의 효과음도 이 값을 곱해서 쓰면 됨</summary>
    public static float SfxScale => sfxScale;

    /// <summary>효과음 크기 바꾸기 (0 ~ 1)</summary>
    public static void SetSfxVolume(float value01)
    {
        sfxScale = Mathf.Clamp01(value01);
        if (instance != null && instance.noiseSource != null && instance.noiseSource.isPlaying)
            instance.noiseSource.volume = instance.noiseBaseVolume * sfxScale;
    }

    private float noiseBaseVolume = 0.5f;

    private AudioSource sfxSource;    // 짧은 소리용
    private AudioSource noiseSource;  // 노이즈(반복)용

    private AudioClip clickClip;
    private AudioClip buttonClip;
    private AudioClip startClip;
    private AudioClip noiseClip;
    private readonly List<AudioClip> typingClips = new List<AudioClip>();
    private int lastTypingIndex = -1;

    // 게임이 켜질 때 자동으로 만들어짐
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        sfxScale = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 100f) / 100f);
        GameObject go = new GameObject("[GameSfx]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameSfx>();
        instance.Build();
    }

    private void Build()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.ignoreListenerPause = true;

        noiseSource = gameObject.AddComponent<AudioSource>();
        noiseSource.playOnAwake = false;
        noiseSource.spatialBlend = 0f;
        noiseSource.loop = true;
        noiseSource.ignoreListenerPause = true;

        clickClip  = Resources.Load<AudioClip>("Sounds/ui_click");
        buttonClip = Resources.Load<AudioClip>("Sounds/button_press");
        startClip  = Resources.Load<AudioClip>("Sounds/game_start");
        noiseClip  = Resources.Load<AudioClip>("Sounds/static_noise");
        for (int i = 1; i <= 9; i++)
        {
            AudioClip c = Resources.Load<AudioClip>("Sounds/typing_" + i);
            if (c == null) break;
            typingClips.Add(c);
        }
        noiseSource.clip = noiseClip;
    }

    // ==============================
    // 다른 스크립트에서 부르는 함수들
    // ==============================

    /// <summary>메인화면 START 버튼 소리</summary>
    public static void PlayGameStart()
    {
        if (instance != null) instance.PlayOne(instance.startClip, StartVolume, 1f);
    }

    /// <summary>메뉴 버튼 소리</summary>
    public static void PlayButton()
    {
        if (instance != null) instance.PlayOne(instance.buttonClip, ButtonVolume, Random.Range(0.97f, 1.03f));
    }

    /// <summary>글자 하나 찍힐 때 타이핑 소리 (띄어쓰기·줄바꿈은 소리 안 남)</summary>
    public static void PlayTyping(char letter)
    {
        if (instance == null || char.IsWhiteSpace(letter)) return;
        if (instance.typingClips.Count == 0) return;

        // 같은 소리가 연속으로 안 나오게 번갈아 고름 + 높낮이·크기를 살짝씩 바꿔서 자연스럽게
        int idx = Random.Range(0, instance.typingClips.Count);
        if (idx == instance.lastTypingIndex && instance.typingClips.Count > 1)
            idx = (idx + 1) % instance.typingClips.Count;
        instance.lastTypingIndex = idx;

        instance.PlayOne(instance.typingClips[idx],
            TypingVolume * Random.Range(0.7f, 1f),
            Random.Range(0.94f, 1.06f));
    }

    /// <summary>지지직 노이즈 시작 (volume: 0~1)</summary>
    public static void StartNoise(float volume = 0.5f)
    {
        if (instance == null || instance.noiseClip == null) return;
        AudioSource s = instance.noiseSource;
        instance.noiseBaseVolume = volume;
        s.volume = volume * sfxScale;
        s.pitch = Random.Range(0.95f, 1.05f);
        if (!s.isPlaying)
        {
            // 매번 다른 지점부터 재생해서 똑같이 들리지 않게
            s.time = Random.Range(0f, instance.noiseClip.length * 0.9f);
            s.Play();
        }
    }

    /// <summary>지지직 노이즈 끄기</summary>
    public static void StopNoise()
    {
        if (instance != null && instance.noiseSource != null)
            instance.noiseSource.Stop();
    }

    private void PlayOne(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || sfxScale <= 0f) return;
        volume *= sfxScale;
        // 높낮이를 바꾸려면 PlayOneShot 하나로는 안 돼서 잠깐 쓰는 AudioSource 사용
        if (Mathf.Approximately(pitch, 1f))
        {
            sfxSource.PlayOneShot(clip, volume);
            return;
        }
        AudioSource s = GetPooledSource();
        s.pitch = pitch;
        s.PlayOneShot(clip, volume);
    }

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private AudioSource GetPooledSource()
    {
        foreach (AudioSource s in pool)
            if (!s.isPlaying) return s;
        AudioSource n = gameObject.AddComponent<AudioSource>();
        n.playOnAwake = false;
        n.spatialBlend = 0f;
        n.ignoreListenerPause = true;
        pool.Add(n);
        return n;
    }

    // ==============================
    // 클릭 감지 — 어떤 걸 눌렀는지 보고 알맞은 소리 재생
    // ==============================

    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    private void Update()
    {
        if (!MouseDownThisFrame()) return;

        Selectable target = FindClickedSelectable();

        // 메인화면 START 버튼 → StartMenuController.OnClickStart()에서 시작 소리를 냄
        if (target is Button b && HasMethod(b, "OnClickStart")) return;

        // 메뉴 버튼 / 토글 (미니게임 안에 있는 버튼은 제외 — 게임 방해 안 되게 딸깍만)
        if (target != null && target.IsInteractable()
            && (target is Button || target is Toggle)
            && target.GetComponentInParent<MinigameBase>(true) == null)
        {
            PlayButton();
            return;
        }

        // 그 외 모든 클릭 → 아주 작은 딸깍
        PlayOne(clickClip, ClickVolume, Random.Range(0.95f, 1.08f));
    }

    private Selectable FindClickedSelectable()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return null;

        PointerEventData data = new PointerEventData(es) { position = PointerPosition() };
        hits.Clear();
        es.RaycastAll(data, hits);
        if (hits.Count == 0) return null;

        // 맨 위에 있는 것 기준
        GameObject top = hits[0].gameObject;
        return top != null ? top.GetComponentInParent<Selectable>() : null;
    }

    private static bool HasMethod(Button b, string method)
    {
        int count = b.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            if (b.onClick.GetPersistentMethodName(i) == method) return true;
        return false;
    }

    private static bool MouseDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null) return Pointer.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }
}
