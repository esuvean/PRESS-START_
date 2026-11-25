using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One watcher on the SAME root GameObject as the minigame's MinigameBase.
// Does not modify the original minigame script, prefab image fields, or UnityEvents.
public abstract class AchievementStandaloneWatcher : MonoBehaviour
{
    [Header("Auto find on same prefab")]
    public MinigameBase game;
    protected float startedAt;
    protected bool disqualified;
    private bool observedActive;
    private bool completed;
    private bool granted;
    private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
    protected abstract int AchievementId { get; }
    protected virtual void ResetWatch() { }
    protected virtual void Poll() { }
    protected virtual bool DidMeetCondition() { return !disqualified; }

    private void Reset()
    {
        if (!game) game = GetComponent<MinigameBase>();
    }
    private void Awake()
    {
        if (!game) game = GetComponent<MinigameBase>();
        if (!game) game = GetComponentInChildren<MinigameBase>(true);
    }
    private void OnEnable()
    {
        MinigameBase.OnGameSuccess += WhenSuccess;
        MinigameBase.OnGameFailure += WhenFailure;
    }
    private void OnDisable()
    {
        MinigameBase.OnGameSuccess -= WhenSuccess;
        MinigameBase.OnGameFailure -= WhenFailure;
    }
    private void Update()
    {
        if (!game || !game.IsGameActive || completed) return;
        if (!observedActive)
        {
            observedActive = true;
            startedAt = Time.unscaledTime;
            disqualified = false;
            granted = false;
            ResetWatch();
        }
        Poll();
    }
    private void WhenSuccess()
    {
        // The game switches IsGameActive to false before firing success.
        if (!game || !observedActive || completed || game.IsGameActive || !game.gameObject.activeInHierarchy)
            return;
        completed = true;
        if (DidMeetCondition()) Grant();
    }
    private void WhenFailure()
    {
        if (game && observedActive && game.gameObject.activeInHierarchy)
            disqualified = true;
    }
    protected void Grant()
    {
        if (granted) return;
        if (AchievementManager.Instance != null)
        {
            granted = true;
            AchievementManager.Instance.Unlock(AchievementId);
        }
        else
            Debug.LogWarning("[Achievement] Missing AchievementManager: " + AchievementId, this);
    }
    protected object Field(string name)
    {
        if (!game || string.IsNullOrEmpty(name)) return null;
        if (!fields.TryGetValue(name, out FieldInfo fi))
        {
            Type t = game.GetType();
            while (t != null && fi == null)
            {
                fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }
            fields[name] = fi;
        }
        return fi != null ? fi.GetValue(game) : null;
    }
    protected T Get<T>(string name, T fallback = default(T))
    {
        object obj = Field(name);
        return obj is T val ? val : fallback;
    }
    protected float Seconds => Time.unscaledTime - startedAt;

    protected static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
    protected static bool MouseClicked()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif
    }
    protected static bool MouseWheelMoved()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.001f;
#else
        return Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f;
#endif
    }
}
