using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterManager : MonoBehaviour
{
    public static ChapterManager Instance;

    public string nextChapterName = "Chapter2";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

 
    public void OnChapterComplete()
    {
        if (!string.IsNullOrEmpty(nextChapterName))
        {
            SceneManager.LoadScene(nextChapterName);
        }
        else
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}