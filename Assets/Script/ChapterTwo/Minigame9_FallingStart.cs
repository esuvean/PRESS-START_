using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Minigame9_FallingStart : MinigameBase
{
    // ============================================
    // UI
    // ============================================

    [Header("UI")]
    public Canvas canvas;

    // 화면 우측 상단
    // START CATCH 0 / 5
    public TextMeshProUGUI scoreText;

    // 화면 아래쪽 상태 안내
    public TextMeshProUGUI statusText;


    // ============================================
    // 게임 영역
    // ============================================

    [Header("Game Area")]
    public RectTransform playArea;
    public RectTransform paddle;


    // ============================================
    // 떨어지는 버튼 Prefab
    // ============================================

    [Header("Prefab")]
    public GameObject fallingButtonPrefab;


    // ============================================
    // 생성 설정
    // ============================================

    [Header("Spawn Settings")]

    // 버튼이 몇 초마다 생성되는지
    public float spawnInterval = 0.8f;

    // 최소 낙하 속도
    public float minFallSpeed = 180f;

    // 최대 낙하 속도
    public float maxFallSpeed = 280f;

    // START가 나올 확률
    [Range(0f, 1f)]
    public float startButtonChance = 0.35f;


    // ============================================
    // 목표
    // ============================================

    [Header("Goal")]

    // START 몇 개 받으면 성공?
    public int goalCount = 5;


    // ============================================
    // Paddle 설정
    // ============================================

    [Header("Paddle")]

    public float normalPaddleWidth = 260f;

    public float minimumPaddleWidth = 130f;

    public float maximumPaddleWidth = 360f;


    // ============================================
    // 버튼 색상
    // ============================================

    [Header("Button Colors")]

    // 처음에는 모든 버튼 같은 색
    public Color normalButtonColor = Color.white;

    // 힌트가 발동되면 START만 민트색
    public Color startHintColor =
        new Color(
            0.40f,
            1f,
            0.84f,
            1f
        );

    // 버튼 글자색
    public Color buttonTextColor = Color.white;


    // ============================================
    // 내부 변수
    // ============================================

    private int successCount = 0;
    private int wrongCount = 0;

    private int combo = 0;

    private bool started = false;

    // 힌트가 켜졌는지
    private bool hintMode = false;

    private Coroutine spawnCoroutine;
    private Coroutine paddleRestoreCoroutine;


    // ============================================
    // 오답 버튼 목록
    // ============================================

    private readonly string[] wrongButtonTexts =
    {
        "LATER",
        "RETRY",
        "CANCEL",
        "QUIT",
        "NOT NOW"
    };


    // ============================================
    // 시작
    // ============================================

    private void Start()
    {
        if (!started)
        {
            StartMinigame();
        }
    }


    public override void StartMinigame()
    {
        if (started)
            return;

        started = true;

        gameName =
            "Button Catch Test";

        instruction =
            "Catch only the START buttons.";

        base.StartMinigame();


        // 처음 상태 초기화
        successCount = 0;
        wrongCount = 0;
        combo = 0;

        // 처음에는 힌트 OFF
        hintMode = false;


        // Paddle 크기 초기화
        if (paddle != null)
        {
            SetPaddleWidth(
                normalPaddleWidth
            );
        }


        UpdateUI();


        if (statusText != null)
        {
            statusText.text =
                "CATCH THE START BUTTON";
        }


        // 버튼 생성 시작
        if (spawnCoroutine != null)
        {
            StopCoroutine(
                spawnCoroutine
            );
        }

        spawnCoroutine =
            StartCoroutine(
                SpawnLoop()
            );
    }


    // ============================================
    // Update
    // ============================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        MovePaddle();
    }


    // ============================================
    // Paddle 마우스 좌우 이동
    // ============================================

    private void MovePaddle()
    {
        if (Mouse.current == null)
            return;

        if (playArea == null ||
            paddle == null)
        {
            return;
        }


        Vector2 mousePosition =
            Mouse.current
                .position
                .ReadValue();


        Camera uiCamera = null;


        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                canvas.worldCamera;
        }


        Vector2 localPoint;


        bool converted =
            RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                playArea,
                mousePosition,
                uiCamera,
                out localPoint
            );


        if (!converted)
            return;


        float halfPaddle =
            paddle.rect.width * 0.5f;


        float minX =
            playArea.rect.xMin
            + halfPaddle;


        float maxX =
            playArea.rect.xMax
            - halfPaddle;


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


    // ============================================
    // 버튼 계속 생성
    // ============================================

    private IEnumerator SpawnLoop()
    {
        // 처음 시작 후 약간 대기
        yield return
            new WaitForSeconds(
                0.5f
            );


        while (isGameActive)
        {
            SpawnButton();


            yield return
                new WaitForSeconds(
                    spawnInterval
                );
        }
    }


    // ============================================
    // 버튼 하나 생성
    // ============================================

    private void SpawnButton()
    {
        if (fallingButtonPrefab == null)
            return;

        if (playArea == null)
            return;


        // START인지 결정
        bool isStart =
            Random.value
            <= startButtonChance;


        // 버튼 글자
        string text;


        if (isStart)
        {
            text = "START";
        }
        else
        {
            text =
                wrongButtonTexts[
                    Random.Range(
                        0,
                        wrongButtonTexts.Length
                    )
                ];
        }


        // Prefab 생성
        GameObject newButton =
            Instantiate(
                fallingButtonPrefab,
                playArea
            );


        RectTransform rt =
            newButton.transform
            as RectTransform;


        // 랜덤 X 위치
        if (rt != null)
        {
            float halfWidth =
                rt.rect.width * 0.5f;


            float randomX =
                Random.Range(
                    playArea.rect.xMin
                    + halfWidth,

                    playArea.rect.xMax
                    - halfWidth
                );


            // PlayArea 위쪽에서 등장
            rt.anchoredPosition =
                new Vector2(
                    randomX,
                    playArea.rect.yMax
                    + 60f
                );
        }


        // FallingButtonItem 설정
        FallingButtonItem item =
            newButton.GetComponent<
                FallingButtonItem
            >();


        if (item != null)
        {
            item.Setup(
                this,
                paddle,
                playArea,
                isStart,
                text,

                Random.Range(
                    minFallSpeed,
                    maxFallSpeed
                ),

                normalButtonColor,
                startHintColor,
                buttonTextColor,

                hintMode
            );
        }
    }


    // ============================================
    // Paddle이 버튼을 받음
    // ============================================

    public void CatchButton(
        bool isStartButton)
    {
        if (!isGameActive)
            return;


        // ========================================
        // START 정답
        // ========================================

        if (isStartButton)
        {
            successCount++;

            combo++;


            if (statusText != null)
            {
                statusText.text =
                    "START RECEIVED";
            }


            // START를 연속으로 2개 이상 받으면
            // Paddle 조금 커짐
            if (combo >= 2)
            {
                float newWidth =
                    paddle.rect.width
                    + 15f;


                newWidth =
                    Mathf.Min(
                        newWidth,
                        maximumPaddleWidth
                    );


                SetPaddleWidth(
                    newWidth
                );
            }


            UpdateUI();


            // ====================================
            // 목표 달성
            // ====================================

            if (successCount >=
                goalCount)
            {
                if (spawnCoroutine != null)
                {
                    StopCoroutine(
                        spawnCoroutine
                    );

                    spawnCoroutine = null;
                }


                if (statusText != null)
                {
                    statusText.text =
                        "BUTTON CATCH TEST COMPLETE";
                }


                Success();
            }
        }


        // ========================================
        // 오답
        // ========================================

        else
        {
            wrongCount++;

            // 연속 성공 초기화
            combo = 0;


            if (statusText != null)
            {
                statusText.text =
                    "WRONG BUTTON";
            }


            // Paddle 잠깐 작아짐
            ShrinkPaddle();


            UpdateUI();
        }
    }


    // ============================================
    // 틀렸을 때 Paddle 축소
    // ============================================

    private void ShrinkPaddle()
    {
        if (paddle == null)
            return;


        float newWidth =
            paddle.rect.width
            * 0.70f;


        newWidth =
            Mathf.Max(
                newWidth,
                minimumPaddleWidth
            );


        SetPaddleWidth(
            newWidth
        );


        // 기존 복구 코루틴이 있으면 취소
        if (paddleRestoreCoroutine != null)
        {
            StopCoroutine(
                paddleRestoreCoroutine
            );
        }


        paddleRestoreCoroutine =
            StartCoroutine(
                RestorePaddle()
            );
    }


    // ============================================
    // Paddle 원래 크기로 복구
    // ============================================

    private IEnumerator RestorePaddle()
    {
        yield return
            new WaitForSeconds(
                1.2f
            );


        if (paddle != null)
        {
            SetPaddleWidth(
                normalPaddleWidth
            );
        }


        paddleRestoreCoroutine = null;
    }


    // ============================================
    // Paddle Width 설정
    // ============================================

    private void SetPaddleWidth(
        float width)
    {
        if (paddle == null)
            return;


        paddle.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );
    }


    // ============================================
    // 점수 UI
    // ============================================

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                $"START CATCH  {successCount} / {goalCount}";
        }
    }


    // ============================================
    // HINT
    // ============================================

    protected override void GiveHint()
    {
        // 힌트 활성화
        hintMode = true;


        if (statusText != null)
        {
            statusText.text =
                "HINT : START SIGNAL HIGHLIGHTED";
        }


        // ========================================
        // 현재 이미 떨어지고 있는 START도
        // 민트색으로 변경
        // ========================================

        if (playArea != null)
        {
            FallingButtonItem[] items =
                playArea
                .GetComponentsInChildren<
                    FallingButtonItem
                >();


            foreach (
                FallingButtonItem item
                in items
            )
            {
                item.ApplyHint();
            }
        }
    }


    // ============================================
    // Restart
    // ============================================

    protected override void RestartGame()
    {
        successCount = 0;

        wrongCount = 0;

        combo = 0;

        // 힌트 다시 OFF
        hintMode = false;


        SetPaddleWidth(
            normalPaddleWidth
        );


        UpdateUI();


        if (statusText != null)
        {
            statusText.text =
                "CATCH THE START BUTTON";
        }
    }


    // ============================================
    // 삭제될 때 Coroutine 정리
    // ============================================

    private void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(
                spawnCoroutine
            );
        }


        if (paddleRestoreCoroutine != null)
        {
            StopCoroutine(
                paddleRestoreCoroutine
            );
        }
    }
}