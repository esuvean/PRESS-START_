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
    public TextMeshProUGUI statusText;

    [Header("Movement Settings")]
    public float detectionRadius = 150f;
    public float fleeDistance = 200f;
    public float pauseTime = 0.4f;

    private int successCount = 0;
    private int failCount = 0;
    private bool isPaused = false;
    private float pauseTimer = 0f;

    public override void StartMinigame()
    {
        base.StartMinigame();
        gameName = "마우스 반응 검사";
        instruction = "마우스를 피해 움직이는 '시작하기' 버튼을 총 3회 클릭한다.";

        successCount = 0;
        failCount = 0;

        startButton.onClick.AddListener(OnStartButtonClick);
        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

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
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else
        {
            mousePos = Input.mousePosition;
        }

        Vector2 buttonPos = buttonTransform.position;
        float distance = Vector2.Distance(mousePos, buttonPos);

        if (distance < detectionRadius)
        {
            Flee(mousePos);
        }
    }

    private void Flee(Vector2 mousePos)
    {
        Vector2 currentPos = buttonTransform.anchoredPosition;
        Vector2 dir = ((Vector2)buttonTransform.position - mousePos).normalized;

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

        //  버튼이 패널(화면) 밖으로 나가지 않도록 가둡니다!
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
        if (eventData.pointerCurrentRaycast.gameObject != startButton.gameObject)
        {
            failCount++;
            CheckHints();
        }
    }

    private void CheckHints()
    {
        if (failCount == 3)
        {
            Debug.Log(" 힌트 (실패 3회): 버튼은 이동한 직후 잠시 멈춥니다.");
        }
        else if (failCount == 6)
        {
            pauseTime = 0.8f;
            Debug.Log(" 힌트 (실패 6회): 버튼 정지 시간이 증가했습니다.");
        }
        else if (failCount == 10)
        {
            fleeDistance = Mathf.Max(50f, fleeDistance - 50f);
            Debug.Log(" 힌트 (실패 10회): 버튼 이동 거리가 감소했습니다.");
        }
    }

    private void UpdateUI()
    {
        if (statusText != null)
            statusText.text = $"성공 횟수 {successCount} / 3";
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}