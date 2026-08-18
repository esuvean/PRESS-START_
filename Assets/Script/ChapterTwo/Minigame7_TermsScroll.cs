using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame7_TermsScroll : MinigameBase
{
    [Header("UI")]
    public ScrollRect termsScroll;
    public Button agreeButton;
    public TextMeshProUGUI statusText;

    [Header("Window")]
    public GameObject termsWindow;

    [Header("Hidden Start Button")]
    public Button hiddenStartButton;

    [Header("Settings")]
    [Range(0f, 0.1f)]
    public float bottomThreshold = 0.02f;

    private bool escapedOnce = false;
    private bool started = false;

    private void Start()
    {
        if (!started)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        if (started)
            return;

        started = true;

        base.StartMinigame();

        gameName = "실행 문서 확인 검사";
        instruction =
            "게임 실행 약관의 마지막까지 이동하여 시작 버튼을 누른다.";

        escapedOnce = false;

        // 약관 창은 처음에 보임
        if (termsWindow != null)
        {
            termsWindow.SetActive(true);
        }

        // 진짜 시작하기 버튼은 처음에 숨김
        if (hiddenStartButton != null)
        {
            hiddenStartButton.gameObject.SetActive(false);

            hiddenStartButton.onClick.RemoveListener(
                OnStartButtonClicked
            );

            hiddenStartButton.onClick.AddListener(
                OnStartButtonClicked
            );
        }

        // 약관 맨 위로
        if (termsScroll != null)
        {
            Canvas.ForceUpdateCanvases();

            termsScroll.verticalNormalizedPosition = 1f;
            termsScroll.scrollSensitivity = 40f;
        }

        if (statusText != null)
        {
            statusText.text =
                "약관을 끝까지 확인하세요.";
        }
    }

    // ===============================
    // 현재 약관 맨 아래인지 확인
    // ===============================

    private bool IsAtBottom()
    {
        if (termsScroll == null)
            return false;

        return termsScroll.verticalNormalizedPosition
               <= bottomThreshold;
    }

    // ===============================
    // 동의 버튼에 마우스 올렸을 때
    // 첫 번째는 맨 위로 도망
    // ===============================

    public void OnAgreePointerEnter()
    {
        if (!isGameActive)
            return;

        if (!IsAtBottom())
            return;

        // 첫 번째 접근
        if (!escapedOnce)
        {
            escapedOnce = true;

            Canvas.ForceUpdateCanvases();

            termsScroll.verticalNormalizedPosition = 1f;

            // 두 번째 스크롤은 더 빠르게
            termsScroll.scrollSensitivity = 80f;

            if (statusText != null)
            {
                statusText.text =
                    "제대로 약관을 읽으셨습니까?";
            }
        }
    }

    // ===============================
    // 약관 동의 버튼
    // ===============================

    public void OnAgreeClicked()
    {
        if (!isGameActive)
            return;

        // 첫 번째 장난을 아직 안 봤으면 동의 불가
        if (!escapedOnce)
        {
            if (statusText != null)
                statusText.text = "약관을 끝까지 확인하세요.";

            return;
        }

        // 다시 맨 아래까지 안 내려왔으면 동의 불가
        if (!IsAtBottom())
        {
            if (statusText != null)
                statusText.text = "다시 약관 마지막까지 이동하세요.";

            return;
        }

        // =========================
        // 모든 약관 동의 완료
        // =========================

        if (statusText != null)
            statusText.text = "모든 약관에 동의했습니다.";

        // 약관 창 끄기
        if (termsWindow != null)
            termsWindow.SetActive(false);

        // 뒤에 있던 START 버튼 켜기
        if (hiddenStartButton != null)
            hiddenStartButton.gameObject.SetActive(true);
    }

    // ===============================
    // 진짜 시작하기 버튼
    // ===============================

    private void OnStartButtonClicked()
    {
        if (!isGameActive)
            return;

        if (statusText != null)
            statusText.text = "실행 문서 확인 완료";

        Success();
    }

    // ===============================
    // 힌트
    // ===============================

    protected override void GiveHint()
    {
        if (statusText != null)
        {
            statusText.text =
                "힌트: 스크롤바를 끝까지 내려보세요.";
        }
    }

    // ===============================
    // 재시작
    // ===============================

    protected override void RestartGame()
    {
        escapedOnce = false;

        if (termsWindow != null)
        {
            termsWindow.SetActive(true);
        }

        if (hiddenStartButton != null)
        {
            hiddenStartButton.gameObject.SetActive(false);
        }

        if (termsScroll != null)
        {
            Canvas.ForceUpdateCanvases();

            termsScroll.verticalNormalizedPosition = 1f;
            termsScroll.scrollSensitivity = 40f;
        }

        if (statusText != null)
        {
            statusText.text =
                "약관을 끝까지 확인하세요.";
        }
    }
}