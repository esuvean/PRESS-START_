using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class Minigame3_MashButton : MinigameBase
{
    [Header("UI References")]
    public RectTransform[] slotTransforms;
    public RectTransform[] pieceTransforms;
    public TextMeshProUGUI[] slotSilhouettes;

    public Button fullStartButton;

    public TextMeshProUGUI failText;       // 실수 X 회
    public TextMeshProUGUI piecesText;     // PIECES X / N
    public TextMeshProUGUI hintText;       // 하단 힌트 텍스트
    public TextMeshProUGUI timerText;      // 우상단 타이머 (TIME 00:00)

    [Header("Settings")]
    public float snapDistance = 100f;      // 슬롯 흡착 인식 거리
    public Vector3 escapeOffset = new Vector3(120f, 60f, 0f); // 마지막 조각 도망 위치 오프셋

    // 내부 상태 변수
    private Vector3[] initialPiecePositions;
    private bool[] isPlaced;
    private int failCount = 0;
    private bool hasEscapedOnce = false;
    private bool isButtonCompleted = false;

    // ★ [타이머 관리 변수]
    private float remainingTime;
    private float elapsedTime = 0f;

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
        gameName = "조각난 시작";
        instruction = "흩어진 버튼 조각을 드래그해 'START' 버튼을 완성하세요.";

        int pieceCount = pieceTransforms != null ? pieceTransforms.Length : 5;

        failCount = 0;
        elapsedTime = 0f;

        // ★ [추가] 제한시간 초기화 (부모 MinigameBase의 timeLimit 적용)
        remainingTime = timeLimit;

        hasEscapedOnce = false;
        isButtonCompleted = false;

        // 개수에 맞게 배열 크기 동적 생성
        initialPiecePositions = new Vector3[pieceCount];
        isPlaced = new bool[pieceCount];

        for (int i = 0; i < pieceCount; i++)
        {
            if (pieceTransforms[i] != null)
            {
                if (initialPiecePositions[i] == Vector3.zero)
                {
                    initialPiecePositions[i] = pieceTransforms[i].position;
                }
                else
                {
                    pieceTransforms[i].position = initialPiecePositions[i];
                }
                pieceTransforms[i].gameObject.SetActive(true);
            }

            if (slotTransforms != null && i < slotTransforms.Length && slotTransforms[i] != null)
            {
                slotTransforms[i].gameObject.SetActive(true);
            }

            isPlaced[i] = false;
        }

        // 통합 완성 버튼 초기화
        if (fullStartButton != null)
        {
            fullStartButton.gameObject.SetActive(false);
            fullStartButton.onClick.RemoveAllListeners();
            fullStartButton.onClick.AddListener(OnFullStartButtonClicked);
        }

        SetupDragEvents();
        UpdateUI();
        UpdateSilhouettes();
    }

    private void SetupDragEvents()
    {
        if (pieceTransforms == null) return;

        for (int i = 0; i < pieceTransforms.Length; i++)
        {
            if (pieceTransforms[i] == null) continue;

            EventTrigger trigger = pieceTransforms[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = pieceTransforms[i].gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();

            int index = i;

            // 드래그 시작
            EventTrigger.Entry entryBegin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entryBegin.callback.AddListener((data) => { OnBeginDragPiece(index, (PointerEventData)data); });
            trigger.triggers.Add(entryBegin);

            // 드래그 중
            EventTrigger.Entry entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entryDrag.callback.AddListener((data) => { OnDragPiece(index, (PointerEventData)data); });
            trigger.triggers.Add(entryDrag);

            // 드래그 종료
            EventTrigger.Entry entryEnd = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            entryEnd.callback.AddListener((data) => { OnEndDragPiece(index, (PointerEventData)data); });
            trigger.triggers.Add(entryEnd);
        }
    }

    private void OnBeginDragPiece(int index, PointerEventData data)
    {
        if (!isGameActive || isButtonCompleted) return;
        if (isPlaced[index]) return;

        pieceTransforms[index].SetAsLastSibling();
    }

    private void OnDragPiece(int index, PointerEventData data)
    {
        if (!isGameActive || isButtonCompleted) return;
        if (isPlaced[index]) return;

        pieceTransforms[index].position = data.position;
    }

    private void OnEndDragPiece(int index, PointerEventData data)
    {
        if (!isGameActive || isButtonCompleted) return;
        if (isPlaced[index]) return;

        float distToCorrectSlot = Vector2.Distance(pieceTransforms[index].position, slotTransforms[index].position);

        bool droppedNearWrongSlot = false;
        for (int j = 0; j < slotTransforms.Length; j++)
        {
            if (j == index) continue;
            if (slotTransforms[j] != null && Vector2.Distance(pieceTransforms[index].position, slotTransforms[j].position) < snapDistance)
            {
                droppedNearWrongSlot = true;
                break;
            }
        }

        if (distToCorrectSlot <= snapDistance)
        {
            SnapPieceToSlot(index);
        }
        else
        {
            ReturnPieceToInitial(index);

            if (droppedNearWrongSlot || distToCorrectSlot < snapDistance * 2f)
            {
                failCount++;
                UpdateSilhouettes();
                UpdateUI();
            }
        }
    }

    private void SnapPieceToSlot(int index)
    {
        pieceTransforms[index].position = slotTransforms[index].position;
        isPlaced[index] = true;

        CheckAssemblyProgress();
    }

    private void ReturnPieceToInitial(int index)
    {
        pieceTransforms[index].position = initialPiecePositions[index];
    }

    private void CheckAssemblyProgress()
    {
        int placedCount = GetPlacedCount();
        int totalCount = pieceTransforms != null ? pieceTransforms.Length : 5;
        UpdateUI();

        if (placedCount == totalCount)
        {
            if (!hasEscapedOnce)
            {
                StartCoroutine(EscapeLastPieceRoutine());
            }
            else
            {
                CompleteButtonMerger();
            }
        }
    }

    private IEnumerator EscapeLastPieceRoutine()
    {
        yield return new WaitForSeconds(0.15f);

        int lastIndex = pieceTransforms.Length - 1;

        hasEscapedOnce = true;
        isPlaced[lastIndex] = false;

        Vector3 targetEscapePos = slotTransforms[lastIndex].position + escapeOffset;
        float elapsed = 0f;
        Vector3 startPos = pieceTransforms[lastIndex].position;

        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            pieceTransforms[lastIndex].position = Vector3.Lerp(startPos, targetEscapePos, elapsed / 0.35f);
            yield return null;
        }

        pieceTransforms[lastIndex].position = targetEscapePos;

        if (hintText != null)
        {
            hintText.text = "[ HINT ] 마지막 조각은 한 번 도망갑니다";
        }
        UpdateUI();
    }

    private void CompleteButtonMerger()
    {
        isButtonCompleted = true;

        int totalCount = pieceTransforms != null ? pieceTransforms.Length : 5;
        for (int i = 0; i < totalCount; i++)
        {
            if (pieceTransforms[i] != null) pieceTransforms[i].gameObject.SetActive(false);
            if (slotTransforms[i] != null) slotTransforms[i].gameObject.SetActive(false);
        }

        if (fullStartButton != null)
        {
            fullStartButton.gameObject.SetActive(true);
            fullStartButton.transform.SetAsLastSibling();
        }

        if (hintText != null)
        {
            hintText.text = "버튼이 완성되었습니다! 클릭하여 검사를 완료하세요.";
        }
    }

    private void OnFullStartButtonClicked()
    {
        if (!isGameActive) return;
        Success();
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        // ★ [추가] 제한시간 카운트다운
        remainingTime -= Time.deltaTime;
        elapsedTime += Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeOut();
            return;
        }

        UpdateTimerUI();

        // 25초 동안 조각을 맞추지 못하고 지체 시 깜빡임 힌트 발동
        if (elapsedTime >= 25f && !isButtonCompleted)
        {
            FlashNextPiece();
        }
    }

    // ★ [추가] 시간 초과 처리
    private void OnTimeOut()
    {
        isGameActive = false;
        if (timerText != null) timerText.text = "TIME 00:00";
        if (hintText != null)
        {
            hintText.text = "시간 초과! 제시간에 버튼을 완성하지 못했습니다.";
        }
    }

    private void FlashNextPiece()
    {
        int totalCount = pieceTransforms != null ? pieceTransforms.Length : 5;
        for (int i = 0; i < totalCount; i++)
        {
            if (!isPlaced[i] && pieceTransforms[i] != null)
            {
                Graphic g = pieceTransforms[i].GetComponent<Graphic>();
                if (g != null)
                {
                    float alpha = Mathf.PingPong(Time.time * 4f, 0.6f) + 0.4f;
                    g.color = new Color(g.color.r, g.color.g, g.color.b, alpha);
                }
                break;
            }
        }
    }

    private void UpdateSilhouettes()
    {
        if (slotSilhouettes == null || slotSilhouettes.Length == 0) return;

        string[] characters = new string[] { "S", "T", "A", "R", "T" };

        for (int i = 0; i < slotSilhouettes.Length; i++)
        {
            if (slotSilhouettes[i] == null) continue;

            if (failCount >= 4)
            {
                if (i < characters.Length) slotSilhouettes[i].text = characters[i];
                slotSilhouettes[i].gameObject.SetActive(true);
            }
            else if (failCount >= 2)
            {
                if (i == 0)
                {
                    if (i < characters.Length) slotSilhouettes[i].text = characters[i];
                    slotSilhouettes[i].gameObject.SetActive(true);
                }
                else
                {
                    slotSilhouettes[i].gameObject.SetActive(false);
                }
            }
            else
            {
                slotSilhouettes[i].gameObject.SetActive(false);
            }
        }

        if (hintText != null && !isButtonCompleted && !hasEscapedOnce)
        {
            if (failCount >= 4) hintText.text = "[ HINT ] 모든 슬롯에 정답 실루엣이 표시됩니다.";
            else if (failCount >= 2) hintText.text = "[ HINT ] 첫 번째 슬롯에 'S' 실루엣이 나타납니다.";
            else hintText.text = "흩어진 버튼 조각을 드래그해 'START' 버튼을 완성하세요.";
        }
    }

    private int GetPlacedCount()
    {
        int count = 0;
        int totalCount = isPlaced != null ? isPlaced.Length : 0;
        for (int i = 0; i < totalCount; i++)
        {
            if (isPlaced[i]) count++;
        }
        return count;
    }

    private void UpdateUI()
    {
        if (failText != null) failText.text = $"실수 {failCount} 회";

        int count = GetPlacedCount();
        int totalCount = pieceTransforms != null ? pieceTransforms.Length : 5;
        if (piecesText != null) piecesText.text = $"PIECES {count} / {totalCount}";
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("TIME {0:00}:{1:00}", minutes, seconds);
        }
    }

    protected override void GiveHint() { }
    protected override void RestartGame() { }
}