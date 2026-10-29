using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SettingsController : MonoBehaviour
{


    // =========================================================
    // PAGE
    // =========================================================

    [Header("Pages")]
    [SerializeField] private GameObject displayPage;
    [SerializeField] private GameObject soundPage;
    [SerializeField] private GameObject infoPage;
    [SerializeField] private GameObject exitPage;


    // =========================================================
    // SELECTED DOT
    // =========================================================

    [Header("Selected Dots")]
    [SerializeField] private GameObject displayDot;
    [SerializeField] private GameObject soundDot;
    [SerializeField] private GameObject infoDot;
    [SerializeField] private GameObject exitDot;


    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessValueText;

    // 예전 "Fullscreen(창 모드)" 자리 → 이제 "Screen FX(화면 노이즈 효과)" ON/OFF
    // (FormerlySerializedAs: 씬/프리팹에 연결돼 있던 토글이 그대로 이어지게 함)
    [FormerlySerializedAs("fullscreenToggle")]
    [SerializeField] private Toggle screenFxToggle;

    [FormerlySerializedAs("fullscreenValueText")]
    [SerializeField] private TMP_Text screenFxValueText;

    [SerializeField] private CanvasGroup brightnessOverlay;


    // =========================================================
    // SOUND
    // =========================================================

    [Header("Sound")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;


    // =========================================================
    // SCREEN
    // =========================================================

    [Header("Screen Controller")]
    [SerializeField] private ScreenController screenController;

    [Header("Settings Window")]
    [SerializeField] private GameObject settingsPanel;

    // =========================================================
    // PLAYER PREFS KEY
    // =========================================================

    // 기존 Brightness 값이 꼬여있을 수 있어서
    // 새로운 키 이름을 사용함.
    private const string BrightnessKey = "BrightnessPercent_v2";
    private const string VolumeKey = "VolumePercent_v2";
    private const string ScreenFxKey = "ScreenFx";


    // 화면 노이즈(CRT 지지직) 효과를 보여줄지 — 다른 스크립트에서 확인용
    // 기본값 ON. OFF면 노이즈 연출 때 화면 효과 없이 소리만 남
    public static bool ScreenEffectsOn
    {
        get { return PlayerPrefs.GetInt(ScreenFxKey, 1) == 1; }
    }



    // 가장 어두울 때 검은 오버레이 강도
    private const float MaxDarkness = 0.85f;



    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // 인스펙터에 연결이 빠져 있어도 동작하도록 자동으로 찾아서 연결
        if (screenController == null)
            screenController = FindAnyObjectByType<ScreenController>();

        // SOUND 탭에 BGM(배경음악) / SFX(효과음) 조절 바 추가
        SoundSettingsRows.Build(volumeSlider, volumeValueText);

        LoadSettings();

        // 설정창을 처음 열었을 때 DISPLAY 탭
        ShowDisplay();
    }


    // =========================================================
    // TAB
    // =========================================================

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void ShowDisplay()
    {
        OpenPage(0);
    }

    public void ShowSound()
    {
        OpenPage(1);
    }

    public void ShowInfo()
    {
        OpenPage(2);
    }

    public void ShowExit()
    {
        OpenPage(3);
    }


    private void OpenPage(int index)
    {
        if (displayPage != null)
            displayPage.SetActive(index == 0);

        if (soundPage != null)
            soundPage.SetActive(index == 1);

        if (infoPage != null)
            infoPage.SetActive(index == 2);

        if (exitPage != null)
            exitPage.SetActive(index == 3);


        if (displayDot != null)
            displayDot.SetActive(index == 0);

        if (soundDot != null)
            soundDot.SetActive(index == 1);

        if (infoDot != null)
            infoDot.SetActive(index == 2);

        if (exitDot != null)
            exitDot.SetActive(index == 3);
    }


    // =========================================================
    // BRIGHTNESS
    // Slider = 0 ~ 100
    // =========================================================

    public void SetBrightness(float value)
    {
        value = Mathf.Clamp(value, 0f, 100f);

        float normalized = value / 100f;

        if (brightnessOverlay != null)
        {
            brightnessOverlay.alpha =
                Mathf.Lerp(0.85f, 0f, normalized);
        }

        if (brightnessValueText != null)
        {
            brightnessValueText.text =
                Mathf.RoundToInt(value) + "%";
        }

        PlayerPrefs.SetFloat(
            BrightnessKey,
            value
        );

        PlayerPrefs.Save();
    }


    private void ApplyBrightness(float value)
    {
        // 0 ~ 100
        value = Mathf.Clamp(value, 0f, 100f);

        // 0 ~ 1
        float normalizedValue = value / 100f;


        if (brightnessOverlay != null)
        {
            // 100% = 검은막 없음
            // 0%   = 검은막 85%
            brightnessOverlay.alpha =
                Mathf.Lerp(
                    MaxDarkness,
                    0f,
                    normalizedValue
                );
        }


        if (brightnessValueText != null)
        {
            brightnessValueText.text =
                Mathf.RoundToInt(value) + "%";
        }
    }


    // =========================================================
    // VOLUME
    // Slider = 0 ~ 100
    // =========================================================

    public void SetVolume(float value)
    {
        value = Mathf.Clamp(value, 0f, 100f);

        ApplyVolume(value);

        PlayerPrefs.SetFloat(
            VolumeKey,
            value
        );

        PlayerPrefs.Save();
    }


    private void ApplyVolume(float value)
    {
        value = Mathf.Clamp(value, 0f, 100f);

        float normalizedVolume =
            value / 100f;


        AudioListener.volume =
            normalizedVolume;


        if (volumeValueText != null)
        {
            volumeValueText.text =
                Mathf.RoundToInt(value) + "%";
        }
    }


    // =========================================================
    // SCREEN FX (화면 노이즈 효과 ON/OFF)
    // 원래 창 모드 자리. 게임은 항상 전체 화면이라 이 기능으로 바꿈
    // =========================================================

    public void SetScreenEffects(bool isOn)
    {
        ApplyScreenEffects(isOn);

        PlayerPrefs.SetInt(
            ScreenFxKey,
            isOn ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    // 예전 이름으로 연결된 버튼이 남아 있어도 동작하게 (Screen FX로 넘김)
    public void SetFullscreen(bool isOn)
    {
        SetScreenEffects(isOn);
    }


    private void ApplyScreenEffects(bool isOn)
    {
        if (screenFxValueText != null)
        {
            screenFxValueText.text =
                isOn ? "ON" : "OFF";
        }
    }


    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void LoadSettings()
    {
        // -----------------------------------------
        // BRIGHTNESS
        // -----------------------------------------

        float brightness =
            PlayerPrefs.GetFloat(
                BrightnessKey,
                100f
            );


        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(
                brightness
            );
        }

        ApplyBrightness(brightness);


        // -----------------------------------------
        // VOLUME
        // -----------------------------------------

        float volume =
            PlayerPrefs.GetFloat(
                VolumeKey,
                100f
            );


        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(
                volume
            );
        }

        ApplyVolume(volume);


        // -----------------------------------------
        // SCREEN FX
        // -----------------------------------------

        bool screenFx = ScreenEffectsOn;


        if (screenFxToggle != null)
        {
            screenFxToggle.SetIsOnWithoutNotify(
                screenFx
            );
        }

        ApplyScreenEffects(screenFx);
    }


    // =========================================================
    // EXIT PAGE
    // =========================================================

    public void NewGame()
    {
        // 완전히 처음부터: 모든 챕터 진행도/완료 기록/튕김 연출 초기화
        Time.timeScale = 1f;
        GameSfx.StopNoise();

        SessionProgress.ResetAll();
        Chapter4Glitch.ResetCrash();

        SceneManager.LoadScene("MainScene");
    }


    public void ContinueGame()
    {
        // 멈췄던 시간 다시 흐르게
        Time.timeScale = 1f;

        bool inChapter = FindAnyObjectByType<ChapterManager>() != null;

        if (inChapter)
        {
            // 챕터 안에서 연 설정창: 창만 닫고 하던 미니게임 그대로 계속
            CloseSettings();
            return;
        }

        // 메인 화면에서 연 설정창: 저장된 진행도로 챕터 선택 화면 열기
        StartMenuController startMenu = FindAnyObjectByType<StartMenuController>();

        if (startMenu != null)
        {
            startMenu.ContinueFromSave();
        }
        else if (screenController != null)
        {
            screenController.ShowCheckProgressScreen();
        }
        else
        {
            CloseSettings();
        }
    }


    public void QuitGame()
    {
#if UNITY_EDITOR

        // 에디터에서는 Application.Quit()이 아무 반응이 없어서 플레이 모드를 멈춤
        Debug.Log("게임 종료 버튼 클릭");
        UnityEditor.EditorApplication.isPlaying = false;

#else

        Application.Quit();

#endif
    }
}