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

    [Header("Separate START Button (after original START click)")]
    public Button separateStartButton;

    [Header("Extra puzzle objects to hide (optional)")]
    public GameObject[] puzzleObjectsToHide;

    private bool waitingForSeparateStart = false;
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
            SetStartButtonActive(
                true
            );

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



        SetStartButtonActive(
            true
        );


        if (hintText != null)
        {
            hintText.text =
                "전원 연결 완료. START 버튼을 누르세요.";
        }
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


        // 마지막 케이블만 빠짐
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
        if (!isGameActive || waitingForSeparateStart)
            return;


        if (startButton == null ||
            !startButton.interactable)
        {
            return;
        }


        Debug.Log("회로 START 클릭: 별도 START 표시");
        ShowSeparateStartStage();
    }

    private void ShowSeparateStartStage()
    {
        if (waitingForSeparateStart)
            return;

        if (separateStartButton == null)
        {
            Debug.LogError("Separate Start Button이 Inspector에 연결되지 않았습니다.");
            return;
        }

        waitingForSeparateStart = true;

        // 배경/게임 패널/제목/설명은 끄지 않습니다.
        // 포트가 포함된 장치의 부모만 찾아서 숨깁니다.
        foreach (CableData cable in cables)
        {
            if (cable == null)
                continue;

            if (cable.targetPorts != null)
            {
                foreach (CircuitPort port in cable.targetPorts)
                {
                    if (port == null)
                        continue;

                    // 보통 PowerDevice, InputDevice, ScreenDevice 등이 됩니다.
                    if (port.transform.parent != null)
                        HidePuzzleObject(port.transform.parent.gameObject);
                    else
                        HidePuzzleObject(port.gameObject);
                }
            }

            if (cable.connectedPort != null)
            {
                Transform parent = cable.connectedPort.transform.parent;
                HidePuzzleObject(parent != null ? parent.gameObject :
                    cable.connectedPort.gameObject);
            }

            if (cable.cableLine != null)
                HidePuzzleObject(cable.cableLine.gameObject);

            if (cable.cableEnd != null)
                HidePuzzleObject(cable.cableEnd.gameObject);
        }

        // 기존 START 장치는 마지막 연결 포트를 포함하므로
        // 퍼즐이 끝난 지금에만 숨깁니다.
        if (startButton != null)
            HidePuzzleObject(startButton.gameObject);

        HideHintLines();

        // 자동 감지되지 않는 장식/케이블이 있으면 Inspector에서 추가 지정.
        if (puzzleObjectsToHide != null)
        {
            foreach (GameObject obj in puzzleObjectsToHide)
                HidePuzzleObject(obj);
        }

        if (hintText != null)
            hintText.text = "회로 연결 완료! START 버튼을 눌러 다음으로 진행하세요.";

        separateStartButton.gameObject.SetActive(true);
        separateStartButton.interactable = true;
        separateStartButton.transform.SetAsLastSibling();
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