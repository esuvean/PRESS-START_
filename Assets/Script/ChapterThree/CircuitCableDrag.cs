using UnityEngine;
using UnityEngine.EventSystems;

public class CircuitCableDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Settings")]
    public Minigame14_PowerCircuit gameManager;

    [Tooltip("Cable1 = 0 / Cable2 = 1 / Cable3 = 2")]
    public int cableIndex;


    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (gameManager == null)
            return;

        gameManager.BeginCableDrag(
            cableIndex
        );
    }


    public void OnDrag(
        PointerEventData eventData)
    {
        if (gameManager == null)
            return;

        gameManager.DragCable(
            cableIndex,
            eventData
        );
    }


    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (gameManager == null)
            return;

        gameManager.EndCableDrag(
            cableIndex,
            eventData
        );
    }
}