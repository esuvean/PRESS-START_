using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class Minigame10_TabTracking : MinigameBase
{
    [Header("Tabs")]
    public Button[] tabButtons;
    public GameObject[] tabPages;
    public RectTransform[] tabContents;
    public string[] tabNames = { "게임", "설정", "도움말", "오류 보고" };

    [Header("Start Buttons")]
    public Button trueStartButton;
    public GameObject fakeButtonPrefab;

    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    public int requiredMoves = 3;

    private int currentTab = 0;
    private int trueButtonTab = 0;
    private int moveCount = 0;
    private bool fakeSpawned = false;

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "활성 화면 추적 검사";
        instruction = "여러 탭 사이를 이동하는 START 버튼을 추적한다.";

        moveCount = 0;
        fakeSpawned = false;

        SetupTabs();
        OpenTab(0);
        MoveTrueButtonToTab(0);

        if (trueStartButton != null)
        {
            trueStartButton.onClick.RemoveListener(OnTrueButtonClicked);
            trueStartButton.onClick.AddListener(OnTrueButtonClicked);
            AddPointerEnterTrap();
        }

        if (statusText != null)
            statusText.text = "START 버튼을 추적하세요.";
    }

    private void SetupTabs()
    {
        if (tabButtons == null) return;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int captured = i;

            if (tabButtons[i] == null) continue;

            tabButtons[i].onClick.RemoveAllListeners();
            tabButtons[i].onClick.AddListener(() => OpenTab(captured));
        }
    }

    private void OpenTab(int index)
    {
        currentTab = index;

        if (tabPages == null) return;

        for (int i = 0; i < tabPages.Length; i++)
        {
            if (tabPages[i] != null)
                tabPages[i].SetActive(i == index);
        }
    }

    private void AddPointerEnterTrap()
    {
        EventTrigger trigger = trueStartButton.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = trueStartButton.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener(_ => OnTrueButtonPointerEnter());
        trigger.triggers.Add(entry);
    }

    private void OnTrueButtonPointerEnter()
    {
        if (!isGameActive || moveCount >= requiredMoves)
            return;

        int nextTab = PickDifferentTab(trueButtonTab);
        MoveTrueButtonToTab(nextTab);
        moveCount++;

        if (statusText != null)
            statusText.text = $"신호 감지: {GetTabName(nextTab)}";

        if (moveCount >= requiredMoves && !fakeSpawned)
        {
            SpawnFakeButton();
        }
    }

    private int PickDifferentTab(int current)
    {
        if (tabContents == null || tabContents.Length <= 1)
            return current;

        int next = current;

        while (next == current)
            next = Random.Range(0, tabContents.Length);

        return next;
    }

    private void MoveTrueButtonToTab(int tabIndex)
    {
        if (trueStartButton == null ||
            tabContents == null ||
            tabIndex < 0 ||
            tabIndex >= tabContents.Length ||
            tabContents[tabIndex] == null)
            return;

        trueButtonTab = tabIndex;
        trueStartButton.transform.SetParent(tabContents[tabIndex], false);

        RectTransform rt = trueStartButton.transform as RectTransform;
        RectTransform parent = tabContents[tabIndex];

        if (rt != null)
        {
            float xLimit = Mathf.Max(0f, parent.rect.width * 0.35f);
            float yLimit = Mathf.Max(0f, parent.rect.height * 0.25f);

            rt.anchoredPosition = new Vector2(
                Random.Range(-xLimit, xLimit),
                Random.Range(-yLimit, yLimit)
            );
            rt.localScale = Vector3.one;
        }
    }

    private void SpawnFakeButton()
    {
        if (fakeButtonPrefab == null ||
            tabContents == null ||
            tabContents.Length == 0)
            return;

        int fakeTab = PickDifferentTab(trueButtonTab);
        GameObject fake = Instantiate(fakeButtonPrefab, tabContents[fakeTab]);

        RectTransform rt = fake.transform as RectTransform;
        RectTransform parent = tabContents[fakeTab];

        if (rt != null)
        {
            float xLimit = Mathf.Max(0f, parent.rect.width * 0.35f);
            float yLimit = Mathf.Max(0f, parent.rect.height * 0.25f);

            rt.anchoredPosition = new Vector2(
                Random.Range(-xLimit, xLimit),
                Random.Range(-yLimit, yLimit)
            );
        }

        Button fakeButton = fake.GetComponent<Button>();

        if (fakeButton != null)
        {
            fakeButton.onClick.RemoveAllListeners();
            fakeButton.onClick.AddListener(() => OnFakeButtonClicked(fake));
        }

        fakeSpawned = true;
    }

    private void OnFakeButtonClicked(GameObject fake)
    {
        if (!isGameActive) return;

        if (statusText != null)
            statusText.text = "가짜 START 버튼입니다.";

        if (fake != null)
            Destroy(fake);
    }

    private void OnTrueButtonClicked()
    {
        if (!isGameActive) return;

        if (moveCount < requiredMoves)
            return;

        Success();
    }

    private string GetTabName(int index)
    {
        if (tabNames != null &&
            index >= 0 &&
            index < tabNames.Length)
            return tabNames[index];

        return $"TAB {index + 1}";
    }

    protected override void GiveHint()
    {
        if (statusText != null)
            statusText.text = $"힌트: 방금 표시된 탭을 확인하세요. ({GetTabName(trueButtonTab)})";
    }

    protected override void RestartGame()
    {
        moveCount = 0;
        fakeSpawned = false;
        OpenTab(0);
        MoveTrueButtonToTab(0);
    }
}
