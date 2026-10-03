using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Minigame12_Breakout : MinigameBase
{
    
  
    public RectTransform playArea;

  
    public RectTransform paddle;

    public RectTransform ballSpawnPoint;
    public BreakoutBallUI ballPrefab;

  

 

  
    public RectTransform brickRowTemplate;

  
    public RectTransform brickParent;

   
    public int brickRowCount = 6;

  
    public float brickRowSpacing = 30f;

    public Color[] brickRowColors;

   
    [HideInInspector]
    public List<RectTransform> normalBricks =
        new List<RectTransform>();

    private bool bricksGenerated = false;

 
    [Header("START Button")]
    public RectTransform startButtonBrick;


    [Header("UI")]
    public TMP_Text missText;
    public TMP_Text timerText;
    public TMP_Text hintText;



  
    public float ballSpeed = 700f;

   
    public int maxMissCount = 5;

 
    public float paddleBonusWidth = 80f;

   
    public float startHintRemainingTime = 15f;



    private BreakoutBallUI currentBall;

    private float remainingTime;
    private int missCount = 0;

    private Vector2 originalPaddleSize;

    private Image startButtonImage;
    private Color startButtonOriginalColor;

    private bool hintShown = false;



    public bool IsGameRunning()
    {
        return isGameActive;
    }

    public RectTransform GetPlayArea()
    {
        return playArea;
    }

    public RectTransform GetPaddle()
    {
        return paddle;
    }


 

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "벽돌깨기 실행 검사";

        instruction =
            "벽돌을 제거하고 START 버튼을 맞추세요.";

        remainingTime = timeLimit;

        missCount = 0;
        hintShown = false;


        // 패들 원래 크기 저장
        if (paddle != null)
        {
            originalPaddleSize =
                paddle.sizeDelta;

            paddle.sizeDelta =
                originalPaddleSize;
        }


        // START 버튼 원래 색 저장
        if (startButtonBrick != null)
        {
            startButtonImage =
                startButtonBrick.GetComponent<Image>();

            if (startButtonImage != null)
            {
                startButtonOriginalColor =
                    startButtonImage.color;
            }
        }


        // 벽돌 자동 생성
        GenerateBrickRows();


        // 모든 벽돌 다시 활성화
        ResetAllBricks();


        UpdateUI();


        // 공 생성
        SpawnBall();
    }




    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;


        // 시간 감소
        remainingTime -= Time.deltaTime;


        // 시간 종료
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            UpdateUI();

            Fail();

            return;
        }


        // 패들 이동
        MovePaddleWithMouse();


        // UI 갱신
        UpdateUI();


        // START 힌트
        if (!hintShown &&
            remainingTime <= startHintRemainingTime)
        {
            hintShown = true;

            ShowStartButtonHint();
        }
    }


  
    private void GenerateBrickRows()
    {
        // 이미 만들어졌다면 다시 생성하지 않음
        if (bricksGenerated)
            return;


        if (brickRowTemplate == null)
        {
            Debug.LogError(
                "Brick Row Template이 연결되지 않았습니다."
            );

            return;
        }


        if (brickParent == null)
        {
            brickParent = playArea;
        }


        normalBricks.Clear();


   

        brickRowTemplate.name =
            "BrickRow_01";


        AddRowBricksToList(
            brickRowTemplate,
            0
        );



        for (int row = 1;
             row < brickRowCount;
             row++)
        {
            GameObject newRowObject =
                Instantiate(
                    brickRowTemplate.gameObject,
                    brickParent
                );


            newRowObject.name =
                $"BrickRow_{row + 1:00}";


            RectTransform newRow =
                newRowObject
                    .GetComponent<RectTransform>();


            // 첫 번째 줄 기준으로 아래쪽 생성
            newRow.anchoredPosition =
                brickRowTemplate.anchoredPosition +
                Vector2.down *
                brickRowSpacing *
                row;


            AddRowBricksToList(
                newRow,
                row
            );
        }


        bricksGenerated = true;


        Debug.Log(
            $"벽돌 자동 생성 완료 : {normalBricks.Count}개"
        );
    }


    private void AddRowBricksToList(
        RectTransform row,
        int rowIndex)
    {
        if (row == null)
            return;


        Color rowColor =
            Color.white;


        // 줄별 색상 선택
        if (brickRowColors != null &&
            brickRowColors.Length > 0)
        {
            int colorIndex =
                rowIndex %
                brickRowColors.Length;


            rowColor =
                brickRowColors[
                    colorIndex
                ];
        }


        for (int i = 0;
             i < row.childCount;
             i++)
        {
            Transform child =
                row.GetChild(i);


            RectTransform brick =
                child
                    .GetComponent<RectTransform>();


            if (brick == null)
                continue;


            normalBricks.Add(brick);


            brick.name =
                $"Normal_Block_R{rowIndex + 1}_C{i + 1}";


            // 색상 설정
            Image image =
                brick.GetComponent<Image>();


            if (image != null)
            {
                image.color =
                    rowColor;

                image.raycastTarget =
                    false;
            }
        }
    }



    private void MovePaddleWithMouse()
    {
        if (playArea == null ||
            paddle == null)
        {
            return;
        }


        Vector2 localPoint;


        Canvas canvas =
            GetComponentInParent<Canvas>();


        Camera uiCamera = null;


        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                canvas.worldCamera;
        }


        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                playArea,
                Input.mousePosition,
                uiCamera,
                out localPoint
            );


        float halfWidth =
            paddle.rect.width * 0.5f;


        float minX =
            playArea.rect.xMin +
            halfWidth;


        float maxX =
            playArea.rect.xMax -
            halfWidth;


        Vector2 position =
            paddle.anchoredPosition;


        position.x =
            Mathf.Clamp(
                localPoint.x,
                minX,
                maxX
            );


        paddle.anchoredPosition =
            position;
    }


  
    private void SpawnBall()
    {
        if (!isGameActive)
            return;


        if (ballPrefab == null ||
            ballSpawnPoint == null ||
            playArea == null)
        {
            Debug.LogError(
                "Ball Prefab / Ball Spawn Point / Play Area를 확인해주세요."
            );

            return;
        }


        if (currentBall != null)
        {
            Destroy(
                currentBall.gameObject
            );
        }


        currentBall =
            Instantiate(
                ballPrefab,
                playArea
            );


        RectTransform ballRect =
            currentBall
                .GetComponent<RectTransform>();


        ballRect.anchoredPosition =
            ballSpawnPoint
                .anchoredPosition;


        // 위쪽으로 시작
        Vector2 direction =
            new Vector2(
                Random.Range(
                    -0.45f,
                    0.45f
                ),
                1f
            )
            .normalized;


        currentBall.Launch(
            this,
            direction,
            ballSpeed
        );
    }


    public void OnBallLost(
        BreakoutBallUI ball)
    {
        if (!isGameActive)
            return;


        if (ball != null)
        {
            Destroy(
                ball.gameObject
            );
        }


        currentBall = null;


        missCount++;


        // MISS 2회 이상이면 패들 확대
        if (missCount >= 2 &&
            paddle != null)
        {
            paddle.sizeDelta =
                new Vector2(
                    originalPaddleSize.x +
                    paddleBonusWidth,

                    originalPaddleSize.y
                );
        }


        UpdateUI();


        // 최대 MISS 도달
        if (missCount >=
            maxMissCount)
        {
            Fail();

            return;
        }


        // 다시 공 생성
        SpawnBall();
    }


 
    public void OnHitNormalBrick(
        RectTransform brick)
    {
        if (!isGameActive)
            return;


        if (brick != null)
        {
            brick.gameObject
                .SetActive(false);
        }
    }


    public void OnHitStartButton()
    {
        if (!isGameActive)
            return;


        Debug.Log(
            "START 버튼 명중! 미니게임 성공!"
        );


        if (currentBall != null)
        {
            Destroy(
                currentBall.gameObject
            );

            currentBall = null;
        }


        Success();
    }


  
    private void ResetAllBricks()
    {
        foreach (
            RectTransform brick
            in normalBricks)
        {
            if (brick != null)
            {
                brick.gameObject
                    .SetActive(true);
            }
        }


        if (startButtonBrick != null)
        {
            startButtonBrick.gameObject
                .SetActive(true);


            if (startButtonImage != null)
            {
                startButtonImage.color =
                    startButtonOriginalColor;
            }
        }
    }



    private void ShowStartButtonHint()
    {
        if (hintText != null)
        {
            hintText.text =
                "HINT : START 버튼을 맞추세요.";
        }


        if (startButtonImage != null)
        {
            startButtonImage.color =
                Color.green;
        }
    }




    private void UpdateUI()
    {
        if (missText != null)
        {
            missText.text =
                "MISS " +
                missCount;
        }


        if (timerText != null)
        {
            int minute =
                Mathf.FloorToInt(
                    remainingTime / 60f
                );


            int second =
                Mathf.FloorToInt(
                    remainingTime % 60f
                );


            timerText.text =
                $"TIME {minute:00}:{second:00}";
        }
    }


    
    protected override void GiveHint()
    {
        ShowStartButtonHint();
    }


    

    protected override void RestartGame()
    {
        isGameActive = false;


        if (currentBall != null)
        {
            Destroy(
                currentBall.gameObject
            );

            currentBall = null;
        }


        StartMinigame();
    }
}