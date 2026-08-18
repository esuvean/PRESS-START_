using UnityEngine;
using TMPro;

public class FallingButtonItem : MonoBehaviour
{
    public RectTransform rectTransform;
    public TextMeshProUGUI labelText;

    private Minigame9_FallingStart owner;
    private bool isTarget;
    private float speed;
    private RectTransform paddle;
    private RectTransform playArea;
    private bool consumed = false;

    public void Init(
        Minigame9_FallingStart gameOwner,
        bool target,
        string label,
        float fallSpeed,
        RectTransform paddleRect,
        RectTransform areaRect)
    {
        owner = gameOwner;
        isTarget = target;
        speed = fallSpeed;
        paddle = paddleRect;
        playArea = areaRect;

        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (labelText != null)
            labelText.text = label;
    }

    private void Update()
    {
        if (consumed || rectTransform == null)
            return;

        rectTransform.anchoredPosition += Vector2.down * speed * Time.deltaTime;

        if (paddle != null && WorldRect(rectTransform).Overlaps(WorldRect(paddle)))
        {
            consumed = true;

            if (owner != null)
                owner.OnItemCaught(isTarget);

            Destroy(gameObject);
            return;
        }

        if (playArea != null &&
            rectTransform.anchoredPosition.y < playArea.rect.yMin - 120f)
        {
            Destroy(gameObject);
        }
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
}
