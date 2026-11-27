using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>챕터5 버튼: 마우스를 올리면 살짝 커짐 (C5Kit.MakeButton 이 자동으로 붙임)</summary>
public class C5Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.06f;
    public float speed = 14f;

    private bool over;
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        over = false;
        transform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        over = selectable == null || selectable.interactable;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        over = false;
    }

    private void Update()
    {
        if (selectable != null && !selectable.interactable)
            over = false;

        // 마우스가 올라와 있지 않고 크기가 이미 원래대로면 손대지 않음 (다른 연출과 겹치지 않게)
        float current = transform.localScale.x;
        float target = over ? hoverScale : 1f;
        if (!over && Mathf.Abs(current - 1f) < 0.002f)
            return;

        float s = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        if (!over && Mathf.Abs(s - 1f) < 0.002f)
            s = 1f;
        transform.localScale = new Vector3(s, s, 1f);
    }
}
