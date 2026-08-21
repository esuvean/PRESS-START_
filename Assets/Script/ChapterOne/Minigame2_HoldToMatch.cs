using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame2_HoldToMatch : MinigameBase
{
    [Header("UI References")]
    public RectTransform resizableButton;   // 크기가 커질 버튼
    public RectTransform targetFrame;       // 초록색 목표 테두리
    public Image targetOutlineImage;        // 반짝임 효과용 테두리 이미지
    public TextMeshProUGUI roundText;      // ROUND 1 / 3
    public TextMeshProUGUI failText;       // 실패 횟수
    public TextMeshProUGUI successText;    // 성공 횟수
    public TextMeshProUGUI hintText;       // 하단 힌트 텍스트

    [Header("Game Configurations")]
    public float[] targetScales = new float[] { 2.0f, 2.8f, 1.6f };
    public float[] baseGrowSpeeds = new float[] { 1.2f, 1.8f, 2.5f };
    public float baseTolerance = 0.25f;

    private int currentRound = 0;
    private int totalRounds = 3;
    private int failCount = 0;
    private int successCount = 0;

    private bool isHolding = false;
    private float currentScale = 1.0f;
    private float currentGrowSpeed;
    private float currentTolerance;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "딱 맞게 눌러 주세요";
        instruction = "버튼을 길게 눌러 목표 크기에 맞춰보세요.";

        currentRound = 0;
        failCount = 0;
        successCount = 0;

        SetupButtonEvents();
        SetupRound();
        UpdateUI();
    }

    private void SetupButtonEvents()
    {
        if (resizableButton == null) return;

        EventTrigger trigger = resizableButton.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = resizableButton.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { OnButtonDown(); });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { OnButtonUp(); });
        trigger.triggers.Add(entryUp);
    }

    private void SetupRound()
    {
        isHolding = false;
        currentScale = 1.0f;

        if (resizableButton != null)
        {
            resizableButton.localScale = Vector3.one * currentScale;
        }

        currentGrowSpeed = baseGrowSpeeds[Mathf.Min(currentRound, baseGrowSpeeds.Length - 1)];
        currentTolerance = baseTolerance;

        if (failCount >= 2) currentTolerance += 0.15f;
        if (failCount >= 6) currentGrowSpeed *= 0.6f;

        if (targetFrame != null)
        {
            float targetScale = targetScales[Mathf.Min(currentRound, targetScales.Length - 1)];
            targetFrame.localScale = Vector3.one * targetScale;
        }

        if (targetOutlineImage != null)
        {
            targetOutlineImage.color = new Color(0.2f, 1f, 0.4f, 1f);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        if (isHolding)
        {
            currentScale += currentGrowSpeed * Time.deltaTime;

            if (resizableButton != null)
            {
                resizableButton.localScale = Vector3.one * currentScale;
            }

            float targetScale = targetScales[Mathf.Min(currentRound, targetScales.Length - 1)];

            if (failCount >= 4 && targetOutlineImage != null)
            {
                float diff = Mathf.Abs(currentScale - targetScale);
                if (diff <= currentTolerance + 0.2f)
                {
                    float alpha = Mathf.PingPong(Time.time * 10f, 1f);
                    targetOutlineImage.color = new Color(0.2f, 1f, 0.4f, alpha);
                }
            }

            if (currentScale > targetScale + currentTolerance + 0.8f)
            {
                OnButtonUp();
            }
        }
    }

    public void OnButtonDown()
    {
        if (!isGameActive) return;
        isHolding = true;
    }

    public void OnButtonUp()
    {
        if (!isGameActive || !isHolding) return;
        isHolding = false;

        EvaluateResult();
    }

    private void EvaluateResult()
    {
        float targetScale = targetScales[Mathf.Min(currentRound, targetScales.Length - 1)];
        float minScale = targetScale - currentTolerance;
        float maxScale = targetScale + currentTolerance;

        if (currentScale >= minScale && currentScale <= maxScale)
        {
            successCount++;
            currentRound++;

            if (currentRound >= totalRounds)
            {
                UpdateUI();
                Success();
            }
            else
            {
                SetupRound();
                UpdateUI();
            }
        }
        else
        {
            failCount++;
            CheckHints();
            SetupRound();
            UpdateUI();
        }
    }

    private void CheckHints()
    {
        if (hintText == null) return;

        if (failCount >= 6) hintText.text = "[ HINT ] 버튼이 커지는 속도가 감소했습니다.";
        else if (failCount >= 4) hintText.text = "[ HINT ] 목표 크기에 가까워지면 테두리가 반짝입니다.";
        else if (failCount >= 2) hintText.text = "[ HINT ] 목표 범위가 조금 넓어졌습니다.";
        else hintText.text = "[ HINT ] 너무 오래 누르면 버튼이 터집니다";
    }

    private void UpdateUI()
    {
        if (roundText != null) roundText.text = $"+ ROUND {currentRound + 1} / {totalRounds} +";
        if (failText != null) failText.text = $"실패 {failCount} 회";
        if (successText != null) successText.text = $"성공 {successCount} 회";
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}