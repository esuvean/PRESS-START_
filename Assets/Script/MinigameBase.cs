using UnityEngine;
using System;

public abstract class MinigameBase : MonoBehaviour
{
    
    public static event Action OnGameSuccess;
    public static event Action OnGameFailure;

    [Header("Game Settings")]
    public string gameName;          // 검사 명칭
    public string instruction;       // 간단한 지시문
    public float timeLimit = 60f;     // 각 유형별 목표 시간 

    protected float currentTimer;
    protected bool isGameActive = false;
    private bool hintGiven = false;

    // 챕터 매니저에 의해 게임이 시작될 때 호출됨
    public virtual void StartMinigame()
    {
        currentTimer = timeLimit;
        isGameActive = true;
        hintGiven = false;
        Debug.Log($"{gameName} 시작 지시: {instruction}");
    }

    protected virtual void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;

       
        if (timeLimit - currentTimer >= 120f && !hintGiven)
        {
            GiveHint();
        }
    }

    // 미니게임 성공 시 호출할 메서드
    protected void Success()
    {
        if (!isGameActive) return;
        isGameActive = false;
        OnGameSuccess?.Invoke();
        Destroy(gameObject);     // 플레이 영역에서 제거
    }

    // 미니게임 실패 시 호출할 메서드 
    protected void Fail()
    {
        OnGameFailure?.Invoke();
        RestartGame();
    }

    protected abstract void GiveHint();   
    protected abstract void RestartGame(); 
}