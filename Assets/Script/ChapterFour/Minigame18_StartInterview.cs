
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Minigame18_StartInterview : MinigameBase
{
    [Header("UI Text")]
    public TMP_Text titleText;
    public TMP_Text questionNumberText;
    public TMP_Text questionText;
    public TMP_Text responseText;

    [Header("Answer Buttons")]
    public Button answerButton1;
    public Button answerButton2;
    public Button answerButton3;

    [Header("Button Text")]
    public TMP_Text answerButton1Text;
    public TMP_Text answerButton2Text;
    public TMP_Text answerButton3Text;

    [Header("Settings")]
    public float answerDelay = 2f;
    public float wrongAnswerDelay = 2.5f;
    public float successDelay = 1f;

    private int currentQuestion = 0;
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
        StopAllCoroutines();

        gameName = "시작 버튼 면접";
        instruction = "질문에 답하고 현재 상황을 판단하세요.";

        base.StartMinigame();

        currentQuestion = 0;
        isProcessing = false;

        if (titleText != null)
        {
            titleText.text = gameName;
        }

        if (answerButton1 != null)
        {
            answerButton1.onClick.RemoveListener(OnAnswer1);
            answerButton1.onClick.AddListener(OnAnswer1);
        }

        if (answerButton2 != null)
        {
            answerButton2.onClick.RemoveListener(OnAnswer2);
            answerButton2.onClick.AddListener(OnAnswer2);
        }

        if (answerButton3 != null)
        {
            answerButton3.onClick.RemoveListener(OnAnswer3);
            answerButton3.onClick.AddListener(OnAnswer3);
        }

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (!isGameActive)
            return;

        isProcessing = false;

        if (responseText != null)
        {
            responseText.text = "";
        }

        SetButtonsInteractable(true);

        if (questionNumberText != null)
        {
            questionNumberText.text =
                $"QUESTION {currentQuestion + 1} / 3";
        }

        switch (currentQuestion)
        {
            case 0:
                SetQuestion(
                    "왜 게임을 시작하려고 합니까?",
                    "재미있어 보여서",
                    "버튼이 있으니까",
                    "여기까지 왔으니까"
                );
                break;

            case 1:
                SetQuestion(
                    "지금까지 한 일은 무엇이라고 생각합니까?",
                    "검사",
                    "게임",
                    "잘 모르겠음"
                );
                break;

            case 2:
                SetQuestion(
                    "게임은 언제 시작됐다고 생각합니까?",
                    "아직 시작되지 않았다",
                    "첫 번째 버튼을 눌렀을 때",
                    "판단할 수 없다"
                );
                break;
        }
    }

    private void SetQuestion(
        string question,
        string answer1,
        string answer2,
        string answer3)
    {
        if (questionText != null)
        {
            questionText.text = question;
        }

        if (answerButton1Text != null)
        {
            answerButton1Text.text = answer1;
        }

        if (answerButton2Text != null)
        {
            answerButton2Text.text = answer2;
        }

        if (answerButton3Text != null)
        {
            answerButton3Text.text = answer3;
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        if (answerButton1 != null)
            answerButton1.interactable = value;

        if (answerButton2 != null)
            answerButton2.interactable = value;

        if (answerButton3 != null)
            answerButton3.interactable = value;
    }

    private void OnAnswer1()
    {
        SelectAnswer(0);
    }

    private void OnAnswer2()
    {
        SelectAnswer(1);
    }

    private void OnAnswer3()
    {
        SelectAnswer(2);
    }

    private void SelectAnswer(int answerIndex)
    {
        if (!isGameActive || isProcessing)
            return;

        isProcessing = true;

        SetButtonsInteractable(false);

        switch (currentQuestion)
        {
            case 0:
                HandleQuestion1(answerIndex);
                break;

            case 1:
                HandleQuestion2(answerIndex);
                break;

            case 2:
                HandleQuestion3(answerIndex);
                break;
        }
    }

    private void HandleQuestion1(int answerIndex)
    {
        if (responseText != null)
        {
            responseText.text = "";
        }

        StartCoroutine(NextQuestionRoutine(answerDelay));
    }

    private void HandleQuestion2(int answerIndex)
    {
        if (responseText != null)
        {
            switch (answerIndex)
            {
                case 0:
                    responseText.text = "그렇게 느껴졌나요?";
                    break;

                case 1:
                    responseText.text = "";
                    break;

                case 2:
                    responseText.text = "판단을 보류했습니다.";
                    break;
            }
        }

        StartCoroutine(NextQuestionRoutine(answerDelay));
    }

    private void HandleQuestion3(int answerIndex)
    {
        if (answerIndex == 2)
        {
            if (responseText != null)
            {
                responseText.text = "검사 완료";
            }

            StartCoroutine(SuccessRoutine());
        }
        else
        {
            if (responseText != null)
            {
                if (answerIndex == 0)
                {
                    responseText.text =
                        "아직 시작되지 않았다고 확신할 수 있습니까?";
                }
                else
                {
                    responseText.text =
                        "그렇다면 지금까지의 행동은 무엇이었습니까?";
                }
            }

            StartCoroutine(RetryQuestionRoutine());
        }
    }

    private IEnumerator NextQuestionRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isGameActive)
            yield break;

        currentQuestion++;

        if (currentQuestion >= 3)
        {
            Success();
            yield break;
        }

        ShowQuestion();
    }

    private IEnumerator RetryQuestionRoutine()
    {
        yield return new WaitForSeconds(wrongAnswerDelay);

        if (!isGameActive)
            yield break;

        ShowQuestion();
    }

    private IEnumerator SuccessRoutine()
    {
        yield return new WaitForSeconds(successDelay);

        if (!isGameActive)
            yield break;

        Success();
    }

    protected override void GiveHint()
    {
        if (responseText != null && !isProcessing)
        {
            responseText.text =
                "힌트: 질문의 의미를 다시 생각해 보세요.";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        if (answerButton1 != null)
        {
            answerButton1.onClick.RemoveListener(OnAnswer1);
        }

        if (answerButton2 != null)
        {
            answerButton2.onClick.RemoveListener(OnAnswer2);
        }

        if (answerButton3 != null)
        {
            answerButton3.onClick.RemoveListener(OnAnswer3);
        }
    }
}
