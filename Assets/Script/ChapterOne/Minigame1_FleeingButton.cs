using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class Minigame1_FleeingButton : MinigameBase, IPointerClickHandler
{
    [Header("UI Reference")]
    public RectTransform buttonTransform;
    public Button startButton;
    public TextMeshProUGUI statusText;   // 성공 횟수 텍스트 
    public TextMeshProUGUI failText;     // 실수/헛클릭 횟수 텍스트 
    public TextMeshProUGUI hintText;     // 힌트 출력 텍스트
    public TextMeshProUGUI timerText;    // 제한시간 타이머 텍스트 

   
    public float detectionRadius = 150f;
    public float fleeDistance = 200f;
    public float pauseTime = 0.4f;
   

    
    public Camera uiCamera;

    private int successCount = 0;
    private int failCount = 0;
    private bool isPaused = false;
    private float pauseTimer = 0f;
    private float remainingTime;

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "마우스 반응 검사";
        instruction = "마우스를 피해 움직이는 '시작하기' 버튼을 총 3회 클릭한다.";

        successCount = 0;
        failCount = 0;
        remainingTime = timeLimit;
        isPaused = false;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        if (hintText != null) hintText.text = "";
        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        // ★ 제한시간 카운트다운
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeOut();
            return;
        }

        if (timerText != null)
        {
            int min = Mathf.FloorToInt(remainingTime / 60f);
            int sec = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("TIME {0:00}:{1:00}", min, sec);
        }

        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0)
            {
                isPaused = false;
            }
            return;
        }

        CheckMouseProximityAndFlee();
    }

    private void CheckMouseProximityAndFlee()
    {
        Vector2 mouseScreenPos = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue();
        }
        else
        {
            mouseScreenPos = Input.mousePosition;
        }

        // ★ 버튼의 월드 좌표를 스크린 좌표로 정확히 변환하여 마우스 거리 계산
        Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonTransform.position);
        float distance = Vector2.Distance(mouseScreenPos, buttonScreenPos);

        if (distance < detectionRadius)
        {
            Flee(mouseScreenPos);
        }
    }

    private void Flee(Vector2 mouseScreenPos)
    {
        Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonTransform.position);
        Vector2 dir = (buttonScreenPos - mouseScreenPos).normalized;

        Vector2 currentPos = buttonTransform.anchoredPosition;

        switch (successCount)
        {
            case 0: // 1단계: 좌우 이동
                currentPos.x += (dir.x > 0 ? 1 : -1) * fleeDistance;
                break;

            case 1: // 2단계: 대각선 이동
                currentPos += dir * fleeDistance;
                break;

            case 2: // 3단계: 작아졌다가 돌아오는 이동
                currentPos += dir * fleeDistance;
                StartCoroutine(ShrinkAndReturnRoutine());
                break;
        }

        // ★ 버튼이 패널(화면 영역) 밖으로 나가지 않도록 가두기
        RectTransform parentRect = buttonTransform.parent as RectTransform;
        if (parentRect != null)
        {
            float limitX = (parentRect.rect.width - buttonTransform.rect.width) / 2f;
            float limitY = (parentRect.rect.height - buttonTransform.rect.height) / 2f;

            currentPos.x = Mathf.Clamp(currentPos.x, -limitX, limitX);
            currentPos.y = Mathf.Clamp(currentPos.y, -limitY, limitY);
        }

        buttonTransform.anchoredPosition = currentPos;

        isPaused = true;
        pauseTimer = pauseTime;
    }

    private System.Collections.IEnumerator ShrinkAndReturnRoutine()
    {
        buttonTransform.localScale = Vector3.one * 0.5f;
        yield return new WaitForSeconds(0.3f);
        buttonTransform.localScale = Vector3.one;
    }

    private void OnStartButtonClick()
    {
        if (!isGameActive) return;

        successCount++;
        fleeDistance += 30f;
        UpdateUI();

        if (successCount >= 3)
        {
            Success();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isGameActive) return;

        // 버튼이 아닌 패널 바탕을 헛클릭했을 때
        if (eventData.pointerCurrentRaycast.gameObject != startButton.gameObject)
        {
            failCount++;
            CheckHints();
            UpdateUI();
        }
    }

    private void CheckHints()
    {
        if (failCount == 3)
        {
            if (hintText != null) hintText.text = "[ HINT ] 버튼은 이동한 직후 잠시 멈춥니다.";
        }
        else if (failCount == 6)
        {
            pauseTime = 0.8f;
            if (hintText != null) hintText.text = "[ HINT ] 버튼 정지 시간이 증가했습니다.";
        }
        else if (failCount == 10)
        {
            fleeDistance = Mathf.Max(50f, fleeDistance - 50f);
            if (hintText != null) hintText.text = "[ HINT ] 버튼 이동 거리가 감소했습니다.";
        }
    }

    private void OnTimeOut()
    {
        isGameActive = false;
        if (hintText != null)
        {
            hintText.text = "시간 초과! 버튼을 제시간에 클릭하지 못했습니다.";
        }
    }

    private void UpdateUI()
    {
        if (statusText != null) statusText.text = $"성공 횟수 {successCount} / 3";
        if (failText != null) failText.text = $"실수 {failCount} 회";
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}