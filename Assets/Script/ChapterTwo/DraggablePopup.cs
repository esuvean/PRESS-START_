using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggablePopup : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Popup")]
    public RectTransform popupRect;
    public Canvas canvas;

    // 팝업이 움직일 수 있는 화면 영역
    public RectTransform moveArea;

    public Button closeButton;

    [Header("Close Trap")]
    public bool closable = true;
    public bool spawnTwoOnClose = false;

    public GameObject nuisancePopupPrefab;
    public Transform popupParent;

    private void Awake()
    {
        if (popupRect == null)
            popupRect = transform as RectTransform;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        // 지정 안 했으면 부모 영역 사용
        if (moveArea == null)
            moveArea = popupRect.parent as RectTransform;

        if (closeButton != null)
        {
            closeButton.interactable = closable;

            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 클릭한 팝업을 가장 앞으로
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (popupRect == null || moveArea == null)
            return;

        float scaleFactor = canvas != null
            ? canvas.scaleFactor
            : 1f;

        popupRect.anchoredPosition +=
            eventData.delta / Mathf.Max(scaleFactor, 0.01f);

        ClampPopup();
    }

    private void ClampPopup()
    {
        if (popupRect == null || moveArea == null)
            return;

        Rect area = moveArea.rect;

        float popupWidth =
            popupRect.rect.width * Mathf.Abs(popupRect.localScale.x);

        float popupHeight =
            popupRect.rect.height * Mathf.Abs(popupRect.localScale.y);

        float minX =
            area.xMin +
            popupWidth * popupRect.pivot.x;

        float maxX =
            area.xMax -
            popupWidth * (1f - popupRect.pivot.x);

        float minY =
            area.yMin +
            popupHeight * popupRect.pivot.y;

        float maxY =
            area.yMax -
            popupHeight * (1f - popupRect.pivot.y);

        Vector2 pos = popupRect.anchoredPosition;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        popupRect.anchoredPosition = pos;
    }

    public void ClosePopup()
    {
        if (!closable)
            return;

        if (spawnTwoOnClose &&
            nuisancePopupPrefab != null)
        {
            Transform parent =
                popupParent != null
                ? popupParent
                : transform.parent;

            for (int i = 0; i < 2; i++)
            {
                GameObject clone =
                    Instantiate(
                        nuisancePopupPrefab,
                        parent
                    );

                RectTransform rt =
                    clone.transform as RectTransform;

                if (rt != null)
                {
                    rt.anchoredPosition +=
                        new Vector2(
                            Random.Range(-160f, 160f),
                            Random.Range(-100f, 100f)
                        );
                }
            }
        }

        Destroy(gameObject);
    }
}