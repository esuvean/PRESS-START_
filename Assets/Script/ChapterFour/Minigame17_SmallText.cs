
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Minigame17_SmallText : MinigameBase
{
    [Header("UI Text")]
    public TMP_Text titleText;
    public TMP_Text roundText;
    public TMP_Text bigInstructionText;
    public TMP_Text smallRuleText;
    public TMP_Text feedbackText;

    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;

    public TMP_Text leftButtonText;
    public TMP_Text rightButtonText;

    [Header("Settings")]
    public float feedbackDelay = 1f;

    private int currentRound = 0;
    private bool isProcessing = false;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "복합 지시 해석 검사";
        instruction = "큰 글씨와 작은 규칙을 함께 읽으세요.";

        base.StartMinigame();

        StopAllCoroutines();

        currentRound = 0;
        isProcessing = false;

        if (titleText != null)
        {
            titleText.text = gameName;
        }

        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnLeftButton);
            leftButton.onClick.AddListener(OnLeftButton);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnRightButton);
            rightButton.onClick.AddListener(OnRightButton);
        }

        ShowRound();
    }

    private void ShowRound()
    {
        if (!isGameActive)
            return;

        isProcessing = false;

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        if (roundText != null)
        {
            roundText.text =
                $"ROUND {currentRound + 1} / 3";
        }

        if (leftButton != null)
        {
            leftButton.gameObject.SetActive(true);
            leftButton.interactable = true;
        }

        if (rightButton != null)
        {
            rightButton.gameObject.SetActive(true);
            rightButton.interactable = true;
        }

        switch (currentRound)
        {
            case 0:
                SetRoundText(
                    "클릭하세요.",
                    "위 지시와 반대로 행동하세요."
                );

                SetButtonText("클릭", "기다리기");
                break;

            case 1:
                SetRoundText(
                    "왼쪽 버튼을 선택하세요.",
                    "방향을 반대로 해석하세요."
                );

                SetButtonText("왼쪽", "오른쪽");
                break;

            case 2:
                SetRoundText(
                    "시작 버튼을 두 번 누르세요.",
                    "표시된 횟수보다 한 번 적게 누르세요."
                );

                SetButtonText("START", "");

                if (rightButton != null)
                {
                    rightButton.gameObject.SetActive(false);
                }

                break;
        }
    }

    private void SetRoundText(
        string bigText,
        string smallText)
    {
        if (bigInstructionText != null)
        {
            bigInstructionText.text = bigText;

            FixBigInstructionText();
        }

        if (smallRuleText != null)
        {
            smallRuleText.text = smallText;
        }
    }

    private void FixBigInstructionText()
    {
        if (bigInstructionText == null)
            return;

        RectTransform rect =
            bigInstructionText.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 position = rect.anchoredPosition;
            position.x = 0f;

            rect.anchoredPosition = position;

            rect.localRotation = Quaternion.identity;
        }

        bigInstructionText.alignment =
            TextAlignmentOptions.Center;

        bigInstructionText.textWrappingMode =
            TextWrappingModes.NoWrap;

        bigInstructionText.ForceMeshUpdate();
    }

    private void SetButtonText(
        string leftText,
        string rightText)
    {
        if (leftButtonText != null)
        {
            leftButtonText.text = leftText;
        }

        if (rightButtonText != null)
        {
            rightButtonText.text = rightText;
        }
    }

    private void OnLeftButton()
    {
        CheckAnswer(0);
    }

    private void OnRightButton()
    {
        CheckAnswer(1);
    }

    private void CheckAnswer(int selectedButton)
    {
        if (!isGameActive || isProcessing)
            return;

        bool isCorrect = false;

        switch (currentRound)
        {
            case 0:
                isCorrect = selectedButton == 1;
                break;

            case 1:
                isCorrect = selectedButton == 1;
                break;

            case 2:
                isCorrect = selectedButton == 0;
                break;
        }

        isProcessing = true;

        if (leftButton != null)
        {
            leftButton.interactable = false;
        }

        if (rightButton != null)
        {
            rightButton.interactable = false;
        }

        if (isCorrect)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "정답입니다!";
            }

            StartCoroutine(CorrectAnswerRoutine());
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text =
                    "틀렸습니다! 작은 규칙을 다시 읽어보세요.";
            }

            StartCoroutine(WrongAnswerRoutine());
        }
    }

    private IEnumerator CorrectAnswerRoutine()
    {
        yield return new WaitForSeconds(feedbackDelay);

        if (!isGameActive)
            yield break;

        currentRound++;

        if (currentRound >= 3)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "검사 완료!";
            }

            Success();
        }
        else
        {
            ShowRound();
        }
    }

    private IEnumerator WrongAnswerRoutine()
    {
        yield return new WaitForSeconds(feedbackDelay);

        if (!isGameActive)
            yield break;

        ShowRound();
    }

    protected override void GiveHint()
    {
        if (feedbackText != null)
        {
            feedbackText.text =
                "힌트: 작은 글씨의 규칙을 먼저 확인하세요.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(OnLeftButton);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(OnRightButton);
        }
    }
}
