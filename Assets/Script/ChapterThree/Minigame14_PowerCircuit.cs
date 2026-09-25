using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class Minigame14_PowerCircuit : MinigameBase
{
    [System.Serializable]
    public class CableData
    {
        [Header("Cable")]
        public RectTransform cableStart;
        public RectTransform cableEnd;
        public RectTransform cableLine;

        [Header("Correct Target")]
        public RectTransform correctPort;

        [Header("Hint")]
        public GameObject hintLine;

        [HideInInspector]
        public Vector2 initialEndPosition;

        [HideInInspector]
        public bool connected;
    }

    [Header("Play Area")]
    public RectTransform playArea;

    [Header("Cables")]
    public CableData[] cables;

    [Header("START Button")]
    public Button startButton;
    public Image startButtonImage;

    [Header("START Button Colors")]
    public Color startOffColor = Color.gray;
    public Color startOnColor = Color.green;

    [Header("UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI errorText;

    [Header("Drop Settings")]
    public float connectDistance = 80f;

    [Header("Error 2 Hint")]
    public Graphic firstTargetPortGraphic;
    public float blinkDuration = 2f;

    [Header("Error 6 Optional")]
    public Animator[] deviceAnimators;

    [Header("Message")]
    public float messageDuration = 2f;

    private int errorCount = 0;
    private int connectedCount = 0;

    private bool hint2Applied = false;
    private bool hint4Applied = false;
    private bool hint6Applied = false;

    private bool circuitCompleted = false;

    private CableData draggingCable;

    private Coroutine messageCoroutine;


    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }


    // =====================================================
    // 게임 시작
    // =====================================================

    public override void StartMinigame()
    {
        gameName = "실행 회로 연결 검사";

        instruction =
            "전원 장치부터 START 버튼까지 케이블을 올바른 순서로 연결하세요.";

        base.StartMinigame();

        StopAllCoroutines();

        errorCount = 0;
        connectedCount = 0;

        hint2Applied = false;
        hint4Applied = false;
        hint6Applied = false;

        circuitCompleted = false;
        draggingCable = null;

        SetupCables();
        SetupStartButton();

        HideHintLines();

        if (hintText != null)
        {
            hintText.text = instruction;
        }

        UpdateErrorUI();
        UpdateTimerUI();
    }


    // =====================================================
    // Update
    // =====================================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        UpdateTimerUI();

        if (draggingCable != null)
        {
            UpdateCableLine(draggingCable);
        }
    }


    // =====================================================
    // 케이블 초기 설정
    // =====================================================

    private void SetupCables()
    {
        if (cables == null)
            return;

        foreach (CableData cable in cables)
        {
            if (cable == null ||
                cable.cableEnd == null)
            {
                continue;
            }

            cable.initialEndPosition =
                cable.cableEnd.anchoredPosition;

            cable.connected = false;

            SetupCableEvent(cable);

            UpdateCableLine(cable);
        }
    }


    private void SetupCableEvent(
        CableData cable)
    {
        EventTrigger trigger =
            cable.cableEnd.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                cable.cableEnd.gameObject
                    .AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();


        // Begin Drag
        EventTrigger.Entry beginEntry =
            new EventTrigger.Entry();

        beginEntry.eventID =
            EventTriggerType.BeginDrag;

        beginEntry.callback.AddListener(
            (data) =>
            {
                BeginCableDrag(
                    cable,
                    (PointerEventData)data
                );
            });

        trigger.triggers.Add(beginEntry);


        // Drag
        EventTrigger.Entry dragEntry =
            new EventTrigger.Entry();

        dragEntry.eventID =
            EventTriggerType.Drag;

        dragEntry.callback.AddListener(
            (data) =>
            {
                DragCable(
                    cable,
                    (PointerEventData)data
                );
            });

        trigger.triggers.Add(dragEntry);


        // End Drag
        EventTrigger.Entry endEntry =
            new EventTrigger.Entry();

        endEntry.eventID =
            EventTriggerType.EndDrag;

        endEntry.callback.AddListener(
            (data) =>
            {
                EndCableDrag(cable);
            });

        trigger.triggers.Add(endEntry);
    }


    // =====================================================
    // 케이블 드래그
    // =====================================================

    private void BeginCableDrag(
        CableData cable,
        PointerEventData data)
    {
        if (!isGameActive ||
            circuitCompleted ||
            cable.connected)
        {
            return;
        }

        draggingCable = cable;

        cable.cableEnd.SetAsLastSibling();
    }


    private void DragCable(
        CableData cable,
        PointerEventData data)
    {
        if (!isGameActive ||
            cable.connected ||
            draggingCable != cable)
        {
            return;
        }

        RectTransform parent =
            cable.cableEnd.parent
                as RectTransform;

        if (parent == null)
            return;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parent,
                data.position,
                data.pressEventCamera,
                out Vector2 localPosition
            );

        cable.cableEnd.anchoredPosition =
            localPosition;

        UpdateCableLine(cable);
    }


    private void EndCableDrag(
        CableData cable)
    {
        if (!isGameActive ||
            cable.connected)
        {
            return;
        }

        draggingCable = null;

        if (IsNearCorrectPort(cable))
        {
            ConnectCable(cable);
        }
        else
        {
            WrongConnection(cable);
        }

        UpdateCableLine(cable);
    }


    // =====================================================
    // 올바른 연결
    // =====================================================

    private bool IsNearCorrectPort(
        CableData cable)
    {
        if (cable.correctPort == null)
            return false;

        float distance =
            Vector2.Distance(
                cable.cableEnd.position,
                cable.correctPort.position
            );

        return distance <= connectDistance;
    }


    private void ConnectCable(
        CableData cable)
    {
        cable.connected = true;

        cable.cableEnd.position =
            cable.correctPort.position;

        connectedCount++;

        ShowMessage(
            "회로가 연결되었습니다."
        );

        UpdateCableLine(cable);

        CheckCircuitComplete();
    }


    // =====================================================
    // 잘못된 연결
    // =====================================================

    private void WrongConnection(
        CableData cable)
    {
        errorCount++;

        cable.cableEnd.anchoredPosition =
            cable.initialEndPosition;

        ShowMessage(
            "잘못된 포트입니다. 다시 연결하세요."
        );

        UpdateErrorUI();

        CheckErrorHints();
    }


    // =====================================================
    // 케이블 선 그리기
    // =====================================================

    private void UpdateCableLine(
        CableData cable)
    {
        if (cable.cableStart == null ||
            cable.cableEnd == null ||
            cable.cableLine == null)
        {
            return;
        }

        RectTransform line =
            cable.cableLine;

        RectTransform parent =
            line.parent as RectTransform;

        if (parent == null)
            return;

        Vector2 startPos =
            parent.InverseTransformPoint(
                cable.cableStart.position
            );

        Vector2 endPos =
            parent.InverseTransformPoint(
                cable.cableEnd.position
            );

        Vector2 direction =
            endPos - startPos;

        float distance =
            direction.magnitude;

        line.anchoredPosition =
            startPos +
            direction * 0.5f;

        line.sizeDelta =
            new Vector2(
                distance,
                line.sizeDelta.y
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        line.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }


    // =====================================================
    // 회로 완성
    // =====================================================

    private void CheckCircuitComplete()
    {
        if (cables == null)
            return;

        foreach (CableData cable in cables)
        {
            if (cable == null ||
                !cable.connected)
            {
                return;
            }
        }

        CircuitComplete();
    }


    private void CircuitComplete()
    {
        circuitCompleted = true;

        if (startButton != null)
        {
            startButton.interactable = true;
        }

        if (startButtonImage != null)
        {
            startButtonImage.color =
                startOnColor;
        }

        if (hintText != null)
        {
            hintText.text =
                "전원 연결 완료. START 버튼을 누르세요.";
        }
    }


    // =====================================================
    // START 버튼
    // =====================================================

    private void SetupStartButton()
    {
        if (startButton == null)
            return;

        startButton.interactable = false;

        startButton.onClick
            .RemoveAllListeners();

        startButton.onClick
            .AddListener(
                OnStartButtonClicked
            );

        if (startButtonImage == null)
        {
            startButtonImage =
                startButton
                    .GetComponent<Image>();
        }

        if (startButtonImage != null)
        {
            startButtonImage.color =
                startOffColor;
        }
    }


    private void OnStartButtonClicked()
    {
        if (!isGameActive ||
            !circuitCompleted)
        {
            return;
        }

        Success();
    }


    // =====================================================
    // 실수 힌트
    // =====================================================

    private void CheckErrorHints()
    {
        // 실수 2회
        if (errorCount >= 2 &&
            !hint2Applied)
        {
            hint2Applied = true;

            if (firstTargetPortGraphic != null)
            {
                StartCoroutine(
                    BlinkFirstPort()
                );
            }
        }

        // 실수 4회
        if (errorCount >= 4 &&
            !hint4Applied)
        {
            hint4Applied = true;

            ShowHintLines();

            ShowMessage(
                "[ HINT ] 케이블과 연결해야 할 장치를 확인하세요."
            );
        }

        // 실수 6회
        if (errorCount >= 6 &&
            !hint6Applied)
        {
            hint6Applied = true;

            DisableDeviceAnimations();

            ShowMessage(
                "[ HINT ] 장치 위치 변경 연출이 제거되었습니다."
            );
        }
    }


    // =====================================================
    // 실수 2 - 첫 포트 점멸
    // =====================================================

    private IEnumerator BlinkFirstPort()
    {
        Color originalColor =
            firstTargetPortGraphic.color;

        float timer = 0f;

        while (timer < blinkDuration)
        {
            timer += Time.deltaTime;

            float alpha =
                Mathf.PingPong(
                    Time.time * 5f,
                    1f
                );

            Color c =
                originalColor;

            c.a =
                Mathf.Lerp(
                    0.2f,
                    1f,
                    alpha
                );

            firstTargetPortGraphic.color =
                c;

            yield return null;
        }

        firstTargetPortGraphic.color =
            originalColor;
    }


    // =====================================================
    // 실수 4 - 연결선 힌트
    // =====================================================

    private void ShowHintLines()
    {
        if (cables == null)
            return;

        foreach (CableData cable in cables)
        {
            if (cable != null &&
                cable.hintLine != null)
            {
                cable.hintLine
                    .SetActive(true);
            }
        }
    }


    private void HideHintLines()
    {
        if (cables == null)
            return;

        foreach (CableData cable in cables)
        {
            if (cable != null &&
                cable.hintLine != null)
            {
                cable.hintLine
                    .SetActive(false);
            }
        }
    }


    // =====================================================
    // 실수 6 - 장치 애니메이션 중지
    // =====================================================

    private void DisableDeviceAnimations()
    {
        if (deviceAnimators == null)
            return;

        foreach (Animator animator
                 in deviceAnimators)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }
    }


    // =====================================================
    // UI
    // =====================================================

    private void UpdateErrorUI()
    {
        if (errorText != null)
        {
            errorText.text =
                $"ERROR {errorCount}";
        }
    }


    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        float displayTime =
            Mathf.Max(
                0f,
                currentTimer
            );

        int minutes =
            Mathf.FloorToInt(
                displayTime / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                displayTime % 60f
            );

        timerText.text =
            string.Format(
                "TIME {0:00}:{1:00}",
                minutes,
                seconds
            );
    }


    private void ShowMessage(
        string message)
    {
        if (hintText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }

        hintText.text = message;

        messageCoroutine =
            StartCoroutine(
                ClearMessageRoutine()
            );
    }


    private IEnumerator
        ClearMessageRoutine()
    {
        yield return new WaitForSeconds(
            messageDuration
        );

        if (hintText != null &&
            isGameActive &&
            !circuitCompleted)
        {
            hintText.text =
                instruction;
        }

        messageCoroutine = null;
    }


    protected override void GiveHint()
    {
    }


    protected override void RestartGame()
    {
        StartMinigame();
    }
}