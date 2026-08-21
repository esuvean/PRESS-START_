

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class Minigame3_MashButton : MinigameBase
{
    
    public RectTransform[] slotTransforms = new RectTransform[4];

   
    public RectTransform[] pieceTransforms = new RectTransform[4];

    
    public TextMeshProUGUI[] slotSilhouettes = new TextMeshProUGUI[4];

   
    public Button fullStartButton;

    [Header("UI References - Labels")]
    public TextMeshProUGUI failText;       // 실수 X 회
    public TextMeshProUGUI piecesText;     // PIECES X / 4 (또는 조립 X / 4)
    public TextMeshProUGUI hintText;       // 하단 힌트 텍스트
    public TextMeshProUGUI timerText;      // 우상단 타이머 (TIME 00:00)

    [Header("Game Settings")]
    public float snapDistance = 100f;      // 슬롯 흡착 인식 거리
    public Vector3 escapeOffset = new Vector3(300f, 180f, 0f); // '기' 조각 도망 위치 오프셋

    // 내부 상태 변수
    private Vector3[] initialPiecePositions = new Vector3[4];
    private bool[] isPlaced = new bool[4];
    private int failCount = 0;
    private bool hasEscapedOnce = false;
    private bool isButtonCompleted = false;
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
        instruction = "흩어진 버튼 조각을 드래그해 '시작하기' 버튼을 완성하세요.";

        failCount = 0;
        elapsedTime = 0f;
        hasEscapedOnce = false;
        isButtonCompleted = false;

        // 초기 위치 저장 및 조각/슬롯 초기화
        for (int i = 0; i < 4; i++)
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

            if (slotTransforms[i] != null)
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
        for (int i = 0; i < pieceTransforms.Length; i++)
        {
            if (pieceTransforms[i] == null) continue;

            EventTrigger trigger = pieceTransforms[i].GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = pieceTransforms[i].gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();

            int index = i; // 클로저 캡처

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
        if (isPlaced[index]) return; // 이미 슬롯에 고정된 조각은 드래그 불가

        pieceTransforms[index].SetAsLastSibling(); // 맨 위 레이어로 이동
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

        // 정답 슬롯과의 거리 계산
        float distToCorrectSlot = Vector2.Distance(pieceTransforms[index].position, slotTransforms[index].position);

        // 오답 슬롯 근처에 놓았는지 체크
        bool droppedNearWrongSlot = false;
        for (int j = 0; j < 4; j++)
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
            // [정답] 올바른 슬롯에 흡착
            SnapPieceToSlot(index);
        }
        else
        {
            // [오답/미흡착] 원래 위치로 복귀
            ReturnPieceToInitial(index);

            // 잘못된 위치나 오답 슬롯에 놓았을 때 실패 카운트 증가
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
        UpdateUI();

        // 4개 조각이 모두 맞춰졌을 때
        if (placedCount == 4)
        {
            if (!hasEscapedOnce)
            {
                //  4개 조각이 맞춰지는 순간, 마지막 '기' 조각이 우상단으로 도망!
                StartCoroutine(EscapeLastPieceRoutine());
            }
            else
            {
                //  도망간 '기' 조각까지 재배치 완료 ? 통합 버튼으로 전환!
                CompleteButtonMerger();
            }
        }
    }

    private IEnumerator EscapeLastPieceRoutine()
    {
        yield return new WaitForSeconds(0.15f);

        hasEscapedOnce = true;
        isPlaced[3] = false; // '기' 조각 고정 해제하여 다시 드래그 가능하게 전환

        // 우상단으로 도망가는 이탈 애니메이션 연출
        Vector3 targetEscapePos = slotTransforms[3].position + escapeOffset;
        float elapsed = 0f;
        Vector3 startPos = pieceTransforms[3].position;

        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            pieceTransforms[3].position = Vector3.Lerp(startPos, targetEscapePos, elapsed / 0.35f);
            yield return null;
        }

        pieceTransforms[3].position = targetEscapePos;

        if (hintText != null)
        {
            hintText.text = "[ HINT ] 마지막 조각은 한 번 도망갑니다";
        }
        UpdateUI();
    }

    private void CompleteButtonMerger()
    {
        isButtonCompleted = true;

        // 개별 조각 및 슬롯 숨기기
        for (int i = 0; i < 4; i++)
        {
            if (pieceTransforms[i] != null) pieceTransforms[i].gameObject.SetActive(false);
            if (slotTransforms[i] != null) slotTransforms[i].gameObject.SetActive(false);
        }

        // 완성된 통합  버튼 표시
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
        Success(); // 검사 완료 (미니게임 성공 처리)
    }

    protected override void Update()
    {
        base.Update();
        if (!isGameActive) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        // 70초 경과 힌트: 안 맞춰진 조각 알파값 반짝임
        if (elapsedTime >= 70f && !isButtonCompleted)
        {
            FlashNextPiece();
        }
    }

    private void FlashNextPiece()
    {
        for (int i = 0; i < 4; i++)
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
        if (slotSilhouettes == null || slotSilhouettes.Length < 4) return;

        string[] characters = new string[] { "시", "작", "하", "기" };

        for (int i = 0; i < 4; i++)
        {
            if (slotSilhouettes[i] == null) continue;

            if (failCount >= 4)
            {
                // 실패 4회 이상: 모든 슬롯에 실루엣 표시
                slotSilhouettes[i].text = characters[i];
                slotSilhouettes[i].gameObject.SetActive(true);
            }
            else if (failCount >= 2)
            {
                // 실패 2회 이상: 첫 번째 슬롯('시')에만 실루엣 표시
                if (i == 0)
                {
                    slotSilhouettes[i].text = characters[i];
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

        // 힌트 텍스트 갱신
        if (hintText != null && !isButtonCompleted && !hasEscapedOnce)
        {
            if (failCount >= 4) hintText.text = "[ HINT ] 모든 슬롯에 정답 실루엣이 표시됩니다.";
            else if (failCount >= 2) hintText.text = "[ HINT ] 첫 번째 슬롯에 '시' 실루엣이 나타납니다.";
            else hintText.text = "흩어진 버튼 조각을 드래그해 '시작하기' 버튼을 완성하세요.";
        }
    }

    private int GetPlacedCount()
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (isPlaced[i]) count++;
        }
        return count;
    }

    private void UpdateUI()
    {
        if (failText != null) failText.text = $"실수 {failCount} 회";

        int count = GetPlacedCount();
        if (piecesText != null) piecesText.text = $"PIECES {count} / 4";
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