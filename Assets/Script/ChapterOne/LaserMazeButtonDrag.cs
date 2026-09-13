using UnityEngine;
using UnityEngine.EventSystems;

public class LaserMazeButtonDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public RectTransform mazeArea;
    public Minigame5_LaserMaze mainController;

    private RectTransform rectTransform;
    private Vector2 dragOffset;
    private bool isHit = false; 

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainController != null && !mainController.IsGameActive()) return;

        isHit = false; 

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragOffset = rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainController != null && !mainController.IsGameActive()) return;
        if (isHit) return; 

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            
            rectTransform.anchoredPosition = localPoint + dragOffset;

           
            ClampToMazeArea();

            
            if (mainController != null)
            {
                mainController.CheckCollisions();
                mainController.CheckGoalArrival();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isHit = false; 
    }

   
    public void ForceEndDrag()
    {
        isHit = true;
    }

    public void ClampToMazeArea()
    {
        if (mazeArea == null || rectTransform == null) return;

        Vector3[] mazeCorners = new Vector3[4];
        Vector3[] buttonCorners = new Vector3[4];
        mazeArea.GetWorldCorners(mazeCorners);
        rectTransform.GetWorldCorners(buttonCorners);

        Transform parentTransform = rectTransform.parent;

        Vector3 mazeMin = parentTransform.InverseTransformPoint(mazeCorners[0]);
        Vector3 mazeMax = parentTransform.InverseTransformPoint(mazeCorners[2]);

        Vector3 buttonMin = parentTransform.InverseTransformPoint(buttonCorners[0]);
        Vector3 buttonMax = parentTransform.InverseTransformPoint(buttonCorners[2]);

        float halfWidth = Mathf.Abs(buttonMax.x - buttonMin.x) * 0.5f;
        float halfHeight = Mathf.Abs(buttonMax.y - buttonMin.y) * 0.5f;

        Vector3 pos = rectTransform.localPosition;
        pos.x = Mathf.Clamp(pos.x, mazeMin.x + halfWidth, mazeMax.x - halfWidth);
        pos.y = Mathf.Clamp(pos.y, mazeMin.y + halfHeight, mazeMax.y - halfHeight);

        rectTransform.localPosition = pos;
    }
}