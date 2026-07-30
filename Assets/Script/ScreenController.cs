using UnityEngine;

public class ScreenController : MonoBehaviour
{
    [Header("화면 패널")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject checkProgressPanel;

    private void Start()
    {
        // 게임을 실행하면 메인 화면부터 표시
        mainPanel.SetActive(true);
        checkProgressPanel.SetActive(false);
    }

    // START 버튼에서 호출할 함수
    public void ShowCheckProgressScreen()
    {
        mainPanel.SetActive(false);
        checkProgressPanel.SetActive(true);
    }

    // 검사 진행 화면에서 메인 화면으로 돌아올 때 사용
    public void ShowMainScreen()
    {
        checkProgressPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
}