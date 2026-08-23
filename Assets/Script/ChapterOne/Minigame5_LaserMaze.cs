using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame5_LaserMaze : MinigameBase, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References - Game Area")]
    public RectTransform startButton;       // 드래그할 버튼
    public RectTransform startPoint;        // 출발 위치
    public RectTransform goalPoint;         // 도착 위치
    public GameObject pathGuideLine;        // 점선 경로 가이드 (5회 실패 힌트)

    [Header("Laser Groups")]
    public List<RectTransform> course1Lasers; // 1코스 고정 레이저들
    public List<RectTransform> course2Lasers; // 2코스 깜빡이는 레이저들

    [Header("UI References - Labels")]
    public TextMeshProUGUI failText;
    public TextMeshProUGUI courseText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;

    [Header("Game Settings")]
    public float goalSnapDistance = 50f;     // GOAL 도착 인정 거리
    public float laserOnDuration = 1.5f;     // 2코스 레이저 켜짐 시간
    public float laserOffDuration = 1.5f;    // 2코스 레이저 꺼짐 시간

    // 내부 상태 변수
    private int currentCourse = 1;
    private int failCount = 0;
    private float elapsedTime = 0f;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private float hitboxShrinkRatio = 1.0f;  // 3회 실패 시 충돌 크기 감소 비율 (0.6f)
    private Coroutine laserBlinkCoroutine;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "시작 버튼 밀수 작전";
        instruction = "시작하기 버튼을 드래그해 레이저를 피해 GOAL까지 이동하세요.";

        currentCourse = 1;
        failCount = 0;
        elapsedTime = 0f;
        hitboxShrinkRatio = 1.0f;
        laserOffDuration = 1.5f;

        if (pathGuideLine != null) pathGuideLine.SetActive(false);

        SetupCourse(1);
        ResetButtonToStart();
        UpdateUI();
    }

    private void SetupCourse(int courseIndex)
    {
        currentCourse = courseIndex;

        // 코스별 레이저 그룹 활성화/비활성화
        foreach (var laser in course1Lasers)
            if (laser != null) laser.gameObject.SetActive(courseIndex == 1);

        foreach (var laser in course2Lasers)
            if (laser != null) laser.gameObject.SetActive(courseIndex == 2);

        // 2코스 깜빡임 루틴 시작
        if (laserBlinkCoroutine != null) StopCoroutine(laserBlinkCoroutine);
        if (courseIndex == 2)
        {
            laserBlinkCoroutine = StartCoroutine(BlinkCourse2Lasers());
        }

        ResetButtonToStart();
        UpdateUI();
    }

    private IEnumerator BlinkCourse2Lasers()
    {
        while (isGameActive && currentCourse == 2)
        {
            // 레이저 켜짐
            SetCourse2LasersActive(true);
            yield return new WaitForSeconds(laserOnDuration);

            // 레이저 꺼짐
            SetCourse2LasersActive(false);
            yield return new WaitForSeconds(laserOffDuration);
        }
    }

    private void SetCourse2LasersActive(bool active)
    {
        foreach (var laser in course2Lasers)
        {
            if (laser != null) laser.gameObject.SetActive(active);
        }
    }

    // 드래그 이벤트 처리
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isGameActive) return;
        isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            startButton.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        dragOffset = (Vector2)startButton.localPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isGameActive || !isDragging) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            startButton.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            startButton.localPosition = localPoint + dragOffset;
            CheckCollisions();
            CheckGoalArrival();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    // 레이저 충돌 검사
    private void CheckCollisions()
    {
        List<RectTransform> activeLasers = (currentCourse == 1) ? course1Lasers : course2Lasers;

        foreach (var laser in activeLasers)
        {
            if (laser != null && laser.gameObject.activeInHierarchy)
            {
                if (IsRectOverlapping(startButton, laser, hitboxShrinkRatio))
                {
                    OnHitLaser();
                    break;
                }
            }
        }
    }

    
    private bool IsRectOverlapping(RectTransform rectA, RectTransform rectB, float shrink)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        rectA.GetWorldCorners(cornersA);
        rectB.GetWorldCorners(cornersB);

        Bounds boundsA = new Bounds(cornersA[0], Vector3.zero);
        for (int i = 1; i < 4; i++) boundsA.Encapsulate(cornersA[i]);
        boundsA.size *= shrink; // 3회 실패 힌트 적용

        Bounds boundsB = new Bounds(cornersB[0], Vector3.zero);
        for (int i = 1; i < 4; i++) boundsB.Encapsulate(cornersB[i]);

        return boundsA.Intersects(boundsB);
    }

    private void OnHitLaser()
    {
        isDragging = false;
        failCount++;
        ResetButtonToStart();
        ApplyFailHints();
        UpdateUI();
    }

    private void CheckGoalArrival()
    {
        float dist = Vector2.Distance(startButton.position, goalPoint.position);
        if (dist <= goalSnapDistance)
        {
            isDragging = false;
            startButton.position = goalPoint.position;

            if (currentCourse == 1)
            {
                SetupCourse(2); // 2코스로 이동
            }
            else
            {
                Success(); // 최종 검사 완료
            }
        }
    }

    private void ResetButtonToStart()
    {
        if (startButton != null && startPoint != null)
        {
            startButton.position = startPoint.position;
        }
    }

    // 실패 횟수별 힌트 시스템
    private void ApplyFailHints()
    {
        if (failCount >= 8)
        {
            // 8회 실패: 레이저 꺼지는 시간 증가 
            laserOffDuration = 3.0f;
            if (hintText != null) hintText.text = "[ HINT ] 레이저가 꺼져 있는 시간이 길어집니다.";
        }
        else if (failCount >= 5)
        {
            // 5회 실패: 경로 가이드 표시
            if (pathGuideLine != null) pathGuideLine.SetActive(true);
            if (hintText != null) hintText.text = "[ HINT ] 안전한 이동 경로가 표시됩니다.";
        }
        else if (failCount >= 3)
        {
            // 3회 실패: 버튼 충돌 범위 감축
            hitboxShrinkRatio = 0.6f;
            if (hintText != null) hintText.text = "[ HINT ] 버튼의 충돌 판정 범위가 감소합니다.";
        }
        else
        {
            if (hintText != null) hintText.text = "벽과 레이저에 닿지 않게 GOAL까지 드래그하세요.";
        }
    }

    private void UpdateUI()
    {
        if (failText != null) failText.text = $"실수 {failCount} 회";
        if (courseText != null) courseText.text = $"코스 {currentCourse} / 2";
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        elapsedTime += Time.deltaTime;
        if (timerText != null)
        {
            int min = Mathf.FloorToInt(elapsedTime / 60f);
            int sec = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = string.Format("TIME {0:00}:{1:00}", min, sec);
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}