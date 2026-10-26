using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame14_PowerCircuit : MinigameBase
{

    [System.Serializable]
    public class CableData
    {

        public RectTransform cableLine;
        public RectTransform cableEnd;


        public RectTransform startAnchor;


        public CircuitPortShape requiredShape;



        public List<CircuitPort> targetPorts =
            new List<CircuitPort>();



        public bool isFinalCable = false;


        [HideInInspector]
        public Vector2 originalEndPosition;

        [HideInInspector]
        public bool connected = false;

        [HideInInspector]
        public CircuitPort connectedPort;
    }




    public RectTransform playArea;



    public List<CableData> cables =
        new List<CableData>();



    public Button startButton;
    public Image startButtonImage;
    [Tooltip("START 장치 안의 START 글씨 (연결 전에는 숨김)")]
    public TMP_Text startLabel;

    [Header("Separate START Button (after original START click)")]
    public Button separateStartButton;

    [Header("Extra puzzle objects to hide (optional)")]
    public GameObject[] puzzleObjectsToHide;

    private bool waitingForSeparateStart = false;

    [Header("START Pop")]
    [Tooltip("회로 연결 완료 후 튀어나오는 START 버튼 크기 배율")]
    public float startPopScale = 1.3f;
    private Vector3 startPopBaseScale = Vector3.zero;
    private readonly Dictionary<GameObject, bool> hiddenPuzzleObjects =
        new Dictionary<GameObject, bool>();

    public Color startDisabledColor =
        Color.gray;

    public Color startEnabledColor =
        Color.green;




    public TMP_Text errorText;
    public TMP_Text timerText;
    public TMP_Text hintText;




    public List<RectTransform> hintLines =
        new List<RectTransform>();





    public float finalDisconnectDelay = 0.7f;


    private int errorCount = 0;

    private float remainingTime;

    private bool finalFakeDisconnectDone = false;

    private bool finalDisconnectRunning = false;



    public override void StartMinigame()
    {
        // 이전 시도에서 숨긴 회로 장치를 원래 표시 상태로 되돌립니다.
        RestorePuzzleObjects();
        waitingForSeparateStart = false;

        base.StartMinigame();


        gameName =
            "START 전원 연결 검사";

        instruction =
            "색상이 아니라 포트의 모양을 보고 연결하세요.";


        remainingTime =
            timeLimit;

        errorCount = 0;

        finalFakeDisconnectDone =
            false;

        finalDisconnectRunning =
            false;




        for (int i = 0;
             i < cables.Count;
             i++)
        {
            CableData cable =
                cables[i];

            if (cable == null)
                continue;


            if (cable.cableEnd != null)
            {
                cable.originalEndPosition =
                    cable.cableEnd.anchoredPosition;
            }


            cable.connected =
                false;

            cable.connectedPort =
                null;


            UpdateCableLine(i);
        }


        // 기존 회로의 START는 원래대로 비활성 상태로 시작합니다.
        if (startLabel != null && startPopBaseScale != Vector3.zero)
            startLabel.rectTransform.localScale = startPopBaseScale;

        SetStartButtonActive(false);

        // 별도 START는 모든 연결을 마치고 기존 START를 누를 때 나타납니다.
        if (separateStartButton != null)
        {
            separateStartButton.onClick.RemoveListener(OnClickSeparateStartButton);
            separateStartButton.onClick.AddListener(OnClickSeparateStartButton);
            separateStartButton.interactable = false;
            separateStartButton.gameObject.SetActive(false);
        }


        // 힌트 라인 숨김
        HideHintLines();


        if (hintText != null)
        {
            hintText.text =
                "색상이 아니라 포트의 모양을 확인하세요.";
        }


        UpdateUI();
    }



    protected override void Update()
    {
        base.Update();


        if (!isGameActive || waitingForSeparateStart)
            return;


        remainingTime -=
            Time.deltaTime;


        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            UpdateUI();

            Fail();

            return;
        }


        UpdateUI();


        for (int i = 0;
             i < cables.Count;
             i++)
        {
            UpdateCableLine(i);
        }
    }


    public void BeginCableDrag(
        int cableIndex)
    {
        if (!isGameActive || waitingForSeparateStart)
            return;


        if (!IsValidCableIndex(
                cableIndex))
        {
            return;
        }


        CableData cable =
            cables[cableIndex];



        cable.connected =
            false;

        cable.connectedPort =
            null;


        SetStartButtonActive(false);
    }



    public void DragCable(
        int cableIndex,
        PointerEventData eventData)
    {
        if (!isGameActive || waitingForSeparateStart)
            return;


        if (!IsValidCableIndex(
                cableIndex))
        {
            return;
        }


        CableData cable =
            cables[cableIndex];


        if (cable.cableEnd == null)
            return;


        RectTransform endParent =
            cable.cableEnd.parent
            as RectTransform;


        if (endParent == null)
            return;


        Vector2 localPosition;


        Camera uiCamera =
            GetUICamera();


        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                endParent,
                eventData.position,
                uiCamera,
                out localPosition))
        {
            cable.cableEnd.anchoredPosition =
                localPosition;
        }


        UpdateCableLine(
            cableIndex
        );
    }




    public void EndCableDrag(
        int cableIndex,
        PointerEventData eventData)
    {
        if (!isGameActive || waitingForSeparateStart)
            return;


        if (!IsValidCableIndex(
                cableIndex))
        {
            return;
        }


        CableData cable =
            cables[cableIndex];


        CircuitPort droppedPort =
            FindPortUnderPointer(
                cable,
                eventData.position
            );


        // 아무 포트에도 놓지 않음
        if (droppedPort == null)
        {
            ResetSingleCable(
                cableIndex
            );

            return;
        }



        if (droppedPort.shape !=
            cable.requiredShape)
        {
            // 잘못 꽂은 포트에서 불꽃이 튐
            PlaySpark(droppedPort.RectTransform.position);

            WrongConnection();

            return;
        }




        ConnectCableToPort(
            cableIndex,
            droppedPort
        );


        EvaluateConnections();
    }



    private void ConnectCableToPort(
        int cableIndex,
        CircuitPort port)
    {
        CableData cable =
            cables[cableIndex];


        cable.connected =
            true;

        cable.connectedPort =
            port;


        if (cable.cableEnd != null &&
            port != null)
        {
            RectTransform endParent =
                cable.cableEnd.parent
                as RectTransform;


            if (endParent != null)
            {
                Vector3 localPosition =
                    endParent.InverseTransformPoint(
                        port.RectTransform.position
                    );


                cable.cableEnd.localPosition =
                    localPosition;
            }
        }


        UpdateCableLine(
            cableIndex
        );
    }



    private void EvaluateConnections()
    {
        if (finalDisconnectRunning)
            return;


        int finalCableIndex =
            -1;

        bool allConnected =
            true;


        for (int i = 0;
             i < cables.Count;
             i++)
        {
            CableData cable =
                cables[i];


            if (cable == null)
                continue;


            if (cable.isFinalCable)
            {
                finalCableIndex =
                    i;
            }


            if (!cable.connected)
            {
                allConnected =
                    false;
            }
        }



        if (!allConnected)
        {
            SetStartButtonActive(
                false
            );

            return;
        }


        if (finalCableIndex < 0)
        {
            ShowStartReady();

            return;
        }




        if (!finalFakeDisconnectDone)
        {
            StartCoroutine(
                FakeFinalDisconnect(
                    finalCableIndex
                )
            );

            return;
        }



        ShowStartReady();
    }




    private IEnumerator FakeFinalDisconnect(
        int finalCableIndex)
    {
        finalDisconnectRunning =
            true;


        if (hintText != null)
        {
            hintText.text =
                "연결 확인 중...";
        }


        yield return
            new WaitForSeconds(
                finalDisconnectDelay
            );


        if (!isGameActive)
        {
            finalDisconnectRunning =
                false;

            yield break;
        }


        // 마지막 케이블이 '파직' 하고 빠짐
        CableData finalCable = cables[finalCableIndex];
        if (finalCable != null && finalCable.cableEnd != null)
            PlaySpark(finalCable.cableEnd.position);

        // 케이블이 몇 번 깜빡이다가 빠짐
        if (finalCable != null && finalCable.cableLine != null)
        {
            Graphic lineGraphic = finalCable.cableLine.GetComponent<Graphic>();
            if (lineGraphic != null)
            {
                Color original = lineGraphic.color;
                for (int i = 0; i < 4; i++)
                {
                    lineGraphic.color = new Color(1f, 0.95f, 0.4f, 1f);
                    yield return new WaitForSeconds(0.04f);
                    lineGraphic.color = new Color(original.r, original.g, original.b, 0.2f);
                    yield return new WaitForSeconds(0.04f);
                }
                lineGraphic.color = original;
            }
        }

        ResetSingleCable(
            finalCableIndex
        );


        finalFakeDisconnectDone =
            true;

        finalDisconnectRunning =
            false;


        SetStartButtonActive(
            false
        );


        if (hintText != null)
        {
            hintText.text =
                "START 연결이 끊어졌습니다. 다시 연결하세요.";
        }
    }




    private void WrongConnection()
    {
        errorCount++;
        ReportMistake();


        if (hintText != null)
        {
            hintText.text =
                "잘못된 연결입니다. 모든 케이블이 분리됩니다.";
        }




        ResetAllConnections();


        // 다시 처음부터
        finalFakeDisconnectDone =
            false;

        finalDisconnectRunning =
            false;


        // ERROR 2 이상
        if (errorCount >= 2)
        {
            if (hintText != null)
            {
                hintText.text =
                    "HINT : 색상이 아니라 ○ □ △ 모양을 확인하세요.";
            }
        }


        // ERROR 4 이상
        if (errorCount >= 4)
        {
            ShowHintLines();
        }


        UpdateUI();
    }




    private void ResetSingleCable(
        int cableIndex)
    {
        if (!IsValidCableIndex(
                cableIndex))
        {
            return;
        }


        CableData cable =
            cables[cableIndex];


        cable.connected =
            false;

        cable.connectedPort =
            null;


        if (cable.cableEnd != null)
        {
            cable.cableEnd.anchoredPosition =
                cable.originalEndPosition;
        }


        UpdateCableLine(
            cableIndex
        );
    }



    private void ResetAllConnections()
    {
        for (int i = 0;
             i < cables.Count;
             i++)
        {
            ResetSingleCable(i);
        }


        SetStartButtonActive(
            false
        );
    }



    private void SetStartButtonActive(
        bool active)
    {
        if (startLabel != null)
            startLabel.gameObject.SetActive(active);

        if (startButton != null)
        {
            startButton.interactable =
                active;
        }


        if (startButtonImage != null)
        {
            startButtonImage.color =
                active
                ? startEnabledColor
                : startDisabledColor;
        }
    }




    public void OnClickStartButton()
    {
        // 회로가 다 연결된 뒤에는 기존 START 장치를 눌러도 바로 클리어
        if (!isGameActive || !waitingForSeparateStart)
            return;

        OnClickSeparateStartButton();
    }

    // 모든 회로가 연결되면 START 장치 위치에 START 버튼이 튀어나옴 (화면 전환 없음)
    private void ShowStartReady()
    {
        if (waitingForSeparateStart)
            return;

        waitingForSeparateStart = true;

        SetStartButtonActive(true);

        if (hintText != null)
            hintText.text = "회로 연결 완료! START 버튼을 누르세요.";

        // START 장치 안에 START 글씨가 튀어나오면서 버튼이 됨
        if (startLabel != null)
            StartCoroutine(PopStartButton(startLabel.rectTransform));
    }

    private IEnumerator PopStartButton(RectTransform target)
    {
        if (startPopBaseScale == Vector3.zero)
            startPopBaseScale = target.localScale;

        Vector3 finalScale = startPopBaseScale;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 살짝 크게 튀어나왔다가 제자리
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            target.localScale = finalScale * t * overshoot;
            yield return null;
        }

        target.localScale = finalScale;
    }

    private void HidePuzzleObject(GameObject obj)
    {
        if (obj == null || obj == gameObject)
            return;

        // 별도 START 자신이나 그 부모, 또는 게임판 전체를 숨기지 않습니다.
        if (separateStartButton != null &&
            separateStartButton.transform.IsChildOf(obj.transform))
            return;

        if (playArea != null && obj == playArea.gameObject)
            return;

        if (!hiddenPuzzleObjects.ContainsKey(obj))
            hiddenPuzzleObjects.Add(obj, obj.activeSelf);

        obj.SetActive(false);
    }

    private void RestorePuzzleObjects()
    {
        foreach (KeyValuePair<GameObject, bool> pair in hiddenPuzzleObjects)
        {
            if (pair.Key != null)
                pair.Key.SetActive(pair.Value);
        }

        hiddenPuzzleObjects.Clear();
    }

    public void OnClickSeparateStartButton()
    {
        if (!isGameActive || !waitingForSeparateStart)
            return;

        if (separateStartButton != null)
            separateStartButton.interactable = false;

        Debug.Log("별도 START 클릭: 미니게임 14 성공");
        Success();
    }

    private CircuitPort FindPortUnderPointer(
        CableData cable,
        Vector2 pointerPosition)
    {
        if (cable.targetPorts == null)
            return null;


        Camera uiCamera =
            GetUICamera();


        foreach (CircuitPort port
                 in cable.targetPorts)
        {
            if (port == null)
                continue;


            if (RectTransformUtility
                .RectangleContainsScreenPoint(
                    port.RectTransform,
                    pointerPosition,
                    uiCamera))
            {
                return port;
            }
        }


        return null;
    }



    // =========================================
    // 전기 스파크 (소리 + 불꽃 파편)
    // =========================================
    [Header("Spark")]
    public int sparkParticleCount = 16;
    public float sparkDuration = 0.45f;

    private void PlaySpark(Vector3 worldPosition)
    {
        GameSfx.Play("spark", 0.75f, 0.08f);
        StartCoroutine(SparkRoutine(worldPosition));
    }

    private IEnumerator SparkRoutine(Vector3 worldPosition)
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            yield break;

        Vector2 center = root.InverseTransformPoint(worldPosition);

        List<RectTransform> bits = new List<RectTransform>();
        List<Vector2> velocities = new List<Vector2>();
        List<Image> images = new List<Image>();

        // 번쩍이는 빛
        Image flash = CreateSparkBit(root, center, 70f, new Color(1f, 1f, 0.85f, 0.9f));
        images.Add(flash);
        bits.Add(flash.rectTransform);
        velocities.Add(Vector2.zero);

        Color[] colors =
        {
            new Color(1f, 0.95f, 0.35f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(0.4f, 0.9f, 1f, 1f)
        };

        for (int i = 0; i < sparkParticleCount; i++)
        {
            float size = Random.Range(5f, 11f);
            Image bit = CreateSparkBit(root, center, size, colors[Random.Range(0, colors.Length)]);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(250f, 650f);

            images.Add(bit);
            bits.Add(bit.rectTransform);
            velocities.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed);
        }

        float elapsed = 0f;

        while (elapsed < sparkDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = elapsed / sparkDuration;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i] == null)
                    continue;

                if (i == 0)
                {
                    // 빛은 커지면서 사라짐
                    bits[i].localScale = Vector3.one * (1f + t * 1.5f);
                }
                else
                {
                    velocities[i] += Vector2.down * 900f * dt;  // 살짝 떨어지게
                    bits[i].anchoredPosition += velocities[i] * dt;
                }

                Color c = images[i].color;
                c.a = (i == 0 ? 0.9f : 1f) * (1f - t);
                images[i].color = c;
            }

            yield return null;
        }

        foreach (RectTransform bit in bits)
        {
            if (bit != null)
                Destroy(bit.gameObject);
        }
    }

    private Image CreateSparkBit(RectTransform parent, Vector2 position, float size, Color color)
    {
        GameObject go = new GameObject("Spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);
        rt.SetAsLastSibling();

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void UpdateCableLine(
        int cableIndex)
    {
        if (!IsValidCableIndex(
                cableIndex))
        {
            return;
        }


        CableData cable =
            cables[cableIndex];


        if (cable.cableLine == null ||
            cable.cableEnd == null ||
            cable.startAnchor == null)
        {
            return;
        }


        RectTransform lineParent =
            cable.cableLine.parent
            as RectTransform;


        if (lineParent == null)
            return;



        cable.cableLine.pivot =
            new Vector2(
                0.5f,
                0.5f
            );



        Vector3 startWorld =
            cable.startAnchor.position;


        Vector3 endWorld =
            cable.cableEnd.position;



        Vector3 middleWorld =
            (
                startWorld +
                endWorld
            )
            * 0.5f;


        cable.cableLine.position =
            middleWorld;



        Vector3 direction =
            endWorld -
            startWorld;


        float worldDistance =
            direction.magnitude;



        float scaleX =
            Mathf.Abs(
                lineParent.lossyScale.x
            );


        if (scaleX < 0.0001f)
        {
            scaleX = 1f;
        }


        float localDistance =
            worldDistance /
            scaleX;




        cable.cableLine
            .SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                localDistance
            );




        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            )
            *
            Mathf.Rad2Deg;


        cable.cableLine.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }



    private void HideHintLines()
    {
        foreach (RectTransform line
                 in hintLines)
        {
            if (line != null)
            {
                line.gameObject
                    .SetActive(false);
            }
        }
    }


    private void ShowHintLines()
    {
        foreach (RectTransform line
                 in hintLines)
        {
            if (line != null)
            {
                line.gameObject
                    .SetActive(true);
            }
        }
    }



    private void UpdateUI()
    {
        if (errorText != null)
        {
            errorText.text =
                "ERROR " +
                errorCount;
        }


        if (timerText != null)
        {
            int minute =
                Mathf.FloorToInt(
                    remainingTime /
                    60f
                );


            int second =
                Mathf.FloorToInt(
                    remainingTime %
                    60f
                );


            timerText.text =
                $"TIME {minute:00}:{second:00}";
        }
    }



    private Camera GetUICamera()
    {
        Canvas canvas =
            GetComponentInParent<Canvas>();


        if (canvas == null)
            return null;


        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }


        return canvas.worldCamera;
    }




    private bool IsValidCableIndex(
        int index)
    {
        return
            index >= 0 &&
            index < cables.Count &&
            cables[index] != null;
    }




    protected override void GiveHint()
    {
        if (hintText != null)
        {
            hintText.text =
                "HINT : 색상이 아니라 포트의 모양을 확인하세요.";
        }


        ShowHintLines();
    }




    protected override void RestartGame()
    {
        StopAllCoroutines();


        isGameActive =
            false;


        ResetAllConnections();


        StartMinigame();
    }
}