
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame27_DamagedCircuit : MinigameBase
{
    [Header("Stage 1 - Conveyor UI")]
    public GameObject conveyorPanel;
    public RectTransform partSpawnLayer;
    public Image conveyorTrack;
    public TMP_Text selectedPartsText;
    public TMP_Text partStatusText;

    [Header("Stage 2 - Circuit UI")]
    public GameObject circuitPanel;
    public Image circuitBoard;
    public RectTransform circuitLineLayer;

    public Button powerNode;
    public Button inputNode;
    public Button displayNode;
    public Button startNode;

    public TMP_Text circuitStatusText;
    public Button finalStartButton;

    [Header("ART - Conveyor Parts")]
    public Sprite powerSprite;
    public Sprite brokenPowerSprite;
    public Sprite startSprite;
    public Sprite exitSprite;
    public Sprite normalCableSprite;
    public Sprite brokenCableSprite;
    public Sprite displaySprite;
    public Sprite errorPortSprite;
    public Sprite inputSprite;

    [Header("ART - Circuit Nodes")]
    public Sprite powerNodeSprite;
    public Sprite inputNodeSprite;
    public Sprite displayNodeSprite;
    public Sprite startNodeSprite;

    [Header("ART - Other")]
    public Sprite conveyorTrackSprite;
    public Sprite circuitBoardSprite;
    public Sprite circuitLineSprite;
    public Sprite finalStartSprite;

    [Header("ART - Options")]
    public bool hidePartTextWhenSpriteExists = false;
    public bool hideNodeTextWhenSpriteExists = false;
    public bool hideFinalTextWhenSpriteExists = true;

    [Header("Conveyor Settings")]
    public Vector2 partSize = new Vector2(185f, 58f);
    public float spawnInterval = 0.8f;
    public float minMoveSpeed = 130f;
    public float maxMoveSpeed = 190f;
    public int maxVisibleParts = 12;

    [Header("Circuit Settings")]
    public float circuitLineThickness = 12f;
    public float readyDisplayTime = 1f;
    public float errorDisplayTime = 1.4f;

    private enum GamePhase
    {
        Selecting,
        Connecting,
        Verifying,
        Error,
        StartReady,
        Completed
    }

    private enum PartType
    {
        Power,
        BrokenPower,
        Start,
        Exit,
        NormalCable,
        BrokenCable,
        Display,
        ErrorPort,
        Input
    }

    private readonly PartType[] requiredParts =
    {
        PartType.Power,
        PartType.Input,
        PartType.Display,
        PartType.Start,
        PartType.NormalCable
    };

    private readonly PartType[] allParts =
    {
        PartType.Power,
        PartType.BrokenPower,
        PartType.Start,
        PartType.Exit,
        PartType.NormalCable,
        PartType.BrokenCable,
        PartType.Display,
        PartType.ErrorPort,
        PartType.Input
    };

    private class MovingPart
    {
        public GameObject obj;
        public RectTransform rect;
        public PartType type;
        public float speed;
    }

    private GamePhase phase;

    private readonly List<MovingPart> movingParts =
        new List<MovingPart>();

    private readonly List<PartType> selectedParts =
        new List<PartType>();

    private readonly List<GameObject> generatedLines =
        new List<GameObject>();

    private float spawnTimer;
    private int spawnSerial;
    private int nextNodeIndex;

    private bool finished;

    // ======================================
    // 시작
    // ======================================

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "손상된 실행 회로";

        instruction =
            "올바른 부품을 수집한 뒤 회로를 연결하십시오.";

        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        finished = false;

        if (!FindReferences())
        {
            Debug.LogError(
                "MG27: 필요한 UI가 없습니다. " +
                "Hierarchy와 Inspector 연결을 확인하세요.",
                this
            );

            isGameActive = false;
            return;
        }

        ClearMovingParts();
        ClearCircuitLines();

        selectedParts.Clear();

        ApplyArtwork();
        SetupButtons();

        BeginSelectionStage();
    }

    // ======================================
    // UI 오브젝트 자동 찾기
    // ======================================

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
            Transform found = FindDeep(
                child,
                objectName
            );

            if (found != null)
                return found;
        }

        return null;
    }

    private T FindUI<T>(string objectName)
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

    private GameObject FindObject(string objectName)
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
        if (conveyorPanel == null)
            conveyorPanel = FindObject("ConveyorPanel");

        if (partSpawnLayer == null)
            partSpawnLayer =
                FindUI<RectTransform>("PartSpawnLayer");

        if (conveyorTrack == null)
            conveyorTrack =
                FindUI<Image>("ConveyorTrack");

        if (selectedPartsText == null)
            selectedPartsText =
                FindUI<TMP_Text>("SelectedPartsText");

        if (partStatusText == null)
            partStatusText =
                FindUI<TMP_Text>("PartStatusText");

        if (circuitPanel == null)
            circuitPanel = FindObject("CircuitPanel");

        if (circuitBoard == null)
            circuitBoard = FindUI<Image>("CircuitBoard");

        if (circuitLineLayer == null)
            circuitLineLayer =
                FindUI<RectTransform>("CircuitLineLayer");

        if (powerNode == null)
            powerNode = FindUI<Button>("PowerNode");

        if (inputNode == null)
            inputNode = FindUI<Button>("InputNode");

        if (displayNode == null)
            displayNode = FindUI<Button>("DisplayNode");

        if (startNode == null)
            startNode = FindUI<Button>("StartNode");

        if (circuitStatusText == null)
            circuitStatusText =
                FindUI<TMP_Text>("CircuitStatusText");

        if (finalStartButton == null)
            finalStartButton =
                FindUI<Button>("FinalStartButton");

        return
            conveyorPanel != null &&
            partSpawnLayer != null &&
            conveyorTrack != null &&
            selectedPartsText != null &&
            partStatusText != null &&
            circuitPanel != null &&
            circuitBoard != null &&
            circuitLineLayer != null &&
            powerNode != null &&
            inputNode != null &&
            displayNode != null &&
            startNode != null &&
            circuitStatusText != null &&
            finalStartButton != null;
    }

    // ======================================
    // 아트팀 이미지 적용
    // ======================================

    private void ApplyArtwork()
    {
        ApplySprite(
            conveyorTrack,
            conveyorTrackSprite,
            false
        );

        ApplySprite(
            circuitBoard,
            circuitBoardSprite,
            false
        );

        SetupNodeArtwork(powerNode, powerNodeSprite);
        SetupNodeArtwork(inputNode, inputNodeSprite);
        SetupNodeArtwork(displayNode, displayNodeSprite);
        SetupNodeArtwork(startNode, startNodeSprite);

        Image finalImage =
            finalStartButton.GetComponent<Image>();

        ApplySprite(
            finalImage,
            finalStartSprite,
            true
        );

        TMP_Text[] finalLabels =
            finalStartButton.GetComponentsInChildren<
                TMP_Text>(true);

        foreach (TMP_Text label in finalLabels)
        {
            bool hide =
                finalStartSprite != null &&
                hideFinalTextWhenSpriteExists;

            label.gameObject.SetActive(!hide);
            label.raycastTarget = false;
        }
    }

    private void ApplySprite(
        Image image,
        Sprite sprite,
        bool preserveAspect)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
    }

    private void SetupNodeArtwork(
        Button button,
        Sprite sprite)
    {
        Image image = button.GetComponent<Image>();

        ApplySprite(image, sprite, true);

        foreach (TMP_Text text in
            button.GetComponentsInChildren<TMP_Text>(true))
        {
            text.gameObject.SetActive(
                sprite == null ||
                !hideNodeTextWhenSpriteExists
            );

            text.raycastTarget = false;
        }
    }

    private Sprite GetPartSprite(PartType part)
    {
        switch (part)
        {
            case PartType.Power: return powerSprite;
            case PartType.BrokenPower: return brokenPowerSprite;
            case PartType.Start: return startSprite;
            case PartType.Exit: return exitSprite;
            case PartType.NormalCable: return normalCableSprite;
            case PartType.BrokenCable: return brokenCableSprite;
            case PartType.Display: return displaySprite;
            case PartType.ErrorPort: return errorPortSprite;
            case PartType.Input: return inputSprite;
        }

        return null;
    }

    private string GetPartName(PartType part)
    {
        switch (part)
        {
            case PartType.Power: return "POWER";
            case PartType.BrokenPower: return "BROKEN POWER";
            case PartType.Start: return "START";
            case PartType.Exit: return "EXIT";
            case PartType.NormalCable: return "NORMAL CABLE";
            case PartType.BrokenCable: return "BROKEN CABLE";
            case PartType.Display: return "DISPLAY";
            case PartType.ErrorPort: return "ERROR PORT";
            case PartType.Input: return "INPUT";
        }

        return "UNKNOWN";
    }

    // ======================================
    // 버튼 기능 연결
    // ======================================

    private void SetupButtons()
    {
        SetupNodeButton(powerNode, 0);
        SetupNodeButton(inputNode, 1);
        SetupNodeButton(displayNode, 2);
        SetupNodeButton(startNode, 3);

        finalStartButton.onClick.RemoveAllListeners();

        finalStartButton.onClick.AddListener(
            OnFinalStartClicked
        );

        finalStartButton.interactable = true;
        finalStartButton.gameObject.SetActive(false);
    }

    private void SetupNodeButton(
        Button button,
        int nodeIndex)
    {
        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(
            () => OnNodeClicked(nodeIndex)
        );

        button.interactable = true;

        Image image = button.GetComponent<Image>();

        if (image != null)
            image.raycastTarget = true;
    }

    // ======================================
    // 1단계: 컨베이어벨트 시작
    // ======================================

    private void BeginSelectionStage()
    {
        phase = GamePhase.Selecting;

        conveyorPanel.SetActive(true);
        circuitPanel.SetActive(false);

        finalStartButton.gameObject.SetActive(false);

        ClearMovingParts();

        spawnTimer = 0f;
        spawnSerial = 0;

        Canvas.ForceUpdateCanvases();

        UpdateSelectionText();

        if (selectedParts.Count > 0)
        {
            partStatusText.text =
                "정상 부품 " +
                selectedParts.Count +
                "개 유지! 나머지 부품을 선택하십시오.";
        }
        else
        {
            partStatusText.text =
                "필요한 부품 5개를 선택하십시오.";
        }

        // 시작 시 컨베이어에 부품 3개 배치
        SpawnSpecificPart(
            PartType.Power,
            -400f,
            -65f
        );

        SpawnSpecificPart(
            PartType.BrokenPower,
            0f,
            0f
        );

        SpawnSpecificPart(
            PartType.Input,
            400f,
            65f
        );

        Debug.Log("MG27: 부품 선택 시작");
    }

    // ======================================
    // 컨베이어 이동 / 생성
    // ======================================

    private void LateUpdate()
    {
        if (!isGameActive ||
            finished ||
            phase != GamePhase.Selecting)
        {
            return;
        }

        UpdateConveyor();
        UpdatePartSpawning();
    }

    private void UpdateConveyor()
    {
        float disappearX =
            -partSpawnLayer.rect.width * 0.5f - 130f;

        for (int i = movingParts.Count - 1;
             i >= 0; i--)
        {
            MovingPart part = movingParts[i];

            if (part.obj == null)
            {
                movingParts.RemoveAt(i);
                continue;
            }

            Vector2 position =
                part.rect.anchoredPosition;

            position.x -=
                part.speed * Time.deltaTime;

            part.rect.anchoredPosition = position;

            if (position.x < disappearX)
            {
                part.obj.SetActive(false);
                Destroy(part.obj);
                movingParts.RemoveAt(i);
            }
        }
    }

    private void UpdatePartSpawning()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;

        if (movingParts.Count >= maxVisibleParts)
            return;

        PartType next = GetNextSpawnType();

        int lane = Random.Range(0, 3);

        float y = (lane - 1) * 65f;

        float x =
            partSpawnLayer.rect.width * 0.5f + 110f;

        SpawnSpecificPart(next, x, y);
    }

    private PartType GetNextSpawnType()
    {
        // 아직 선택하지 않은 정상 부품이
        // 비교적 자주 등장하도록 조절
        List<PartType> missing =
            new List<PartType>();

        foreach (PartType required in requiredParts)
        {
            if (!selectedParts.Contains(required))
                missing.Add(required);
        }

        if (missing.Count > 0 &&
            Random.value < 0.6f)
        {
            return missing[
                Random.Range(0, missing.Count)
            ];
        }

        return allParts[
            Random.Range(0, allParts.Length)
        ];
    }

    // ======================================
    // 컨베이어 부품 생성
    // ======================================

    private void SpawnSpecificPart(
        PartType type,
        float x,
        float y)
    {
        GameObject go = new GameObject(
            "Part_" + type + "_" + (++spawnSerial),
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        go.transform.SetParent(
            partSpawnLayer,
            false
        );

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.sizeDelta = partSize;
        rect.anchoredPosition = new Vector2(x, y);

        Image image = go.GetComponent<Image>();

        Sprite sprite = GetPartSprite(type);

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            image.color =
                new Color32(49, 91, 124, 255);
        }

        image.raycastTarget = true;

        // 이미지가 없거나 글자를 표시하도록
        // 설정했을 때 기본 TMP 글자 표시
        if (sprite == null ||
            !hidePartTextWhenSpriteExists)
        {
            GameObject textObject = new GameObject(
                "PartLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            textObject.transform.SetParent(
                go.transform,
                false
            );

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();

            text.text = GetPartName(type);
            text.fontSize = 23f;
            text.fontStyle = FontStyles.Bold;
            text.alignment =
                TextAlignmentOptions.Center;

            text.color = Color.white;
            text.raycastTarget = false;
        }

        MovingPart part = new MovingPart();

        part.obj = go;
        part.rect = rect;
        part.type = type;
        part.speed = Random.Range(
            Mathf.Max(10f, minMoveSpeed),
            Mathf.Max(minMoveSpeed + 1f, maxMoveSpeed)
        );

        Button button = go.GetComponent<Button>();

        button.onClick.AddListener(
            () => SelectPart(part)
        );

        movingParts.Add(part);
    }

    // ======================================
    // 부품 선택
    // ======================================

    private void SelectPart(MovingPart part)
    {
        if (phase != GamePhase.Selecting ||
            !isGameActive ||
            finished ||
            part == null)
        {
            return;
        }

        if (!movingParts.Contains(part))
            return;

        selectedParts.Add(part.type);

        string nameOfPart =
            GetPartName(part.type);

        movingParts.Remove(part);

        part.obj.SetActive(false);
        Destroy(part.obj);

        UpdateSelectionText();

        partStatusText.text =
            nameOfPart + " 선택 완료";

        Debug.Log(
            "MG27: " + nameOfPart +
            " 선택 (" +
            selectedParts.Count + "/5)"
        );

        if (selectedParts.Count >= 5)
        {
            EnterCircuitStage();
        }
    }

    private void UpdateSelectionText()
    {
        selectedPartsText.text =
            "SELECTED : " +
            selectedParts.Count +
            " / 5";
    }

    private void ClearMovingParts()
    {
        foreach (MovingPart part in movingParts)
        {
            if (part.obj != null)
            {
                part.obj.SetActive(false);
                Destroy(part.obj);
            }
        }

        movingParts.Clear();
    }

    // ======================================
    // 2단계: 회로 연결 시작
    // ======================================

    private void EnterCircuitStage()
    {
        phase = GamePhase.Connecting;

        ClearMovingParts();

        conveyorPanel.SetActive(false);
        circuitPanel.SetActive(true);

        finalStartButton.gameObject.SetActive(false);

        Canvas.ForceUpdateCanvases();

        ResetCircuitConnections();

        circuitStatusText.text =
            "POWER부터 순서대로 연결하십시오.";

        Debug.Log("MG27: 회로 연결 시작");
    }

    private Button GetNode(int index)
    {
        switch (index)
        {
            case 0: return powerNode;
            case 1: return inputNode;
            case 2: return displayNode;
            case 3: return startNode;
        }

        return null;
    }

    // ======================================
    // 회로 노드 클릭
    // ======================================

    private void OnNodeClicked(int nodeIndex)
    {
        if (phase != GamePhase.Connecting ||
            !isGameActive ||
            finished)
        {
            return;
        }

        if (nodeIndex != nextNodeIndex)
        {
            // 순서가 틀리면 회로만 다시 연결
            ResetCircuitConnections();

            circuitStatusText.text =
                "연결 순서 오류! POWER부터 다시 연결하십시오.";

            Debug.Log(
                "MG27: 잘못된 회로 연결 순서"
            );

            return;
        }

        nextNodeIndex++;

        RefreshNodeAppearance();

        if (nextNodeIndex >= 2)
        {
            Button previous =
                GetNode(nextNodeIndex - 2);

            Button current =
                GetNode(nextNodeIndex - 1);

            CreateCircuitLine(
                previous.GetComponent<RectTransform>(),
                current.GetComponent<RectTransform>()
            );
        }

        if (nextNodeIndex >= 4)
        {
            StartCoroutine(VerifyCircuitRoutine());
            return;
        }

        Button next = GetNode(nextNodeIndex);

        circuitStatusText.text =
            "다음 연결 : " +
            next.name.Replace("Node", "").ToUpper();
    }

    // ======================================
    // 회로 연결선 자동 생성
    // ======================================

    private void CreateCircuitLine(
        RectTransform fromNode,
        RectTransform toNode)
    {
        Vector2 from =
            circuitLineLayer.InverseTransformPoint(
                fromNode.position
            );

        Vector2 to =
            circuitLineLayer.InverseTransformPoint(
                toNode.position
            );

        Vector2 difference = to - from;

        GameObject go = new GameObject(
            "CircuitLine",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        go.transform.SetParent(
            circuitLineLayer,
            false
        );

        RectTransform rect =
            go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            (from + to) * 0.5f;

        rect.sizeDelta = new Vector2(
            difference.magnitude,
            circuitLineThickness
        );

        float angle =
            Mathf.Atan2(
                difference.y,
                difference.x
            ) * Mathf.Rad2Deg;

        rect.localRotation =
            Quaternion.Euler(0f, 0f, angle);

        Image image = go.GetComponent<Image>();

        image.color =
            new Color32(77, 225, 195, 255);

        if (circuitLineSprite != null)
        {
            image.sprite = circuitLineSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        image.raycastTarget = false;

        generatedLines.Add(go);
    }

    private void ClearCircuitLines()
    {
        foreach (GameObject line in generatedLines)
        {
            if (line != null)
            {
                line.SetActive(false);
                Destroy(line);
            }
        }

        generatedLines.Clear();
    }

    private void ResetCircuitConnections()
    {
        nextNodeIndex = 0;

        ClearCircuitLines();
        RefreshNodeAppearance();
    }

    private void RefreshNodeAppearance()
    {
        for (int i = 0; i < 4; i++)
        {
            Button node = GetNode(i);

            if (node == null)
                continue;

            Image image = node.GetComponent<Image>();

            if (image != null)
            {
                // 아트 이미지가 있으면 원본 색 유지
                if (image.sprite != null)
                {
                    image.color = Color.white;
                }
                else
                {
                    image.color = i < nextNodeIndex
                        ? new Color32(22, 160, 133, 255)
                        : new Color32(40, 76, 104, 255);
                }
            }

            foreach (TMP_Text text in
                node.GetComponentsInChildren<TMP_Text>(
                    true
                ))
            {
                text.color = i < nextNodeIndex
                    ? new Color32(113, 255, 213, 255)
                    : Color.white;
            }
        }
    }

    // ======================================
    // 부품 검증
    // ======================================

    private bool HasAllCorrectParts()
    {
        if (selectedParts.Count != requiredParts.Length)
            return false;

        HashSet<PartType> distinct =
            new HashSet<PartType>(selectedParts);

        if (distinct.Count != requiredParts.Length)
            return false;

        foreach (PartType required in requiredParts)
        {
            if (!distinct.Contains(required))
                return false;
        }

        return true;
    }

    private IEnumerator VerifyCircuitRoutine()
    {
        phase = GamePhase.Verifying;

        // 올바르지 않은 부품이어도
        // 먼저 가짜 성공 화면 표시
        circuitStatusText.text = "SYSTEM READY";
        circuitStatusText.color =
            new Color32(90, 255, 181, 255);

        Debug.Log("MG27: SYSTEM READY");

        yield return new WaitForSeconds(
            readyDisplayTime
        );

        if (!isGameActive || finished)
            yield break;

        if (HasAllCorrectParts())
        {
            ShowFinalStartButton();
        }
        else
        {
            phase = GamePhase.Error;

            circuitStatusText.text =
                "SYSTEM ERROR\n잘못된 부품이 발견되었습니다.";

            circuitStatusText.color =
                new Color32(255, 109, 109, 255);

            Debug.Log(
                "MG27: SYSTEM ERROR - 잘못된 부품"
            );

            yield return new WaitForSeconds(
                errorDisplayTime
            );

            if (!isGameActive || finished)
                yield break;

            KeepCorrectPartsOnly();
            BeginSelectionStage();
        }
    }

    // ======================================
    // 잘못된 부품만 다시 선택
    // ======================================

    private void KeepCorrectPartsOnly()
    {
        List<PartType> correct =
            new List<PartType>();

        foreach (PartType part in selectedParts)
        {
            bool required = false;

            foreach (PartType needed in requiredParts)
            {
                if (part == needed)
                {
                    required = true;
                    break;
                }
            }

            // 같은 정상 부품을 중복 선택했으면
            // 하나만 보존
            if (required && !correct.Contains(part))
            {
                correct.Add(part);
            }
        }

        selectedParts.Clear();
        selectedParts.AddRange(correct);

        Debug.Log(
            "MG27: 정상 부품 " +
            correct.Count +
            "개 유지, 잘못된 부품 제거"
        );
    }

    // ======================================
    // 최종 START 활성화
    // ======================================

    private void ShowFinalStartButton()
    {
        phase = GamePhase.StartReady;

        circuitStatusText.text =
            "SYSTEM READY\n회로 복구 완료! START를 누르십시오.";

        circuitStatusText.color =
            new Color32(90, 255, 181, 255);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();

        Debug.Log(
            "MG27: 최종 START 활성화"
        );
    }

    private void OnFinalStartClicked()
    {
        if (phase != GamePhase.StartReady ||
            !isGameActive ||
            finished)
        {
            return;
        }

        finished = true;
        phase = GamePhase.Completed;

        Debug.Log("MG27: 검사 완료");

        Success();
    }

    protected override void GiveHint()
    {
        if (phase == GamePhase.Selecting)
        {
            partStatusText.text =
                "정상 부품: POWER, INPUT, DISPLAY, " +
                "START, NORMAL CABLE";
        }
        else if (phase == GamePhase.Connecting)
        {
            circuitStatusText.text =
                "연결 순서: POWER → INPUT → DISPLAY → START";
        }
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
