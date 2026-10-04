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


        // START 비활성화
        SetStartButtonActive(false);


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


        if (!isGameActive)
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
        if (!isGameActive)
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
        if (!isGameActive)
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
        if (!isGameActive)
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
        if (!isGameActive)
            return;


        if (startButton == null ||
            !startButton.interactable)
        {
            return;
        }


        Debug.Log(
            "START 버튼 실행 성공!"
        );


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