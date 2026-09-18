using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
   
    public GameObject titlePanel;          
    public GameObject chapterSelectPanel; 

   
    public string chapter1SceneName = "Chapter1";
    public string chapter2SceneName = "Chapter2";
    public string chapter3SceneName = "Chapter3";
    public string chapter4SceneName = "Chapter4";

    private void Start()
    {
        
        if (titlePanel != null) titlePanel.SetActive(true);
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(false);
    }

   
    public void OnClickStart()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(true);
    }

   
    public void OnClickChapter1()
    {
        SceneManager.LoadScene(chapter1SceneName);
    }

   
    public void OnClickChapter2()
    {
        SceneManager.LoadScene(chapter2SceneName);
    }

  
    public void OnClickChapter3()
    {
        SceneManager.LoadScene(chapter3SceneName);
    }

   
    public void OnClickChapter4()
    {
        SceneManager.LoadScene(chapter4SceneName);
    }

    
    public void OnClickBackToTitle()
    {
        if (chapterSelectPanel != null) chapterSelectPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
    }

 
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}