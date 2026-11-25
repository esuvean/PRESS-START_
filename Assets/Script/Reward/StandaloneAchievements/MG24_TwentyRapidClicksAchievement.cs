
using UnityEngine;
using UnityEngine.UI;

// 업적 27 - 손가락 안 아프세요?
// 조건: MG24에서 버튼 20회 연타
// 기존 Minigame24_WaitAt99.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG24_TwentyRapidClicksAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 27;

    private Minigame24_WaitAt99 original;
    private Button trackedButton;

    private int clickCount;
    private bool hooked;
    private bool wasWaitingAt99;

    protected override void ResetWatch()
    {
        original = game as Minigame24_WaitAt99;

        clickCount = 0;
        hooked = false;
        wasWaitingAt99 = false;

        RegisterButton();

        Debug.Log(
            "[MG24 Achievement] 연타 감지 시작"
        );
    }

    protected override void Poll()
    {
        if (original == null)
            original = game as Minigame24_WaitAt99;

        if (!hooked)
            RegisterButton();

        // 버튼 클릭 직전의 상태를 기억
        wasWaitingAt99 =
            IsWaitingAt99();
    }

    private bool IsWaitingAt99()
    {
        object state = Field("currentState");

        return state != null &&
               state.ToString() == "WaitingAt99";
    }

    private void RegisterButton()
    {
        if (original == null ||
            original.completeButton == null)
            return;

        trackedButton = original.completeButton;

        // 중복 이벤트 방지
        trackedButton.onClick.RemoveListener(
            OnButtonClicked
        );

        trackedButton.onClick.AddListener(
            OnButtonClicked
        );

        hooked = true;
    }

    private void OnButtonClicked()
    {
        if (game == null || !game.IsGameActive)
            return;

        // 99% 구간에서 누른 클릭만 인정
        // 마지막 클릭에서 START 준비 상태로
        // 바뀐 경우도 인정
        bool validClick =
            IsWaitingAt99() ||
            (
                wasWaitingAt99 &&
                Field("currentState")?.ToString()
                    == "StartReady"
            );

        if (!validClick)
            return;

        clickCount++;

        Debug.Log(
            $"[MG24 Achievement] 버튼 클릭: " +
            $"{clickCount}/20회"
        );

        if (clickCount >= 20)
        {
            Grant();

            Debug.Log(
                "[MG24 Achievement] " +
                "히든 업적 27번 달성!"
            );
        }

        wasWaitingAt99 = IsWaitingAt99();
    }
}
