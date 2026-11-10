
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame24_WaitAt99 : MinigameBase
{
    [Header("Loading UI")]
    public Image loadingBarFill;
    public TMP_Text loadingText;
    public TMP_Text percentText;

    [Header("Buttons")]
    public Button completeButton;
    public Button realStartButton;

    [Header("Message UI")]
    public TMP_Text fakeInstructionText;
    public TMP_Text smallHintText;
    public TMP_Text errorText;

    [Header("Loading Speed")]
    public float firstLoadingTime = 1.2f;
    public float secondLoadingTime = 1.2f;
    public float thirdLoadingTime = 1.4f;

    public float loadingPause = 0.3f;

    [Header("99 Percent Waiting")]
    public float waitAt99Time = 5f;

    [Header("Effects")]
    public float errorDisplayTime = 1.3f;
    public float successDisplayTime = 0.6f;

    private enum LoadingState
    {
        Loading,
        WaitingAt99,
        StartReady,
        Error,
        Finishing,
        Completed
    }

    private LoadingState currentState;

    private float currentPercent;
    private int failureCount;

    private bool referencesReady;
    private bool hintColorSaved;

    private Color originalHintColor;

    // =========================================
    // START
    // =========================================

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "99%에서 기다리십시오";

        instruction =
            "로딩이 완료될 때까지 기다리십시오.";

        // MG24에는 시간 제한이 없음
        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        referencesReady = FindReferences();

        if (!referencesReady)
        {
            Debug.LogError(
                "MG24: 필수 UI 연결이 누락되었습니다.",
                this
            );

            isGameActive = false;
            return;
        }

        SetupButtons();

        failureCount = 0;

        ResetLoadingUI();

        BeginLoading();

        Debug.Log("MG24: 로딩 검사 시작");
    }

    // =========================================
    // UI 자동 연결
    // =========================================

    private Transform FindDeep(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        if (parent.name == objectName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result =
                FindDeep(child, objectName);

            if (result != null)
                return result;
        }

        return null;
    }

    private T FindUI<T>(string objectName)
        where T : Component
    {
        Transform found =
            FindDeep(transform, objectName);

        if (found == null)
            return null;

        return found.GetComponent<T>();
    }

    private bool FindReferences()
    {
        if (loadingBarFill == null)
        {
            loadingBarFill =
                FindUI<Image>("LoadingBarFill");
        }

        if (loadingText == null)
        {
            loadingText =
                FindUI<TMP_Text>("LoadingText");
        }

        if (percentText == null)
        {
            percentText =
                FindUI<TMP_Text>("PercentText");
        }

        if (completeButton == null)
        {
            completeButton =
                FindUI<Button>("CompleteButton");
        }

        if (realStartButton == null)
        {
            realStartButton =
                FindUI<Button>("RealStartButton");
        }

        if (fakeInstructionText == null)
        {
            fakeInstructionText =
                FindUI<TMP_Text>("FakeInstructionText");
        }

        if (smallHintText == null)
        {
            smallHintText =
                FindUI<TMP_Text>("SmallHintText");
        }

        if (errorText == null)
        {
            errorText =
                FindUI<TMP_Text>("ErrorText");
        }

        // 필수 UI 검사
        bool valid = true;

        if (loadingBarFill == null)
        {
            Debug.LogError(
                "MG24: LoadingBarFill 없음",
                this
            );

            valid = false;
        }

        if (percentText == null)
        {
            Debug.LogError(
                "MG24: PercentText 없음",
                this
            );

            valid = false;
        }

        if (completeButton == null)
        {
            Debug.LogError(
                "MG24: CompleteButton 없음",
                this
            );

            valid = false;
        }

        if (realStartButton == null)
        {
            Debug.LogError(
                "MG24: RealStartButton 없음",
                this
            );

            valid = false;
        }

        if (loadingBarFill != null)
        {
            // Inspector에서 설정이 빠졌더라도
            // 왼쪽부터 채워지게 자동 설정
            loadingBarFill.type =
                Image.Type.Filled;

            loadingBarFill.fillMethod =
                Image.FillMethod.Horizontal;

            loadingBarFill.fillOrigin = 0;

            if (loadingBarFill.sprite == null)
            {
                Debug.LogWarning(
                    "MG24: LoadingBarFill의 " +
                    "Source Image에 UI Sprite를 넣어주세요.",
                    this
                );
            }
        }

        if (smallHintText != null &&
            !hintColorSaved)
        {
            originalHintColor =
                smallHintText.color;

            hintColorSaved = true;
        }

        return valid;
    }

    // =========================================
    // 버튼 이벤트 연결
    // =========================================

    private void SetupButtons()
    {
        completeButton.onClick.RemoveListener(
            OnCompleteClicked
        );

        completeButton.onClick.AddListener(
            OnCompleteClicked
        );

        realStartButton.onClick.RemoveListener(
            OnRealStartClicked
        );

        realStartButton.onClick.AddListener(
            OnRealStartClicked
        );

        completeButton.interactable = true;
        realStartButton.interactable = true;
    }

    // =========================================
    // 처음 상태로 UI 설정
    // =========================================

    private void ResetLoadingUI()
    {
        currentPercent = 0f;

        SetLoadingPercent(0f);

        if (loadingText != null)
        {
            loadingText.text = "LOADING...";
        }

        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(false);
            completeButton.interactable = true;
        }

        if (realStartButton != null)
        {
            realStartButton.gameObject.SetActive(false);
            realStartButton.interactable = true;
        }

        if (fakeInstructionText != null)
        {
            fakeInstructionText.text =
                "마지막 1%를 직접 완료하십시오.";

            fakeInstructionText.gameObject.SetActive(
                false
            );
        }

        if (smallHintText != null)
        {
            smallHintText.gameObject.SetActive(false);

            UpdateHintText();
        }

        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    // =========================================
    // 로딩 시작
    // =========================================

    private void BeginLoading()
    {
        currentState = LoadingState.Loading;

        ResetLoadingUI();

        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        Debug.Log("MG24: 로딩 0% 시작");

        // 0% -> 35%
        yield return AnimateLoading(
            35f,
            firstLoadingTime
        );

        yield return new WaitForSecondsRealtime(
            loadingPause
        );

        // 35% -> 72%
        yield return AnimateLoading(
            72f,
            secondLoadingTime
        );

        yield return new WaitForSecondsRealtime(
            loadingPause
        );

        // 72% -> 99%
        yield return AnimateLoading(
            99f,
            thirdLoadingTime
        );

        if (currentState != LoadingState.Loading)
        {
            yield break;
        }

        SetLoadingPercent(99f);

        Enter99Percent();
    }

    // =========================================
    // 로딩바 애니메이션
    // =========================================

    private IEnumerator AnimateLoading(
        float targetPercent,
        float duration)
    {
        float startPercent = currentPercent;

        duration = Mathf.Max(0.05f, duration);

        float timer = 0f;

        while (timer < duration)
        {
            if (currentState != LoadingState.Loading)
            {
                yield break;
            }

            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / duration
            );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            float percent = Mathf.Lerp(
                startPercent,
                targetPercent,
                t
            );

            SetLoadingPercent(percent);

            yield return null;
        }

        SetLoadingPercent(targetPercent);
    }

    // =========================================
    // 실제 로딩 퍼센트 적용
    // =========================================

    private void SetLoadingPercent(float percent)
    {
        currentPercent = Mathf.Clamp(
            percent,
            0f,
            100f
        );

        if (loadingBarFill != null)
        {
            loadingBarFill.fillAmount =
                currentPercent / 100f;
        }

        if (percentText != null)
        {
            percentText.text =
                Mathf.RoundToInt(currentPercent) + "%";
        }
    }

    // =========================================
    // 99% 정지
    // =========================================

    private void Enter99Percent()
    {
        currentState = LoadingState.WaitingAt99;

        SetLoadingPercent(99f);

        Debug.Log("MG24: 로딩 99%에서 정지");

        // 함정 버튼 등장
        completeButton.gameObject.SetActive(true);
        completeButton.interactable = true;

        realStartButton.gameObject.SetActive(false);

        if (fakeInstructionText != null)
        {
            fakeInstructionText.gameObject.SetActive(true);
        }

        if (smallHintText != null)
        {
            smallHintText.gameObject.SetActive(true);

            UpdateHintText();
        }

        // 실패 2~3회라면 안내문이 잠시 밝아짐
        if (failureCount >= 2 &&
            failureCount < 4)
        {
            StartCoroutine(HintFlashRoutine());
        }

        // 5초 동안 기다리기
        StartCoroutine(WaitAt99Routine());
    }

    // =========================================
    // 아무것도 누르지 않고 기다리기
    // =========================================

    private IEnumerator WaitAt99Routine()
    {
        float waited = 0f;

        while (waited < waitAt99Time)
        {
            if (currentState !=
                LoadingState.WaitingAt99)
            {
                yield break;
            }

            waited += Time.unscaledDeltaTime;

            yield return null;
        }

        if (currentState != LoadingState.WaitingAt99)
        {
            yield break;
        }

        ShowRealStartButton();
    }

    // =========================================
    // 진짜 START 등장
    // =========================================

    private void ShowRealStartButton()
    {
        currentState = LoadingState.StartReady;

        // 함정 버튼 숨기기
        completeButton.gameObject.SetActive(false);

        // 진짜 START 버튼 등장
        realStartButton.gameObject.SetActive(true);
        realStartButton.interactable = true;

        if (fakeInstructionText != null)
        {
            fakeInstructionText.gameObject.SetActive(false);
        }

        Debug.Log(
            "MG24: 5초 대기 성공! START 등장"
        );
    }

    // =========================================
    // COMPLETE 클릭 = 실패
    // =========================================

    private void OnCompleteClicked()
    {
        if (!isGameActive)
            return;

        if (currentState != LoadingState.WaitingAt99)
            return;

        currentState = LoadingState.Error;

        failureCount++;

        Debug.Log(
            "MG24: COMPLETE 클릭! 실패 " +
            failureCount + "회"
        );

        // 기다림 / 힌트 애니메이션 중단
        StopAllCoroutines();

        completeButton.gameObject.SetActive(false);
        realStartButton.gameObject.SetActive(false);

        if (fakeInstructionText != null)
        {
            fakeInstructionText.gameObject.SetActive(false);
        }

        if (smallHintText != null)
        {
            smallHintText.gameObject.SetActive(false);
        }

        // 오류와 동시에 0% 초기화
        SetLoadingPercent(0f);

        if (errorText != null)
        {
            errorText.text =
                "ERROR\n" +
                "USER INTERRUPTED PROCESS\n" +
                "로딩이 다시 0%부터 시작합니다.";

            errorText.gameObject.SetActive(true);
        }

        StartCoroutine(ErrorRoutine());
    }

    // =========================================
    // 오류 표시 후 로딩 재시작
    // =========================================

    private IEnumerator ErrorRoutine()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, errorDisplayTime)
        );

        if (!isGameActive)
        {
            yield break;
        }

        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }

        BeginLoading();
    }

    // =========================================
    // 힌트 변경
    // =========================================

    private void UpdateHintText()
    {
        if (smallHintText == null)
            return;

        Color color = hintColorSaved
            ? originalHintColor
            : Color.white;

        if (failureCount >= 4)
        {
            // 실패 4회 이상
            smallHintText.text =
                "입력이 항상 필요한 것은 아닙니다.";

            color.a = 1f;

            smallHintText.fontSize = 22f;
        }
        else
        {
            smallHintText.text =
                "※ 입력하지 않고 기다리십시오.";

            color.a = 0.45f;

            smallHintText.fontSize = 17f;
        }

        smallHintText.color = color;
    }

    // =========================================
    // 2회 실패 후 안내문 밝아지는 효과
    // =========================================

    private IEnumerator HintFlashRoutine()
    {
        if (smallHintText == null)
            yield break;

        float duration = 1.5f;
        float timer = 0f;

        Color baseColor = smallHintText.color;

        while (timer < duration)
        {
            if (currentState !=
                LoadingState.WaitingAt99)
            {
                yield break;
            }

            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / duration
            );

            float brightness =
                Mathf.Sin(progress * Mathf.PI);

            Color color = baseColor;

            color.a = Mathf.Lerp(
                0.45f,
                1f,
                brightness
            );

            smallHintText.color = color;

            yield return null;
        }

        smallHintText.color = baseColor;
    }

    // =========================================
    // 진짜 START 클릭
    // =========================================

    private void OnRealStartClicked()
    {
        if (!isGameActive)
            return;

        if (currentState != LoadingState.StartReady)
            return;

        currentState = LoadingState.Finishing;

        realStartButton.interactable = false;

        // 드디어 로딩 100%
        SetLoadingPercent(100f);

        if (loadingText != null)
        {
            loadingText.text = "LOADING 100%";
        }

        if (smallHintText != null)
        {
            smallHintText.text = "검사 완료.";

            Color color = smallHintText.color;
            color.a = 1f;
            smallHintText.color = color;
        }

        Debug.Log("MG24: 로딩 100% 완료");

        StopAllCoroutines();

        StartCoroutine(FinishRoutine());
    }

    // =========================================
    // 클리어 연출 후 Success()
    // =========================================

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, successDisplayTime)
        );

        if (!isGameActive)
        {
            yield break;
        }

        currentState = LoadingState.Completed;

        Debug.Log("MG24: 검사 완료!");

        Success();
    }

    // =========================================
    // MinigameBase
    // =========================================

    protected override void GiveHint()
    {
        // 기획서의 실패 횟수 힌트는
        // UpdateHintText에서 직접 처리
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(
                OnCompleteClicked
            );
        }

        if (realStartButton != null)
        {
            realStartButton.onClick.RemoveListener(
                OnRealStartClicked
            );
        }
    }
}
