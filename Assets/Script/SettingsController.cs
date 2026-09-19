using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Text fullscreenValueText;

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
    private const string FullscreenKey = "Fullscreen";



    // 가장 어두울 때 검은 오버레이 강도
    private const float MaxDarkness = 0.85f;



    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
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
    // FULLSCREEN
    // =========================================================

    public void SetFullscreen(bool isFullscreen)
    {
        ApplyFullscreen(isFullscreen);

        PlayerPrefs.SetInt(
            FullscreenKey,
            isFullscreen ? 1 : 0
        );

        PlayerPrefs.Save();
    }


    private void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;


        if (fullscreenValueText != null)
        {
            fullscreenValueText.text =
                isFullscreen ? "ON" : "OFF";
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
        // FULLSCREEN
        // -----------------------------------------

        bool fullscreen =
            PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1;


        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(
                fullscreen
            );
        }

        ApplyFullscreen(fullscreen);
    }


    // =========================================================
    // EXIT PAGE
    // =========================================================

    public void NewGame()
    {
        // 나중에 저장 시스템이 완성되면
        // 여기에서 진행도 초기화를 추가하면 됨.

        SceneManager.LoadScene("MainScene");
    }


    public void ContinueGame()
    {
        // SettingsController가 UIManager에 붙어있기 때문에
        // gameObject.SetActive(false)는 절대 하면 안 됨.

        if (screenController != null)
        {
            screenController.ShowMainScreen();
        }
    }


    public void QuitGame()
    {
#if UNITY_EDITOR

        Debug.Log("게임 종료 버튼 클릭");

#else

        Application.Quit();

#endif
    }
}