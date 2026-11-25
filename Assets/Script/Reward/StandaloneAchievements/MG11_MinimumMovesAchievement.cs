
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// 업적 13 - 미니멀리스트
// 조건: 최소 블록 이동 횟수로 클리어
// 원본 Minigame11_PathPuzzle.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG11_MinimumMovesAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 13;

    [Header("Stage 1 Minimum Moves")]
    [Min(0)]
    public int stage1MinimumMoves = 0;

    [Header("Stage 2 Minimum Moves")]
    [Min(0)]
    public int stage2MinimumMoves = 0;

    private Minigame11_PathPuzzle original;

    private int stage1Moves;
    private int stage2Moves;

    private readonly Dictionary<RectTransform, Vector2>
        dragStartPositions =
        new Dictionary<RectTransform, Vector2>();

    private readonly HashSet<RectTransform>
        registeredBlocks =
        new HashSet<RectTransform>();

    // ========================================
    // 시작 시 초기화
    // ========================================

    protected override void ResetWatch()
    {
        original = game as Minigame11_PathPuzzle;

        stage1Moves = 0;
        stage2Moves = 0;

        dragStartPositions.Clear();

        if (original == null)
        {
            Debug.LogWarning(
                "[MG11 Achievement] 원본 게임을 찾지 못했습니다."
            );
            return;
        }

        // 기존 드래그 이벤트에 업적 감지만 추가
        RegisterStage(original.stage1, 1);
        RegisterStage(original.stage2, 2);

        if (stage1MinimumMoves <= 0 ||
            stage2MinimumMoves <= 0)
        {
            Debug.LogWarning(
                "[MG11 Achievement] Inspector에서 " +
                "각 스테이지의 실제 최소 이동 횟수를 설정하세요."
            );
        }

        Debug.Log("[MG11 Achievement] 블록 이동 감지 시작");
    }

    // ========================================
    // 각 스테이지 블록 등록
    // ========================================

    private void RegisterStage(
        Minigame11_PathPuzzle.StageData stage,
        int stageNumber)
    {
        if (stage == null)
            return;

        RegisterBlocks(
            stage.horizontalBlocks,
            stageNumber
        );

        RegisterBlocks(
            stage.verticalBlocks,
            stageNumber
        );
    }

    private void RegisterBlocks(
        RectTransform[] blocks,
        int stageNumber)
    {
        if (blocks == null)
            return;

        foreach (RectTransform block in blocks)
        {
            if (block == null)
                continue;

            if (!registeredBlocks.Add(block))
                continue;

            EventTrigger trigger =
                block.GetComponent<EventTrigger>();

            if (trigger == null)
                continue;

            // 드래그 시작 위치 기록
            EventTrigger.Entry begin =
                new EventTrigger.Entry();

            begin.eventID =
                EventTriggerType.BeginDrag;

            begin.callback.AddListener(
                data => BeginBlockMove(
                    block,
                    stageNumber
                )
            );

            trigger.triggers.Add(begin);

            // 드래그 종료 위치 확인
            EventTrigger.Entry end =
                new EventTrigger.Entry();

            end.eventID =
                EventTriggerType.EndDrag;

            end.callback.AddListener(
                data => EndBlockMove(
                    block,
                    stageNumber
                )
            );

            trigger.triggers.Add(end);
        }
    }

    // ========================================
    // 블록 이동 시작
    // ========================================

    private void BeginBlockMove(
        RectTransform block,
        int stageNumber)
    {
        if (original == null ||
            !original.IsGameActive)
            return;

        if (Get<int>("currentStageNumber", 1)
            != stageNumber)
            return;

        dragStartPositions[block] =
            block.anchoredPosition;
    }

    // ========================================
    // 블록 이동 종료
    // ========================================

    private void EndBlockMove(
        RectTransform block,
        int stageNumber)
    {
        if (!dragStartPositions.TryGetValue(
            block,
            out Vector2 startPosition))
        {
            return;
        }

        dragStartPositions.Remove(block);

        if (original == null ||
            !original.IsGameActive)
            return;

        if (Get<int>("currentStageNumber", 1)
            != stageNumber)
            return;

        float distance =
            Vector2.Distance(
                startPosition,
                block.anchoredPosition
            );

        // 실제로 위치가 변하지 않았으면
        // 이동 횟수에 포함하지 않음
        if (distance <= 1f)
            return;

        if (stageNumber == 1)
        {
            stage1Moves++;

            Debug.Log(
                $"[MG11 Achievement] Stage 1 " +
                $"이동: {stage1Moves}회"
            );
        }
        else
        {
            stage2Moves++;

            Debug.Log(
                $"[MG11 Achievement] Stage 2 " +
                $"이동: {stage2Moves}회"
            );
        }
    }

    // ========================================
    // 최종 업적 판정
    // ========================================

    protected override bool DidMeetCondition()
    {
        Debug.Log(
            "[MG11 Achievement] 최종 이동 횟수 | " +
            $"Stage 1: {stage1Moves}회 / " +
            $"Stage 2: {stage2Moves}회"
        );

        if (stage1MinimumMoves <= 0 ||
            stage2MinimumMoves <= 0)
        {
            Debug.LogWarning(
                "[MG11 Achievement] " +
                "최소 이동 횟수가 설정되지 않았습니다."
            );

            return false;
        }

        bool stage1Success =
            stage1Moves <= stage1MinimumMoves;

        bool stage2Success =
            stage2Moves <= stage2MinimumMoves;

        bool success =
            !disqualified &&
            stage1Success &&
            stage2Success;

        Debug.Log(
            "[MG11 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
