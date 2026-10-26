using UnityEngine;

public class ScreenController : MonoBehaviour
{
    [Header("화면 패널")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject checkProgressPanel;
    [SerializeField] private GameObject settingsPanel;


    private void Start()
    {
        mainPanel.SetActive(true);
        checkProgressPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }


    // START 버튼
    public void ShowCheckProgressScreen()
    {
        mainPanel.SetActive(false);
        checkProgressPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }


    // 메인 화면
    public void ShowMainScreen()
    {
        mainPanel.SetActive(true);
        checkProgressPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }


    // 설정을 챕터 선택 화면에서 열었는지 기억 (닫으면 그 화면으로 돌아감)
    private bool openedFromCheckProgress = false;

    // SETTINGS 버튼 (메인 화면 / 챕터 선택 화면 톱니바퀴 둘 다)
    public void ShowSettingsScreen()
    {
        openedFromCheckProgress = checkProgressPanel.activeSelf;

        mainPanel.SetActive(false);
        checkProgressPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }


    // BACK 버튼
    public void CloseSettingsScreen()
    {
        if (openedFromCheckProgress)
        {
            openedFromCheckProgress = false;
            ShowCheckProgressScreen();

            ChapterProgressUI progress = checkProgressPanel.GetComponent<ChapterProgressUI>();
            if (progress != null)
                progress.RefreshProgress();

            return;
        }

        ShowMainScreen();
    }
}