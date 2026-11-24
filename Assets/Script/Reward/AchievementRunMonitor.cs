using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Attach this once to ChapterManager.
// Its event-driven checks run only when relevant minigame signals arrive.
public enum AchievementAction
{
    // Opt in only after a minigame actually reports all hints / wrong clicks / lost balls.
    // Prevents awarding "perfect run" achievements before their hooks are installed.
    NegativeConditionTrackingEnabled,
    HintUsed,
    MG02CenterSuccess,
    MG02CenterFailed,
    MG04FakeButtonClicked,
    MG07MouseWheelUsed,
    MG08Accepted,
    MG09WrongReceipt,
    MG11OptimalMovesVerified,
    MG12BallLost,
    MG14WrongConnectionOrCorrection,
    MG19RunSucceeded,
    MG19RunFailed,
    MG21HiddenStartFoundBeforeEnd,
    MG26FirstAppearanceTargetCaught,
    MG26WrongCatchOrTargetMissed,
    MG03WrongPlacement,
    MG06TrapPopupTriggered,
    MG13BadPartSelected,
    MG17SmallTextInstructionViolated,
    MG18WrongConnection,
    MG23WrongOrder,
    MG24RapidClick,
    MG25PopupLeftOpen,
    MG27FakeSuccessShown
}

[DisallowMultipleComponent]
public sealed class AchievementRunMonitor : MonoBehaviour
{
    private static int activeMinigame;
    private static float startTime;
    private static bool finished;
    private static bool usedHint;
    private static bool negativeConditionTrackingEnabled;
    private static bool violated;
    private static bool optimalMovesVerified;
    private static bool firstRunSucceeded;
    private static bool runPressed;
    private static bool hiddenStartFoundEarly;
    private static bool popupLeftOpen;
    private static int centerHits;
    private static int goodFirstCatches;
    private static int rapidClickStreak;
    private static float lastRapidClickTime;
    private static float idleFrom;
    private static bool idleTenSeconds;
    private static Vector2 lastMousePosition;
    private static int chapterFiveFailures;

    private static int wrongPlacementCount;
    private static int trapPopupCount;
    private static int badPartCount;
    private static int smallTextViolations;
    private static int wrongConnectionCount;
    private static int wrongOrderCount;

    private void OnEnable()
    {
        MinigameBase.OnGameSuccess += OnMinigameSuccess;
        MinigameBase.OnGameFailure += OnMinigameFailure;
    }

    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -= OnMinigameSuccess;
        MinigameBase.OnGameFailure -= OnMinigameFailure;
    }

    // Called from ChapterManager after Instantiate, before StartMinigame().
    public static void Begin(MinigameBase game)
    {
        if (game == null) return;
        string typeName = game.GetType().Name;
        const string prefix = "Minigame";
        int pos = typeName.IndexOf(prefix, System.StringComparison.OrdinalIgnoreCase);
        if (pos < 0)
        {
            Debug.LogWarning("[Achievements] Cannot determine minigame ID from " + typeName);
            activeMinigame = 0;
            return;
        }
        pos += prefix.Length;
        int end = pos;
        while (end < typeName.Length && char.IsDigit(typeName[end])) end++;
        if (!int.TryParse(typeName.Substring(pos, end - pos), out activeMinigame))
        {
            Debug.LogWarning("[Achievements] Cannot parse minigame ID from " + typeName);
            activeMinigame = 0;
            return;
        }

        finished = false;
        startTime = Time.time;
        usedHint = false;
        negativeConditionTrackingEnabled = false;
        violated = false;
        optimalMovesVerified = false;
        firstRunSucceeded = false;
        runPressed = false;
        hiddenStartFoundEarly = false;
        popupLeftOpen = false;
        centerHits = 0;
        goodFirstCatches = 0;
        rapidClickStreak = 0;
        lastRapidClickTime = -100f;
        idleFrom = Time.time;
        idleTenSeconds = false;
        lastMousePosition = GetMousePosition();
        wrongPlacementCount = 0;
        trapPopupCount = 0;
        badPartCount = 0;
        smallTextViolations = 0;
        wrongConnectionCount = 0;
        wrongOrderCount = 0;
    }

    public static void Report(AchievementAction action)
    {
        if (finished || activeMinigame == 0) return;

        // Actions from the wrong minigame must never contribute.
        switch (action)
        {
            case AchievementAction.NegativeConditionTrackingEnabled:
                negativeConditionTrackingEnabled = true; break;
            case AchievementAction.HintUsed:
                usedHint = true; break;
            case AchievementAction.MG02CenterSuccess:
                if (activeMinigame == 2) centerHits++;
                break;
            case AchievementAction.MG02CenterFailed:
                if (activeMinigame == 2) violated = true;
                break;
            case AchievementAction.MG04FakeButtonClicked:
                if (activeMinigame == 4) violated = true;
                break;
            case AchievementAction.MG07MouseWheelUsed:
                if (activeMinigame == 7) violated = true;
                break;
            case AchievementAction.MG08Accepted:
                if (activeMinigame == 8) violated = true;
                break;
            case AchievementAction.MG09WrongReceipt:
                if (activeMinigame == 9) violated = true;
                break;
            case AchievementAction.MG11OptimalMovesVerified:
                if (activeMinigame == 11) optimalMovesVerified = true;
                break;
            case AchievementAction.MG12BallLost:
                if (activeMinigame == 12) violated = true;
                break;
            case AchievementAction.MG14WrongConnectionOrCorrection:
                if (activeMinigame == 14) violated = true;
                break;
            case AchievementAction.MG19RunSucceeded:
                if (activeMinigame == 19 && !runPressed)
                { runPressed = true; firstRunSucceeded = true; }
                break;
            case AchievementAction.MG19RunFailed:
                if (activeMinigame == 19) runPressed = true;
                break;
            case AchievementAction.MG21HiddenStartFoundBeforeEnd:
                if (activeMinigame == 21) hiddenStartFoundEarly = true;
                break;
            case AchievementAction.MG26FirstAppearanceTargetCaught:
                if (activeMinigame == 26) goodFirstCatches++;
                break;
            case AchievementAction.MG26WrongCatchOrTargetMissed:
                if (activeMinigame == 26) violated = true;
                break;
            case AchievementAction.MG03WrongPlacement:
                if (activeMinigame == 3 && ++wrongPlacementCount >= 3) Unlock(21);
                break;
            case AchievementAction.MG06TrapPopupTriggered:
                if (activeMinigame == 6 && ++trapPopupCount >= 3) Unlock(22);
                break;
            case AchievementAction.MG13BadPartSelected:
                if (activeMinigame == 13 && ++badPartCount >= 3) Unlock(23);
                break;
            case AchievementAction.MG17SmallTextInstructionViolated:
                if (activeMinigame == 17 && ++smallTextViolations >= 3) Unlock(24);
                break;
            case AchievementAction.MG18WrongConnection:
                if (activeMinigame == 18 && ++wrongConnectionCount >= 3) Unlock(25);
                break;
            case AchievementAction.MG23WrongOrder:
                if (activeMinigame == 23 && ++wrongOrderCount >= 3) Unlock(26);
                break;
            case AchievementAction.MG24RapidClick:
                if (activeMinigame == 24)
                {
                    float now = Time.unscaledTime;
                    rapidClickStreak = (now - lastRapidClickTime <= 0.35f)
                        ? rapidClickStreak + 1 : 1;
                    lastRapidClickTime = now;
                    if (rapidClickStreak >= 20) Unlock(27);
                }
                break;
            case AchievementAction.MG25PopupLeftOpen:
                if (activeMinigame == 25) popupLeftOpen = true;
                break;
            case AchievementAction.MG27FakeSuccessShown:
                if (activeMinigame == 27) Unlock(29);
                break;
        }
    }

    private void Update()
    {
        // Only MG16 needs a continuous input check. Zero mouse polling on the other 29 games.
        if (activeMinigame != 16 || finished || idleTenSeconds) return;
        Vector2 pos = GetMousePosition();
        bool changed = (pos - lastMousePosition).sqrMagnitude > 0.01f;
        lastMousePosition = pos;
        if (changed || IsMousePressed()) idleFrom = Time.time;
        if (Time.time - idleFrom >= 10f) idleTenSeconds = true;
    }

    private static Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private static bool IsMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        return mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
#else
        return false;
#endif
    }

    private void OnMinigameSuccess()
    {
        if (finished || activeMinigame <= 0) return;
        finished = true;
        float seconds = Time.time - startTime;
        switch (activeMinigame)
        {
            case 1: if (negativeConditionTrackingEnabled && seconds <= 20f && !usedHint) Unlock(7); break;
            case 2: if (centerHits == 3 && !violated) Unlock(8); break;
            case 4: if (negativeConditionTrackingEnabled && seconds <= 15f && !violated) Unlock(9); break;
            case 7: if (negativeConditionTrackingEnabled && !violated) Unlock(10); break;
            case 8: if (negativeConditionTrackingEnabled && !violated) Unlock(11); break;
            case 9: if (negativeConditionTrackingEnabled && !violated) Unlock(12); break;
            case 11: if (optimalMovesVerified) Unlock(13); break;
            case 12: if (negativeConditionTrackingEnabled && !violated) Unlock(14); break;
            case 14: if (negativeConditionTrackingEnabled && !violated) Unlock(15); break;
            case 16: if (idleTenSeconds) Unlock(16); break;
            case 19: if (firstRunSucceeded) Unlock(17); break;
            case 21: if (hiddenStartFoundEarly) Unlock(18); break;
            case 22: if (negativeConditionTrackingEnabled && seconds <= 90f && !usedHint) Unlock(19); break;
            case 26: if (goodFirstCatches == 5 && !violated) Unlock(20); break;
            case 25: if (popupLeftOpen) Unlock(28); break;
        }
    }

    private void OnMinigameFailure()
    {
        if (finished || activeMinigame <= 0) return;
        finished = true;
        if (activeMinigame >= 21 && activeMinigame <= 30)
            chapterFiveFailures++;
    }

    // Call ONLY when an ending scene actually becomes visible to the player.
    public static void ReportEndingShown(bool devilEnding)
    {
        if (devilEnding)
        {
            if (chapterFiveFailures >= 3) Unlock(30);
        }
        else
        {
            Unlock(6);
        }
    }

    // Reset failure attempts when beginning an entirely NEW game (not a retry).
    public static void ResetChapterFiveFailureCount() => chapterFiveFailures = 0;

    private static void Unlock(int id)
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.Unlock(id);
        else
            Debug.LogWarning("[Achievements] AchievementManager.Instance is missing. Achievement " + id);
    }
}
