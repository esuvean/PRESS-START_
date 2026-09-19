using UnityEngine;

public class SettingsOpenButton : MonoBehaviour
{
    public void OpenSettings()
    {
        SettingsController settingsController =
            FindFirstObjectByType<SettingsController>();

        if (settingsController != null)
        {
            settingsController.OpenSettings();
        }
        else
        {
            Debug.LogWarning("현재 씬에서 SettingsController를 찾을 수 없습니다.");
        }
    }
}