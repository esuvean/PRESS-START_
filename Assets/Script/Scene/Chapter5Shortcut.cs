using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 챕터5 바로가기 단축키 (씬에 따로 넣을 필요 없음 - 게임이 켜지면 스스로 생김)
///
///  F5 : 어느 화면에서든 바로 챕터5로 이동해서 플레이
///       → 챕터1~4는 다 깬 것으로 처리 (챕터5 등장 연출도 이미 본 것으로)
///       → 챕터5는 MG21부터 처음 시작
///
/// F5 는 에디터에서도, 빌드한 게임에서도 동작함 (발표/시연용)
///
/// [유니티 에디터에서만]
///  F6 : 챕터1~4 클리어 상태로 → 챕터 선택 화면에서 챕터5 등장 연출 (어느 씬에서 눌러도 메인으로 가서 보여 줌)
///  F7 : 챕터5 실패 횟수 +1 (3번째부터 ? → 악마 아이콘 + B곡)
/// ※ 에디터에서는 키를 누르기 전에 Game 창을 마우스로 한 번 클릭해야 키가 들어감
/// </summary>
public class Chapter5Shortcut : MonoBehaviour
{
    public const KeyCode GoKey = KeyCode.F5;
    private const string Chapter5Scene = "Chapter5";

    private static Chapter5Shortcut instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("[Chapter5Shortcut]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Chapter5Shortcut>();
        Debug.Log("[Chapter5] 단축키 준비됨 - F5: 바로 챕터5 플레이 / (에디터) F6: 챕터5 등장 연출, F7: 실패 +1");
    }

    private void Update()
    {
        if (Input.GetKeyDown(GoKey))
            GoToChapter5();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F6))
            TestRevealChapter5();
        if (Input.GetKeyDown(KeyCode.F7))
            TestAddFail();
#endif
    }

#if UNITY_EDITOR
    // F6: 챕터1~4 클리어 → 챕터 선택 화면에서 챕터5 등장 연출
    private static void TestRevealChapter5()
    {
        Time.timeScale = 1f;
        StartMenuController menu = FindMenu();

        if (menu == null)
        {
            // 챕터 씬 등 메인이 아닌 곳: 클리어 상태로 만들고 메인의 챕터 선택 화면으로
            for (int i = 1; i <= 4; i++)
            {
                SessionProgress.SetProgress(i, 5);
                SessionProgress.CompleteChapter(i);
            }
            SessionProgress.chapter5Revealed = false;
            SessionProgress.openChapterSelectOnLoad = true;
            Debug.Log("[테스트] F6 → 메인 화면으로 가서 챕터5 등장 연출");
            SceneManager.LoadScene("MainScene");
            return;
        }

        bool panelOpen = menu.chapterSelectPanel.activeInHierarchy;
        GetController(menu).TestUnlock();

        if (!panelOpen)
            menu.ContinueFromSave();
    }

    // F7: 챕터5 실패 횟수 +1
    private static void TestAddFail()
    {
        StartMenuController menu = FindMenu();
        if (menu != null)
        {
            GetController(menu).TestAddFail(); // 화면이 열려 있으면 바로 아이콘 연출
            return;
        }

        SessionProgress.chapter5FailCount++;
        Debug.Log($"[테스트] F7 → 챕터5 실패 횟수: {SessionProgress.chapter5FailCount}");
    }

    private static StartMenuController FindMenu()
    {
        StartMenuController menu = FindAnyObjectByType<StartMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.chapterSelectPanel == null)
            return null;
        return menu;
    }

    private static Chapter5SelectController GetController(StartMenuController menu)
    {
        Chapter5SelectController ctrl = menu.chapterSelectPanel.GetComponent<Chapter5SelectController>();
        if (ctrl == null)
            ctrl = menu.chapterSelectPanel.AddComponent<Chapter5SelectController>();
        return ctrl;
    }
#endif

    /// <summary>챕터1~4를 깬 상태로 만들고 챕터5를 처음부터 시작</summary>
    public static void GoToChapter5()
    {
        for (int i = 1; i <= 4; i++)
        {
            SessionProgress.SetProgress(i, 5);
            SessionProgress.CompleteChapter(i);
        }

        // 챕터5는 처음(MG21)부터
        SessionProgress.ResetChapterProgress(5);
        SessionProgress.chapter5Revealed = true;
        SessionProgress.openChapterSelectOnLoad = false;

        Time.timeScale = 1f;
        Debug.Log("[Chapter5] F5 → 챕터5로 이동");
        SceneManager.LoadScene(Chapter5Scene);
    }
}
