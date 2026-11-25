
using UnityEngine;
using System.Collections;

// 업적 20 - 한 글자도 놓치지 않아
// 조건:
// 1. S-T-A-R-T 목표 문자를 첫 등장에 모두 수집
// 2. 오답 수집 없이 클리어
// 기존 Minigame26_FallingLetters.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG26_FirstCatchPerfectAchievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 20;

    private const string TargetWord = "START";

    private Minigame26_FallingLetters original;

    private int lastProgress;
    private bool firstTargetSeen;
    private GameObject firstTargetObject;
    private bool allFiveCollected;

    // =====================================
    // 미니게임 시작 시 초기화
    // =====================================

    protected override void ResetWatch()
    {
        original = game as Minigame26_FallingLetters;

        lastProgress = 0;
        firstTargetSeen = false;
        firstTargetObject = null;
        allFiveCollected = false;

        Debug.Log(
            "[MG26 Achievement] 첫 등장 수집 감지 시작"
        );
    }

    // =====================================
    // 문자 및 오류 상태 감지
    // =====================================

    protected override void Poll()
    {
        if (original == null || disqualified)
            return;

        // 잘못된 문자 수집 시 원본 게임은
        // Phase.Error로 전환됨
        object phase = Field("phase");

        if (phase != null &&
            phase.ToString() == "Error")
        {
            disqualified = true;

            Debug.Log(
                "[MG26 Achievement] 오답 수집 - 업적 실패"
            );

            return;
        }

        int progress = Get<int>("collectedCount");

        if (progress < lastProgress)
        {
            disqualified = true;
            return;
        }

        // 목표 문자 수집에 성공한 경우
        if (progress > lastProgress)
        {
            // 첫 등장 문자를 확인하지 못했거나
            // 그 문자가 여전히 떨어지고 있으면 실패
            if (!firstTargetSeen ||
                IsFirstTargetStillFalling())
            {
                disqualified = true;

                Debug.Log(
                    "[MG26 Achievement] 첫 등장 문자 " +
                    "수집 조건 실패"
                );
            }

            if (progress >= TargetWord.Length)
            {
                allFiveCollected = true;
            }

            // 다음 목표 문자 추적 준비
            firstTargetSeen = false;
            firstTargetObject = null;
        }

        lastProgress = progress;

        if (progress >= TargetWord.Length)
            return;

        // 수집 단계에서만 검사
        if (phase == null ||
            phase.ToString() != "Catching")
            return;

        IEnumerable letters =
            Field("activeLetters") as IEnumerable;

        if (letters == null)
            return;

        bool firstStillPresent = false;
        GameObject newFirstTarget = null;

        foreach (object item in letters)
        {
            char character =
                StandaloneFieldUtil.ReadChar(
                    item, "character"
                );

            GameObject obj =
                StandaloneFieldUtil.ReadGameObject(
                    item, "obj"
                );

            if (obj == null)
                continue;

            // 이전에 추적한 첫 등장 문자인지 확인
            if (firstTargetSeen &&
                ReferenceEquals(obj, firstTargetObject))
            {
                firstStillPresent = true;
            }

            // 아직 목표 문자의 첫 등장을
            // 기록하지 않았다면 등록
            if (!firstTargetSeen &&
                newFirstTarget == null &&
                character == TargetWord[progress])
            {
                newFirstTarget = obj;
            }
        }

        if (firstTargetSeen)
        {
            // 수집 개수가 증가하지 않았는데
            // 첫 등장 문자가 사라졌다면 놓친 것
            if (!firstStillPresent ||
                firstTargetObject == null ||
                !firstTargetObject.activeSelf)
            {
                disqualified = true;

                Debug.Log(
                    "[MG26 Achievement] 첫 등장 목표 문자 놓침"
                );
            }
        }
        else if (newFirstTarget != null)
        {
            firstTargetSeen = true;
            firstTargetObject = newFirstTarget;

            Debug.Log(
                "[MG26 Achievement] 목표 문자 " +
                TargetWord[progress] +
                " 첫 등장 확인"
            );
        }
    }

    // =====================================
    // 첫 등장 문자가 아직 화면에 있는지
    // =====================================

    private bool IsFirstTargetStillFalling()
    {
        if (firstTargetObject == null)
            return false;

        IEnumerable letters =
            Field("activeLetters") as IEnumerable;

        if (letters == null)
            return false;

        foreach (object item in letters)
        {
            GameObject obj =
                StandaloneFieldUtil.ReadGameObject(
                    item, "obj"
                );

            if (ReferenceEquals(obj, firstTargetObject))
                return true;
        }

        return false;
    }

    // =====================================
    // 최종 클리어 시 판정
    // =====================================

    protected override bool DidMeetCondition()
    {
        // 최종 상태를 한 번 더 확인
        Poll();

        bool success =
            !disqualified &&
            allFiveCollected &&
            Get<int>("collectedCount") == 5;

        Debug.Log(
            "[MG26 Achievement] 최종 판정: " +
            (success ? "업적 달성" : "업적 미달성")
        );

        return success;
    }
}
