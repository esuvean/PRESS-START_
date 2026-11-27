
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingsAchievementButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(OpenAchievements);
        button.onClick.AddListener(OpenAchievements);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenAchievements);
    }

    public void OpenAchievements()
    {
        // 비활성 오브젝트까지 포함해서 검색
        AchievementUI[] controllers =
            Object.FindObjectsByType<AchievementUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        AchievementUI selected = null;

        // 현재 챕터 씬에 있는 컨트롤러 우선 검색
        foreach (AchievementUI ui in controllers)
        {
            if (ui == null || ui.achievementPanel == null)
                continue;

            if (ui.gameObject.scene == gameObject.scene &&
                ui.gameObject.activeInHierarchy)
            {
                selected = ui;
                break;
            }
        }

        // 못 찾으면 활성 상태의 다른 컨트롤러 검색
        if (selected == null)
        {
            foreach (AchievementUI ui in controllers)
            {
                if (ui != null &&
                    ui.achievementPanel != null &&
                    ui.gameObject.activeInHierarchy)
                {
                    selected = ui;
                    break;
                }
            }
        }

        if (selected == null)
        {
            Debug.LogError(
                "[Achievement] AchievementUIController를 찾지 못했습니다."
            );
            return;
        }

        selected.Open();

        Debug.Log(
            "[Achievement] 컨트롤러 찾기 성공: " +
            selected.gameObject.name +
            " / Panel Active: " +
            selected.achievementPanel.activeInHierarchy
        );
    }
}
