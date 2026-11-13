using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 챕터 연출(샷건 / 게임오버 화면)에 필요한 그림과 글꼴 묶음.
/// Assets/Resources/ChapterFxResources.asset 에 저장되어 있고, 게임이 시작될 때 자동으로 불러온다.
/// </summary>
[CreateAssetMenu(fileName = "ChapterFxResources", menuName = "PRESS START/Chapter Fx Resources")]
public class ChapterFxResources : ScriptableObject
{
    [Tooltip("샷건(키보드 내리치기) 애니메이션 프레임 shotgun_00 ~ shotgun_15")]
    public List<Sprite> shotgunFrames = new List<Sprite>();

    [Tooltip("게임오버(TIME OVER) 화면 글꼴")]
    public TMP_FontAsset failFont;

    [Tooltip("챕터5 선택 카드 아이콘 (?) - CT5.png")]
    public Sprite chapter5QuestionIcon;

    [Tooltip("챕터5 선택 카드 아이콘 (악마) - CT5 bug.png")]
    public Sprite chapter5DevilIcon;

    [Tooltip("챕터5만 남았을 때 챕터 선택 화면 배경 - 설정배경2.png")]
    public Sprite chapter5Background;
}
