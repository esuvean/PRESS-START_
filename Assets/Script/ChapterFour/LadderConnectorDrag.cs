
using UnityEngine;
using UnityEngine.EventSystems;

public class LadderConnectorDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Minigame19_StartLadder game;
    private RectTransform rect;
    private RectTransform dragLayer;

    private Transform previousParent;
    private Vector2 previousPosition;
    private Vector2 previousSize;

    private int rowIndex;
    private bool dragging;

    public void Initialize(
        Minigame19_StartLadder owner,
        int row,
        RectTransform layer)
    {
        game = owner;
        rowIndex = row;
        dragLayer = layer;
        rect = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (game == null || !game.CanDrag(rowIndex))
            return;

        dragging = true;

        previousParent = rect.parent;
        previousPosition = rect.anchoredPosition;
        previousSize = rect.sizeDelta;

        rect.SetParent(dragLayer, true);
        rect.SetAsLastSibling();

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || dragLayer == null)
            return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragLayer,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPosition))
        {
            rect.position = worldPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;

        if (game != null &&
            game.TryDropConnector(rowIndex, rect, eventData))
        {
            return;
        }

        rect.SetParent(previousParent, false);
        rect.anchoredPosition = previousPosition;
        rect.sizeDelta = previousSize;
    }
}
