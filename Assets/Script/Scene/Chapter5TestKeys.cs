using UnityEngine;

/// <summary>
/// 챕터5 연출 테스트 키 (에디터에서만 동작, 빌드에서는 아무것도 안 함)
/// Chapter5SelectController가 메인 씬의 UIManager에 자동으로 붙여 줌.
///  F6 : 챕터1~4 클리어 상태로 만들고 챕터 선택 화면을 열어 챕터5 등장 연출 보기 (시작 화면에서 눌러도 됨)
///  F7 : 챕터5 실패 횟수 +1 (3번째부터 ? → 악마 아이콘 연출)
/// ※ 키를 누르기 전에 Game 창을 마우스로 한 번 클릭해야 키가 들어감
/// </summary>
public class Chapter5TestKeys : MonoBehaviour
{
#if UNITY_EDITOR
    private StartMenuController menu;

    private void Awake()
    {
        menu = GetComponent<StartMenuController>();
        Debug.Log("[Chapter5] 테스트 키 준비됨 - Game 창 클릭 후 F6: 챕터5 열기 / F7: 실패 +1");
    }

    private void Update()
    {
        bool f6 = Input.GetKeyDown(KeyCode.F6);
        bool f7 = Input.GetKeyDown(KeyCode.F7);
        if (!f6 && !f7)
            return;

        if (menu == null || menu.chapterSelectPanel == null)
            return;

        Chapter5SelectController ctrl = menu.chapterSelectPanel.GetComponent<Chapter5SelectController>();
        if (ctrl == null)
            ctrl = menu.chapterSelectPanel.AddComponent<Chapter5SelectController>();

        bool panelOpen = menu.chapterSelectPanel.activeInHierarchy;

        if (f6) ctrl.TestUnlock();
        if (f7) ctrl.TestAddFail();

        // 챕터 선택 화면이 아직 안 열려 있으면 열어 줌 (열리면서 연출 시작)
        if (!panelOpen)
            menu.ContinueFromSave();
    }
#endif
}
