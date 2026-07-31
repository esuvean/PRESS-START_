using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame1_FleeingButton : MinigameBase, IPointerClickHandler
{
    [Header("UI Reference")]
    public RectTransform buttonTransform;
    public Button startButton;
    public TextMeshProUGUI statusText; // 성공 횟수 0/3 표시

    [Header("Movement Settings")]
    public float detectionRadius = 150f; // 마우스 감지 거리
    public float fleeDistance = 200f;     // 도망치는 거리
    public float pauseTime = 0.4f;        // 도망 후 정지 시간

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
        Vector2 mousePos = Input.mousePosition;
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
            case 0: //  좌우로만 도망
                currentPos.x += (dir.x > 0 ? 1 : -1) * fleeDistance;
                break;

            case 1: //  화면 모서리 방향으로 도망
                currentPos += dir * fleeDistance;
                break;

            case 2: //  작아진 상태로 도망
                currentPos += dir * fleeDistance;
                StartCoroutine(ShrinkAndReturnRoutine());
                break;
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
        fleeDistance += 30f; // 난이도 상승: 성공할 때마다 속도/거리 증가
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
