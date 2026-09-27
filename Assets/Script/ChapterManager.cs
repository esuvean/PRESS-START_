using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChapterManager : MonoBehaviour
{
   
    public Transform canvasTransform;

    
    public GameObject loadingPanel;
    public Image loadingBar;
    public TMP_Text loadingPercentText;

   
    public float loadingDuration = 0.35f;

    [System.Serializable]
    public class ChapterData
    {
        public string chapterName;
        public List<GameObject> minigamePrefabs;
    }

    [Header("Chapter Settings")]
    public List<ChapterData> chapters;

  

    [Header("Chapter Transition")]
    [Tooltip("현재 Scene의 모든 미니게임 완료 후 이동할 Scene 이름")]
    public string nextSceneName;

    [Tooltip("Next Scene Name이 비어있을 때 이동할 Scene")]
    public string fallbackSceneName = "MainScene";

 

    private int currentChapterIndex = 0;
    private int currentMinigameIndex = 0;

    private GameObject currentActiveGameInstance;

    private bool isTransitioning = false;


   

    private void OnEnable()
    {
        MinigameBase.OnGameSuccess += HandleMinigameSuccess;
        MinigameBase.OnGameFailure += HandleMinigameFailure;
    }


    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -= HandleMinigameSuccess;
        MinigameBase.OnGameFailure -= HandleMinigameFailure;
    }


   
    private void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        ResetLoadingUI();

        StartCurrentMinigame();
    }


   
    private void StartCurrentMinigame()
    {
      
        if (currentChapterIndex >= chapters.Count)
        {
            StartCoroutine(LoadNextChapterScene());
            return;
        }


        ChapterData activeChapter =
            chapters[currentChapterIndex];


        
        if (currentMinigameIndex >=
            activeChapter.minigamePrefabs.Count)
        {
            Debug.Log(
                $"{activeChapter.chapterName} 클리어!"
            );

            currentChapterIndex++;
            currentMinigameIndex = 0;

            StartCurrentMinigame();

            return;
        }


        GameObject gamePrefab =
            activeChapter
                .minigamePrefabs[
                    currentMinigameIndex
                ];


        if (gamePrefab == null)
        {
            Debug.LogError(
                $"{activeChapter.chapterName}의 " +
                $"{currentMinigameIndex}번 미니게임 Prefab이 비어 있습니다."
            );

            return;
        }


        currentActiveGameInstance =
            Instantiate(
                gamePrefab,
                canvasTransform
            );


        MinigameBase gameScript =
            currentActiveGameInstance
                .GetComponent<MinigameBase>();


        if (gameScript != null)
        {
            gameScript.StartMinigame();
        }
        else
        {
            Debug.LogError(
                $"{gamePrefab.name}에 MinigameBase를 상속받은 스크립트가 없습니다."
            );
        }
    }


   
    private void HandleMinigameSuccess()
    {
        if (isTransitioning)
        {
            return;
        }


        Debug.Log("미니게임 성공!");


        if (currentActiveGameInstance != null)
        {
            Destroy(
                currentActiveGameInstance
            );

            currentActiveGameInstance = null;
        }


        currentMinigameIndex++;


        StartCoroutine(
            LoadNextMinigame()
        );
    }


    private IEnumerator LoadNextMinigame()
    {
        isTransitioning = true;


        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }


        ResetLoadingUI();


        float timer = 0f;


        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    timer /
                    loadingDuration
                );


            if (loadingBar != null)
            {
                loadingBar.fillAmount =
                    progress;
            }


            if (loadingPercentText != null)
            {
                loadingPercentText.text =
                    Mathf.RoundToInt(
                        progress * 100f
                    )
                    + "%";
            }


            yield return null;
        }


        if (loadingBar != null)
        {
            loadingBar.fillAmount = 1f;
        }


        if (loadingPercentText != null)
        {
            loadingPercentText.text =
                "100%";
        }


        yield return new WaitForSeconds(
            0.05f
        );


        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }


        isTransitioning = false;


        StartCurrentMinigame();
    }


 
    private IEnumerator LoadNextChapterScene()
    {
        if (isTransitioning)
        {
            yield break;
        }


        isTransitioning = true;


        Debug.Log(
            "현재 챕터의 모든 미니게임을 완료했습니다."
        );


        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }


        ResetLoadingUI();


        float timer = 0f;


        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    timer /
                    loadingDuration
                );


            if (loadingBar != null)
            {
                loadingBar.fillAmount =
                    progress;
            }


            if (loadingPercentText != null)
            {
                loadingPercentText.text =
                    Mathf.RoundToInt(
                        progress * 100f
                    )
                    + "%";
            }


            yield return null;
        }


        if (loadingBar != null)
        {
            loadingBar.fillAmount = 1f;
        }


        if (loadingPercentText != null)
        {
            loadingPercentText.text =
                "100%";
        }


        yield return new WaitForSeconds(
            0.1f
        );


        string sceneToLoad;


        if (!string.IsNullOrWhiteSpace(
            nextSceneName))
        {
            sceneToLoad =
                nextSceneName;
        }
        else
        {
            sceneToLoad =
                fallbackSceneName;
        }


        Debug.Log(
            $"다음 Scene으로 이동: {sceneToLoad}"
        );


        SceneManager.LoadScene(
            sceneToLoad
        );
    }


    
    private void HandleMinigameFailure()
    {
        Debug.Log("미니게임 실패");

        
    }


 

    private void ResetLoadingUI()
    {
        if (loadingBar != null)
        {
            loadingBar.fillAmount = 0f;
        }


        if (loadingPercentText != null)
        {
            loadingPercentText.text =
                "0%";
        }
    }
}