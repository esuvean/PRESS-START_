using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 배경음악(BGM) 담당. 씬에 따로 붙일 필요 없음 (게임이 시작되면 스스로 만들어짐)
///
/// 음악 파일 위치: Assets/Resources/Sounds/BGM/
///   bgm_main.ogg       - 메인화면 (인트로 연출이 끝나고 화면이 전체화면으로 커졌을 때부터)
///   bgm_chapter1~4.ogg - 챕터 1~4
/// → 같은 이름의 파일로 바꿔 넣으면 음악만 쉽게 교체할 수 있음
///
/// 언제 나오나
///   - 메인화면 처음 켰을 때: 조용함 → 인트로 연출 → 모니터가 전체화면이 되는 순간 bgm_main 시작
///   - 챕터를 끝내고 메인화면으로 돌아왔을 때: 바로 bgm_main
///   - Chapter1~4 씬: 각 챕터 음악
/// 음악이 바뀔 때는 부드럽게 줄어들었다가 커짐
///
/// 전체 소리 크기는 설정 화면의 볼륨(AudioListener.volume)을 그대로 따라감
/// </summary>
public class GameBgm : MonoBehaviour
{
    // ==============================
    // 음악 크기 (0 ~ 1) — 효과음(0.5 정도)보다 작게, 방해 안 되는 정도
    // ==============================
    private const float BgmVolume = 0.25f;

    private const float FadeOutTime = 0.6f;  // 음악이 줄어드는 시간(초)
    private const float FadeInTime  = 1.5f;  // 음악이 커지는 시간(초)

    private static GameBgm instance;

    // ==============================
    // 배경음악 볼륨 (설정 화면의 BGM 바) — 0 ~ 1
    // ==============================
    public const string BgmVolumeKey = "BgmVolumePercent";
    private static float musicScale = 1f;

    /// <summary>배경음악 크기 바꾸기 (0 ~ 1) — 나오고 있는 음악에도 바로 적용</summary>
    public static void SetMusicVolume(float value01)
    {
        musicScale = Mathf.Clamp01(value01);
        if (instance != null && instance.fadeRoutine == null && instance.source.isPlaying)
            instance.source.volume = TargetVolume;
    }

    private static float TargetVolume => BgmVolume * musicScale * duckCurrent;

    // ==============================
    // 잠깐 음악 줄이기 (샷건처럼 큰 효과음이 잘 들리게)
    // ==============================
    private const float DuckLevel = 0.15f;     // 줄였을 때 음악 크기 (원래의 15%)
    private const float DuckDownTime = 0.35f;  // 줄어드는 데 걸리는 시간(초)
    private const float DuckUpTime = 1.2f;     // 다시 커지는 데 걸리는 시간(초)

    private static float duckCurrent = 1f;
    private static float duckTarget = 1f;

    /// <summary>true = 음악을 잠깐 작게 / false = 원래대로 (서서히)</summary>
    public static void Duck(bool on)
    {
        duckTarget = on ? DuckLevel : 1f;
    }

    private void Update()
    {
        if (Mathf.Approximately(duckCurrent, duckTarget)) return;

        float speed = (1f - DuckLevel) / (duckTarget < duckCurrent ? DuckDownTime : DuckUpTime);
        duckCurrent = Mathf.MoveTowards(duckCurrent, duckTarget, speed * Time.unscaledDeltaTime);

        // 음악이 바뀌는 중(페이드)이 아니면 바로 반영
        if (fadeRoutine == null && source.isPlaying)
            source.volume = TargetVolume;
    }

    private AudioSource source;
    private Coroutine fadeRoutine;
    private string currentName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        musicScale = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 100f) / 100f);
        duckCurrent = duckTarget = 1f;
        GameObject go = new GameObject("[GameBgm]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameBgm>();

        instance.source = go.AddComponent<AudioSource>();
        instance.source.playOnAwake = false;
        instance.source.loop = true;
        instance.source.spatialBlend = 0f;
        instance.source.ignoreListenerPause = true;
        instance.source.volume = 0f;

        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    // 에디터에서 챕터 씬을 바로 재생했을 때도 음악이 나오게 (첫 씬 확인용)
    private void Start()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.name != "MainScene")
            OnSceneLoaded(active, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 바뀔 때마다 알맞은 음악 고르기
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;

        // 샷건 도중에 씬이 바뀌어도 음악이 작은 채로 남지 않게
        duckTarget = 1f;


        // Chapter1 ~ Chapter4
        if (name.StartsWith("Chapter") && name.Length == 8 && char.IsDigit(name[7]))
        {
            Play("bgm_chapter" + name[7]);
            return;
        }

        if (name == "MainScene")
        {
            // 챕터를 끝내고 돌아온 경우(인트로 없이 챕터 선택 화면) → 바로 메인 음악
            if (SessionProgress.openChapterSelectOnLoad)
                Play("bgm_main");
            else
                Stop(); // 처음 켰을 때는 조용히 → 인트로 연출이 끝나면 StartMenuController가 PlayMain()을 부름
        }
    }

    // ==============================
    // 다른 스크립트에서 부르는 함수들
    // ==============================

    /// <summary>메인화면 음악 재생 (인트로 연출이 끝나고 전체화면이 됐을 때)</summary>
    public static void PlayMain()
    {
        if (instance != null) instance.Play("bgm_main");
    }

    /// <summary>원하는 음악 재생 (Resources/Sounds/BGM 안의 파일 이름)</summary>
    public static void PlayByName(string fileName)
    {
        if (instance != null) instance.Play(fileName);
    }

    /// <summary>음악 끄기 (서서히)</summary>
    public static void StopMusic()
    {
        if (instance != null) instance.Stop();
    }

    // ==============================

    private void Play(string fileName)
    {
        // 이미 같은 음악이 나오고 있으면 그대로 둠 (처음부터 다시 시작하지 않게)
        if (currentName == fileName && source.isPlaying) return;

        AudioClip clip = Resources.Load<AudioClip>("Sounds/BGM/" + fileName);
        if (clip == null)
        {
            Debug.LogWarning("[GameBgm] 음악 파일을 찾지 못했습니다: Resources/Sounds/BGM/" + fileName);
            return;
        }

        currentName = fileName;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwitchTo(clip));
    }

    private void Stop()
    {
        currentName = null;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwitchTo(null));
    }

    private IEnumerator SwitchTo(AudioClip next)
    {
        // 1) 지금 음악 서서히 줄이기
        if (source.isPlaying)
        {
            float start = source.volume;
            for (float t = 0f; t < FadeOutTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / FadeOutTime);
                yield return null;
            }
            source.Stop();
        }
        source.volume = 0f;

        if (next == null) yield break;

        // 2) 새 음악 서서히 키우기
        source.clip = next;
        source.Play();
        for (float t = 0f; t < FadeInTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0f, TargetVolume, t / FadeInTime);
            yield return null;
        }
        source.volume = TargetVolume;
        fadeRoutine = null;
    }
}
