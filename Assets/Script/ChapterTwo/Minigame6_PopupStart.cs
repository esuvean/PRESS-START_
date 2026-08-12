using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame6_PopupStart : MinigameBase
{
    [Header("UI Reference")]
    public Button hiddenStartButton;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        // ChapterManager 없이 씬에서 직접 테스트할 때도 실행
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "다중 창 탐색 검사";
        instruction = "여러 개의 팝업창 뒤에 숨겨진 시작 버튼을 찾는다.";

        if (hiddenStartButton != null)
        {
            hiddenStartButton.onClick.RemoveListener(OnStartClicked);
            hiddenStartButton.onClick.AddListener(OnStartClicked);
        }

        if (statusText != null)
            statusText.text = "팝업창을 이동하여 START 버튼을 찾으세요.";
    }

    private void OnStartClicked()
    {
        if (!isGameActive) return;

        if (statusText != null)
            statusText.text = "START 버튼 발견";

        Success();
    }

    protected override void GiveHint()
    {
        if (statusText != null)
            statusText.text = "힌트: 창의 제목 표시줄을 드래그할 수 있습니다.";
    }

    protected override void RestartGame()
    {
        // 이 게임은 실패로 전체 초기화하지 않고 계속 탐색하는 방식입니다.
    }
}
