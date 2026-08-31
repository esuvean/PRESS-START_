using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame5_LaserMaze : MinigameBase
{
    [Header("UI References - Game Area")]
    public RectTransform mazeArea;          // 미로 전체 영역 (초록색 테두리 상자)
    public RectTransform startButton;       // 드래그할 버튼
    public RectTransform startPoint;        // 출발 위치
    public RectTransform goalPoint;         // 도착 위치
    public GameObject pathGuideLine;        // 5회 실패 힌트 점선

    [Header("Laser Groups")]
    public List<RectTransform> course1Lasers;
    public List<RectTransform> course2Lasers;

    [Header("UI References - Labels")]
    public TextMeshProUGUI failText;
    public TextMeshProUGUI courseText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;

    [Header("Game Settings")]
    public float goalSnapDistance = 50f;
    public float laserOnDuration = 1.5f;
    public float laserOffDuration = 1.5f;
    public float timeLimit = 60f;           // ★ 제한시간 설정 (기본값 60초)

    private int currentCourse = 1;
    private int failCount = 0;
    private float remainingTime;            // ★ 남은 시간 카운트다운 변수
    private float hitboxShrinkRatio = 1.0f;
    private Coroutine laserBlinkCoroutine;

    public bool IsGameActive()
    {
        return isGameActive;
    }

    private void Start()
    {
        if (!isGameActive) StartMinigame();
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "안전 이동 검사";
        instruction = "'START' 버튼을 드래그하여 레이저를 피해 출발 지점에서 도착 지점까지 이동시키기.";

        currentCourse = 1;
        failCount = 0;
        remainingTime = timeLimit; // 제한시간으로 초기화
        hitboxShrinkRatio = 1.0f;
        laserOffDuration = 1.5f;

        if (pathGuideLine != null) pathGuideLine.SetActive(false);

        SetupCourse(1);
    }

    private void SetupCourse(int courseIndex)
    {
        currentCourse = courseIndex;

        foreach (var laser in course1Lasers)
            if (laser != null) laser.gameObject.SetActive(courseIndex == 1);

        foreach (var laser in course2Lasers)
            if (laser != null) laser.gameObject.SetActive(courseIndex == 2);

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
            SetCourse2LasersActive(true);
            yield return new WaitForSeconds(laserOnDuration);
            SetCourse2LasersActive(false);
            yield return new WaitForSeconds(laserOffDuration);
        }
    }

    private void SetCourse2LasersActive(bool active)
    {
        foreach (var laser in course2Lasers)
            if (laser != null) laser.gameObject.SetActive(active);
    }

    public void CheckCollisions()
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
        boundsA.size *= shrink;

        Bounds boundsB = new Bounds(cornersB[0], Vector3.zero);
        for (int i = 1; i < 4; i++) boundsB.Encapsulate(cornersB[i]);

        return boundsA.Intersects(boundsB);
    }

    private void OnHitLaser()
    {
        failCount++;
        ResetButtonToStart();
        ApplyFailHints();
        UpdateUI();
    }

    public void CheckGoalArrival()
    {
        if (goalPoint == null || startButton == null) return;

        float dist = Vector2.Distance(startButton.position, goalPoint.position);
        if (dist <= goalSnapDistance)
        {
            startButton.position = goalPoint.position;

            if (currentCourse == 1)
            {
                SetupCourse(2);
            }
            else
            {
                Success();
            }
        }
    }

    public void ResetButtonToStart()
    {
        if (startButton != null && startPoint != null)
        {
            startButton.position = startPoint.position;

            var dragComp = startButton.GetComponent<LaserMazeButtonDrag>();
            if (dragComp != null) dragComp.ClampToMazeArea();
        }
    }

    private void ApplyFailHints()
    {
        if (failCount >= 8)
        {
            laserOffDuration = 3.0f;
            if (hintText != null) hintText.text = "[ HINT ] 레이저가 꺼져 있는 시간이 길어집니다.";
        }
        else if (failCount >= 5)
        {
            if (pathGuideLine != null) pathGuideLine.SetActive(true);
            if (hintText != null) hintText.text = "[ HINT ] 안전한 이동 경로가 표시됩니다.";
        }
        else if (failCount >= 3)
        {
            hitboxShrinkRatio = 0.6f;
            if (hintText != null) hintText.text = "[ HINT ] 버튼의 충돌 판정 범위가 감소합니다.";
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

        // 남은 시간 카운트다운
        remainingTime -= Time.deltaTime;

        // 0초 이하로 떨어졌을 때 처리
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeOut();
        }

        // UI 표시 (MM:SS)
        if (timerText != null)
        {
            int min = Mathf.FloorToInt(remainingTime / 60f);
            int sec = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("TIME {0:00}:{1:00}", min, sec);
        }
    }

    private void OnTimeOut()
    {
        isGameActive = false;
        if (hintText != null)
        {
            hintText.text = "시간 초과! 실패했습니다.";
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}