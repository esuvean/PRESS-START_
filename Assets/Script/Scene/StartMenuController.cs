using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
   
    public GameObject titlePanel;
    public GameObject chapterSelectPanel;
    public GameObject settingsPanel;


  
    public ChapterProgressUI chapterProgressUI;


   
    public string chapter1SceneName = "Chapter1";
    public string chapter2SceneName = "Chapter2";
    public string chapter3SceneName = "Chapter3";
    public string chapter4SceneName = "Chapter4";



    private void Start()
    {
        // 챕터를 끝내고 돌아온 경우
        if (SessionProgress.openChapterSelectOnLoad)
        {
            ShowChapterSelect();

            SessionProgress.openChapterSelectOnLoad =
                false;
        }
        else
        {
            ShowTitle();
        }
    }


  

    private void ShowTitle()
    {
        if (titlePanel != null)
        {
            titlePanel.SetActive(true);
        }


        if (chapterSelectPanel != null)
        {
            chapterSelectPanel.SetActive(false);
        }


        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }


  
    private void ShowChapterSelect()
    {
        if (titlePanel != null)
        {
            titlePanel.SetActive(false);
        }


        if (chapterSelectPanel != null)
        {
            chapterSelectPanel.SetActive(true);
        }


        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }


        if (chapterProgressUI != null)
        {
            chapterProgressUI.RefreshProgress();
        }
    }



    public void OnClickStart()
    {
        ShowChapterSelect();
    }


  

    public void OnClickChapter1()
    {
        Debug.Log(
            "Chapter1 Scene으로 이동"
        );


        SceneManager.LoadScene(
            chapter1SceneName
        );
    }


    public void OnClickChapter2()
    {
        Debug.Log(
            "Chapter2 Scene으로 이동"
        );


        SceneManager.LoadScene(
            chapter2SceneName
        );
    }


    
    public void OnClickChapter3()
    {
        Debug.Log(
            "Chapter3 Scene으로 이동"
        );


        SceneManager.LoadScene(
            chapter3SceneName
        );
    }


  
    public void OnClickChapter4()
    {
        Debug.Log(
            "Chapter4 Scene으로 이동"
        );


        SceneManager.LoadScene(
            chapter4SceneName
        );
    }


    public void OnClickSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }


    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }


    public void OnClickQuit()
    {
        Application.Quit();
    }
}