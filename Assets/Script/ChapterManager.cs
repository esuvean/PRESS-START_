using UnityEngine;
using System.Collections.Generic;

public class ChapterManager : MonoBehaviour
{
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
        // 첫 번째 챕터의 첫 게임 시작
        StartCurrentMinigame();
    }

    private void StartCurrentMinigame()
    {
        // 예외 처리 (모든 챕터를 다 깬 경우)
        if (currentChapterIndex >= chapters.Count)
        {
            Debug.Log("?? 모든 챕터를 클리어하셨습니다! 게임 클리어!");
            return;
        }

        ChapterData activeChapter = chapters[currentChapterIndex];

        if (currentMinigameIndex >= activeChapter.minigamePrefabs.Count)
        {
            // 한 챕터의 5개 게임을 다 깬 경우 다음 챕터로 이동한다
            Debug.Log($"?? {activeChapter.chapterName} 클리어! 다음 챕터로 넘어갑니다.");
            currentChapterIndex++;
            currentMinigameIndex = 0;
            StartCurrentMinigame();
            return;
        }

        // 현재 순서의 미니게임 프리팹을 화면에 생성
        GameObject gamePrefab = activeChapter.minigamePrefabs[currentMinigameIndex];
        currentActiveGameInstance = Instantiate(gamePrefab, transform);

        // 미니게임 시작 신호 주는 부분
        MinigameBase gameScript = currentActiveGameInstance.GetComponent<MinigameBase>();
        if (gameScript != null)
        {
            gameScript.StartMinigame();
        }
    }

    private void HandleMinigameSuccess()
    {
        Debug.Log(" 미니게임 성공 ");
        

        
        currentMinigameIndex++;
        Invoke(nameof(StartCurrentMinigame), 1f); // 1초 뒤 다음 게임 시작 
    }

    private void HandleMinigameFailure()
    {
        Debug.Log("미니게임 실패");
       
    }
}
