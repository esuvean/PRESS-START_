using UnityEngine;
using System;

public abstract class MinigameBase : MonoBehaviour
{
    
    public static event Action OnGameSuccess;
    public static event Action OnGameFailure;

    // 실수(장애물 충돌, 오답 등)가 날 때마다 알림 - 샷건 연출에서 사용
    public static event Action OnMistake;

    // 제한 시간이 0이 되는 순간 한 번 알림 - 샷건 연출에서 사용
    public static event Action OnTimeOut;
    private bool timeOutReported = false;

    protected void ReportMistake()
    {
        OnMistake?.Invoke();
    }

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
        timeOutReported = false;
        Debug.Log($"{gameName} 시작 지시: {instruction}");
    }

    protected virtual void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;

        if (!timeOutReported && currentTimer <= 0f)
        {
            timeOutReported = true;
            OnTimeOut?.Invoke();
        }

       
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