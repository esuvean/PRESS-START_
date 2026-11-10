
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MG22AssemblyDragFix : MonoBehaviour
{
    private Minigame22_ScatteredStart manager;

    private RectTransform assemblyRect;
    private Canvas rootCanvas;
    private Camera uiCamera;

    private readonly string[] letterNames =
    {
        "LetterBlock_S",
        "LetterBlock_T1",
        "LetterBlock_A",
        "LetterBlock_R",
        "LetterBlock_T2"
    };

    private RectTransform[] letters =
        new RectTransform[5];

    private MG22LetterDrag[] letterScripts =
        new MG22LetterDrag[5];

    private RectTransform[] slots =
        new RectTransform[5];

    private RectTransform draggingLetter;
    private MG22LetterDrag draggingScript;

    private Transform originalParent;
    private Vector2 originalPosition;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalSize;
    private Vector3 originalScale;
    private int originalSiblingIndex;

    private Vector3 pointerOffset;

    private bool isDragging;

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        manager = GetComponentInParent<
            Minigame22_ScatteredStart>();

        assemblyRect = GetComponent<RectTransform>();

        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null)
        {
            rootCanvas = rootCanvas.rootCanvas;

            if (rootCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null;
            }
            else
            {
                uiCamera = rootCanvas.worldCamera;

                if (uiCamera == null)
                    uiCamera = Camera.main;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            letters[i] = FindRect(letterNames[i]);

            slots[i] = FindRect(
                "TargetSlot_" + i
            );

            if (letters[i] == null)
            {
                Debug.LogWarning(
                    "MG22 DragFix: 글자 없음 - " +
                    letterNames[i],
                    this
                );

                continue;
            }

            letterScripts[i] =
                letters[i].GetComponent<
                    MG22LetterDrag>();

            if (letterScripts[i] == null)
            {
                Debug.LogWarning(
                    "MG22 DragFix: 드래그 컴포넌트 없음 - " +
                    letterNames[i],
                    this
                );
            }
            else
            {
                // 기존 UI 드래그 방식과 충돌 방지
                // Snap / ResetHome 함수는 그대로 사용
                letterScripts[i].enabled = false;
            }
        }

        Debug.Log("MG22: 직접 드래그 기능 준비 완료");
    }

    private RectTransform FindRect(string targetName)
    {
        RectTransform[] all =
            GetComponentsInChildren<
                RectTransform>(true);

        foreach (RectTransform rect in all)
        {
            if (rect.name == targetName)
                return rect;
        }

        return null;
    }

    private void Update()
    {
        if (manager == null ||
            !manager.CanDrag())
        {
            return;
        }

        Vector2 mouse = GetMousePosition();

        if (!isDragging && MouseDown())
        {
            BeginDrag(mouse);
        }

        if (isDragging && MouseHeld())
        {
            MoveDrag(mouse);
        }

        if (isDragging && MouseUp())
        {
            EndDrag(mouse);
        }
    }

    private void BeginDrag(Vector2 mouse)
    {
        for (int i = 4; i >= 0; i--)
        {
            RectTransform letter = letters[i];

            if (letter == null ||
                letterScripts[i] == null)
            {
                continue;
            }

            if (!letter.gameObject.activeInHierarchy)
                continue;

            bool inside =
                RectTransformUtility
                .RectangleContainsScreenPoint(
                    letter,
                    mouse,
                    uiCamera
                );

            if (!inside)
                continue;

            draggingLetter = letter;
            draggingScript = letterScripts[i];

            originalParent = letter.parent;

            originalPosition =
                letter.anchoredPosition;

            originalAnchorMin = letter.anchorMin;
            originalAnchorMax = letter.anchorMax;
            originalPivot = letter.pivot;
            originalSize = letter.sizeDelta;
            originalScale = letter.localScale;

            originalSiblingIndex =
                letter.GetSiblingIndex();

            Vector3 pointerWorld;

            if (RectTransformUtility
                .ScreenPointToWorldPointInRectangle(
                    assemblyRect,
                    mouse,
                    uiCamera,
                    out pointerWorld))
            {
                pointerOffset =
                    letter.position - pointerWorld;
            }
            else
            {
                pointerOffset = Vector3.zero;
            }

            // 글자를 조합창 맨 앞에 표시
            letter.SetParent(assemblyRect, true);
            letter.SetAsLastSibling();

            isDragging = true;

            Debug.Log(
                "MG22: 글자 드래그 시작 - " +
                draggingScript.Letter
            );

            break;
        }
    }

    private void MoveDrag(Vector2 mouse)
    {
        if (draggingLetter == null)
            return;

        Vector3 worldPoint;

        bool success = RectTransformUtility
            .ScreenPointToWorldPointInRectangle(
                assemblyRect,
                mouse,
                uiCamera,
                out worldPoint
            );

        if (success)
        {
            draggingLetter.position =
                worldPoint + pointerOffset;
        }
    }

    private void EndDrag(Vector2 mouse)
    {
        bool placed = false;

        for (int i = 0; i < 5; i++)
        {
            if (slots[i] == null)
                continue;

            bool inside =
                RectTransformUtility
                .RectangleContainsScreenPoint(
                    slots[i],
                    mouse,
                    uiCamera
                );

            if (!inside)
                continue;

            manager.DropOnSlot(
                draggingScript,
                i
            );

            // 슬롯 배치 성공 확인
            if (draggingScript.CurrentSlot == i)
            {
                placed = true;

                Debug.Log(
                    "MG22: " +
                    draggingScript.Letter +
                    " 글자를 " +
                    (i + 1) +
                    "번 슬롯에 배치"
                );
            }

            break;
        }

        // 빈칸에 놓지 않았다면 원래 자리로
        if (!placed)
        {
            RestoreOriginalPosition();
        }

        draggingLetter = null;
        draggingScript = null;
        isDragging = false;
    }

    private void RestoreOriginalPosition()
    {
        if (draggingLetter == null ||
            originalParent == null)
        {
            return;
        }

        draggingLetter.SetParent(
            originalParent,
            false
        );

        draggingLetter.anchorMin =
            originalAnchorMin;

        draggingLetter.anchorMax =
            originalAnchorMax;

        draggingLetter.pivot =
            originalPivot;

        draggingLetter.sizeDelta =
            originalSize;

        draggingLetter.localScale =
            originalScale;

        draggingLetter.anchoredPosition =
            originalPosition;

        int maxSibling =
            originalParent.childCount - 1;

        draggingLetter.SetSiblingIndex(
            Mathf.Clamp(
                originalSiblingIndex,
                0,
                maxSibling
            )
        );
    }

    private void OnDisable()
    {
        if (isDragging)
        {
            RestoreOriginalPosition();
        }

        isDragging = false;
        draggingLetter = null;
        draggingScript = null;
    }

    private bool MouseDown()
    {
        bool result = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            result |=
                Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        result |= Input.GetMouseButtonDown(0);
#endif

        return result;
    }

    private bool MouseHeld()
    {
        bool result = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            result |=
                Mouse.current.leftButton.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        result |= Input.GetMouseButton(0);
#endif

        return result;
    }

    private bool MouseUp()
    {
        bool result = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            result |=
                Mouse.current.leftButton.wasReleasedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        result |= Input.GetMouseButtonUp(0);
#endif

        return result;
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
