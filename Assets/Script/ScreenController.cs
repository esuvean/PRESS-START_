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


    // SETTINGS 버튼
    public void ShowSettingsScreen()
    {
        mainPanel.SetActive(false);
        checkProgressPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }


    // BACK 버튼
    public void CloseSettingsScreen()
    {
        ShowMainScreen();
    }
}