using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Minigame9_FallingStart : MinigameBase
{
    [Header("UI Reference")]
    public RectTransform playArea;
    public RectTransform paddle;
    public GameObject fallingItemPrefab;
    public Canvas canvas;
    public TextMeshProUGUI statusText;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.9f;
    public float minSpeed = 180f;
    public float maxSpeed = 320f;
    [Range(0f, 1f)]
    public float targetChance = 0.38f;

    [Header("Goal")]
    public int targetCatchGoal = 5;

    private readonly string[] wrongLabels =
    {
        "종료하기",
        "취소하기",
        "나중에 하기",
        "다시 하기",
        "시작 안 함"
    };

    private int successCount = 0;
    private int wrongCount = 0;
    private float basePaddleWidth;
    private Coroutine spawnRoutine;

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "버튼 수신 검사";
        instruction = "떨어지는 버튼 중 '시작하기' 버튼만 받는다.";

        successCount = 0;
        wrongCount = 0;

        if (paddle != null)
            basePaddleWidth = paddle.rect.width;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        UpdateStatus();

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || paddle == null || playArea == null)
            return;

        Vector2 screenPos = Input.mousePosition;

        if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();

        Camera cam = null;
        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea, screenPos, cam, out Vector2 localPos))
        {
            float halfPaddle = paddle.rect.width * 0.5f;
            float minX = playArea.rect.xMin + halfPaddle;
            float maxX = playArea.rect.xMax - halfPaddle;

            Vector2 p = paddle.anchoredPosition;
            p.x = Mathf.Clamp(localPos.x, minX, maxX);
            paddle.anchoredPosition = p;
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (isGameActive)
        {
            SpawnItem();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnItem()
    {
        if (fallingItemPrefab == null || playArea == null)
            return;

        bool target = Random.value <= targetChance;
        string label = target
            ? "시작하기"
            : wrongLabels[Random.Range(0, wrongLabels.Length)];

        GameObject obj = Instantiate(fallingItemPrefab, playArea);
        RectTransform rt = obj.transform as RectTransform;

        if (rt != null)
        {
            float half = rt.rect.width * 0.5f;
            float x = Random.Range(
                playArea.rect.xMin + half,
                playArea.rect.xMax - half
            );

            rt.anchoredPosition = new Vector2(
                x,
                playArea.rect.yMax + 60f
            );
        }

        FallingButtonItem item = obj.GetComponent<FallingButtonItem>();

        if (item != null)
        {
            item.Init(
                this,
                target,
                label,
                Random.Range(minSpeed, maxSpeed),
                paddle,
                playArea
            );
        }
    }

    public void OnItemCaught(bool target)
    {
        if (!isGameActive) return;

        if (target)
        {
            successCount++;
            ResizePaddle(Mathf.Min(basePaddleWidth * 1.25f,
                paddle.rect.width + 12f));

            if (successCount >= targetCatchGoal)
            {
                UpdateStatus();
                Success();
                return;
            }
        }
        else
        {
            wrongCount++;
            ResizePaddle(Mathf.Max(basePaddleWidth * 0.55f,
                paddle.rect.width - 24f));
        }

        UpdateStatus();
    }

    private void ResizePaddle(float width)
    {
        if (paddle == null) return;
        paddle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    private void UpdateStatus()
    {
        if (statusText != null)
            statusText.text = $"START 수신 {successCount} / {targetCatchGoal}";
    }

    protected override void GiveHint()
    {
        if (statusText != null)
            statusText.text = $"힌트: '시작하기'만 받으세요. ({successCount}/{targetCatchGoal})";
    }

    protected override void RestartGame()
    {
        successCount = 0;
        wrongCount = 0;

        if (paddle != null && basePaddleWidth > 0f)
            ResizePaddle(basePaddleWidth);

        UpdateStatus();
    }

    private void OnDestroy()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
    }
}
