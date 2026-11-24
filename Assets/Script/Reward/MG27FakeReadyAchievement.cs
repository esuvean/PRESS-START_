
using UnityEngine;
using TMPro;

public class MG27FakeReadyAchievement : MonoBehaviour
{
    [Header("MG27 UI")]
    public TMP_Text circuitStatusText;

    private bool sawSystemReady;
    private bool achievementReported;

    private void Awake()
    {
        // Inspector 연결을 놓쳤다면 자동 검색
        if (circuitStatusText == null)
        {
            TMP_Text[] texts =
                GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text.name == "CircuitStatusText")
                {
                    circuitStatusText = text;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        sawSystemReady = false;
        achievementReported = false;
    }

    private void Update()
    {
        if (achievementReported ||
            circuitStatusText == null)
        {
            return;
        }

        string status = circuitStatusText.text;

        if (string.IsNullOrEmpty(status))
            return;

        // 1. 가짜 성공 메시지 확인
        if (status.Contains("SYSTEM READY"))
        {
            sawSystemReady = true;
        }

        // 2. 가짜 성공 이후 오류가 발생하면
        // 히든 업적 29번 획득
        if (sawSystemReady &&
            status.Contains("SYSTEM ERROR"))
        {
            AchievementManager manager =
                AchievementManager.Instance;

            if (manager == null)
            {
                Debug.LogWarning(
                    "[MG27 Achievement] " +
                    "AchievementManager가 없습니다."
                );

                return;
            }

            manager.ReportMG27FakeReadySeen();

            achievementReported = true;

            Debug.Log(
                "[MG27 Achievement] " +
                "히든 업적 29번 달성 조건 확인!"
            );
        }
    }
}
