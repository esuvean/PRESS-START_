
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Minigame25_PopupMaze : MinigameBase
{
    [Header("Maze UI")]
    public RectTransform mazeBoard;
    public RectTransform mazeWallsLayer;
    public Button startButton;
    public RectTransform goalZone;

    [Header("Popup UI")]
    public RectTransform popupLayer;
    public GameObject normalPopup;
    public GameObject trapPopup;
    public GameObject movePopup;

    [Header("Maze Settings")]
    [Range(5, 19)]
    public int columns = 11;

    [Range(3, 13)]
    public int rows = 7;

    public float wallThickness = 8f;

    public Color wallColor =
        new Color32(105, 174, 219, 255);

    [Header("Wall Collision")]
    [Tooltip("START가 벽에 가까워졌을 때 충돌로 판정할 여유 거리")]
    public float wallCollisionPadding = 1f;

    [Tooltip("작을수록 빠른 드래그에서 벽을 정확히 검사합니다.")]
    public float collisionStep = 3f;

    [Header("Popup Settings")]
    public int minPopups = 12;
    public int maxPopups = 15;

    [Range(0.5f, 1f)]
    public float popupScale = 0.82f;

    private bool[,,] paths;

    private float cellWidth;
    private float cellHeight;

    private Vector2 startPosition;

    private bool ready;
    private bool cleared;

    private readonly List<GameObject> generatedWalls =
        new List<GameObject>();

    // 실제 벽의 사각형 충돌 영역
    private readonly List<Rect> wallColliders =
        new List<Rect>();

    private readonly List<MG25PopupWindow> activePopups =
        new List<MG25PopupWindow>();

    private readonly Vector3[] startWorldCorners =
        new Vector3[4];

    private int popupSerial;

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1)
    };

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "팝업 미로";

        instruction =
            "팝업을 치우고 START를 GOAL까지 옮기십시오.";

        timeLimit = 999999f;

        base.StartMinigame();

        ready = false;
        cleared = false;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG25: UI 연결을 확인하세요.",
                this
            );

            isGameActive = false;
            return;
        }

        Canvas.ForceUpdateCanvases();

        RemovePreviouslyGenerated();

        GenerateMaze();
        PrepareStartButton();
        SpawnInitialPopups();

        ready = true;

        Debug.Log("MG25: 팝업 미로 시작");
    }

    // =====================================
    // UI 자동 검색
    // =====================================

    private Transform FindDeep(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        if (parent.name == objectName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindDeep(
                child,
                objectName
            );

            if (result != null)
                return result;
        }

        return null;
    }

    private T GetNamed<T>(string objectName)
        where T : Component
    {
        Transform found = FindDeep(
            transform,
            objectName
        );

        return found == null
            ? null
            : found.GetComponent<T>();
    }

    private GameObject GetNamedObject(
        string objectName)
    {
        Transform found = FindDeep(
            transform,
            objectName
        );

        return found == null
            ? null
            : found.gameObject;
    }

    private bool FindReferences()
    {
        if (mazeBoard == null)
            mazeBoard =
                GetNamed<RectTransform>("MazeBoard");

        if (mazeWallsLayer == null)
            mazeWallsLayer =
                GetNamed<RectTransform>("MazeWallsLayer");

        if (startButton == null)
            startButton =
                GetNamed<Button>("StartButton");

        if (goalZone == null)
            goalZone =
                GetNamed<RectTransform>("GoalZone");

        if (popupLayer == null)
            popupLayer =
                GetNamed<RectTransform>("PopupLayer");

        if (normalPopup == null)
            normalPopup = GetNamedObject("NormalPopup");

        if (trapPopup == null)
            trapPopup = GetNamedObject("TrapPopup");

        if (movePopup == null)
            movePopup = GetNamedObject("MovePopup");

        return
            mazeBoard != null &&
            mazeWallsLayer != null &&
            startButton != null &&
            goalZone != null &&
            popupLayer != null &&
            normalPopup != null &&
            trapPopup != null &&
            movePopup != null;
    }

    // =====================================
    // 이전 게임 오브젝트 정리
    // =====================================

    private void RemovePreviouslyGenerated()
    {
        foreach (GameObject wall in generatedWalls)
        {
            if (wall != null)
            {
                wall.SetActive(false);
                Destroy(wall);
            }
        }

        generatedWalls.Clear();
        wallColliders.Clear();

        foreach (MG25PopupWindow popup in activePopups)
        {
            if (popup != null)
            {
                popup.gameObject.SetActive(false);
                Destroy(popup.gameObject);
            }
        }

        activePopups.Clear();
        popupSerial = 0;

        normalPopup.SetActive(false);
        trapPopup.SetActive(false);
        movePopup.SetActive(false);
    }

    // =====================================
    // 미로 셀 위치
    // =====================================

    private Vector2 CellCenter(int col, int row)
    {
        return new Vector2(
            -mazeBoard.rect.width * 0.5f +
            (col + 0.5f) * cellWidth,

            -mazeBoard.rect.height * 0.5f +
            (row + 0.5f) * cellHeight
        );
    }

    // =====================================
    // 미로 자동 생성 (DFS)
    // =====================================

    private void GenerateMaze()
    {
        columns = Mathf.Max(5, columns);
        rows = Mathf.Max(3, rows);

        cellWidth =
            mazeBoard.rect.width / columns;

        cellHeight =
            mazeBoard.rect.height / rows;

        paths = new bool[columns, rows, 4];

        bool[,] visited =
            new bool[columns, rows];

        Stack<Vector2Int> stack =
            new Stack<Vector2Int>();

        Vector2Int startCell =
            new Vector2Int(0, rows / 2);

        visited[startCell.x, startCell.y] = true;

        stack.Push(startCell);

        while (stack.Count > 0)
        {
            Vector2Int cell = stack.Peek();

            List<int> candidates =
                new List<int>();

            for (int dir = 0; dir < 4; dir++)
            {
                Vector2Int next =
                    cell + Directions[dir];

                if (next.x >= 0 &&
                    next.x < columns &&
                    next.y >= 0 &&
                    next.y < rows &&
                    !visited[next.x, next.y])
                {
                    candidates.Add(dir);
                }
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            int direction =
                candidates[Random.Range(
                    0,
                    candidates.Count
                )];

            Vector2Int destination =
                cell + Directions[direction];

            // 양쪽 셀의 통로를 연결
            paths[
                cell.x,
                cell.y,
                direction
            ] = true;

            paths[
                destination.x,
                destination.y,
                (direction + 2) % 4
            ] = true;

            visited[
                destination.x,
                destination.y
            ] = true;

            stack.Push(destination);
        }

        float width = mazeBoard.rect.width;
        float height = mazeBoard.rect.height;

        float thick =
            Mathf.Max(3f, wallThickness);

        // 내부 벽 생성
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 center = CellCenter(x, y);

                if (x < columns - 1 &&
                    !paths[x, y, 0])
                {
                    CreateWall(
                        "Wall_V",
                        new Vector2(
                            center.x + cellWidth * 0.5f,
                            center.y
                        ),
                        new Vector2(
                            thick,
                            cellHeight + thick
                        )
                    );
                }

                if (y < rows - 1 &&
                    !paths[x, y, 1])
                {
                    CreateWall(
                        "Wall_H",
                        new Vector2(
                            center.x,
                            center.y + cellHeight * 0.5f
                        ),
                        new Vector2(
                            cellWidth + thick,
                            thick
                        )
                    );
                }
            }
        }

        // 바깥쪽 경계 벽
        CreateWall(
            "Border_Left",
            new Vector2(-width * 0.5f, 0f),
            new Vector2(thick, height)
        );

        CreateWall(
            "Border_Right",
            new Vector2(width * 0.5f, 0f),
            new Vector2(thick, height)
        );

        CreateWall(
            "Border_Top",
            new Vector2(0f, height * 0.5f),
            new Vector2(width, thick)
        );

        CreateWall(
            "Border_Bottom",
            new Vector2(0f, -height * 0.5f),
            new Vector2(width, thick)
        );

        startPosition = CellCenter(
            0,
            rows / 2
        );

        startButton
            .GetComponent<RectTransform>()
            .anchoredPosition = startPosition;

        goalZone.anchoredPosition =
            CellCenter(columns - 1, rows / 2);
    }

    // =====================================
    // 벽 생성 + 사각형 충돌 영역 저장
    // =====================================

    private void CreateWall(
        string wallName,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            wallName,
            typeof(RectTransform),
            typeof(Image)
        );

        go.transform.SetParent(
            mazeWallsLayer,
            false
        );

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();

        image.color = wallColor;
        image.raycastTarget = false;

        generatedWalls.Add(go);

        // ★ 수정 핵심:
        // 화면에 그려지는 벽 전체를
        // 충돌 영역으로 등록한다.
        Rect collisionRect = new Rect(
            position.x - size.x * 0.5f,
            position.y - size.y * 0.5f,
            size.x,
            size.y
        );

        wallColliders.Add(collisionRect);
    }

    // =====================================
    // START 버튼 드래그 준비
    // =====================================

    private void PrepareStartButton()
    {
        startButton.interactable = true;

        Image image =
            startButton.GetComponent<Image>();

        if (image != null)
            image.raycastTarget = true;

        foreach (TMP_Text text in
            startButton.GetComponentsInChildren<
                TMP_Text>(true))
        {
            text.raycastTarget = false;
        }

        MG25StartDrag drag =
            startButton.GetComponent<MG25StartDrag>();

        if (drag == null)
        {
            drag = startButton.gameObject
                .AddComponent<MG25StartDrag>();
        }

        drag.Configure(this);

        startButton.onClick.RemoveAllListeners();

        startButton.onClick.AddListener(
            ResetStartPosition
        );

        startButton.transform.SetAsLastSibling();
    }

    // =====================================
    // 팝업 12~15개 생성
    // =====================================

    private void SpawnInitialPopups()
    {
        int minCount =
            Mathf.Min(minPopups, maxPopups);

        int maxCount =
            Mathf.Max(minPopups, maxPopups);

        int count = Random.Range(
            minCount,
            maxCount + 1
        );

        count = Mathf.Clamp(count, 4, 25);

        List<MG25PopupKind> types =
            new List<MG25PopupKind>();

        types.Add(MG25PopupKind.Trap);
        types.Add(MG25PopupKind.Trap);
        types.Add(MG25PopupKind.Move);
        types.Add(MG25PopupKind.Move);

        while (types.Count < count)
        {
            types.Add(MG25PopupKind.Normal);
        }

        for (int i = types.Count - 1; i > 0; i--)
        {
            int randomIndex =
                Random.Range(0, i + 1);

            MG25PopupKind temp = types[i];

            types[i] = types[randomIndex];
            types[randomIndex] = temp;
        }

        for (int i = 0; i < count; i++)
        {
            int slot = i % 12;

            int col = slot % 4;
            int row = slot / 4;

            Vector2 position = new Vector2(
                -540f + col * 360f,
                210f - row * 210f
            );

            position += new Vector2(
                Random.Range(-32f, 32f),
                Random.Range(-22f, 22f)
            );

            if (i >= 12)
            {
                position += new Vector2(
                    Random.Range(-100f, 100f),
                    Random.Range(-60f, 60f)
                );
            }

            SpawnPopup(types[i], position);
        }
    }

    // =====================================
    // 팝업 생성
    // =====================================

    private MG25PopupWindow SpawnPopup(
        MG25PopupKind kind,
        Vector2 position)
    {
        GameObject template;

        if (kind == MG25PopupKind.Trap)
        {
            template = trapPopup;
        }
        else if (kind == MG25PopupKind.Move)
        {
            template = movePopup;
        }
        else
        {
            template = normalPopup;
        }

        GameObject instance = Instantiate(
            template,
            popupLayer,
            false
        );

        instance.name =
            "Popup_" + kind + "_" + (++popupSerial);

        RectTransform rect =
            instance.GetComponent<RectTransform>();

        rect.localScale =
            Vector3.one * popupScale;

        rect.anchoredPosition =
            ClampPopup(rect, position);

        instance.SetActive(true);
        instance.transform.SetAsLastSibling();

        MG25PopupWindow popup =
            instance.GetComponent<MG25PopupWindow>();

        if (popup == null)
        {
            popup =
                instance.AddComponent<MG25PopupWindow>();
        }

        popup.Configure(
            this,
            kind,
            popupLayer
        );

        activePopups.Add(popup);

        return popup;
    }

    // =====================================
    // 팝업 이동 범위
    // =====================================

    public Vector2 ClampPopup(
        RectTransform rect,
        Vector2 desired)
    {
        float maxX = Mathf.Max(
            0f,
            (
                popupLayer.rect.width -
                rect.rect.width *
                Mathf.Abs(rect.localScale.x)
            ) * 0.5f
        );

        float maxY = Mathf.Max(
            0f,
            (
                popupLayer.rect.height -
                rect.rect.height *
                Mathf.Abs(rect.localScale.y)
            ) * 0.5f
        );

        return new Vector2(
            Mathf.Clamp(desired.x, -maxX, maxX),
            Mathf.Clamp(desired.y, -maxY, maxY)
        );
    }

    // =====================================
    // 팝업 X 클릭
    // =====================================

    public void ClosePopup(MG25PopupWindow popup)
    {
        if (!isGameActive ||
            cleared ||
            popup == null)
        {
            return;
        }

        if (popup.Kind == MG25PopupKind.Move)
            return;

        Vector2 position =
            popup.Rect.anchoredPosition;

        bool isTrap =
            popup.Kind == MG25PopupKind.Trap;

        activePopups.Remove(popup);

        popup.gameObject.SetActive(false);
        Destroy(popup.gameObject);

        if (isTrap)
        {
            int count = Random.Range(2, 4);

            for (int i = 0; i < count; i++)
            {
                Vector2 nextPosition =
                    position + new Vector2(
                        Random.Range(-250f, 250f),
                        Random.Range(-170f, 170f)
                    );

                MG25PopupKind nextKind =
                    Random.value < 0.75f
                        ? MG25PopupKind.Normal
                        : MG25PopupKind.Move;

                SpawnPopup(
                    nextKind,
                    nextPosition
                );
            }

            Debug.Log(
                "MG25: 함정 팝업! " +
                count + "개 추가"
            );
        }
    }

    // =====================================
    // START 이동 상태
    // =====================================

    public bool CanDragStart()
    {
        return ready && isGameActive && !cleared;
    }

    public RectTransform Board
    {
        get { return mazeBoard; }
    }

    public Vector2 CurrentStartPosition
    {
        get
        {
            return startButton
                .GetComponent<RectTransform>()
                .anchoredPosition;
        }
    }

    // =====================================
    // 좌표를 미로 셀로 변환
    // =====================================

    private Vector2Int CellOf(Vector2 position)
    {
        int x = Mathf.Clamp(
            Mathf.FloorToInt(
                (
                    position.x +
                    mazeBoard.rect.width * 0.5f
                ) / cellWidth
            ),
            0,
            columns - 1
        );

        int y = Mathf.Clamp(
            Mathf.FloorToInt(
                (
                    position.y +
                    mazeBoard.rect.height * 0.5f
                ) / cellHeight
            ),
            0,
            rows - 1
        );

        return new Vector2Int(x, y);
    }

    // =====================================
    // 통로 연결 확인
    // =====================================

    private bool IsPassable(
        Vector2 from,
        Vector2 to)
    {
        Vector2Int a = CellOf(from);
        Vector2Int b = CellOf(to);

        if (a == b)
            return true;

        Vector2Int delta = b - a;

        int direction;

        if (delta == Vector2Int.right)
            direction = 0;
        else if (delta == Vector2Int.up)
            direction = 1;
        else if (delta == Vector2Int.left)
            direction = 2;
        else if (delta == Vector2Int.down)
            direction = 3;
        else
            return false;

        return paths[a.x, a.y, direction];
    }

    // =====================================
    // ★ START 실제 사각형 크기 계산
    // =====================================

    private void GetStartBoundsOffsets(
        out Vector2 minOffset,
        out Vector2 maxOffset)
    {
        RectTransform startRect =
            startButton.GetComponent<RectTransform>();

        startRect.GetWorldCorners(startWorldCorners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;

        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector3 local =
                mazeBoard.InverseTransformPoint(
                    startWorldCorners[i]
                );

            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);

            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }

        Vector2 currentCenter =
            startRect.anchoredPosition;

        // START의 현재 좌표에 대한
        // 사각형 네 방향의 상대 위치
        minOffset = new Vector2(
            minX - currentCenter.x,
            minY - currentCenter.y
        );

        maxOffset = new Vector2(
            maxX - currentCenter.x,
            maxY - currentCenter.y
        );
    }

    // =====================================
    // ★ START 전체 사각형과 벽 충돌 검사
    // =====================================

    private bool TouchesAnyWall(
        Vector2 candidatePosition,
        Vector2 minOffset,
        Vector2 maxOffset)
    {
        float padding =
            Mathf.Max(0f, wallCollisionPadding);

        float left =
            candidatePosition.x +
            minOffset.x - padding;

        float right =
            candidatePosition.x +
            maxOffset.x + padding;

        float bottom =
            candidatePosition.y +
            minOffset.y - padding;

        float top =
            candidatePosition.y +
            maxOffset.y + padding;

        for (int i = 0; i < wallColliders.Count; i++)
        {
            Rect wall = wallColliders[i];

            // 한 변이라도 맞닿으면 충돌 처리
            bool overlaps =
                left <= wall.xMax &&
                right >= wall.xMin &&
                bottom <= wall.yMax &&
                top >= wall.yMin;

            if (overlaps)
            {
                return true;
            }
        }

        return false;
    }

    // =====================================
    // ★ START 이동 / 개선된 충돌 판정
    // =====================================

    public bool TryMoveStart(Vector2 desired)
    {
        if (!CanDragStart())
            return false;

        RectTransform startRect =
            startButton.GetComponent<RectTransform>();

        Vector2 from =
            startRect.anchoredPosition;

        // 실제 START 크기를 반영
        Vector2 minOffset;
        Vector2 maxOffset;

        GetStartBoundsOffsets(
            out minOffset,
            out maxOffset
        );

        float limitLeft =
            -mazeBoard.rect.width * 0.5f -
            minOffset.x;

        float limitRight =
            mazeBoard.rect.width * 0.5f -
            maxOffset.x;

        float limitBottom =
            -mazeBoard.rect.height * 0.5f -
            minOffset.y;

        float limitTop =
            mazeBoard.rect.height * 0.5f -
            maxOffset.y;

        desired.x = Mathf.Clamp(
            desired.x,
            limitLeft,
            limitRight
        );

        desired.y = Mathf.Clamp(
            desired.y,
            limitBottom,
            limitTop
        );

        // 이동 거리를 작게 나누어 검사
        // 마우스가 빠르게 움직여도
        // 벽을 건너뛰지 않도록 한다.
        float stepSize = Mathf.Clamp(
            collisionStep,
            1f,
            6f
        );

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(
                Vector2.Distance(from, desired) /
                stepSize
            )
        );

        Vector2 previous = from;

        for (int i = 1; i <= steps; i++)
        {
            Vector2 next = Vector2.Lerp(
                from,
                desired,
                i / (float)steps
            );

            // 1. 미로 통로 연결 확인
            bool passageAllowed =
                IsPassable(previous, next);

            // 2. START 버튼 사각형 전체 확인
            bool wallCollision =
                TouchesAnyWall(
                    next,
                    minOffset,
                    maxOffset
                );

            if (!passageAllowed || wallCollision)
            {
                ResetStartPosition();

                Debug.Log(
                    "MG25: START가 벽에 닿았습니다! " +
                    "출발 위치로 복귀"
                );

                return false;
            }

            previous = next;
        }

        // 충돌이 없을 때만 실제로 이동
        startRect.anchoredPosition = desired;

        Vector2Int goalCell =
            new Vector2Int(
                columns - 1,
                rows / 2
            );

        // GOAL 도착 판정
        if (CellOf(desired) == goalCell &&
            Vector2.Distance(
                desired,
                goalZone.anchoredPosition
            ) <
            Mathf.Min(
                cellWidth,
                cellHeight
            ) * 0.48f)
        {
            cleared = true;

            Debug.Log(
                "MG25: GOAL 도착! 검사 완료"
            );

            Success();
        }

        return true;
    }

    // =====================================
    // START 출발 위치 복귀
    // =====================================

    public void ResetStartPosition()
    {
        if (startButton != null && !cleared)
        {
            startButton
                .GetComponent<RectTransform>()
                .anchoredPosition = startPosition;
        }
    }

    protected override void GiveHint()
    {
        // 기획서에 별도 힌트 없음
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}

// =========================================
// 팝업 종류
// =========================================

public enum MG25PopupKind
{
    Normal,
    Trap,
    Move
}

// =========================================
// 팝업 동작
// =========================================

public class MG25PopupWindow : MonoBehaviour
{
    private Minigame25_PopupMaze owner;

    public MG25PopupKind Kind
    {
        get;
        private set;
    }

    public RectTransform Rect
    {
        get;
        private set;
    }

    public void Configure(
        Minigame25_PopupMaze manager,
        MG25PopupKind kind,
        RectTransform layer)
    {
        owner = manager;
        Kind = kind;

        Rect = GetComponent<RectTransform>();

        Transform title =
            transform.Find("TitleBar");

        if (title == null)
        {
            Debug.LogWarning(
                "MG25: TitleBar 없음 - " + name,
                this
            );

            return;
        }

        Image titleGraphic =
            title.GetComponent<Image>();

        if (titleGraphic != null)
            titleGraphic.raycastTarget = true;

        foreach (TMP_Text text in
            title.GetComponentsInChildren<TMP_Text>(true))
        {
            text.raycastTarget = false;
        }

        MG25PopupTitleDrag drag =
            title.GetComponent<MG25PopupTitleDrag>();

        if (drag == null)
        {
            drag = title.gameObject
                .AddComponent<MG25PopupTitleDrag>();
        }

        drag.Configure(
            manager,
            Rect,
            layer
        );

        Transform close =
            title.Find("CloseButton");

        if (kind == MG25PopupKind.Move)
        {
            if (close != null)
                close.gameObject.SetActive(false);

            return;
        }

        if (close == null)
        {
            Debug.LogWarning(
                "MG25: CloseButton 없음 - " + name,
                this
            );

            return;
        }

        Button button =
            close.GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => owner.ClosePopup(this)
            );

            Image image =
                button.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = true;
        }
    }
}

// =========================================
// 팝업 제목 표시줄 드래그
// =========================================

public class MG25PopupTitleDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Minigame25_PopupMaze owner;
    private RectTransform popup;
    private RectTransform layer;

    private Vector2 offset;
    private bool dragging;

    public void Configure(
        Minigame25_PopupMaze manager,
        RectTransform popupRect,
        RectTransform popupLayer)
    {
        owner = manager;
        popup = popupRect;
        layer = popupLayer;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (popup == null || layer == null)
            return;

        Vector2 local;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                layer,
                eventData.position,
                eventData.pressEventCamera,
                out local))
        {
            return;
        }

        offset = popup.anchoredPosition - local;

        popup.SetAsLastSibling();

        dragging = true;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging || owner == null)
            return;

        Vector2 local;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                layer,
                eventData.position,
                eventData.pressEventCamera,
                out local))
        {
            popup.anchoredPosition =
                owner.ClampPopup(
                    popup,
                    local + offset
                );
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        dragging = false;
    }
}

// =========================================
// START 버튼 드래그
// =========================================

public class MG25StartDrag :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private Minigame25_PopupMaze owner;

    private bool dragging;
    private bool blocked;

    private Vector2 offset;

    public void Configure(
        Minigame25_PopupMaze manager)
    {
        owner = manager;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (owner == null ||
            !owner.CanDragStart())
        {
            return;
        }

        Vector2 local;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                owner.Board,
                eventData.position,
                eventData.pressEventCamera,
                out local))
        {
            return;
        }

        offset =
            owner.CurrentStartPosition - local;

        dragging = true;
        blocked = false;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (!dragging || blocked || owner == null)
            return;

        Vector2 local;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                owner.Board,
                eventData.position,
                eventData.pressEventCamera,
                out local))
        {
            if (!owner.TryMoveStart(local + offset))
            {
                // 벽에 닿으면 마우스를 다시 놓고
                // START부터 재시도해야 함
                blocked = true;
            }
        }
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        dragging = false;
        blocked = false;
    }
}
