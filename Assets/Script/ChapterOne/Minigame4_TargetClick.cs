using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class Minigame4_TargetClick : MinigameBase
{
    [Header("UI References - Buttons")]
    public Button[] candidateButtons;      // 12개 후보 버튼 배열
    public int realButtonIndex = 8;       

    [Header("UI References - Labels")]
    public TextMeshProUGUI failText;       // 오답 X 회
    public TextMeshProUGUI candidatesText; // CANDIDATES 12
    public TextMeshProUGUI hintText;       // 하단 힌트
    public TextMeshProUGUI timerText;      // TIME 00:00

    private int failCount = 0;
    private float elapsedTime = 0f;
    private bool isHoveringReal = false;
    private float hoverTimer = 0f;
    private bool hasTriggeredBounce = false;

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
        gameName = "진짜 시작은 하나";
        instruction = "여러 버튼 중 진짜 '시작하기' 버튼을 찾아 클릭하세요.";

        failCount = 0;
        elapsedTime = 0f;
        isHoveringReal = false;
        hoverTimer = 0f;
        hasTriggeredBounce = false;

        SetupButtons();
        UpdateUI();
    }

    private void SetupButtons()
    {
        if (candidateButtons == null) return;

        for (int i = 0; i < candidateButtons.Length; i++)
        {
            if (candidateButtons[i] == null) continue;

            candidateButtons[i].gameObject.SetActive(true);
            candidateButtons[i].transform.localScale = Vector3.one;
            candidateButtons[i].onClick.RemoveAllListeners();

            int index = i;
            candidateButtons[i].onClick.AddListener(() => OnCandidateClicked(index));

            // 호버 감지 컴포넌트 자동 첨부
            ButtonHoverDetector detector = candidateButtons[i].gameObject.GetComponent<ButtonHoverDetector>();
            if (detector == null)
            {
                detector = candidateButtons[i].gameObject.AddComponent<ButtonHoverDetector>();
            }

            detector.Init(index == realButtonIndex, OnRealButtonHover, OnRealButtonExit);
        }
    }

    private void OnCandidateClicked(int index)
    {
        if (!isGameActive) return;

        if (index == realButtonIndex)
        {
            // 진짜 버튼 클릭 성공
            Success();
        }
        else
        {
            // 가짜 버튼 클릭 실패
            failCount++;
            SplitFakeButton(index);
            CheckHintConditions();
            UpdateUI();
        }
    }

    // 가짜 버튼 분열 기믹 
    private void SplitFakeButton(int index)
    {
        Button original = candidateButtons[index];
        if (original == null || !original.gameObject.activeSelf) return;

        original.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        GameObject clone = Instantiate(original.gameObject, original.transform.parent);
        clone.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        Button cloneBtn = clone.GetComponent<Button>();
        if (cloneBtn != null)
        {
            cloneBtn.onClick.RemoveAllListeners();
            cloneBtn.onClick.AddListener(() => {
                failCount++;
                CheckHintConditions();
                UpdateUI();
            });
        }
    }

    private void OnRealButtonHover() { isHoveringReal = true; }
    private void OnRealButtonExit() { isHoveringReal = false; hoverTimer = 0f; }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        // 진짜 버튼 약 1초 호버 시 커졌다 작아지는 애니메이션
        if (isHoveringReal && !hasTriggeredBounce)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= 1.0f)
            {
                hasTriggeredBounce = true;
                StartCoroutine(BounceButtonAnimation(candidateButtons[realButtonIndex].transform));
            }
        }
    }

    private IEnumerator BounceButtonAnimation(Transform btnTransform)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.25f;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            btnTransform.localScale = Vector3.Lerp(originalScale, targetScale, t / 0.2f);
            yield return null;
        }

        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            btnTransform.localScale = Vector3.Lerp(targetScale, originalScale, t / 0.2f);
            yield return null;
        }

        btnTransform.localScale = originalScale;
    }

    private void CheckHintConditions()
    {
        if (failCount >= 8)
        {
            // 실패 8회: 가짜 버튼 일부 제거
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                if (i != realButtonIndex && i % 2 == 0 && candidateButtons[i] != null)
                {
                    candidateButtons[i].gameObject.SetActive(false);
                }
            }
            if (hintText != null) hintText.text = "[ HINT ] 가짜 버튼 일부가 제거되었습니다.";
        }
        else if (failCount >= 5)
        {
            // 실패 5회: 진짜 버튼 외곽선 강조
            StartCoroutine(FlashRealButtonOutline());
            if (hintText != null) hintText.text = "[ HINT ] 진짜 버튼의 외곽선이 깜빡입니다.";
        }
        else if (failCount >= 3)
        {
            // 실패 3회: 반응 힌트 출력
            if (hintText != null) hintText.text = "[ HINT ] 정상적인 버튼은 마우스에 반응합니다.";
        }
    }

    private IEnumerator FlashRealButtonOutline()
    {
        Button realBtn = candidateButtons[realButtonIndex];
        if (realBtn == null) yield break;

        Graphic g = realBtn.GetComponent<Graphic>();
        if (g == null) yield break;

        Color origColor = g.color;
        Color brightColor = Color.green;

        for (int i = 0; i < 3; i++)
        {
            g.color = brightColor;
            yield return new WaitForSeconds(0.2f);
            g.color = origColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void UpdateUI()
    {
        if (failText != null) failText.text = $"오답 {failCount} 회";
        if (candidatesText != null) candidatesText.text = $"CANDIDATES {candidateButtons.Length}";
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = string.Format("TIME {0:00}:{1:00}", minutes, seconds);
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}

// 호버 이벤트 전용 보조 클래스
public class ButtonHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool isReal;
    private System.Action onEnter;
    private System.Action onExit;

    public void Init(bool isRealButton, System.Action enterCallback, System.Action exitCallback)
    {
        isReal = isRealButton;
        onEnter = enterCallback;
        onExit = exitCallback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isReal && onEnter != null) onEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isReal && onExit != null) onExit.Invoke();
    }
}