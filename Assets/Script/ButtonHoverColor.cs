using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverColor : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("색상이 바뀔 대상")]
    [SerializeField] private Graphic buttonImage;
    [SerializeField] private Graphic buttonText;

    [Header("빛나는 효과")]
    [SerializeField] private Outline buttonGlow;
    [SerializeField] private Outline textGlow;

    [Header("색상")]
    [SerializeField] private Color normalColor = Color.white;

    [SerializeField]
    private Color hoverColor =
        new Color32(102, 255, 214, 255); // #66FFD6

    [SerializeField]
    private Color glowColor =
        new Color32(102, 255, 214, 170); // 반투명 민트

    private void Awake()
    {
        SetNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormal();
    }

    private void SetHover()
    {
        // 테두리와 글자 민트색
        if (buttonImage != null)
            buttonImage.color = hoverColor;

        if (buttonText != null)
            buttonText.color = hoverColor;

        // 빛나는 효과 켜기
        if (buttonGlow != null)
        {
            buttonGlow.effectColor = glowColor;
            buttonGlow.enabled = true;
        }

        if (textGlow != null)
        {
            textGlow.effectColor = glowColor;
            textGlow.enabled = true;
        }
    }

    private void SetNormal()
    {
        // 원래 흰색으로 복구
        if (buttonImage != null)
            buttonImage.color = normalColor;

        if (buttonText != null)
            buttonText.color = normalColor;

        // 빛나는 효과 끄기
        if (buttonGlow != null)
            buttonGlow.enabled = false;

        if (textGlow != null)
            textGlow.enabled = false;
    }

    private void OnDisable()
    {
        SetNormal();
    }
}