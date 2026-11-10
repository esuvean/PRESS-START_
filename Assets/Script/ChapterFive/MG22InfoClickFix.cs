
using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MG22InfoClickFix : MonoBehaviour
{
    [Header("MG22 Manager")]
    public Minigame22_ScatteredStart manager;

    [Header("Link ID")]
    public string linkId = "hidden_a";

    private TMP_Text targetText;
    private Canvas canvas;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();

        canvas = GetComponentInParent<Canvas>();

        if (manager == null)
        {
            manager = GetComponentInParent<
                Minigame22_ScatteredStart>();
        }

        if (targetText == null)
        {
            Debug.LogError(
                "MG22: TMP Text가 없습니다.",
                this
            );
        }
    }

    private void Update()
    {
        if (manager == null || targetText == null)
            return;

        if (!targetText.gameObject.activeInHierarchy)
            return;

        if (!MousePressed())
            return;

        Vector2 mousePosition = GetMousePosition();

        Camera camera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera;

            if (camera == null)
                camera = Camera.main;
        }

        RectTransform rect = targetText.rectTransform;

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                mousePosition,
                camera))
        {
            return;
        }

        targetText.ForceMeshUpdate();

        // 먼저 TextMeshPro의 기본 링크 감지 사용
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            targetText,
            mousePosition,
            camera
        );

        if (linkIndex >= 0 &&
            linkIndex < targetText.textInfo.linkCount)
        {
            string clickedId =
                targetText.textInfo.linkInfo[linkIndex]
                    .GetLinkID();

            if (clickedId == linkId)
            {
                Collect();
                return;
            }
        }

        // 기본 링크 감지가 실패할 경우
        // 글자 자체의 위치를 직접 검사
        Vector2 localPoint;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                rect,
                mousePosition,
                camera,
                out localPoint))
        {
            return;
        }

        TMP_TextInfo info = targetText.textInfo;

        for (int i = 0; i < info.linkCount; i++)
        {
            TMP_LinkInfo link = info.linkInfo[i];

            if (link.GetLinkID() != linkId)
                continue;

            int start = link.linkTextfirstCharacterIndex;
            int end = start + link.linkTextLength;

            for (int j = start;
                 j < end && j < info.characterCount;
                 j++)
            {
                TMP_CharacterInfo character =
                    info.characterInfo[j];

                if (!character.isVisible)
                    continue;

                Vector3 bottomLeft = character.bottomLeft;
                Vector3 topRight = character.topRight;

                float padding = 6f;

                bool inside =
                    localPoint.x >= bottomLeft.x - padding &&
                    localPoint.x <= topRight.x + padding &&
                    localPoint.y >= bottomLeft.y - padding &&
                    localPoint.y <= topRight.y + padding;

                if (inside)
                {
                    Collect();
                    return;
                }
            }
        }
    }

    private void Collect()
    {
        Debug.Log(
            "MG22: 숨겨진 글자 클릭 감지 - " + linkId
        );

        manager.OnLinkClicked(linkId);
    }

    private bool MousePressed()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            pressed |=
                Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetMouseButtonDown(0);
#endif

        return pressed;
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(-99999f, -99999f);
#endif
    }
}
