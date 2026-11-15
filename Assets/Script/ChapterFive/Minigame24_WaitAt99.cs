
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

    [Header("99% 이후: 내려가는 퍼센트를 연타로 100%까지 올리기")]
    [Tooltip("99%에서 고장 나서 처음에 이만큼까지 뚝 떨어짐")]
    public float dropTo = 85f;
    [Tooltip("1초에 내려가는 퍼센트")]
    public float drainPerSecond = 5f;
    [Tooltip("버튼 한 번 누를 때 올라가는 퍼센트")]
    public float pushPerClick = 2f;
    [Tooltip("이 퍼센트까지 내려가면 실패 (틀린 횟수 +1, 다시 시도)")]
    public float failPercent = 50f;

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
        gameName = "99%에서 멈춘 로딩";

        instruction =
            "내려가는 로딩을 버튼 연타로 100%까지 올리십시오.";

        // 제한 시간은 프리팹 Inspector의 Time Limit 값 사용 (챕터5 도구가 60초/90초로 맞춤)

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
                Mathf.FloorToInt(currentPercent) + "%";
        }
    }

    // =========================================
    // 99% 정지
    // =========================================

    private void Enter99Percent()
    {
        currentState = LoadingState.WaitingAt99;

        SetLoadingPercent(99f);

        Debug.Log("MG24: 99%에서 고장 → 퍼센트가 내려가기 시작");

        // 버튼은 보이지만 떨어지는 동안은 아직 못 누름
        completeButton.gameObject.SetActive(true);
        completeButton.interactable = false;
        completeButton.transform.localScale = Vector3.one;

        realStartButton.gameObject.SetActive(false);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (fakeInstructionText != null)
        {
            fakeInstructionText.text =
                "로딩이 거꾸로 내려갑니다! 버튼을 연타해서 100%로 올리세요!";
            fakeInstructionText.gameObject.SetActive(true);
        }

        if (smallHintText != null)
        {
            smallHintText.gameObject.SetActive(true);
            UpdateHintText();
        }

        StartCoroutine(DrainRoutine());
    }

    // 99% → 뚝 떨어진 뒤 계속 조금씩 내려감 (버튼 연타로 막기)
    private IEnumerator DrainRoutine()
    {
        // 1) 고장: 99% → dropTo 까지 빠르게 떨어짐
        float start = currentPercent;
        float t = 0f;
        const float dropTime = 0.7f;

        while (t < dropTime)
        {
            if (currentState != LoadingState.WaitingAt99)
                yield break;

            t += Time.unscaledDeltaTime;
            SetLoadingPercent(Mathf.Lerp(start, dropTo, Mathf.Clamp01(t / dropTime)));
            yield return null;
        }

        // 2) 이제 버튼을 누를 수 있음, 퍼센트는 계속 내려감
        completeButton.interactable = true;

        while (currentState == LoadingState.WaitingAt99)
        {
            SetLoadingPercent(currentPercent - drainPerSecond * Time.unscaledDeltaTime);

            if (currentPercent <= failPercent)
            {
                DrainFailed();
                yield break;
            }

            yield return null;
        }
    }

    // 너무 많이 내려감 = 실패 (틀린 횟수 +1) → 99%부터 다시
    private void DrainFailed()
    {
        currentState = LoadingState.Error;
        failureCount++;

        // 챕터5: 틀린 횟수 +1
        ReportMistake();

        Debug.Log("MG24: 퍼센트가 너무 내려감! 실패 " + failureCount + "회");

        completeButton.gameObject.SetActive(false);

        if (fakeInstructionText != null)
            fakeInstructionText.gameObject.SetActive(false);

        if (smallHintText != null)
            smallHintText.gameObject.SetActive(false);

        if (errorText != null)
        {
            errorText.text =
                "ERROR\n" +
                "LOADING DROPPED\n" +
                "로딩이 너무 많이 내려갔습니다. 다시 시도합니다.";

            errorText.gameObject.SetActive(true);
        }

        StartCoroutine(RetryFrom99Routine());
    }

    private IEnumerator RetryFrom99Routine()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, errorDisplayTime)
        );

        if (!isGameActive)
            yield break;

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        // 처음(0%)부터가 아니라 99%부터 다시
        Enter99Percent();
    }

    // 버튼 눌린 느낌 (살짝 작아졌다 돌아옴)
    private IEnumerator PunchButton()
    {
        Transform tr = completeButton.transform;
        tr.localScale = Vector3.one * 0.92f;

        float t = 0f;
        while (t < 0.08f)
        {
            t += Time.unscaledDeltaTime;
            tr.localScale = Vector3.Lerp(Vector3.one * 0.92f, Vector3.one, t / 0.08f);
            yield return null;
        }

        tr.localScale = Vector3.one;
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
            "MG24: START 등장"
        );
    }

    // =========================================
    // COMPLETE 클릭 = 실패
    // =========================================

    private void OnCompleteClicked()
    {
        if (!isGameActive)
            return;

        if (currentState != LoadingState.WaitingAt99 ||
            !completeButton.interactable)
            return;

        // 연타할 때마다 퍼센트가 올라감
        SetLoadingPercent(currentPercent + pushPerClick);
        StartCoroutine(PunchButton());

        if (currentPercent >= 100f)
        {
            SetLoadingPercent(100f);

            if (loadingText != null)
                loadingText.text = "LOADING...";

            Debug.Log("MG24: 100% 달성! START 등장");

            ShowRealStartButton();
        }
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

        if (failureCount >= 2)
        {
            // 두 번 이상 실패하면 더 분명한 힌트
            smallHintText.text =
                "※ 쉬지 말고 버튼을 아주 빠르게 연타하세요!";

            color.a = 1f;
        }
        else
        {
            smallHintText.text =
                "※ 손을 멈추면 퍼센트가 계속 내려갑니다.";

            color.a = 0.8f;
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
