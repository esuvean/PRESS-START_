using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// MG18 답 버튼: 마우스를 올리면 살짝 커지고, 내리면 원래 크기로 돌아옴
public class Minigame18_ButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public float hoverScale = 1.05f;
    public float speed = 14f;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
    }

    private void Update()
    {
        if (button != null && !button.interactable)
            targetScale = baseScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed
        );
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }
}
