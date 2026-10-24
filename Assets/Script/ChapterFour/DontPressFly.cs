using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니게임 16 (누르지 마세요) 연출
///  - 남은 시간 10 ~ 7초 : START 버튼 위에서 손가락(클릭유도)이 클릭하라고 꼬심
///  - 남은 시간 7초 이하 : 파리가 왼쪽 위에서 날아와 START 버튼에 앉음
///  - START 버튼을 누르면 : 파리가 터지며 죽고, 시간은 DontPress 쪽에서 10초로 리셋됨
///
/// 이미지는 스프라이트 시트 한 장을 RawImage에 넣고 uvRect(보여줄 칸)를 바꿔서 애니메이션함
/// </summary>
public class DontPressFly : MonoBehaviour
{
    private enum FlyState { Hidden, Flying, Sitting, Dying }

    [Header("연결")]
    public DontPress game;
    public RectTransform startButton;
    public RawImage guideImage;   // 클릭유도.png
    public RawImage flyImage;     // 파리 이미지 (날기/앉기/죽기를 번갈아 씀)

    [Header("파리 시트")]
    public Texture flyMoveTexture;   // 파리 옆날기.png
    public Texture flyIdleTexture;   // 파리 앞날기.png
    public Texture flyDeathTexture;  // 파리 클릭 죽기.png

    [Header("시트 안의 칸 위치 (0~1 비율)")]
    public Rect[] guideFrames;
    public Rect[] flyMoveFrames;
    public Rect[] flyIdleFrames;
    public Rect[] flyDeathFrames;

    [Header("크기 (화면 픽셀)")]
    public Vector2 guideSize = new Vector2(190f, 190f);
    public Vector2 flyMoveSize = new Vector2(125f, 130f);
    public Vector2 flyIdleSize = new Vector2(130f, 148f);
    public Vector2 flyDeathSize = new Vector2(184f, 245f);

    [Header("타이밍")]
    [Tooltip("남은 시간이 이 값 이하가 되면 손가락이 사라지고 파리가 날아옴")]
    public float flyAppearTime = 7f;
    public float flyTravelTime = 1.6f;   // 날아오는 데 걸리는 시간(초)
    public float guideFps = 8f;
    public float flyFps = 14f;
    public float deathFps = 14f;

    [Header("위치 (START 버튼 중심 기준)")]
    public Vector2 guideOffset = new Vector2(210f, -95f);
    public Vector2 flySitOffset = new Vector2(170f, 45f);
    [Tooltip("파리가 출발하는 위치 (패널 중심 기준, 왼쪽 위 화면 밖)")]
    public Vector2 flyStartPosition = new Vector2(-1050f, 620f);

    [Header("START 위에서 돌아다니기")]
    [Tooltip("START 버튼 크기 중 파리가 돌아다닐 범위 (1 = 버튼 끝까지)")]
    [Range(0.1f, 1f)] public float wanderAreaRatio = 0.8f;
    public float wanderSpeed = 420f;      // 이동 속도 (픽셀/초)
    public float wanderPauseMin = 0.25f;  // 멈춰서 꼼지락거리는 시간
    public float wanderPauseMax = 0.9f;

    [Header("사운드 (비워 두면 Resources/Sounds 에서 자동으로 불러옴)")]
    public AudioClip flyBuzzClip;      // mg16_fly_buzz (반복)
    public AudioClip guideClickClip;   // mg16_click_guide
    public AudioClip failClip;         // mg16_fail (띠로리)
    [Range(0f, 1f)] public float flyBuzzVolume = 0.35f;
    [Range(0f, 1f)] public float guideClickVolume = 0.45f;
    [Range(0f, 1f)] public float failVolume = 0.6f;

    [Header("제한 시간 (오른쪽 위 TIME 00:00)")]
    public TMP_Text timeText;

    [Header("START 버튼이 저절로 눌리는 듯한 연출")]
    [Tooltip("남은 시간이 이 값 이하가 되면 버튼이 눌렸다 올라왔다 함")]
    public float pressTime = 4f;
    public float pressPeriod = 0.5f;     // 한 번 눌렸다 올라오는 시간(초)
    [Range(0.5f, 1f)] public float pressScale = 0.9f;  // 눌렸을 때 크기 비율
    public float pressDepth = 10f;       // 눌렸을 때 아래로 내려가는 정도(픽셀)

    [Header("남은 시간 깜빡임")]
    [Tooltip("남은 시간이 이 값 이하가 되면 깜빡임")]
    public float blinkTime = 4f;
    public Color blinkColor = new Color(0.9647059f, 0.07058824f, 0.5058824f, 1f); // #F61281
    public float blinkInterval = 0.15f;  // 몇 초마다 색이 바뀌는지
    public Graphic[] blinkTargets;       // 깜빡일 것들 (남은 시간 글자, 로딩바)

    private FlyState state = FlyState.Hidden;
    private AudioSource loopSource;
    private AudioSource oneShotSource;
    private int lastGuideFrame = -1;
    private Color[] blinkOriginal;
    private bool blinking;
    private Vector2 buttonBasePos;
    private Vector3 buttonBaseScale = Vector3.one;
    private bool pressing;
    private float animTime;
    private float flyProgress;
    private Vector2 flyFrom;
    private bool wasActive;
    private Vector2 sitPos;          // 버튼 중심 기준 현재 위치
    private Vector2 wanderTarget;
    private float pauseTimer;

    private void Awake()
    {
        if (guideImage != null) guideImage.raycastTarget = false;
        if (flyImage != null) flyImage.raycastTarget = false;

        if (flyBuzzClip == null) flyBuzzClip = Resources.Load<AudioClip>("Sounds/mg16_fly_buzz");
        if (guideClickClip == null) guideClickClip = Resources.Load<AudioClip>("Sounds/mg16_click_guide");
        if (failClip == null) failClip = Resources.Load<AudioClip>("Sounds/mg16_fail");

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.clip = flyBuzzClip;

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;

        if (startButton != null)
        {
            buttonBasePos = startButton.anchoredPosition;
            buttonBaseScale = startButton.localScale;
        }

        if (blinkTargets != null)
        {
            blinkOriginal = new Color[blinkTargets.Length];
            for (int i = 0; i < blinkTargets.Length; i++)
                if (blinkTargets[i] != null) blinkOriginal[i] = blinkTargets[i].color;
        }

        HideAll();
    }

    private static float SfxScale => GameSfx.SfxScale;

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null || oneShotSource == null) return;
        oneShotSource.PlayOneShot(clip, volume * SfxScale);
    }

    // 파리 윙윙 소리: 파리가 보일 때만, 움직일 때는 크고 높게
    private void UpdateBuzz()
    {
        if (loopSource == null || flyBuzzClip == null) return;

        bool shouldPlay = (state == FlyState.Flying || state == FlyState.Sitting) && Time.timeScale > 0f;
        if (shouldPlay && !loopSource.isPlaying)
        {
            loopSource.time = Random.Range(0f, flyBuzzClip.length * 0.9f);
            loopSource.Play();
        }
        else if (!shouldPlay && loopSource.isPlaying)
        {
            loopSource.Stop();
        }

        if (!shouldPlay) return;

        bool moving = state == FlyState.Flying || pauseTimer <= 0f;
        float targetVol = flyBuzzVolume * (moving ? 1f : 0.55f) * SfxScale;
        float targetPitch = moving ? 1.12f : 0.92f;
        loopSource.volume = Mathf.MoveTowards(loopSource.volume, targetVol, Time.unscaledDeltaTime * 2f);
        loopSource.pitch = Mathf.MoveTowards(loopSource.pitch, targetPitch, Time.unscaledDeltaTime * 2f);
    }

    // 남은 시간이 얼마 안 남으면 남은 시간 글자와 로딩바를 분홍색으로 깜빡임
    private void UpdateBlink(float remaining, bool active)
    {
        if (blinkTargets == null || blinkOriginal == null) return;

        bool on = active && remaining <= blinkTime && remaining > 0f;
        if (!on)
        {
            if (blinking) RestoreBlink();
            return;
        }

        blinking = true;
        bool pink = Mathf.FloorToInt(Time.time / Mathf.Max(0.02f, blinkInterval)) % 2 == 0;
        for (int i = 0; i < blinkTargets.Length; i++)
            if (blinkTargets[i] != null)
                blinkTargets[i].color = pink ? blinkColor : blinkOriginal[i];
    }

    // 기존 미니게임처럼 오른쪽 위에 전체 제한 시간 표시
    private void UpdateTimeText()
    {
        if (timeText == null || game == null) return;
        float t = Mathf.Max(0f, game.RemainingTime);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timeText.text = $"TIME {minutes:00}:{seconds:00}";
    }

    // 남은 시간이 얼마 안 남으면 START 버튼이 "꾹... 꾹..." 저절로 눌리는 것처럼 움직임
    private void UpdatePress(float remaining, bool active)
    {
        if (startButton == null) return;

        bool on = active && remaining <= pressTime && remaining > 0f;
        if (!on)
        {
            if (pressing)
            {
                pressing = false;
                startButton.anchoredPosition = buttonBasePos;
                startButton.localScale = buttonBaseScale;
            }
            return;
        }

        pressing = true;
        float p = (Time.time % Mathf.Max(0.05f, pressPeriod)) / Mathf.Max(0.05f, pressPeriod);

        // 0 ~ 0.2 : 빠르게 꾹 눌림 / 0.2 ~ 0.45 : 눌린 채 잠깐 멈춤 / 0.45 ~ 1 : 통! 하고 올라옴
        float down;
        if (p < 0.2f) down = p / 0.2f;
        else if (p < 0.45f) down = 1f;
        else
        {
            float u = (p - 0.45f) / 0.55f;
            down = Mathf.Max(0f, 1f - u) - Mathf.Sin(u * Mathf.PI) * 0.15f; // 살짝 튀어오름
        }

        float s = Mathf.LerpUnclamped(1f, pressScale, down);
        startButton.localScale = new Vector3(buttonBaseScale.x * (1f + (1f - s) * 0.3f), buttonBaseScale.y * s, buttonBaseScale.z);
        startButton.anchoredPosition = buttonBasePos + Vector2.down * (pressDepth * down);
    }

    private void RestoreBlink()
    {
        blinking = false;
        for (int i = 0; i < blinkTargets.Length; i++)
            if (blinkTargets[i] != null) blinkTargets[i].color = blinkOriginal[i];
    }

    private void OnEnable()
    {
        if (game == null) game = GetComponent<DontPress>();
        if (game != null) game.StartPressed += OnStartPressed;
    }

    private void OnDisable()
    {
        if (game != null) game.StartPressed -= OnStartPressed;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        animTime += dt;

        bool active = game != null && game.IsGameActive;
        float remaining = game != null ? game.RemainingWaitTime : 0f;

        UpdateBlink(remaining, active);
        UpdatePress(remaining, active);
        UpdateTimeText();

        // 게임이 끝나면(성공) 손가락·파리 정리 (죽는 중이면 끝까지 보여줌)
        if (!active)
        {
            if (wasActive && state != FlyState.Dying) HideAll();
            wasActive = false;
            if (state == FlyState.Dying) UpdateDying();
            UpdateBuzz();
            return;
        }
        wasActive = true;

        // ---- 손가락 (클릭유도) ----
        bool showGuide = remaining > flyAppearTime && state != FlyState.Dying;
        if (guideImage != null)
        {
            if (guideImage.gameObject.activeSelf != showGuide)
                guideImage.gameObject.SetActive(showGuide);

            if (showGuide)
            {
                int frame = SetFrame(guideImage, guideFrames, guideFps, true);
                // 손가락이 반짝이기 시작하는 칸(3번째)마다 "뾱" 소리
                if (frame == 2 && lastGuideFrame != 2 && Time.timeScale > 0f)
                    PlayOneShot(guideClickClip, guideClickVolume);
                lastGuideFrame = frame;
                guideImage.rectTransform.sizeDelta = guideSize;
                guideImage.rectTransform.anchoredPosition = ButtonCenter() + guideOffset;
            }
        }

        // ---- 파리 ----
        switch (state)
        {
            case FlyState.Hidden:
                if (remaining <= flyAppearTime) BeginFlying();
                break;
            case FlyState.Flying:
                UpdateFlying(dt);
                break;
            case FlyState.Sitting:
                UpdateSitting();
                break;
            case FlyState.Dying:
                UpdateDying();
                break;
        }

        UpdateBuzz();
    }

    // START 버튼을 눌렀을 때 (DontPress가 알려줌)
    private void OnStartPressed()
    {
        // 참지 못하고 눌렀음 → 띠로리
        PlayOneShot(failClip, failVolume);
        lastGuideFrame = -1;

        if (state == FlyState.Flying || state == FlyState.Sitting)
        {
            state = FlyState.Dying;
            animTime = 0f;
            flyImage.texture = flyDeathTexture;
            flyImage.rectTransform.sizeDelta = flyDeathSize;
            flyImage.rectTransform.localScale = Vector3.one;
            flyImage.rectTransform.localRotation = Quaternion.identity;
        }
    }

    private void BeginFlying()
    {
        if (flyImage == null) return;
        state = FlyState.Flying;
        flyProgress = 0f;
        animTime = 0f;
        flyFrom = flyStartPosition;
        flyImage.texture = flyMoveTexture;
        flyImage.rectTransform.sizeDelta = flyMoveSize;
        flyImage.rectTransform.anchoredPosition = flyFrom;
        flyImage.gameObject.SetActive(true);
    }

    private void UpdateFlying(float dt)
    {
        flyProgress += dt / Mathf.Max(0.05f, flyTravelTime);
        float t = Mathf.Clamp01(flyProgress);
        float eased = 1f - (1f - t) * (1f - t);   // 처음엔 빠르고 도착할 때 느려짐

        Vector2 target = ButtonCenter() + flySitOffset;
        Vector2 pos = Vector2.Lerp(flyFrom, target, eased);
        // 파리답게 위아래로 흔들리며 날기
        pos.y += Mathf.Sin(t * Mathf.PI * 3f) * 70f * (1f - t);
        flyImage.rectTransform.anchoredPosition = pos;

        // 날아가는 방향으로 그림 뒤집기 (시트는 오른쪽을 보고 있음)
        float dir = target.x >= flyFrom.x ? 1f : -1f;
        flyImage.rectTransform.localScale = new Vector3(dir, 1f, 1f);

        SetFrame(flyImage, flyMoveFrames, flyFps, true);

        if (t >= 1f)
        {
            state = FlyState.Sitting;
            animTime = 0f;
            sitPos = flySitOffset;
            wanderTarget = sitPos;
            pauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            flyImage.texture = flyIdleTexture;
            flyImage.rectTransform.sizeDelta = flyIdleSize;
            flyImage.rectTransform.localScale = Vector3.one;
        }
    }

    private void UpdateSitting()
    {
        float dt = Time.deltaTime;

        if (pauseTimer > 0f)
        {
            // 잠깐 멈춰서 꼼지락
            pauseTimer -= dt;
            if (pauseTimer <= 0f) wanderTarget = RandomPointOnButton();
        }
        else
        {
            // START 글자 위를 이리저리 기어다님
            sitPos = Vector2.MoveTowards(sitPos, wanderTarget, wanderSpeed * dt);
            if ((sitPos - wanderTarget).sqrMagnitude < 4f)
                pauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
        }

        Vector2 pos = ButtonCenter() + sitPos;
        pos += new Vector2(Mathf.Sin(animTime * 37f), Mathf.Cos(animTime * 29f)) * 2f; // 부르르 떨기
        flyImage.rectTransform.anchoredPosition = pos;

        // 움직이는 방향으로 몸을 기울임
        Vector2 move = wanderTarget - sitPos;
        float tilt = pauseTimer > 0f
            ? Mathf.Sin(animTime * 2.3f) * 8f
            : Mathf.Clamp(-move.x * 0.15f, -25f, 25f);
        flyImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        SetFrame(flyImage, flyIdleFrames, flyFps, true);
    }

    // START 버튼 안쪽의 아무 위치 (버튼 중심 기준)
    private Vector2 RandomPointOnButton()
    {
        Vector2 half = new Vector2(150f, 40f);
        if (startButton != null)
        {
            Vector2 size = startButton.rect.size;
            Vector3 s = startButton.localScale;
            half = new Vector2(size.x * Mathf.Abs(s.x), size.y * Mathf.Abs(s.y)) * 0.5f * wanderAreaRatio;
        }

        // 너무 짧게 움직이면 티가 안 나서, 지금 위치에서 어느 정도 떨어진 곳을 고름
        Vector2 p = sitPos;
        for (int i = 0; i < 6; i++)
        {
            p = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            if ((p - sitPos).magnitude > half.x * 0.4f) break;
        }
        return p;
    }

    private void UpdateDying()
    {
        if (flyDeathFrames == null || flyDeathFrames.Length == 0)
        {
            HideFly();
            return;
        }

        int frame = Mathf.FloorToInt(animTime * deathFps);
        if (frame >= flyDeathFrames.Length)
        {
            HideFly();
            return;
        }
        flyImage.uvRect = flyDeathFrames[frame];
    }

    private int SetFrame(RawImage image, Rect[] frames, float fps, bool loop)
    {
        if (image == null || frames == null || frames.Length == 0) return -1;
        int frame = Mathf.FloorToInt(animTime * fps);
        frame = loop ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1);
        image.uvRect = frames[frame];
        return frame;
    }

    private Vector2 ButtonCenter()
    {
        return startButton != null ? startButton.anchoredPosition : Vector2.zero;
    }

    private void HideFly()
    {
        state = FlyState.Hidden;
        if (flyImage != null)
        {
            flyImage.gameObject.SetActive(false);
            flyImage.rectTransform.localRotation = Quaternion.identity;
            flyImage.rectTransform.localScale = Vector3.one;
        }
    }

    private void HideAll()
    {
        HideFly();
        if (guideImage != null) guideImage.gameObject.SetActive(false);
    }
}
