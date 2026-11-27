using UnityEngine;

/// <summary>
/// START 모이는 연출에 쓰는 글자 그림 (S, T, A, R, T)
/// Assets/Resources/C5StartMergeArt.asset 으로 저장됨 (에디터 도구 Chapter5SkinTool2630 이 자동으로 만듦)
/// StartButtonFeedback 이 모든 미니게임 성공 때 이 그림을 불러서 씀
/// </summary>
[CreateAssetMenu(fileName = "C5StartMergeArt", menuName = "PRESS START/START 모이는 연출 그림")]
public class C5StartMergeArt : ScriptableObject
{
    public Sprite[] letters = new Sprite[5];
}
