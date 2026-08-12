using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggablePopup : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Popup")]
    public RectTransform popupRect;
    public Canvas canvas;
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

        if (closeButton != null)
        {
            closeButton.interactable = closable;
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 잡은 팝업을 맨 앞으로
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (popupRect == null)
            return;

        float scaleFactor = canvas != null
            ? canvas.scaleFactor
            : 1f;

        // 기존 드래그 기능
        popupRect.anchoredPosition +=
            eventData.delta / Mathf.Max(scaleFactor, 0.01f);

        // ★ Canvas 밖으로 나가지 않게 제한
        KeepInsideCanvas();
    }

    // ==================================================
    // 팝업이 실제 Canvas 화면 밖으로 나가지 않도록 제한
    // ==================================================
    private void KeepInsideCanvas()
    {
        if (popupRect == null || canvas == null)
            return;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        // 팝업의 실제 화면상 모서리
        Vector3[] popupCorners = new Vector3[4];

        // Canvas의 실제 화면상 모서리
        Vector3[] canvasCorners = new Vector3[4];

        popupRect.GetWorldCorners(popupCorners);
        canvasRect.GetWorldCorners(canvasCorners);

        Vector3 offset = Vector3.zero;

        // 왼쪽으로 나갔을 때
        if (popupCorners[0].x < canvasCorners[0].x)
        {
            offset.x +=
                canvasCorners[0].x - popupCorners[0].x;
        }

        // 오른쪽으로 나갔을 때
        if (popupCorners[2].x > canvasCorners[2].x)
        {
            offset.x -=
                popupCorners[2].x - canvasCorners[2].x;
        }

        // 아래쪽으로 나갔을 때
        if (popupCorners[0].y < canvasCorners[0].y)
        {
            offset.y +=
                canvasCorners[0].y - popupCorners[0].y;
        }

        // 위쪽으로 나갔을 때
        if (popupCorners[2].y > canvasCorners[2].y)
        {
            offset.y -=
                popupCorners[2].y - canvasCorners[2].y;
        }

        // 필요한 만큼만 다시 안쪽으로 이동
        popupRect.position += offset;
    }
    // "아니요" 버튼을 누르면 현재 팝업이 2개 더 생성됨
    public void DuplicatePopupTwice()
    {
        Transform parent = transform.parent;

        for (int i = 0; i < 2; i++)
        {
            GameObject clone = Instantiate(gameObject, parent);

            RectTransform rt = clone.GetComponent<RectTransform>();

            if (rt != null)
            {
                // 기존 팝업 위치를 기준으로 살짝 떨어뜨려 생성
                if (i == 0)
                {
                    rt.anchoredPosition =
                        popupRect.anchoredPosition +
                        new Vector2(-120f, -80f);
                }
                else
                {
                    rt.anchoredPosition =
                        popupRect.anchoredPosition +
                        new Vector2(120f, 80f);
                }
            }
        }
    }
    public void ClosePopup()
    {
        if (!closable)
            return;

        if (spawnTwoOnClose && nuisancePopupPrefab != null)
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