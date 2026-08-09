using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class Minigame2_TimingButton : MinigameBase
{
    [Header("UI References")]
    public RectTransform gaugeBar;        // 게이지 전체 배경 바
    public RectTransform targetArea;      // 목표 영역 
    public RectTransform indicator;       // 좌우로 움직이는 바늘
    public Button actionButton;           // 누르는 버튼
    public TextMeshProUGUI statusText;    // 성공 횟수 텍스트

    [Header("Game Settings")]
    public float baseMoveSpeed = 400f;    // 바늘 이동 속도
    public int targetSuccessCount = 3;    // 목표 성공 횟수

    private float currentSpeed;
    private int successCount = 0;
    private int failCount = 0;
    private bool movingRight = true;
    private float gaugeWidth = 0f;

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "타이밍 반응 검사";
        instruction = "움직이는 바늘이 녹색 영역 안에 들어왔을 때 버튼(또는 Space키)을 누르세요!";

        successCount = 0;
        failCount = 0;
        currentSpeed = baseMoveSpeed;

        if (gaugeBar != null)
        {
            gaugeWidth = gaugeBar.rect.width;
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnPressButton);
        }

        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        MoveIndicator();

        // 키보드 스페이스바 입력 지원
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnPressButton();
        }
    }

    private void MoveIndicator()
    {
        if (gaugeBar == null || indicator == null) return;

        float halfGauge = gaugeWidth / 2f;
        Vector2 pos = indicator.anchoredPosition;

        if (movingRight)
        {
            pos.x += currentSpeed * Time.deltaTime;
            if (pos.x >= halfGauge)
            {
                pos.x = halfGauge;
                movingRight = false;
            }
        }
        else
        {
            pos.x -= currentSpeed * Time.deltaTime;
            if (pos.x <= -halfGauge)
            {
                pos.x = -halfGauge;
                movingRight = true;
            }
        }

        indicator.anchoredPosition = pos;
    }

    public void OnPressButton()
    {
        if (!isGameActive) return;

        if (IsIndicatorInTarget())
        {
            
            successCount++;
            currentSpeed += 120f;
            UpdateUI();

            if (successCount >= targetSuccessCount)
            {
                Success();
            }
        }
        else
        {
            
            failCount++;
            CheckHints();
        }
    }

    private bool IsIndicatorInTarget()
    {
        if (indicator == null || targetArea == null) return false;

        float indicatorX = indicator.anchoredPosition.x;
        float targetX = targetArea.anchoredPosition.x;
        float targetHalfWidth = targetArea.rect.width / 2f;

        float minX = targetX - targetHalfWidth;
        float maxX = targetX + targetHalfWidth;

        return indicatorX >= minX && indicatorX <= maxX;
    }

    private void CheckHints()
    {
        if (failCount == 3)
        {
            Debug.Log(" 힌트 (실패 3회): 바늘이 정중앙(녹색)에 다가왔을 때 누르세요.");
        }
        else if (failCount == 6)
        {
            currentSpeed = Mathf.Max(250f, currentSpeed - 100f); // 속도 완화
            Debug.Log(" 힌트 (실패 6회): 바늘 속도가 조금 느려졌습니다.");
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"성공 횟수 {successCount} / {targetSuccessCount}";
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}