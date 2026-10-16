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


    public List<ChapterData> chapters;


    [Tooltip("Chapter1 Scene = 1 / Chapter2 Scene = 2 / Chapter3 Scene = 3")]
    public int chapterNumber = 1;


    [Header("Scene Settings")]
    public string mainSceneName = "MainScene";



    private int currentChapterIndex = 0;
    private int currentMinigameIndex = 0;

    private GameObject currentActiveGameInstance;

    private bool isTransitioning = false;



    private void OnEnable()
    {
        MinigameBase.OnGameSuccess +=
            HandleMinigameSuccess;

        MinigameBase.OnGameFailure +=
            HandleMinigameFailure;
    }


    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -=
            HandleMinigameSuccess;

        MinigameBase.OnGameFailure -=
            HandleMinigameFailure;
    }



    private void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        ResetLoadingUI();

        // 중간에 나갔다가 다시 들어오면 이어서 시작
        ResumeFromSavedProgress();

        StartCurrentMinigame();
    }


    // 저장된 진행도(깬 미니게임 수)만큼 건너뛰고 시작
    private void ResumeFromSavedProgress()
    {
        int done = SessionProgress.GetProgress(chapterNumber);

        // 챕터를 다 깼거나 진행도가 없으면 처음부터
        if (done <= 0 ||
            done >= GetTotalMinigameCount() ||
            SessionProgress.IsChapterCompleted(chapterNumber))
        {
            return;
        }

        currentChapterIndex = 0;
        currentMinigameIndex = done;

        while (currentChapterIndex < chapters.Count &&
               chapters[currentChapterIndex] != null &&
               currentMinigameIndex >= chapters[currentChapterIndex].minigamePrefabs.Count)
        {
            currentMinigameIndex -= chapters[currentChapterIndex].minigamePrefabs.Count;
            currentChapterIndex++;
        }

        Debug.Log($"Chapter {chapterNumber} - {done}번째 미니게임부터 이어서 시작");
    }


    // 실패 화면의 RETRY 버튼: 지금 미니게임을 처음부터 다시
    public void RestartCurrentMinigame()
    {
        if (isTransitioning)
            return;

        if (currentActiveGameInstance != null)
        {
            Destroy(currentActiveGameInstance);
            currentActiveGameInstance = null;
        }

        StartCurrentMinigame();
    }


    // 실패 화면의 EXIT 버튼: 메인 화면 챕터 선택으로 (진행도는 유지)
    public void ExitToMainScene()
    {
        SessionProgress.openChapterSelectOnLoad = true;
        SceneManager.LoadScene(mainSceneName);
    }



    private void StartCurrentMinigame()
    {
        if (isTransitioning)
            return;


        // 이 Scene의 모든 ChapterData 완료
        if (currentChapterIndex >=
            chapters.Count)
        {
            CompleteChapter();
            return;
        }


        ChapterData activeChapter =
            chapters[currentChapterIndex];


        if (activeChapter == null)
        {
            Debug.LogError(
                "ChapterData가 없습니다."
            );

            return;
        }


        // 현재 ChapterData의 모든 미니게임 완료
        if (currentMinigameIndex >=
            activeChapter.minigamePrefabs.Count)
        {
            Debug.Log(
                $"{activeChapter.chapterName} 완료!"
            );

            currentChapterIndex++;
            currentMinigameIndex = 0;

            StartCurrentMinigame();

            return;
        }


        GameObject gamePrefab =
            activeChapter.minigamePrefabs[
                currentMinigameIndex
            ];


        if (gamePrefab == null)
        {
            Debug.LogError(
                $"Chapter {chapterNumber}의 " +
                $"{currentMinigameIndex}번 " +
                "미니게임 Prefab이 비어 있습니다."
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
                $"{gamePrefab.name}에 " +
                "MinigameBase 상속 스크립트가 없습니다."
            );
        }
    }



    private void HandleMinigameSuccess()
    {
        if (isTransitioning)
            return;


        Debug.Log(
            $"Chapter {chapterNumber} - 미니게임 성공!"
        );


        // =========================================
        // 기존 코드
        // =========================================

        if (currentActiveGameInstance != null)
        {
            Destroy(
                currentActiveGameInstance
            );

            currentActiveGameInstance = null;
        }


        currentMinigameIndex++;


        // 현재 챕터 진행도 저장
        SessionProgress.SetProgress(
            chapterNumber,
            currentMinigameIndex
        );


        Debug.Log(
            $"Chapter {chapterNumber} 진행도 : " +
            $"{SessionProgress.GetProgress(chapterNumber)}/" +
            $"{GetTotalMinigameCount()}"
        );


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



    private void CompleteChapter()
    {
        if (isTransitioning)
            return;


        int totalGames =
            GetTotalMinigameCount();


        // 마지막에 확실하게 저장
        SessionProgress.SetProgress(
            chapterNumber,
            totalGames
        );


        SessionProgress.CompleteChapter(
            chapterNumber
        );


        Debug.Log(
            $"Chapter {chapterNumber} 최종 완료 : " +
            $"{SessionProgress.GetProgress(chapterNumber)}/" +
            $"{totalGames}"
        );


        // MainScene에서 SYSTEM CHECK 바로 열기
        SessionProgress.openChapterSelectOnLoad =
            true;


        StartCoroutine(
            ReturnToMainScene()
        );
    }



    private int GetTotalMinigameCount()
    {
        int total = 0;


        if (chapters == null)
            return 0;


        foreach (ChapterData chapter
                 in chapters)
        {
            if (chapter != null &&
                chapter.minigamePrefabs != null)
            {
                total +=
                    chapter.minigamePrefabs.Count;
            }
        }


        return total;
    }



    private IEnumerator ReturnToMainScene()
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
            0.1f
        );


        SceneManager.LoadScene(
            mainSceneName
        );
    }



    private void HandleMinigameFailure()
    {
        Debug.Log(
            $"Chapter {chapterNumber} - 미니게임 실패"
        );
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