using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChapterManager : MonoBehaviour
{
    [Header("UI Reference")]
    public Transform canvasTransform;

    [Header("Loading UI")]
    public GameObject loadingPanel;
    public Image loadingBar;
    public TMP_Text loadingPercentText;

    [Header("Loading Settings")]
    public float loadingDuration = 0.35f;

    [System.Serializable]
    public class ChapterData
    {
        public string chapterName;
        public List<GameObject> minigamePrefabs;
    }

    [Header("Chapter Settings")]
    public List<ChapterData> chapters;

    private int currentChapterIndex = 0;
    private int currentMinigameIndex = 0;
    private GameObject currentActiveGameInstance;

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

    void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        // 첫 번째 챕터의 첫 게임 시작
        StartCurrentMinigame();
    }

    private void StartCurrentMinigame()
    {
        // 모든 챕터를 다 깬 경우 메인화면으로 이동
        if (currentChapterIndex >= chapters.Count)
        {
            Debug.Log("모든 챕터를 클리어하셨습니다! 메인 화면으로 돌아갑니다.");
            SceneManager.LoadScene("MainScene");
            return;
        }

        ChapterData activeChapter = chapters[currentChapterIndex];

        if (currentMinigameIndex >= activeChapter.minigamePrefabs.Count)
        {
            // 한 챕터의 게임들을 다 깬 경우 다음 챕터로 이동
            Debug.Log($"{activeChapter.chapterName} 클리어! 다음 챕터로 넘어갑니다.");
            currentChapterIndex++;
            currentMinigameIndex = 0;
            StartCurrentMinigame();
            return;
        }

        GameObject gamePrefab = activeChapter.minigamePrefabs[currentMinigameIndex];
        currentActiveGameInstance = Instantiate(gamePrefab, canvasTransform);

        MinigameBase gameScript = currentActiveGameInstance.GetComponent<MinigameBase>();
        if (gameScript != null)
        {
            gameScript.StartMinigame();
        }
    }

    private void HandleMinigameSuccess()
    {
        Debug.Log("미니게임 성공!");

        if (currentActiveGameInstance != null)
        {
            Destroy(currentActiveGameInstance);
            currentActiveGameInstance = null;
        }

        currentMinigameIndex++;

        StartCoroutine(LoadNextMinigame());
    }

    private IEnumerator LoadNextMinigame()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        float timer = 0f;

        while (timer < loadingDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / loadingDuration);

            if (loadingBar != null)
                loadingBar.fillAmount = progress;

            if (loadingPercentText != null)
                loadingPercentText.text =
                    Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        if (loadingBar != null)
            loadingBar.fillAmount = 1f;

        if (loadingPercentText != null)
            loadingPercentText.text = "100%";

        yield return new WaitForSeconds(0.05f);

        StartCurrentMinigame();

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void HandleMinigameFailure()
    {
        Debug.Log("미니게임 실패");
    }
}