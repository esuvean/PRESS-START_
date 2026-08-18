using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame8_TiltBoard : MinigameBase,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Reference")]
    public RectTransform board;
    public RectTransform movingButton;
    public RectTransform goalSlot;
    public TextMeshProUGUI statusText;

    [Header("Tilt Settings")]
    public float maxTiltAngle = 15f;
    public float tiltResponse = 6f;
    public float acceleration = 900f;
    public float friction = 0.985f;
    public float fallMargin = 80f;

    private float targetTilt = 0f;
    private float currentTilt = 0f;
    private float velocityX = 0f;
    private Vector2 startPosition;

    public override void StartMinigame()
    {
        base.StartMinigame();

        gameName = "화면 방향 보정 검사";
        instruction = "게임판을 기울여 START 버튼을 목표 슬롯까지 이동시킨다.";

        if (movingButton != null)
            startPosition = movingButton.anchoredPosition;

        ResetButton();

        if (statusText != null)
            statusText.text = "화면을 좌우로 드래그해 판을 기울이세요.";
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || board == null || movingButton == null)
            return;

        currentTilt = Mathf.Lerp(
            currentTilt,
            targetTilt,
            1f - Mathf.Exp(-tiltResponse * Time.deltaTime)
        );

        board.localRotation = Quaternion.Euler(
            0f, 0f, -currentTilt * maxTiltAngle
        );

        velocityX += currentTilt * acceleration * Time.deltaTime;
        velocityX *= Mathf.Pow(friction, Time.deltaTime * 60f);

        Vector2 pos = movingButton.anchoredPosition;
        pos.x += velocityX * Time.deltaTime;
        movingButton.anchoredPosition = pos;

        CheckGoal();
        CheckFall();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isGameActive) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isGameActive) return;

        targetTilt += eventData.delta.x / 180f;
        targetTilt = Mathf.Clamp(targetTilt, -1f, 1f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        targetTilt = 0f;
    }

    private void CheckGoal()
    {
        if (goalSlot == null) return;

        if (WorldRect(movingButton).Overlaps(WorldRect(goalSlot)))
        {
            if (statusText != null)
                statusText.text = "목표 슬롯 도착";

            Success();
        }
    }

    private void CheckFall()
    {
        float halfWidth = board.rect.width * 0.5f;

        if (Mathf.Abs(movingButton.anchoredPosition.x) >
            halfWidth + fallMargin)
        {
            if (statusText != null)
                statusText.text = "버튼이 게임판 밖으로 떨어졌습니다.";

            ResetButton();
        }
    }

    private void ResetButton()
    {
        if (movingButton != null)
            movingButton.anchoredPosition = startPosition;

        velocityX = 0f;
        targetTilt = 0f;
        currentTilt = 0f;

        if (board != null)
            board.localRotation = Quaternion.identity;
    }

    private Rect WorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        return Rect.MinMaxRect(
            corners[0].x, corners[0].y,
            corners[2].x, corners[2].y
        );
    }

    protected override void GiveHint()
    {
        if (statusText != null)
            statusText.text = "힌트: 천천히 기울여 버튼 속도를 조절하세요.";
    }

    protected override void RestartGame()
    {
        ResetButton();
    }
}
