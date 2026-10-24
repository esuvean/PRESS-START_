
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DontPress : MinigameBase
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public TMP_Text timerText;
    public TMP_Text temptationText;
    public Button startButton;

    [Header("Timer Bar")]
    [Tooltip("16로딩 이미지 (Image Type = Filled). 남은 시간만큼 왼쪽부터 칸이 남습니다.")]
    public Image timerBarFill;
    [Tooltip("로딩바 칸 수 (16로딩 이미지의 칸 개수와 같게)")]
    public int timerBarSegments = 21;

    [Header("Settings")]
    public float waitTime = 10f;

    private float remainingTime;

    // 연출(DontPressFly)에서 쓰는 값
    public float RemainingWaitTime => remainingTime;
    public event System.Action StartPressed;
    private int lastMessageStage = -1;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "입력 억제 검사";
        instruction = "아무 버튼도 누르지 마세요.";

        base.StartMinigame();

        remainingTime = waitTime;
        lastMessageStage = -1;

        FixTemptationText();

        if (titleText != null)
        {
            titleText.text = gameName;
        }

        if (instructionText != null)
        {
            instructionText.text = instruction;
        }

        if (temptationText != null)
        {
            temptationText.text = "기다려 주세요.";
        }

        if (startButton != null)
        {
            startButton.interactable = true;

            startButton.onClick.RemoveListener(OnStartButtonClicked);
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        UpdateTimerText();
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            UpdateTimerText();

            if (temptationText != null)
            {
                temptationText.text = "검사 완료!";
            }

            if (startButton != null)
            {
                startButton.interactable = false;
            }

            Success();
            return;
        }

        UpdateTimerText();
        UpdateTemptationMessage();
    }


    private void UpdateTimerText()
    {
        UpdateTimerBar();

        if (timerText == null)
            return;

        timerText.text =
            $"남은 시간:{Mathf.CeilToInt(remainingTime)}초";
    }

    // 남은 시간만큼 로딩바 칸을 보여줌 (한 칸씩 사라짐)
    private void UpdateTimerBar()
    {
        if (timerBarFill == null)
            return;

        float ratio = waitTime > 0f
            ? Mathf.Clamp01(remainingTime / waitTime)
            : 0f;

        if (timerBarSegments > 0)
        {
            ratio = Mathf.Ceil(ratio * timerBarSegments) / timerBarSegments;
        }

        timerBarFill.fillAmount = ratio;
    }

    private void UpdateTemptationMessage()
    {
        int stage = 0;

        if (remainingTime <= 2f)
        {
            stage = 4;
        }
        else if (remainingTime <= 4f)
        {
            stage = 3;
        }
        else if (remainingTime <= 6f)
        {
            stage = 2;
        }
        else if (remainingTime <= 8f)
        {
            stage = 1;
        }

        if (stage == lastMessageStage)
            return;

        lastMessageStage = stage;

        if (temptationText == null)
            return;

        switch (stage)
        {
            case 0:
                temptationText.text = "기다려 주세요.";
                break;

            case 1:
                temptationText.text = "한 번 정도는 괜찮습니다.";
                break;

            case 2:
                temptationText.text = "버튼에 그냥 손을 올려 주세요.";
                break;

            case 3:
                temptationText.text = "지금 누르면 보너스가 지급됩니다.";
                break;

            case 4:
                temptationText.text = "정말 안 누르실 건가요?";
                break;
        }
    }

    private void OnStartButtonClicked()
    {
        if (!isGameActive)
            return;

        remainingTime = waitTime;
        lastMessageStage = 0;

        StartPressed?.Invoke();

        if (temptationText != null)
        {
            temptationText.text =
                "처음부터 다시 기다려 주세요.";
        }

        UpdateTimerText();

        Debug.Log("START 버튼 클릭! 타이머 10초 초기화");
    }

    private void FixTemptationText()
    {
        if (temptationText == null)
            return;

        // 위치는 프리팹에서 배치한 그대로 사용 (코드로 옮기지 않음)

        temptationText.alignment =
            TextAlignmentOptions.Center;

        temptationText.textWrappingMode =
            TextWrappingModes.NoWrap;

        temptationText.overflowMode =
            TextOverflowModes.Overflow;

        temptationText.enableAutoSizing = false;
        temptationText.fontSize = 32f;

        temptationText.ForceMeshUpdate();
    }

    protected override void GiveHint()
    {
        if (temptationText != null)
        {
            temptationText.text =
                "버튼을 누르지 않으면 통과할 수 있습니다.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }
}
