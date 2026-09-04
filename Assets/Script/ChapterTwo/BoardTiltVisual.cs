using UnityEngine;

public class BoardTiltVisual : MonoBehaviour
{
    [Header("Target")]
    public RectTransform visual;

    [Header("Tilt Settings")]
    public float horizontalAngle = 7f;
    public float verticalSquash = 0.10f;
    public float horizontalSquash = 0.06f;
    public float positionOffset = 12f;
    public float smoothSpeed = 8f;

    private Vector2 targetTilt;
    private Vector2 currentTilt;

    private Vector3 originalScale;
    private Vector2 originalPosition;

    private void Awake()
    {
        if (visual == null)
            visual = transform as RectTransform;

        originalScale = visual.localScale;
        originalPosition = visual.anchoredPosition;
    }

    public void SetTilt(Vector2 tilt)
    {
        targetTilt = Vector2.ClampMagnitude(tilt, 1f);
    }

    private void Update()
    {
        currentTilt = Vector2.Lerp(
            currentTilt,
            targetTilt,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (visual == null)
            return;

        // 좌우 드래그
        // 판 전체가 좌우로 살짝 기울어짐
        float zRotation =
            -currentTilt.x * horizontalAngle;

        visual.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                zRotation
            );

        // 위/아래 드래그
        // 판 높이가 눌리면서 원근감처럼 보임
        float scaleY =
            1f -
            Mathf.Abs(currentTilt.y) * verticalSquash;

        // 좌우도 살짝 압축
        float scaleX =
            1f -
            Mathf.Abs(currentTilt.x) * horizontalSquash;

        visual.localScale =
            new Vector3(
                originalScale.x * scaleX,
                originalScale.y * scaleY,
                originalScale.z
            );

        // 위로 기울이면 판이 약간 위로
        // 아래로 기울이면 판이 약간 아래로
        visual.anchoredPosition =
            originalPosition +
            new Vector2(
                currentTilt.x * positionOffset,
                currentTilt.y * positionOffset
            );
    }

    public void ResetTilt()
    {
        targetTilt = Vector2.zero;
    }
}