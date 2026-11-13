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

    // F6/F7 키는 이제 Chapter5Shortcut 이 처리함 (어느 씬에서도 동작, 두 번 눌리는 것 방지)
#endif
}
