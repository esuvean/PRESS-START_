using UnityEngine;
using UnityEngine.UI;

public class SettingsMinigameBridge : MonoBehaviour
{
    [Header("Settings Controller")]
    [SerializeField] private SettingsController settingsController;


    [Header("Tab Buttons")]
    [SerializeField] private Button displayTabButton;
    [SerializeField] private Button soundTabButton;
    [SerializeField] private Button infoTabButton;
    [SerializeField] private Button exitTabButton;


    [Header("Back Button")]
    [SerializeField] private Button backButton;


    [Header("START Slots")]
    [SerializeField] private RectTransform displayStartSlot;
    [SerializeField] private RectTransform soundStartSlot;
    [SerializeField] private RectTransform infoStartSlot;
    [SerializeField] private RectTransform exitStartSlot;


    // =====================================================
    // 외부에서 사용할 수 있게 공개
    // =====================================================

    public Button DisplayTabButton => displayTabButton;
    public Button SoundTabButton => soundTabButton;
    public Button InfoTabButton => infoTabButton;
    public Button ExitTabButton => exitTabButton;

    public Button BackButton => backButton;

    public RectTransform DisplayStartSlot => displayStartSlot;
    public RectTransform SoundStartSlot => soundStartSlot;
    public RectTransform InfoStartSlot => infoStartSlot;
    public RectTransform ExitStartSlot => exitStartSlot;


    // =====================================================
    // Settings 조작
    // =====================================================

    public void OpenDisplay()
    {
        if (settingsController == null)
            return;

        settingsController.OpenSettings();
        settingsController.ShowDisplay();
    }


    public void CloseSettings()
    {
        if (settingsController == null)
            return;

        settingsController.CloseSettings();
    }
}