using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject checkProgressPanel;
    public GameObject settingsPanel;

    [Header("Progress UI")]
    public ChapterProgressUI chapterProgressUI;

    private void Start()
    {
        // 챕터 완료 후 메인씬으로 돌아온 경우
        if (SessionProgress.openChapterSelectOnLoad)
        {
            OpenChapterSelectDirectly();
            SessionProgress.openChapterSelectOnLoad = false;
        }
        else
        {
            ShowMainPanel();
        }
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (checkProgressPanel != null) checkProgressPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OpenChapterSelectDirectly()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (checkProgressPanel != null) checkProgressPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (chapterProgressUI != null)
        {
            chapterProgressUI.RefreshProgress();
        }
    }

    public void OnClickStart()
    {
        OpenChapterSelectDirectly();
    }

    public void OnClickSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnClickCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }

    public void OnClickChapter1()
    {
        if (SessionProgress.IsChapterUnlocked(1))
            SceneManager.LoadScene("Chapter1");
    }

    public void OnClickChapter2()
    {
        if (SessionProgress.IsChapterUnlocked(2))
            SceneManager.LoadScene("Chapter2");
    }

    public void OnClickChapter3()
    {
        if (SessionProgress.IsChapterUnlocked(3))
            SceneManager.LoadScene("Chapter3");
    }

    public void OnClickChapter4()
    {
        if (SessionProgress.IsChapterUnlocked(4))
            SceneManager.LoadScene("Chapter4");
    }
}