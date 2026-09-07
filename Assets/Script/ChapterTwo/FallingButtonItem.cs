using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FallingButtonItem : MonoBehaviour
{
    [Header("UI")]
    public RectTransform rectTransform;
    public Image buttonImage;
    public TextMeshProUGUI labelText;

    private Minigame9_FallingStart owner;
    private RectTransform paddle;
    private RectTransform playArea;

    private bool isStartButton;
    private float fallSpeed;
    private bool handled = false;

    private Color normalColor;
    private Color hintColor;

    public void Setup(
        Minigame9_FallingStart gameOwner,
        RectTransform paddleRect,
        RectTransform playAreaRect,
        bool startButton,
        string buttonText,
        float speed,
        Color normalButtonColor,
        Color startHintColor,
        Color textColor,
        bool hintActive)
    {
        owner = gameOwner;
        paddle = paddleRect;
        playArea = playAreaRect;

        isStartButton = startButton;
        fallSpeed = speed;

        normalColor = normalButtonColor;
        hintColor = startHintColor;

        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (labelText != null)
        {
            labelText.text = buttonText;
            labelText.color = textColor;
        }

        if (buttonImage != null)
        {
            // 처음엔 모두 같은 색
            buttonImage.color = normalColor;

            // 힌트가 이미 켜졌다면 START만 민트
            if (hintActive && isStartButton)
                buttonImage.color = hintColor;
        }
    }

    // 힌트 발동 시 호출
    public void ApplyHint()
    {
        if (isStartButton && buttonImage != null)
        {
            buttonImage.color = hintColor;
        }
    }

    private void Update()
    {
        if (handled || rectTransform == null)
            return;

        rectTransform.anchoredPosition +=
            Vector2.down * fallSpeed * Time.deltaTime;

        if (paddle != null &&
            GetWorldRect(rectTransform).Overlaps(GetWorldRect(paddle)))
        {
            handled = true;

            if (owner != null)
                owner.CatchButton(isStartButton);

            Destroy(gameObject);
            return;
        }

        if (playArea != null)
        {
            if (rectTransform.anchoredPosition.y <
                playArea.rect.yMin - 100f)
            {
                Destroy(gameObject);
            }
        }
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];

        rt.GetWorldCorners(corners);

        return Rect.MinMaxRect(
            corners[0].x,
            corners[0].y,
            corners[2].x,
            corners[2].y
        );
    }
}