using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame7_TermsScroll : MinigameBase
{
    [Header("UI Reference")]
    public ScrollRect termsScroll;
    public Button agreeButton;
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    [Range(0f, 0.2f)]
    public float bottomThreshold = 0.03f;

    private bool escapedOnce = false;

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "실행 문서 확인 검사";
        instruction = "약관의 마지막까지 이동하여 시작 버튼을 누른다.";

        escapedOnce = false;

        if (termsScroll != null)
            termsScroll.verticalNormalizedPosition = 1f;

        if (agreeButton != null)
        {
            agreeButton.onClick.RemoveListener(OnAgreeClicked);
            agreeButton.onClick.AddListener(OnAgreeClicked);
            AddPointerEnterTrap();
        }

        if (statusText != null)
            statusText.text = "약관을 끝까지 확인하세요.";
    }

    private void AddPointerEnterTrap()
    {
        EventTrigger trigger = agreeButton.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = agreeButton.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        // 프리팹 인스턴스는 한 번만 StartMinigame 되므로 새 엔트리를 추가합니다.
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener(_ => OnAgreePointerEnter());
        trigger.triggers.Add(entry);
    }

    private bool IsAtBottom()
    {
        return termsScroll != null &&
               termsScroll.verticalNormalizedPosition <= bottomThreshold;
    }

    private void OnAgreePointerEnter()
    {
        if (!isGameActive || escapedOnce || !IsAtBottom())
            return;

        escapedOnce = true;

        if (termsScroll != null)
            termsScroll.verticalNormalizedPosition = 1f;

        if (statusText != null)
            statusText.text = "약관 위치가 초기화되었습니다.";
    }

    private void OnAgreeClicked()
    {
        if (!isGameActive) return;

        if (!escapedOnce)
        {
            if (statusText != null)
                statusText.text = "먼저 약관 마지막까지 이동하세요.";
            return;
        }

        if (!IsAtBottom())
        {
            if (statusText != null)
                statusText.text = "다시 약관 마지막까지 이동하세요.";
            return;
        }

        Success();
    }

    protected override void GiveHint()
    {
        if (statusText != null)
            statusText.text = "힌트: 마우스 휠 또는 스크롤바를 사용하세요.";
    }

    protected override void RestartGame()
    {
        if (termsScroll != null)
            termsScroll.verticalNormalizedPosition = 1f;
    }
}
