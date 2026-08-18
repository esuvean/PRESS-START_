using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class Minigame3_MashButton : MinigameBase
{
    [Header("UI Reference")]
    public Image gaugeFill;              // 채워질 게이지 
    public Button mashButton;            // 연타할 버튼
    public TextMeshProUGUI statusText;    // 달성도 텍스트

    [Header("Game Settings")]
    public int targetClickCount = 15;     // 목표 클릭 횟수
    public float decayRate = 1.0f;        // 초당 감퇴하는 게이지 양 

    private float currentProgress = 0f;  

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "연타 반응 검사";
        instruction = "버튼(또는 Space키)을 빠르게 연타하여 게이지를 가득 채우세요!";

        currentProgress = 0f;

        if (mashButton != null)
        {
            mashButton.onClick.RemoveAllListeners();
            mashButton.onClick.AddListener(OnMashButtonClick);
        }

        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        // 시간이 지남에 따라 게이지가 조금씩 감소
        if (currentProgress > 0)
        {
            currentProgress -= (decayRate / targetClickCount) * Time.deltaTime;
            currentProgress = Mathf.Clamp01(currentProgress);
            UpdateUI();
        }

        // 스페이스바 연타 지원
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnMashButtonClick();
        }
    }

    private void OnMashButtonClick()
    {
        if (!isGameActive) return;

        currentProgress += 1f / targetClickCount;
        currentProgress = Mathf.Clamp01(currentProgress);

        UpdateUI();

        if (currentProgress >= 1f)
        {
            Success();
        }
    }

    private void UpdateUI()
    {
        if (gaugeFill != null)
        {
            gaugeFill.fillAmount = currentProgress;
        }

        if (statusText != null)
        {
            int percentage = Mathf.FloorToInt(currentProgress * 100f);
            statusText.text = $"달성도: {percentage}%";
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}