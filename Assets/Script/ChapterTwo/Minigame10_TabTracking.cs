using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame10_TabTracking : MinigameBase
{
    // =========================================================
    // 진행 단계
    // =========================================================

    private enum Step
    {
        InitialStart,

        WaitSettingsButton,

        DisplayStart,

        WaitSoundTab,
        SoundStart,

        WaitInfoTab,
        InfoStart,

        WaitExitTab,
        ExitStart,

        WaitBack,

        FinalStart,

        Finished
    }


    // =========================================================
    // MG10 UI
    // =========================================================

    [Header("MG10 UI")]
    [SerializeField] private Button startButton;

    [SerializeField]
    private RectTransform initialStartSlot;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private TMP_Text statusText;


    // =========================================================
    // 마지막 화면
    // =========================================================

    [Header("Final Screen")]
    [Tooltip("마지막 화면에서 숨길 오브젝트")]
    [SerializeField]
    private GameObject[] hideOnFinal;


    // =========================================================
    // Blink
    // =========================================================

    [Header("Blink")]
    [SerializeField]
    private float blinkSpeed = 6f;

    [SerializeField]
    [Range(0f, 1f)]
    private float blinkMinAlpha = 0.65f;


    [Header("Blink Color")]
    [SerializeField]
    private Color blinkColor =
        new Color32(102, 255, 214, 255); // #66FFD6


    // =========================================================
    // 내부 변수
    // =========================================================

    private SettingsMinigameBridge settingsBridge;

    private Step currentStep;

    private Coroutine blinkCoroutine;

    private CanvasGroup blinkingCanvasGroup;

    private float originalBlinkAlpha = 1f;


    // 반짝이는 버튼 안의 이미지 / 글자
    private Graphic[] blinkingGraphics;

    // 원래 색상 저장
    private Color[] originalColors;


    // Button의 Color Tint가 색을 덮어쓰는 것 방지
    private Button blinkingButton;

    private Selectable.Transition originalTransition;


    // 기존 ButtonHoverColor가 흰색으로 되돌리는 것 방지
    private ButtonHoverColor blinkingHoverColor;

    private bool blinkingHoverWasEnabled;


    // =========================================================
    // START MINIGAME
    // =========================================================

    public override void StartMinigame()
    {
        base.StartMinigame();


        gameName = "활성 화면 추적 검사";

        instruction =
            "이동하는 START 버튼을 추적하세요.";


        // =====================================================
        // SettingsSystem 찾기
        // =====================================================

        settingsBridge =
            FindFirstObjectByType<SettingsMinigameBridge>();


        if (settingsBridge == null)
        {
            Debug.LogError(
                "SettingsMinigameBridge를 찾을 수 없습니다."
            );

            return;
        }


        // =====================================================
        // 혹시 설정창이 열려 있으면 닫기
        // =====================================================

        settingsBridge.CloseSettings();


        // =====================================================
        // START 버튼 세팅
        // =====================================================

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();

            startButton.onClick.AddListener(
                OnStartButtonClicked
            );


            MoveStartButton(
                initialStartSlot
            );


            startButton.gameObject.SetActive(true);
        }


        // =====================================================
        // 톱니바퀴
        // MG10에서는 일반 설정 버튼 대신
        // MG10 진행용 버튼으로 사용
        // =====================================================

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();

            settingsButton.onClick.AddListener(
                OnSettingsButtonClicked
            );


            settingsButton.gameObject.SetActive(true);
        }


        // =====================================================
        // SOUND TAB 감지
        // =====================================================

        if (settingsBridge.SoundTabButton != null)
        {
            settingsBridge.SoundTabButton.onClick.AddListener(
                OnSoundTabClicked
            );
        }


        // =====================================================
        // INFO TAB 감지
        // =====================================================

        if (settingsBridge.InfoTabButton != null)
        {
            settingsBridge.InfoTabButton.onClick.AddListener(
                OnInfoTabClicked
            );
        }


        // =====================================================
        // EXIT TAB 감지
        // =====================================================

        if (settingsBridge.ExitTabButton != null)
        {
            settingsBridge.ExitTabButton.onClick.AddListener(
                OnExitTabClicked
            );
        }


        // =====================================================
        // BACK 버튼 감지
        // =====================================================

        if (settingsBridge.BackButton != null)
        {
            settingsBridge.BackButton.onClick.AddListener(
                OnBackButtonClicked
            );


            // 마지막 단계가 되기 전에는 BACK 사용 금지
            settingsBridge.BackButton.interactable = false;
        }


        // =====================================================
        // 최초 단계
        // =====================================================

        currentStep = Step.InitialStart;


        SetStatus(
            "START 버튼을 누르세요."
        );
    }


    // =========================================================
    // START BUTTON CLICK
    // =========================================================

    private void OnStartButtonClicked()
    {
        if (!isGameActive)
            return;


        switch (currentStep)
        {
            // =================================================
            // 처음 START
            // =================================================

            case Step.InitialStart:

                // START를 DISPLAY 안으로 먼저 이동
                MoveStartButton(
                    settingsBridge.DisplayStartSlot
                );


                currentStep =
                    Step.WaitSettingsButton;


                // 톱니바퀴 민트색 반짝임
                if (settingsButton != null)
                {
                    StartBlink(
                        settingsButton.gameObject
                    );
                }


                SetStatus(
                    "설정 버튼을 확인하세요."
                );

                break;


            // =================================================
            // DISPLAY START
            // =================================================

            case Step.DisplayStart:

                // START를 SOUND로 이동
                MoveStartButton(
                    settingsBridge.SoundStartSlot
                );


                currentStep =
                    Step.WaitSoundTab;


                // SOUND 탭 민트 반짝임
                if (settingsBridge.SoundTabButton != null)
                {
                    StartBlink(
                        settingsBridge.SoundTabButton.gameObject
                    );
                }


                SetStatus(
                    "SOUND 탭을 확인하세요."
                );

                break;


            // =================================================
            // SOUND START
            // =================================================

            case Step.SoundStart:

                // START를 INFO로 이동
                MoveStartButton(
                    settingsBridge.InfoStartSlot
                );


                currentStep =
                    Step.WaitInfoTab;


                // INFO 탭 민트 반짝임
                if (settingsBridge.InfoTabButton != null)
                {
                    StartBlink(
                        settingsBridge.InfoTabButton.gameObject
                    );
                }


                SetStatus(
                    "INFO 탭을 확인하세요."
                );

                break;


            // =================================================
            // INFO START
            // =================================================

            case Step.InfoStart:

                // START를 EXIT로 이동
                MoveStartButton(
                    settingsBridge.ExitStartSlot
                );


                currentStep =
                    Step.WaitExitTab;


                // EXIT 탭 민트 반짝임
                if (settingsBridge.ExitTabButton != null)
                {
                    StartBlink(
                        settingsBridge.ExitTabButton.gameObject
                    );
                }


                SetStatus(
                    "EXIT 탭을 확인하세요."
                );

                break;


            // =================================================
            // EXIT START
            // =================================================

            case Step.ExitStart:

                // START를 원래 미니게임 화면으로 미리 이동
                MoveStartButton(
                    initialStartSlot
                );


                // 아직 설정창 안이므로 START 숨김
                startButton.gameObject.SetActive(false);


                currentStep =
                    Step.WaitBack;


                // 이제 BACK 버튼 사용 가능
                if (settingsBridge.BackButton != null)
                {
                    settingsBridge.BackButton.interactable = true;


                    // BACK 버튼 민트 반짝임
                    StartBlink(
                        settingsBridge.BackButton.gameObject
                    );
                }


                SetStatus(
                    "BACK 버튼을 확인하세요."
                );

                break;


            // =================================================
            // 마지막 START
            // =================================================

            case Step.FinalStart:

                FinishMinigame();

                break;
        }
    }


    // =========================================================
    // SETTINGS BUTTON
    // =========================================================

    private void OnSettingsButtonClicked()
    {
        if (currentStep != Step.WaitSettingsButton)
            return;


        StopBlink();


        // 설정창 열기 + DISPLAY 페이지
        settingsBridge.OpenDisplay();


        currentStep =
            Step.DisplayStart;


        SetStatus(
            "DISPLAY에서 START를 찾았습니다."
        );
    }


    // =========================================================
    // SOUND TAB
    // =========================================================

    private void OnSoundTabClicked()
    {
        if (currentStep != Step.WaitSoundTab)
            return;


        StopBlink();


        currentStep =
            Step.SoundStart;


        SetStatus(
            "SOUND에서 START를 찾았습니다."
        );
    }


    // =========================================================
    // INFO TAB
    // =========================================================

    private void OnInfoTabClicked()
    {
        if (currentStep != Step.WaitInfoTab)
            return;


        StopBlink();


        currentStep =
            Step.InfoStart;


        SetStatus(
            "INFO에서 START를 찾았습니다."
        );
    }


    // =========================================================
    // EXIT TAB
    // =========================================================

    private void OnExitTabClicked()
    {
        if (currentStep != Step.WaitExitTab)
            return;


        StopBlink();


        currentStep =
            Step.ExitStart;


        SetStatus(
            "EXIT에서 START를 찾았습니다."
        );
    }


    // =========================================================
    // BACK
    // =========================================================

    private void OnBackButtonClicked()
    {
        if (currentStep != Step.WaitBack)
            return;


        StopBlink();


        // 설정창 닫기
        settingsBridge.CloseSettings();


        currentStep =
            Step.FinalStart;


        // =====================================================
        // 마지막 화면에서 필요 없는 UI 숨기기
        // =====================================================

        if (hideOnFinal != null)
        {
            foreach (GameObject obj in hideOnFinal)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }


        // 톱니바퀴 숨기기
        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(false);
        }


        // START 원래 자리
        MoveStartButton(
            initialStartSlot
        );


        // START 다시 보이게
        startButton.gameObject.SetActive(true);


        // 상태 문구 제거
        SetStatus("");
    }


    // =========================================================
    // START 이동
    // =========================================================

    private void MoveStartButton(
        RectTransform targetSlot
    )
    {
        if (startButton == null ||
            targetSlot == null)
            return;


        RectTransform startRect =
            startButton.GetComponent<RectTransform>();


        if (startRect == null)
            return;


        // 부모 이동
        startRect.SetParent(
            targetSlot,
            false
        );


        // 중앙 기준
        startRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        startRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        startRect.pivot =
            new Vector2(0.5f, 0.5f);


        // Slot 중앙
        startRect.anchoredPosition =
            Vector2.zero;


        // 크기 정상화
        startRect.localScale =
            Vector3.one;
    }


    // =========================================================
    // 민트색 BLINK 시작
    // =========================================================

    private void StartBlink(
        GameObject target
    )
    {
        // 이전 반짝임 정리
        StopBlink();


        if (target == null)
            return;


        // =====================================================
        // 기존 Hover 스크립트가
        // 색을 다시 흰색으로 바꾸는 것 방지
        // =====================================================

        blinkingHoverColor =
            target.GetComponent<ButtonHoverColor>();


        if (blinkingHoverColor != null)
        {
            blinkingHoverWasEnabled =
                blinkingHoverColor.enabled;


            blinkingHoverColor.enabled =
                false;
        }


        // =====================================================
        // Button Color Tint가 색상 덮어쓰는 것 방지
        // =====================================================

        blinkingButton =
            target.GetComponent<Button>();


        if (blinkingButton != null)
        {
            originalTransition =
                blinkingButton.transition;


            blinkingButton.transition =
                Selectable.Transition.None;
        }


        // =====================================================
        // CanvasGroup
        // =====================================================

        blinkingCanvasGroup =
            target.GetComponent<CanvasGroup>();


        if (blinkingCanvasGroup == null)
        {
            blinkingCanvasGroup =
                target.AddComponent<CanvasGroup>();
        }


        originalBlinkAlpha =
            blinkingCanvasGroup.alpha;


        // =====================================================
        // 버튼 안에 있는
        // Image / TMP Text 전부 가져오기
        // =====================================================

        blinkingGraphics =
            target.GetComponentsInChildren<Graphic>(
                true
            );


        originalColors =
            new Color[
                blinkingGraphics.Length
            ];


        for (int i = 0;
             i < blinkingGraphics.Length;
             i++)
        {
            originalColors[i] =
                blinkingGraphics[i].color;
        }


        // =====================================================
        // Coroutine 시작
        // =====================================================

        blinkCoroutine =
            StartCoroutine(
                BlinkRoutine()
            );
    }


    // =========================================================
    // 민트색 BLINK 반복
    // =========================================================

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // 0 ~ 1 반복
            float wave =
                (
                    Mathf.Sin(
                        Time.unscaledTime
                        * blinkSpeed
                    )
                    + 1f
                )
                * 0.5f;


            // =================================================
            // 투명도도 살짝 반짝임
            // =================================================

            if (blinkingCanvasGroup != null)
            {
                blinkingCanvasGroup.alpha =
                    Mathf.Lerp(
                        blinkMinAlpha,
                        1f,
                        wave
                    );
            }


            // =================================================
            // 원래 색 ↔ 민트
            // =================================================

            if (blinkingGraphics != null &&
                originalColors != null)
            {
                for (
                    int i = 0;
                    i < blinkingGraphics.Length;
                    i++
                )
                {
                    if (blinkingGraphics[i] == null)
                        continue;


                    Color newColor =
                        Color.Lerp(
                            originalColors[i],
                            blinkColor,
                            wave
                        );


                    // Graphic 자체의 기존 Alpha 유지
                    newColor.a =
                        originalColors[i].a;


                    blinkingGraphics[i].color =
                        newColor;
                }
            }


            yield return null;
        }
    }


    // =========================================================
    // BLINK 종료
    // =========================================================

    private void StopBlink()
    {
        // =====================================================
        // Coroutine 종료
        // =====================================================

        if (blinkCoroutine != null)
        {
            StopCoroutine(
                blinkCoroutine
            );

            blinkCoroutine = null;
        }


        // =====================================================
        // Alpha 원래대로
        // =====================================================

        if (blinkingCanvasGroup != null)
        {
            blinkingCanvasGroup.alpha =
                originalBlinkAlpha;
        }


        // =====================================================
        // 색상 원래대로
        // =====================================================

        if (blinkingGraphics != null &&
            originalColors != null)
        {
            for (
                int i = 0;
                i < blinkingGraphics.Length;
                i++
            )
            {
                if (blinkingGraphics[i] != null)
                {
                    blinkingGraphics[i].color =
                        originalColors[i];
                }
            }
        }


        // =====================================================
        // Button Transition 복구
        // =====================================================

        if (blinkingButton != null)
        {
            blinkingButton.transition =
                originalTransition;
        }


        // =====================================================
        // Hover Script 복구
        // =====================================================

        if (blinkingHoverColor != null)
        {
            blinkingHoverColor.enabled =
                blinkingHoverWasEnabled;
        }


        blinkingCanvasGroup = null;

        blinkingGraphics = null;

        originalColors = null;

        blinkingButton = null;

        blinkingHoverColor = null;
    }


    // =========================================================
    // STATUS
    // =========================================================

    private void SetStatus(
        string message
    )
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }
    }


    // =========================================================
    // FINISH
    // =========================================================

    private void FinishMinigame()
    {
        currentStep =
            Step.Finished;


        StopBlink();


        // 혹시 설정창이 남아있으면 닫기
        if (settingsBridge != null)
        {
            settingsBridge.CloseSettings();


            if (settingsBridge.BackButton != null)
            {
                settingsBridge.BackButton.interactable =
                    true;
            }
        }


        Time.timeScale = 1f;


        // MinigameBase.Success()
        // → ChapterManager가 다음 진행
        Success();
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        StopBlink();


        if (settingsBridge == null)
            return;


        // =====================================================
        // SOUND
        // =====================================================

        if (settingsBridge.SoundTabButton != null)
        {
            settingsBridge.SoundTabButton.onClick.RemoveListener(
                OnSoundTabClicked
            );
        }


        // =====================================================
        // INFO
        // =====================================================

        if (settingsBridge.InfoTabButton != null)
        {
            settingsBridge.InfoTabButton.onClick.RemoveListener(
                OnInfoTabClicked
            );
        }


        // =====================================================
        // EXIT
        // =====================================================

        if (settingsBridge.ExitTabButton != null)
        {
            settingsBridge.ExitTabButton.onClick.RemoveListener(
                OnExitTabClicked
            );
        }


        // =====================================================
        // BACK
        // =====================================================

        if (settingsBridge.BackButton != null)
        {
            settingsBridge.BackButton.onClick.RemoveListener(
                OnBackButtonClicked
            );


            settingsBridge.BackButton.interactable =
                true;
        }


        Time.timeScale = 1f;
    }


    // =========================================================
    // HINT
    // =========================================================

    protected override void GiveHint()
    {
        switch (currentStep)
        {
            case Step.WaitSettingsButton:

                SetStatus(
                    "HINT : 설정 버튼을 확인하세요."
                );

                break;


            case Step.WaitSoundTab:

                SetStatus(
                    "HINT : SOUND 탭을 확인하세요."
                );

                break;


            case Step.WaitInfoTab:

                SetStatus(
                    "HINT : INFO 탭을 확인하세요."
                );

                break;


            case Step.WaitExitTab:

                SetStatus(
                    "HINT : EXIT 탭을 확인하세요."
                );

                break;


            case Step.WaitBack:

                SetStatus(
                    "HINT : BACK 버튼을 확인하세요."
                );

                break;
        }
    }


    // =========================================================
    // RESTART
    // =========================================================

    protected override void RestartGame()
    {
        StopBlink();


        if (settingsBridge != null)
        {
            settingsBridge.CloseSettings();


            if (settingsBridge.BackButton != null)
            {
                settingsBridge.BackButton.interactable =
                    false;
            }
        }


        MoveStartButton(
            initialStartSlot
        );


        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
        }


        if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(true);
        }


        currentStep =
            Step.InitialStart;
    }
}