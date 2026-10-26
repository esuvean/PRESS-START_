
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
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

    [Header("Button Art")]
    public Sprite normalButtonSprite;
    [Tooltip("클릭한 답 버튼에 보여줄 핑크 버튼")]
    public Sprite selectedButtonSprite;
    [Tooltip("선택하지 않은 버튼은 살짝 어둡게")]
    public Color unselectedDimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Robot Speech")]
    [Tooltip("로봇이 한 글자씩 말하는 속도 (초)")]
    public float typingSpeed = 0.03f;

    [Header("Settings")]
    public float answerDelay = 2f;
    public float wrongAnswerDelay = 2.5f;
    public float successDelay = 1f;

    private int currentQuestion = 0;
    private bool isProcessing = false;

    private Coroutine typingRoutine;
    private string currentQuestionLine = "";

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

        gameName = "사용자 실행 의사 분석";
        instruction = "시작 버튼이 제시하는 질문에 답하며 게임의 현재 상태를 판단한다.";

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

        AddHoverEffect(answerButton1);
        AddHoverEffect(answerButton2);
        AddHoverEffect(answerButton3);

        ShowQuestion();
    }

    private void AddHoverEffect(Button button)
    {
        if (button != null &&
            button.GetComponent<Minigame18_ButtonHover>() == null)
        {
            button.gameObject.AddComponent<Minigame18_ButtonHover>();
        }
    }

    // 로봇 말풍선에 한 글자씩 대사를 표시
    private void Say(string line)
    {
        if (questionText == null)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        questionText.text = line;
        typingRoutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        questionText.maxVisibleCharacters = 0;
        questionText.ForceMeshUpdate();

        int total = questionText.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            questionText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        questionText.maxVisibleCharacters = 99999;
        typingRoutine = null;
    }

    private void ResetButtonVisuals()
    {
        Button[] buttons = { answerButton1, answerButton2, answerButton3 };

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();

            if (image != null)
            {
                if (normalButtonSprite != null)
                    image.sprite = normalButtonSprite;

                image.color = Color.white;
            }
        }

        // 이전에 누른 버튼이 '선택됨' 상태로 남아 있으면 마우스를 올려도 반응하지 않으므로 해제
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void MarkChosenButton(int answerIndex)
    {
        Button[] buttons = { answerButton1, answerButton2, answerButton3 };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            Image image = buttons[i].GetComponent<Image>();

            if (image == null)
                continue;

            if (i == answerIndex)
            {
                if (selectedButtonSprite != null)
                    image.sprite = selectedButtonSprite;

                image.color = Color.white;
            }
            else
            {
                image.color = unselectedDimColor;
            }
        }
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

        ResetButtonVisuals();
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
        currentQuestionLine = question;
        Say(question);

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
        MarkChosenButton(answerIndex);

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
        StartCoroutine(NextQuestionRoutine(answerDelay));
    }

    private void HandleQuestion2(int answerIndex)
    {
        switch (answerIndex)
        {
            case 0:
                Say("그렇게 느껴졌나요?");
                break;

            case 2:
                Say("판단을 보류했습니다.");
                break;
        }

        StartCoroutine(NextQuestionRoutine(answerDelay));
    }

    private void HandleQuestion3(int answerIndex)
    {
        if (answerIndex == 2)
        {
            Say("검사 완료");

            StartCoroutine(SuccessRoutine());
        }
        else
        {
            if (answerIndex == 0)
            {
                Say("아직 시작되지 않았다고 확신할 수 있습니까?");
            }
            else
            {
                Say("그렇다면 지금까지의 행동은 무엇이었습니까?");
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
        if (questionText != null && !isProcessing)
        {
            Say(currentQuestionLine +
                "\n<size=70%><color=#E0287D>힌트: 질문의 의미를 다시 생각해 보세요.</color></size>");
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
