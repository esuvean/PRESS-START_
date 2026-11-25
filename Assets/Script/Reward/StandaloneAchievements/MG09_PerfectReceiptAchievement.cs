
using UnityEngine;
using System.Collections.Generic;

// 업적 12 - 선택적 수신
// 조건: 오답 수신 및 정답 놓침 없이 클리어
// 기존 Minigame9_FallingStart.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG09_PerfectReceiptAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 12;

    private Minigame9_FallingStart original;

    private readonly Dictionary<int, FallingButtonItem> tracked =
        new Dictionary<int, FallingButtonItem>();

    private readonly List<int> toRemove =
        new List<int>();

    private int disappearedStartCount;

    protected override void ResetWatch()
    {
        original = game as Minigame9_FallingStart;

        tracked.Clear();
        toRemove.Clear();

        disappearedStartCount = 0;

        Debug.Log("[MG09 Achievement] 업적 감지 시작");
    }

    protected override void Poll()
    {
        if (original == null)
            return;

        // 1. 오답을 한 번이라도 받았다면 실격
        if (Get<int>("wrongCount") > 0)
        {
            disqualified = true;
        }

        // 2. 현재 떨어지는 START 버튼 추적
        FallingButtonItem[] items =
            original.GetComponentsInChildren<FallingButtonItem>(true);

        foreach (FallingButtonItem item in items)
        {
            if (item == null)
                continue;

            bool isStart =
                StandaloneFieldUtil.ReadBool(
                    item, "isStartButton"
                );

            if (!isStart)
                continue;

            int id = item.GetHashCode();

            if (!tracked.ContainsKey(id))
            {
                tracked.Add(id, item);
            }

            // 3. START가 바닥 밖으로 떨어졌는지 확인
            RectTransform rect = item.rectTransform;

            if (rect != null && original.playArea != null)
            {
                float bottom =
                    original.playArea.rect.yMin - 100f;

                bool caught =
                    StandaloneFieldUtil.ReadBool(
                        item, "handled"
                    );

                if (!caught &&
                    rect.anchoredPosition.y < bottom)
                {
                    disqualified = true;

                    Debug.Log(
                        "[MG09 Achievement] START 놓침 - 업적 실패"
                    );
                }
            }
        }

        // 4. 사라진 START 버튼 누적 개수 계산
        // 정상 수신된 START도 삭제되므로
        // 현재까지 받은 총 개수와 비교
        toRemove.Clear();

        foreach (var pair in tracked)
        {
            if (pair.Value == null)
            {
                disappearedStartCount++;
                toRemove.Add(pair.Key);
            }
        }

        foreach (int id in toRemove)
        {
            tracked.Remove(id);
        }

        int successCount =
            Get<int>("successCount");

        // 사라진 START가 정상 수신 개수보다 많으면
        // 적어도 하나를 놓쳤다는 의미
        if (disappearedStartCount > successCount)
        {
            disqualified = true;

            Debug.Log(
                "[MG09 Achievement] START 놓침 감지"
            );
        }
    }

    protected override bool DidMeetCondition()
    {
        bool success =
            !disqualified &&
            Get<int>("wrongCount") == 0;

        Debug.Log(
            "[MG09 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
