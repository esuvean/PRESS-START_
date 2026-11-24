
using UnityEngine;

public class MG25PopupAchievement : MonoBehaviour
{
    [Header("MG25 UI")]
    [Tooltip("MG25의 PopupLayer 오브젝트를 연결하세요.")]
    public RectTransform popupLayer;

    private bool achievementReported;

    private void Awake()
    {
        FindPopupLayer();
    }

    private void OnEnable()
    {
        achievementReported = false;

        MinigameBase.OnGameSuccess +=
            CheckPopupAchievement;
    }

    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -=
            CheckPopupAchievement;
    }

    // ====================================
    // 팝업 레이어 자동 검색
    // ====================================

    private void FindPopupLayer()
    {
        if (popupLayer != null)
            return;

        RectTransform[] rects =
            GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rect in rects)
        {
            if (rect.name == "PopupLayer")
            {
                popupLayer = rect;
                return;
            }
        }
    }

    // ====================================
    // MG25 성공 시 업적 판정
    // ====================================

    private void CheckPopupAchievement()
    {
        if (!isActiveAndEnabled ||
            achievementReported)
        {
            return;
        }

        FindPopupLayer();

        if (popupLayer == null)
        {
            Debug.LogWarning(
                "[MG25 Achievement] PopupLayer가 없습니다.",
                this
            );

            return;
        }

        // 실제로 생성되어 화면에 남아 있는
        // MG25 팝업만 검사
        MG25PopupWindow[] popupWindows =
            popupLayer.GetComponentsInChildren<
                MG25PopupWindow>(false);

        int remainingCount = 0;

        foreach (MG25PopupWindow popup in popupWindows)
        {
            if (popup != null &&
                popup.gameObject.activeInHierarchy)
            {
                remainingCount++;
            }
        }

        Debug.Log(
            "[MG25 Achievement] 클리어 시 남은 팝업: " +
            remainingCount + "개"
        );

        // 팝업을 하나라도 남기고 클리어
        if (remainingCount <= 0)
            return;

        AchievementManager manager =
            AchievementManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                "[MG25 Achievement] " +
                "AchievementManager가 없습니다."
            );

            return;
        }

        manager.ReportMinigameClear(
            25,
            popupRemaining: true
        );

        achievementReported = true;

        Debug.Log(
            "[MG25 Achievement] " +
            "히든 업적 28번 달성 조건 확인!"
        );
    }
}
