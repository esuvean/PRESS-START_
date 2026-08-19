using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame4_TargetClick : MinigameBase
{
    [Header("UI Reference")]
    public RectTransform targetButtonTransform;
    public Button targetButton;
    public RectTransform spawnArea;       // 타겟이 나타날 영역
    public TextMeshProUGUI statusText;

    [Header("Game Settings")]
    public int targetClickCount = 5;      // 목표 클릭 횟수

    private int currentCount = 0;

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "순발력 반응 검사";
        instruction = "화면에 무작위로 나타나는 타겟 버튼을 빠르게 클릭하세요!";

        currentCount = 0;

        if (targetButton != null)
        {
            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(OnTargetClick);
        }

        MoveTargetToRandomPosition();
        UpdateUI();
    }

    private void OnTargetClick()
    {
        if (!isGameActive) return;

        currentCount++;
        UpdateUI();

        if (currentCount >= targetClickCount)
        {
            Success();
        }
        else
        {
            MoveTargetToRandomPosition();
        }
    }

    private void MoveTargetToRandomPosition()
    {
        if (spawnArea == null || targetButtonTransform == null) return;

        float areaWidth = spawnArea.rect.width;
        float areaHeight = spawnArea.rect.height;
        float btnWidth = targetButtonTransform.rect.width;
        float btnHeight = targetButtonTransform.rect.height;

       
        float maxX = (areaWidth - btnWidth) / 2f;
        float maxY = (areaHeight - btnHeight) / 2f;

        float randomX = Random.Range(-maxX, maxX);
        float randomY = Random.Range(-maxY, maxY);

        targetButtonTransform.anchoredPosition = new Vector2(randomX, randomY);
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"남은 타겟: {targetClickCount - currentCount}";
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}