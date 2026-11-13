
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Minigame22_ScatteredStart : MinigameBase
{
    [Header("SETTINGS Tab Buttons")]
    public Button displayTabButton;
    public Button soundTabButton;
    public Button infoTabButton;
    public Button exitTabButton;

    [Header("Selected Dots (Optional)")]
    public GameObject displayDot;
    public GameObject soundDot;
    public GameObject infoDot;
    public GameObject exitDot;

    [Header("Tab Sprites (Optional)")]
    public Sprite tabNormalSprite;
    public Sprite tabSelectedSprite;

    [Header("Settings UI (Optional)")]
    public CanvasGroup brightnessOverlay;
    public TMP_Text timeText;

    [Header("MG22 Puzzle")]
    [Range(0.01f, 0.5f)]
    public float sliderRevealFraction = 0.12f;

    public float finalTDelay = 5f;

    private GameObject displayPage;
    private GameObject soundPage;
    private GameObject infoPage;
    private GameObject exitPage;

    private Slider brightnessSlider;
    private Slider volumeSlider;

    private TMP_Text brightnessValueText;
    private TMP_Text volumeValueText;
    private TMP_Text screenFxValueText;

    private Toggle screenFxToggle;

    private Button hiddenS;
    private Button hiddenFirstT;
    private Button hiddenR;
    private Button quitButton;
    private Button finalButton;

    private TMP_Text aboutValue;
    private TMP_Text detailValue;
    private TMP_Text assemblyHint;

    private RectTransform exitPopup;
    private Vector2 exitPopupHome;

    private GameObject assemblyPanel;
    private Canvas rootCanvas;

    private readonly bool[] found = new bool[5];

    private readonly char[] answer =
    {
        'S', 'T', 'A', 'R', 'T'
    };

    private readonly string[] blockNames =
    {
        "LetterBlock_S",
        "LetterBlock_T1",
        "LetterBlock_A",
        "LetterBlock_R",
        "LetterBlock_T2"
    };

    private readonly MG22LetterDrag[] blocks =
        new MG22LetterDrag[5];

    private readonly RectTransform[] slots =
        new RectTransform[5];

    private readonly MG22LetterDrag[] placed =
        new MG22LetterDrag[5];

    private float initialBrightness;
    private float initialVolume;
    private float elapsed;
    private float lastTReadyAt = -1f;
    private float originalAudioVolume;

    private bool lastTEnabled;
    private bool assemblyShowing;
    private bool ready;

    // =====================================
    // 초기화
    // =====================================

    private void Awake()
    {
        originalAudioVolume = AudioListener.volume;

        // 복사된 SETTINGS의 기존 컨트롤러가
        // 페이지를 중복으로 조작하지 않도록 함
        foreach (MonoBehaviour behaviour in
            GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != this &&
                behaviour.GetType().Name ==
                "SettingsController")
            {
                behaviour.enabled = false;
            }
        }
    }

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "흩어진 START";

        instruction =
            "SETTINGS 화면에 흩어진 글자를 찾으십시오.";

        // MG22에는 시간제한 없음
        timeLimit = 999999f;

        base.StartMinigame();

        FindReferences();
        BindControls();
        SetupDragAndLinks();
        ResetPuzzle();

        ready = true;

        Debug.Log("MG22 : 게임 시작");
    }

    // =====================================
    // UI 자동 검색
    // =====================================

    private Transform FindChild(
        Transform parent,
        string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result =
                FindChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private GameObject FindObject(string objectName)
    {
        Transform t =
            FindChild(transform, objectName);

        return t == null ? null : t.gameObject;
    }

    private T FindUI<T>(string objectName)
        where T : Component
    {
        GameObject go = FindObject(objectName);

        return go == null
            ? null
            : go.GetComponent<T>();
    }

    private void WarnIfMissing(
        UnityEngine.Object reference,
        string name)
    {
        if (reference == null)
        {
            Debug.LogWarning(
                "MG22 : 찾을 수 없는 오브젝트 - " +
                name,
                this
            );
        }
    }

    private Button TabButtonByName(
        string name,
        string label)
    {
        Button exact = FindUI<Button>(name);

        if (exact != null)
            return exact;

        GameObject tabs = FindObject("Tabs");

        if (tabs == null)
            return null;

        Button[] buttons =
            tabs.GetComponentsInChildren<Button>(true);

        foreach (Button b in buttons)
        {
            TMP_Text text =
                b.GetComponentInChildren<TMP_Text>(true);

            if (text != null &&
                text.text.Trim().Equals(
                    label,
                    StringComparison.OrdinalIgnoreCase))
            {
                return b;
            }
        }

        return null;
    }

    private void FindReferences()
    {
        displayPage = FindObject("DisplayPage");
        soundPage = FindObject("SoundPage");
        infoPage = FindObject("InfoPage");
        exitPage = FindObject("ExitPage");

        if (displayTabButton == null)
            displayTabButton =
                TabButtonByName("DisplayTab", "DISPLAY");

        if (soundTabButton == null)
            soundTabButton =
                TabButtonByName("SoundTab", "SOUND");

        if (infoTabButton == null)
            infoTabButton =
                TabButtonByName("InfoTab", "INFO");

        if (exitTabButton == null)
            exitTabButton =
                TabButtonByName("ExitTab", "EXIT");

        brightnessSlider =
            FindUI<Slider>("BrightnessSlider");

        volumeSlider =
            FindUI<Slider>("VolumeSlider");

        brightnessValueText =
            FindUI<TMP_Text>("BrightnessValueText");

        volumeValueText =
            FindUI<TMP_Text>("VolumeValueText");

        screenFxToggle =
            FindUI<Toggle>("FullscreenToggle");

        if (screenFxToggle == null)
            screenFxToggle =
                FindUI<Toggle>("ScreenFxToggle");

        screenFxValueText =
            FindUI<TMP_Text>("FullscreenValueText");

        if (screenFxValueText == null)
            screenFxValueText =
                FindUI<TMP_Text>("ScreenFxValueText");

        hiddenS =
            FindUI<Button>("HiddenSButton");

        hiddenFirstT =
            FindUI<Button>("HiddenT1Button");

        hiddenR =
            FindUI<Button>("HiddenRButton");

        quitButton =
            FindUI<Button>("QuitButton");

        aboutValue =
            FindUI<TMP_Text>("AboutValue");

        detailValue =
            FindUI<TMP_Text>("InfoDetailText");

        exitPopup =
            FindUI<RectTransform>("ExitPopup");

        assemblyPanel =
            FindObject("AssemblyPanel");

        assemblyHint =
            FindUI<TMP_Text>("AssemblyHintText");

        finalButton =
            FindUI<Button>("FinalStartButton");

        if (timeText == null)
            timeText = FindUI<TMP_Text>("TimeText");

        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        // 없는 UI를 알려주되 게임 초기화를
        // 중단하지는 않는다.
        WarnIfMissing(displayPage, "DisplayPage");
        WarnIfMissing(soundPage, "SoundPage");
        WarnIfMissing(infoPage, "InfoPage");
        WarnIfMissing(exitPage, "ExitPage");

        WarnIfMissing(
            brightnessSlider,
            "BrightnessSlider"
        );

        WarnIfMissing(volumeSlider, "VolumeSlider");
        WarnIfMissing(hiddenS, "HiddenSButton");
        WarnIfMissing(hiddenFirstT, "HiddenT1Button");
        WarnIfMissing(aboutValue, "AboutValue");
        WarnIfMissing(detailValue, "InfoDetailText");
        WarnIfMissing(hiddenR, "HiddenRButton");
        WarnIfMissing(exitPopup, "ExitPopup");
        WarnIfMissing(assemblyPanel, "AssemblyPanel");
        WarnIfMissing(finalButton, "FinalStartButton");
    }

    // =====================================
    // 기존 버튼 이벤트 교체
    // =====================================

    private void ResetButton(
        Button button,
        UnityAction handler)
    {
        if (button == null)
            return;

        // 복사해 온 SETTINGS의 기존
        // OnClick 이벤트를 실행 중 교체한다.
        button.onClick =
            new Button.ButtonClickedEvent();

        button.onClick.AddListener(handler);
    }

    private void BindControls()
    {
        ResetButton(displayTabButton, ShowDisplay);
        ResetButton(soundTabButton, ShowSound);
        ResetButton(infoTabButton, ShowInfo);
        ResetButton(exitTabButton, ShowExit);

        ResetButton(hiddenS, () => Collect(0));
        ResetButton(hiddenFirstT, () => Collect(1));
        ResetButton(hiddenR, () => Collect(3));

        ResetButton(quitButton, ShowFakeExit);
        ResetButton(finalButton, FinishPuzzle);

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged =
                new Slider.SliderEvent();

            brightnessSlider.onValueChanged.AddListener(
                OnBrightnessChanged
            );

            ApplyBrightness(brightnessSlider.value);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged =
                new Slider.SliderEvent();

            volumeSlider.onValueChanged.AddListener(
                OnVolumeChanged
            );

            ApplyVolume(volumeSlider.value);
        }

        if (screenFxToggle != null)
        {
            screenFxToggle.onValueChanged =
                new Toggle.ToggleEvent();

            screenFxToggle.onValueChanged.AddListener(
                SetScreenEffects
            );

            SetScreenEffects(screenFxToggle.isOn);
        }
    }

    // =====================================
    // A / 마지막 T / 드래그 설정
    // =====================================

    private void SetupDragAndLinks()
    {
        SetupLink(aboutValue, "hidden_a");

        SetupLink(
            detailValue,
            "hidden_last_t"
        );

        // EXIT 팝업 드래그
        if (exitPopup != null)
        {
            MG22PopupDrag drag =
                exitPopup.GetComponent<MG22PopupDrag>();

            if (drag == null)
            {
                drag =
                    exitPopup.gameObject
                        .AddComponent<MG22PopupDrag>();
            }

            drag.Setup(rootCanvas);

            Image image =
                exitPopup.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = true;

            foreach (TMP_Text child in
                exitPopup.GetComponentsInChildren<TMP_Text>(
                    true))
            {
                child.raycastTarget = false;
            }
        }

        // 다섯 개의 글자 블록 / 슬롯
        for (int i = 0; i < 5; i++)
        {
            GameObject blockGO =
                FindObject(blockNames[i]);

            GameObject slotGO =
                FindObject("TargetSlot_" + i);

            if (blockGO == null || slotGO == null)
            {
                Debug.LogWarning(
                    "MG22 : 글자 블록 또는 슬롯 없음 - " +
                    blockNames[i],
                    this
                );

                continue;
            }

            MG22LetterDrag drag =
                blockGO.GetComponent<MG22LetterDrag>();

            if (drag == null)
            {
                drag =
                    blockGO.AddComponent<MG22LetterDrag>();
            }

            drag.Setup(this, answer[i]);
            blocks[i] = drag;

            RectTransform rect =
                slotGO.GetComponent<RectTransform>();

            slots[i] = rect;

            MG22DropSlot drop =
                slotGO.GetComponent<MG22DropSlot>();

            if (drop == null)
            {
                drop =
                    slotGO.AddComponent<MG22DropSlot>();
            }

            drop.Setup(this, i);

            Image slotImage =
                slotGO.GetComponent<Image>();

            if (slotImage != null)
                slotImage.raycastTarget = true;

            Image blockImage =
                blockGO.GetComponent<Image>();

            if (blockImage != null)
                blockImage.raycastTarget = true;

            foreach (TMP_Text t in
                slotGO.GetComponentsInChildren<TMP_Text>(
                    true))
            {
                t.raycastTarget = false;
            }

            foreach (TMP_Text t in
                blockGO.GetComponentsInChildren<TMP_Text>(
                    true))
            {
                t.raycastTarget = false;
            }
        }
    }

    private void SetupLink(
        TMP_Text text,
        string linkId)
    {
        if (text == null)
            return;

        text.richText = true;
        text.raycastTarget = true;

        MG22TextLinkClick click =
            text.GetComponent<MG22TextLinkClick>();

        if (click == null)
        {
            click =
                text.gameObject
                    .AddComponent<MG22TextLinkClick>();
        }

        click.Setup(this, text, linkId);

        if (!text.text.Contains(
            "<link=\"" + linkId + "\">"))
        {
            Debug.LogWarning(
                "MG22 : " + text.name +
                "에 link 태그가 없습니다.",
                text
            );
        }
    }

    // =====================================
    // 퍼즐 리셋
    // =====================================

    private void ResetPuzzle()
    {
        ready = false;

        elapsed = 0f;
        lastTReadyAt = -1f;
        lastTEnabled = false;
        assemblyShowing = false;

        for (int i = 0; i < 5; i++)
        {
            found[i] = false;
            placed[i] = null;

            if (blocks[i] != null)
                blocks[i].ResetHome();
        }

        initialBrightness =
            brightnessSlider != null
                ? brightnessSlider.value
                : 0f;

        initialVolume =
            volumeSlider != null
                ? volumeSlider.value
                : 0f;

        if (hiddenS != null)
            hiddenS.gameObject.SetActive(false);

        if (hiddenFirstT != null)
            hiddenFirstT.gameObject.SetActive(false);

        if (hiddenR != null)
            hiddenR.gameObject.SetActive(false);

        if (exitPopup != null)
        {
            exitPopupHome = exitPopup.anchoredPosition;

            exitPopup.gameObject.SetActive(false);
        }

        if (assemblyPanel != null)
            assemblyPanel.SetActive(false);

        if (finalButton != null)
        {
            finalButton.gameObject.SetActive(false);
            finalButton.interactable = true;
        }

        if (assemblyHint != null)
        {
            assemblyHint.text =
                "획득한 글자를 올바른 순서로 배치하십시오.";
        }

        ShowDisplay();
        UpdateProgress();
        UpdateClock();

        ready = true;
    }

    // =====================================
    // SETTINGS 페이지 전환
    // 기존 SettingsController 방식
    // =====================================

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

        // 선택된 탭 표시
        if (displayDot != null)
            displayDot.SetActive(index == 0);

        if (soundDot != null)
            soundDot.SetActive(index == 1);

        if (infoDot != null)
            infoDot.SetActive(index == 2);

        if (exitDot != null)
            exitDot.SetActive(index == 3);

        // 선택된 탭 이미지
        Button[] tabs =
        {
            displayTabButton,
            soundTabButton,
            infoTabButton,
            exitTabButton
        };

        if (tabNormalSprite != null &&
            tabSelectedSprite != null)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null)
                    continue;

                Image image =
                    tabs[i].GetComponent<Image>();

                if (image != null)
                {
                    image.sprite =
                        i == index
                            ? tabSelectedSprite
                            : tabNormalSprite;
                }
            }
        }
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

        // EXIT를 눌러도 실제로 종료되지 않음
        ShowFakeExit();
    }

    // =====================================
    // 원본 설정 스크립트 기반
    // Brightness / Volume / Screen FX
    // =====================================

    public void SetBrightness(float value)
    {
        ApplyBrightness(value);
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessSlider == null)
            return;

        float percent = Mathf.InverseLerp(
            brightnessSlider.minValue,
            brightnessSlider.maxValue,
            value
        );

        if (brightnessOverlay != null)
        {
            brightnessOverlay.alpha =
                Mathf.Lerp(0.85f, 0f, percent);
        }

        if (brightnessValueText != null)
        {
            brightnessValueText.text =
                Mathf.RoundToInt(percent * 100f) + "%";
        }
    }

    public void SetVolume(float value)
    {
        ApplyVolume(value);
    }

    private void ApplyVolume(float value)
    {
        if (volumeSlider == null)
            return;

        float percent = Mathf.InverseLerp(
            volumeSlider.minValue,
            volumeSlider.maxValue,
            value
        );

        AudioListener.volume = percent;

        if (volumeValueText != null)
        {
            volumeValueText.text =
                Mathf.RoundToInt(percent * 100f) + "%";
        }
    }

    public void SetScreenEffects(bool isOn)
    {
        if (screenFxValueText != null)
        {
            screenFxValueText.text =
                isOn ? "ON" : "OFF";
        }
    }

    // =====================================
    // DISPLAY - 숨겨진 S
    // =====================================

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value);

        if (!isGameActive ||
            found[0] ||
            hiddenS == null ||
            brightnessSlider == null)
        {
            return;
        }

        float threshold =
            sliderRevealFraction *
            (brightnessSlider.maxValue -
             brightnessSlider.minValue);

        if (Mathf.Abs(value - initialBrightness)
            >= threshold)
        {
            hiddenS.gameObject.SetActive(true);
        }
    }

    // =====================================
    // SOUND - 첫 번째 T
    // =====================================

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);

        if (!isGameActive ||
            found[1] ||
            hiddenFirstT == null ||
            volumeSlider == null)
        {
            return;
        }

        float threshold =
            sliderRevealFraction *
            (volumeSlider.maxValue -
             volumeSlider.minValue);

        if (Mathf.Abs(value - initialVolume)
            >= threshold)
        {
            hiddenFirstT.gameObject.SetActive(true);
        }
    }

    // =====================================
    // EXIT - 가짜 종료창과 R
    // =====================================

    private void ShowFakeExit()
    {
        if (exitPage == null ||
            exitPopup == null)
        {
            return;
        }

        OpenPage(3);

        exitPopup.anchoredPosition =
            exitPopupHome;

        if (hiddenR != null)
        {
            hiddenR.gameObject.SetActive(
                !found[3]
            );
        }

        exitPopup.gameObject.SetActive(true);
        exitPopup.SetAsLastSibling();
    }

    // =====================================
    // 시간 / INFO 탐색
    // =====================================

    protected override void Update()
    {
        base.Update();

        if (!ready || !isGameActive)
            return;

        elapsed += Time.unscaledDeltaTime;

        UpdateClock();

        // 앞의 네 글자를 획득하고
        // 5초 지나면 마지막 T 발견 가능
        if (!lastTEnabled &&
            lastTReadyAt >= 0f &&
            Time.unscaledTime >= lastTReadyAt)
        {
            lastTEnabled = true;

            Debug.Log("MG22 : 마지막 T 활성화");
        }

        if (assemblyShowing)
            return;

        bool hoveringA =
            !found[2] &&
            IsHoveringLink(
                aboutValue,
                "hidden_a"
            );

        bool hoveringT =
            lastTEnabled &&
            !found[4] &&
            IsHoveringLink(
                detailValue,
                "hidden_last_t"
            );

        AnimateLink(
            aboutValue,
            "hidden_a",
            hoveringA,
            false,
            found[2]
        );

        AnimateLink(
            detailValue,
            "hidden_last_t",
            hoveringT,
            lastTEnabled && !found[4],
            found[4]
        );
    }

    private void UpdateClock()
    {
        if (timeText == null)
            return;

        int seconds =
            Mathf.FloorToInt(elapsed);

        timeText.text = string.Format(
            "TIME {0:00}:{1:00}",
            seconds / 60,
            seconds % 60
        );
    }

    private Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(-99999f, -99999f);
#endif
    }

    private Camera EventCamera()
    {
        if (rootCanvas == null ||
            rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera;
    }

    private bool IsHoveringLink(
        TMP_Text text,
        string id)
    {
        if (text == null ||
            !text.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector2 pos = MousePosition();
        Camera camera = EventCamera();

        if (!RectTransformUtility
            .RectangleContainsScreenPoint(
                text.rectTransform,
                pos,
                camera))
        {
            return false;
        }

        text.ForceMeshUpdate();

        int index =
            TMP_TextUtilities.FindIntersectingLink(
                text,
                pos,
                camera
            );

        return
            index >= 0 &&
            index < text.textInfo.linkCount &&
            text.textInfo.linkInfo[index]
                .GetLinkID() == id;
    }

    // =====================================
    // 글자 흔들림 / 마지막 T 점멸
    // =====================================

    private void AnimateLink(
        TMP_Text text,
        string id,
        bool hovering,
        bool flicker,
        bool acquired)
    {
        if (text == null ||
            !text.gameObject.activeInHierarchy)
        {
            return;
        }

        text.ForceMeshUpdate();

        TMP_TextInfo info = text.textInfo;

        for (int k = 0;
             k < info.linkCount;
             k++)
        {
            TMP_LinkInfo link =
                info.linkInfo[k];

            if (link.GetLinkID() != id)
                continue;

            int end =
                link.linkTextfirstCharacterIndex +
                link.linkTextLength;

            for (int i =
                link.linkTextfirstCharacterIndex;
                i < end && i < info.characterCount;
                i++)
            {
                TMP_CharacterInfo ch =
                    info.characterInfo[i];

                if (!ch.isVisible)
                    continue;

                TMP_MeshInfo mesh =
                    info.meshInfo[
                        ch.materialReferenceIndex
                    ];

                Color32 color;

                if (acquired)
                {
                    color =
                        new Color32(
                            125, 255, 175, 255
                        );
                }
                else if (hovering)
                {
                    color =
                        new Color32(
                            255, 233, 120, 255
                        );
                }
                else
                {
                    color =
                        new Color32(
                            255, 255, 255, 255
                        );
                }

                // 0.4초 간격으로 점멸
                if (flicker &&
                    Mathf.FloorToInt(
                        Time.unscaledTime / 0.4f
                    ) % 2 == 0)
                {
                    color.a = 35;
                }

                float wave = hovering
                    ? Mathf.Sin(
                        Time.unscaledTime * 18f + i
                    ) * 1.3f
                    : 0f;

                for (int v = 0; v < 4; v++)
                {
                    mesh.colors32[
                        ch.vertexIndex + v
                    ] = color;

                    mesh.vertices[
                        ch.vertexIndex + v
                    ].y += wave;
                }
            }
        }

        text.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32 |
            TMP_VertexDataUpdateFlags.Vertices
        );
    }

    // =====================================
    // INFO 문자 클릭
    // =====================================

    public void OnLinkClicked(string id)
    {
        if (!isGameActive || assemblyShowing)
            return;

        if (id == "hidden_a" &&
            !found[2] &&
            infoPage != null &&
            infoPage.activeInHierarchy)
        {
            Collect(2);
        }

        if (id == "hidden_last_t" &&
            lastTEnabled &&
            !found[4] &&
            infoPage != null &&
            infoPage.activeInHierarchy)
        {
            Collect(4);
        }
    }

    // =====================================
    // 글자 획득
    // 0 S
    // 1 T
    // 2 A
    // 3 R
    // 4 T
    // =====================================

    private void Collect(int index)
    {
        if (!isGameActive ||
            index < 0 ||
            index >= found.Length ||
            found[index])
        {
            return;
        }

        found[index] = true;

        if (index == 0 && hiddenS != null)
            hiddenS.gameObject.SetActive(false);

        if (index == 1 && hiddenFirstT != null)
            hiddenFirstT.gameObject.SetActive(false);

        if (index == 3)
        {
            if (hiddenR != null)
                hiddenR.gameObject.SetActive(false);

            if (exitPopup != null)
                exitPopup.gameObject.SetActive(false);
        }

        UpdateProgress();

        // S, T, A, R 모두 획득
        if (found[0] &&
            found[1] &&
            found[2] &&
            found[3] &&
            lastTReadyAt < 0f)
        {
            lastTReadyAt =
                Time.unscaledTime +
                Mathf.Max(0f, finalTDelay);

            Debug.Log(
                "MG22 : 5초 후 마지막 T 활성화"
            );
        }

        bool all = true;

        foreach (bool value in found)
        {
            if (!value)
                all = false;
        }

        if (all)
        {
            OpenAssembly();
        }

        Debug.Log(
            "MG22 : 글자 획득 - " +
            answer[index]
        );
    }

    // =====================================
    // 수집 현황
    // =====================================

    private void UpdateProgress()
    {
        string progress = "";

        for (int i = 0; i < 5; i++)
        {
            if (i != 0)
                progress += " - ";

            progress += found[i]
                ? answer[i].ToString()
                : "_";
        }

        string[] names =
        {
            "StartSlot_Display",
            "StartSlot_Sound",
            "StartSlot_Info",
            "StartSlot_Exit"
        };

        foreach (string name in names)
        {
            GameObject go =
                FindObject(name);

            if (go == null)
                continue;

            TMP_Text text =
                go.GetComponent<TMP_Text>();

            if (text == null)
            {
                text =
                    go.GetComponentInChildren<TMP_Text>(
                        true
                    );
            }

            if (text != null)
            {
                text.text = progress;
            }
        }
    }

    // =====================================
    // 조합창 열기
    // =====================================

    private void OpenAssembly()
    {
        if (assemblyPanel == null)
            return;

        assemblyShowing = true;

        assemblyPanel.SetActive(true);

        assemblyPanel.transform.SetAsLastSibling();

        for (int i = 0; i < 5; i++)
        {
            placed[i] = null;

            if (blocks[i] != null)
            {
                blocks[i].ResetHome();
            }
        }

        if (finalButton != null)
        {
            finalButton.gameObject.SetActive(false);
        }

        UpdateSlotTexts();

        if (assemblyHint != null)
        {
            assemblyHint.text =
                "글자 블록을 빈칸으로 드래그하세요.";
        }
    }

    public bool CanDrag()
    {
        return isGameActive && assemblyShowing;
    }

    public RectTransform GetAssemblyRect()
    {
        return assemblyPanel == null
            ? null
            : assemblyPanel.GetComponent<RectTransform>();
    }

    // =====================================
    // 글자를 슬롯에 배치
    // =====================================

    public void DropOnSlot(
        MG22LetterDrag block,
        int index)
    {
        if (!CanDrag() ||
            block == null ||
            index < 0 ||
            index >= 5 ||
            slots[index] == null)
        {
            return;
        }

        int previousIndex = block.CurrentSlot;

        MG22LetterDrag occupant =
            placed[index];

        // 다른 글자가 있으면 자리 교환
        if (occupant != null &&
            occupant != block)
        {
            if (previousIndex >= 0 &&
                previousIndex < 5)
            {
                placed[previousIndex] = occupant;

                occupant.Snap(
                    slots[previousIndex],
                    previousIndex
                );
            }
            else
            {
                occupant.ResetHome();
            }
        }
        else if (previousIndex >= 0 &&
                 previousIndex < 5 &&
                 previousIndex != index)
        {
            placed[previousIndex] = null;
        }

        placed[index] = block;

        block.Snap(
            slots[index],
            index
        );

        UpdateSlotTexts();
        CheckAnswer();
    }

    private void UpdateSlotTexts()
    {
        for (int i = 0; i < 5; i++)
        {
            if (slots[i] == null)
                continue;

            TMP_Text text =
                slots[i]
                    .GetComponentInChildren<TMP_Text>(
                        true
                    );

            if (text != null)
            {
                text.text =
                    placed[i] == null ? "_" : " ";
            }
        }
    }

    // =====================================
    // START 순서 확인
    // =====================================

    private void CheckAnswer()
    {
        bool complete = true;
        bool correct = true;

        for (int i = 0; i < 5; i++)
        {
            if (placed[i] == null)
            {
                complete = false;
                correct = false;
                break;
            }

            if (placed[i].Letter != answer[i])
            {
                correct = false;
            }
        }

        if (finalButton != null)
        {
            finalButton.gameObject.SetActive(
                correct
            );
        }

        if (assemblyHint == null)
            return;

        if (correct)
        {
            assemblyHint.text =
                "START 복구 완료! 버튼을 누르세요.";
        }
        else if (complete)
        {
            assemblyHint.text =
                "순서가 틀렸습니다. 다시 배치하세요.";
        }
        else
        {
            assemblyHint.text =
                "획득한 글자를 올바른 순서로 배치하세요.";
        }
    }

    // =====================================
    // 최종 클리어
    // =====================================

    private void FinishPuzzle()
    {
        if (!CanDrag())
            return;

        for (int i = 0; i < 5; i++)
        {
            if (placed[i] == null ||
                placed[i].Letter != answer[i])
            {
                return;
            }
        }

        if (finalButton != null)
            finalButton.interactable = false;

        Debug.Log("MG22 : START 복구 성공!");

        Success();
    }

    protected override void GiveHint()
    {
        Debug.Log(
            "MG22 힌트 : SETTINGS의 모든 탭을 탐색하세요."
        );
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        AudioListener.volume =
            originalAudioVolume;
    }
}

// =====================================
// TextMeshPro 링크 클릭 처리
// =====================================

public class MG22TextLinkClick :
    MonoBehaviour,
    IPointerClickHandler
{
    private Minigame22_ScatteredStart manager;
    private TMP_Text text;
    private string id;

    public void Setup(
        Minigame22_ScatteredStart owner,
        TMP_Text target,
        string targetId)
    {
        manager = owner;
        text = target;
        id = targetId;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (manager == null || text == null)
            return;

        text.ForceMeshUpdate();

        int index =
            TMP_TextUtilities.FindIntersectingLink(
                text,
                eventData.position,
                eventData.pressEventCamera
            );

        if (index < 0 ||
            index >= text.textInfo.linkCount)
        {
            return;
        }

        if (text.textInfo.linkInfo[index]
            .GetLinkID() == id)
        {
            manager.OnLinkClicked(id);
        }
    }
}

// =====================================
// EXIT 팝업 드래그
// =====================================

public class MG22PopupDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rect;
    private Canvas root;

    public void Setup(Canvas rootCanvas)
    {
        rect = GetComponent<RectTransform>();
        root = rootCanvas;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (rect == null)
            return;

        float scale =
            root == null
                ? 1f
                : Mathf.Max(
                    0.01f,
                    root.scaleFactor
                );

        rect.anchoredPosition +=
            new Vector2(
                eventData.delta.x / scale,
                0f
            );
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
    }
}

// =====================================
// START 글자 드래그
// =====================================

public class MG22LetterDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public char Letter { get; private set; }

    public int CurrentSlot { get; private set; } = -1;

    private Minigame22_ScatteredStart manager;

    private RectTransform rect;
    private RectTransform assemblyRect;
    private CanvasGroup group;

    private RectTransform homeParent;

    private Vector2 homePosition;
    private Vector2 homeAnchorMin;
    private Vector2 homeAnchorMax;
    private Vector2 homePivot;
    private Vector2 homeSize;

    private Vector3 homeScale;

    private Transform dragParent;
    private Vector2 dragPosition;

    private bool saved;
    private bool dragging;
    private bool accepted;

    private Vector2 pointerOffset;

    public void Setup(
        Minigame22_ScatteredStart owner,
        char character)
    {
        manager = owner;
        Letter = character;

        rect = GetComponent<RectTransform>();

        group = GetComponent<CanvasGroup>();

        if (group == null)
        {
            group =
                gameObject.AddComponent<CanvasGroup>();
        }

        if (!saved)
        {
            homeParent =
                rect.parent as RectTransform;

            homePosition = rect.anchoredPosition;

            homeAnchorMin = rect.anchorMin;
            homeAnchorMax = rect.anchorMax;
            homePivot = rect.pivot;
            homeSize = rect.sizeDelta;

            homeScale = rect.localScale;

            saved = true;
        }
    }

    public void ResetHome()
    {
        if (!saved)
            return;

        rect.SetParent(homeParent, false);

        rect.anchorMin = homeAnchorMin;
        rect.anchorMax = homeAnchorMax;

        rect.pivot = homePivot;
        rect.sizeDelta = homeSize;

        rect.anchoredPosition = homePosition;
        rect.localScale = homeScale;

        CurrentSlot = -1;

        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
    }

    public void Snap(
        RectTransform target,
        int index)
    {
        rect.SetParent(target, false);

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = Vector2.zero;
        rect.localScale = homeScale;

        CurrentSlot = index;
        accepted = true;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (manager == null ||
            !manager.CanDrag())
        {
            return;
        }

        assemblyRect =
            manager.GetAssemblyRect();

        if (assemblyRect == null)
            return;

        dragging = true;
        accepted = false;

        dragParent = rect.parent;
        dragPosition = rect.anchoredPosition;

        Vector2 pointerLocal;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                assemblyRect,
                eventData.position,
                eventData.pressEventCamera,
                out pointerLocal
            );

        Vector3 centerLocal =
            assemblyRect.InverseTransformPoint(
                rect.position
            );

        pointerOffset =
            new Vector2(
                centerLocal.x,
                centerLocal.y
            ) - pointerLocal;

        rect.SetParent(assemblyRect, true);
        rect.SetAsLastSibling();

        group.alpha = 0.85f;
        group.blocksRaycasts = false;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging)
            return;

        Vector2 local;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                assemblyRect,
                eventData.position,
                eventData.pressEventCamera,
                out local
            ))
        {
            rect.anchoredPosition =
                local + pointerOffset;
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;

        group.alpha = 1f;
        group.blocksRaycasts = true;

        if (!accepted)
        {
            rect.SetParent(dragParent, false);

            rect.anchoredPosition =
                dragPosition;
        }
    }
}

// =====================================
// START 슬롯 드롭 처리
// =====================================

public class MG22DropSlot :
    MonoBehaviour,
    IDropHandler
{
    private Minigame22_ScatteredStart manager;
    private int index;

    public void Setup(
        Minigame22_ScatteredStart owner,
        int slotIndex)
    {
        manager = owner;
        index = slotIndex;
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        if (manager == null ||
            eventData.pointerDrag == null)
        {
            return;
        }

        MG22LetterDrag block =
            eventData.pointerDrag
                .GetComponent<MG22LetterDrag>();

        if (block != null)
        {
            manager.DropOnSlot(block, index);
        }
    }
}
