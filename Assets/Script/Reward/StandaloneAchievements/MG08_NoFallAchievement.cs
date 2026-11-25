
using UnityEngine;

// 업적 11 - 흔들림 없는 손
// 조건: MG08에서 게임판 밖으로 추락하지 않고 클리어
// 기존 미니게임 스크립트 수정 불필요

[DisallowMultipleComponent]
public sealed class MG08_NoFallAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 11;

    private Minigame8_TiltBoard original;
    private bool hasFallen;

    // =====================================
    // 시작 시 초기화
    // =====================================

    protected override void ResetWatch()
    {
        original = game as Minigame8_TiltBoard;
        hasFallen = false;

        Debug.Log("[MG08 Achievement] 추락 감지 시작");
    }

    // =====================================
    // 게임판 밖으로 추락했는지 검사
    // =====================================

    protected override void Poll()
    {
        if (hasFallen || original == null)
            return;

        if (original.board == null ||
            original.movingButton == null)
            return;

        Vector2 pos =
            original.movingButton.anchoredPosition;

        float halfW =
            original.movingButton.rect.width * 0.5f;

        float halfH =
            original.movingButton.rect.height * 0.5f;

        float minX = original.board.rect.xMin - halfW;
        float maxX = original.board.rect.xMax + halfW;

        float minY = original.board.rect.yMin - halfH;
        float maxY = original.board.rect.yMax + halfH;

        // 원본 MG08과 동일한 게임판 이탈 기준
        if (pos.x < minX ||
            pos.x > maxX ||
            pos.y < minY ||
            pos.y > maxY)
        {
            hasFallen = true;

            Debug.Log(
                "[MG08 Achievement] 게임판 밖으로 추락! " +
                "업적 획득 불가"
            );
        }
    }

    // =====================================
    // 클리어 시 업적 판정
    // =====================================

    protected override bool DidMeetCondition()
    {
        bool success = !hasFallen;

        Debug.Log(
            "[MG08 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
