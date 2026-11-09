using UnityEngine;

public class SettingsOpenButton : MonoBehaviour
{
    // 설정 버튼이 다른 UI(배경, 완료 패널, 글자 등)에 가려서 안 눌리는 일이 없도록
    // 항상 같은 부모 안에서 맨 위(마지막 순서)에 둠
    private void OnEnable()
    {
        transform.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        Transform parent = transform.parent;

        if (parent != null &&
            transform.GetSiblingIndex() != parent.childCount - 1)
        {
            transform.SetAsLastSibling();
        }
    }

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