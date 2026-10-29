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

    [Header("Pixel Tab Sprites (선택된 탭은 노란 테두리)")]
    [SerializeField] private Sprite tabNormalSprite;
    [SerializeField] private Sprite tabSelectedSprite;

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

        // 챕터 안에서만: EXIT 탭에 "Chapter Select"(챕터 선택 화면으로 나가기) 버튼 추가
        CreateChapterSelectButton();

        // EXIT 탭 버튼 정리 (Continue는 뺌) + 간격 맞추기
        LayoutExitButtons();

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

        // 선택된 탭 테두리를 노란색으로
        UpdateTabSprites(index);
    }


    private void UpdateTabSprites(int index)
    {
        if (tabNormalSprite == null || tabSelectedSprite == null || displayPage == null)
            return;

        Transform pages = displayPage.transform.parent;
        Transform tabs = pages != null && pages.parent != null ? pages.parent.Find("Tabs") : null;

        if (tabs == null)
            return;

        // Tabs 안 순서: DISPLAY / SOUND / INFO / EXIT
        int tabIndex = 0;
        foreach (Transform tab in tabs)
        {
            Image img = tab.GetComponent<Image>();
            if (img == null) continue;

            img.sprite = tabIndex == index ? tabSelectedSprite : tabNormalSprite;
            tabIndex++;
        }
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

        // 모니터 화면/지지직 인트로 없이 전체 화면 시작 화면부터
        SessionProgress.skipIntroOnLoad = true;

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


    // =========================================================
    // 게임 중간에 나가기 (챕터 → 챕터 선택 화면)
    // 지금까지 깬 미니게임 수는 저장된 채로 나감
    // =========================================================

    public void ExitToChapterSelect()
    {
        Time.timeScale = 1f;
        GameSfx.StopNoise();

        ChapterManager chapterManager = FindAnyObjectByType<ChapterManager>();

        if (chapterManager != null)
        {
            chapterManager.ExitToMainScene();
        }
        else
        {
            SessionProgress.openChapterSelectOnLoad = true;
            SceneManager.LoadScene("MainScene");
        }
    }


    // EXIT 탭 버튼 배치 (위에서부터)
    //   메인 화면: NewGame / Quit
    //   챕터 안:   NewGame / Chapter Select / Quit
    // Chapter Select 버튼은 Quit 버튼을 복사해서 만들기 때문에 모양·글꼴이 똑같이 나옴
    private const float ExitButtonWidth = 320f;
    private const float ExitButtonGap = 80f;
    // 버튼 묶음의 가운데 높이 (탭 아래 ~ 구분선 위 빈 공간의 정가운데)
    private const float ExitButtonCenterY = -99f;

    private void CreateChapterSelectButton()
    {
        // 메인 화면 설정창에는 필요 없음 (챕터 안에서만)
        if (FindAnyObjectByType<ChapterManager>() == null)
            return;

        if (exitPage == null)
            return;

        Transform page = exitPage.transform;

        // 이미 만들었으면 다시 만들지 않음
        if (page.Find("ChapterSelectButton") != null)
            return;

        Transform quit = page.Find("QuitButton");

        if (quit == null)
            return;

        // 1) Quit 버튼 복사
        GameObject copy = Instantiate(quit.gameObject, page);
        copy.name = "ChapterSelectButton";
        copy.transform.SetSiblingIndex(quit.GetSiblingIndex());

        // 2) 복사하면 "게임 종료" 연결까지 따라오므로 지우고 "챕터 선택으로" 연결
        Button button = copy.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ExitToChapterSelect);
        }

        // 3) 글자 바꾸기
        TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Chapter Select";

    }


    private void LayoutExitButtons()
    {
        if (exitPage == null)
            return;

        Transform page = exitPage.transform;

        // Continue 버튼은 사용 안 함 (BACK / START랑 기능이 겹쳐서 뺌)
        Transform cont = page.Find("ContinueButton (1)");
        if (cont != null)
            cont.gameObject.SetActive(false);

        string[] order = { "NewGameButton", "ChapterSelectButton", "QuitButton" };

        // 있는 버튼만 모으기 (메인 화면 2개 / 챕터 안 3개)
        System.Collections.Generic.List<RectTransform> buttons = new System.Collections.Generic.List<RectTransform>();
        foreach (string name in order)
        {
            RectTransform rt = page.Find(name) as RectTransform;
            if (rt != null)
                buttons.Add(rt);
        }

        // 가운데 정렬: 버튼 개수와 상관없이 묶음 전체가 빈 공간 가운데에 오게
        float y = ExitButtonCenterY + (buttons.Count - 1) * ExitButtonGap * 0.5f;
        foreach (RectTransform rt in buttons)
        {
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(ExitButtonWidth, rt.sizeDelta.y);
            y -= ExitButtonGap;
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