using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    
    public string firstChapterName = "Chapter1";

    
    public void OnClickStart()
    {
        SceneManager.LoadScene(firstChapterName);
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