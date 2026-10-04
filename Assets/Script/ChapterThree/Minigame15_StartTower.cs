using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Minigame15_StartTower : MinigameBase
{
    [Header("Play Area")]
    public RectTransform playArea;
    public RectTransform floor;
    public RectTransform spawnPoint;

    [Header("Letter Blocks - S T A R T Order")]
    public RectTransform[] letterBlocks;

    [Header("Completed START")]
    public GameObject completionPanel;
    public Button completedStartButton;

    [Header("UI")]
    public TMP_Text hintText;
    public TMP_Text timerText;
    public TMP_Text fallText;

    [Header("Gameplay Background")]
    public Graphic gameplayBackground;

    [Header("Block Control")]
    public float horizontalMoveSpeed = 900f;
    public float horizontalFollowStrength = 12f;
    public float fastDropSpeed = 350f;

    [Header("Physics")]
    public float gravityScale = 30f;
    public float stableVelocityThreshold = 8f;
    public float stableAngularThreshold = 10f;
    public float landingStableTime = 0.7f;
    public PhysicsMaterial2D blockPhysicsMaterial;

    [Header("Fall Settings")]
    public float respawnDelay = 0.6f;
    public float fallMargin = 100f;

    [Header("Final Stability")]
    public float stabilityCheckTime = 3f;

    [Header("Fall Assist")]
    [Range(0.1f, 1f)]
    public float horizontalSlowMultiplier = 0.65f;

    [Header("Center Hint")]
    public float centerHintTime = 70f;
    public RectTransform centerHintLine;

    private RectTransform activeBlock;
    private Rigidbody2D activeBody;
    private BoxCollider2D activeCollider;

    private readonly List<RectTransform> placedBlocks =
        new List<RectTransform>();

    private int currentBlockIndex;
    private int fallCount;

    private float elapsedTime;
    private float currentHorizontalSpeed;
    private float landingTimer;

    private bool hasTouchedSupport;
    private bool isRespawning;
    private bool speedAssistApplied;
    private bool centerHintShown;
    private bool stabilityChecking;
    private bool towerCompleted;

    private Canvas parentCanvas;

    private Coroutine messageCoroutine;
    private Coroutine stabilityCoroutine;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "실행 구조 안정성 검사";
        instruction = "S, T, A, R, T 블록을 안정적으로 쌓으세요.";

        base.StartMinigame();

        StopAllCoroutines();

        parentCanvas = GetComponentInParent<Canvas>();

        if (gameplayBackground == null)
        {
            gameplayBackground = GetComponent<Graphic>();
        }

        currentBlockIndex = 0;
        fallCount = 0;
        elapsedTime = 0f;
        landingTimer = 0f;

        currentHorizontalSpeed = horizontalMoveSpeed;

        hasTouchedSupport = false;
        isRespawning = false;
        speedAssistApplied = false;
        centerHintShown = false;
        stabilityChecking = false;
        towerCompleted = false;

        placedBlocks.Clear();

        if (gameplayBackground != null)
        {
            gameplayBackground.enabled = true;
        }

        if (playArea != null)
        {
            playArea.gameObject.SetActive(true);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = instruction;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        if (fallText != null)
        {
            fallText.gameObject.SetActive(true);
        }

        if (centerHintLine != null)
        {
            centerHintLine.gameObject.SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        SetupCompletedStartButton();

        SetupFloorPhysics();
        SetupAllBlockPhysics();
        ResetLetterBlocks();

        UpdateFallUI();
        UpdateTimerUI();

        SpawnCurrentBlock();
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        elapsedTime = timeLimit - currentTimer;

        UpdateTimerUI();
        CheckCenterHint();

        if (centerHintShown)
        {
            UpdateCenterHintLine();
        }

        if (!isRespawning && CheckPlacedTowerCollapsed())
        {
            HandleTowerCollapse();
            return;
        }

        if (activeBlock == null ||
            stabilityChecking ||
            towerCompleted ||
            isRespawning)
        {
            return;
        }

        if (IsBlockBelowFailLine(activeBlock))
        {
            OnActiveBlockFallen();
            return;
        }

        if (!hasTouchedSupport)
        {
            MoveBlockHorizontally();

            if (Input.GetMouseButtonDown(0))
            {
                FastDrop();
            }

            if (activeCollider != null &&
                activeCollider.IsTouchingLayers())
            {
                hasTouchedSupport = true;
                landingTimer = 0f;

                if (activeBody != null)
                {
                    activeBody.constraints =
                        RigidbodyConstraints2D.None;

                    Vector2 velocity =
                        activeBody.linearVelocity;

                    velocity.x *= 0.15f;

                    activeBody.linearVelocity =
                        velocity;
                }
            }
        }
        else
        {
            CheckActiveBlockStability();
        }
    }

    private void SetupFloorPhysics()
    {
        if (floor == null)
            return;

        BoxCollider2D collider =
            floor.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider =
                floor.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.size = floor.rect.size;
        collider.offset = Vector2.zero;

        if (blockPhysicsMaterial != null)
        {
            collider.sharedMaterial =
                blockPhysicsMaterial;
        }

        Rigidbody2D body =
            floor.GetComponent<Rigidbody2D>();

        if (body == null)
        {
            body =
                floor.gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void SetupAllBlockPhysics()
    {
        if (letterBlocks == null)
            return;

        foreach (RectTransform block in letterBlocks)
        {
            if (block == null)
                continue;

            SetupBlockPhysics(block);
            DisableBlockRaycasts(block);
        }
    }

    private void SetupBlockPhysics(RectTransform block)
    {
        BoxCollider2D collider =
            block.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider =
                block.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.size = block.rect.size;
        collider.offset = Vector2.zero;

        if (blockPhysicsMaterial != null)
        {
            collider.sharedMaterial =
                blockPhysicsMaterial;
        }

        Rigidbody2D body =
            block.GetComponent<Rigidbody2D>();

        if (body == null)
        {
            body =
                block.gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.linearDamping = 0.3f;
        body.angularDamping = 0.4f;
    }

    private void DisableBlockRaycasts(RectTransform block)
    {
        Graphic[] graphics =
            block.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private void SpawnCurrentBlock()
    {
        if (letterBlocks == null ||
            currentBlockIndex >= letterBlocks.Length)
        {
            return;
        }

        activeBlock =
            letterBlocks[currentBlockIndex];

        if (activeBlock == null)
            return;

        activeBlock.gameObject.SetActive(true);

        Vector2 spawnPosition = Vector2.zero;

        if (spawnPoint != null)
        {
            spawnPosition =
                spawnPoint.anchoredPosition;
        }
        else if (playArea != null)
        {
            spawnPosition =
                new Vector2(
                    0f,
                    playArea.rect.yMax - 60f
                );
        }

        activeBlock.anchoredPosition =
            spawnPosition;

        activeBlock.localRotation =
            Quaternion.identity;

        activeBody =
            activeBlock.GetComponent<Rigidbody2D>();

        activeCollider =
            activeBlock.GetComponent<BoxCollider2D>();

        if (activeBody == null ||
            activeCollider == null)
        {
            SetupBlockPhysics(activeBlock);

            activeBody =
                activeBlock.GetComponent<Rigidbody2D>();

            activeCollider =
                activeBlock.GetComponent<BoxCollider2D>();
        }

        activeCollider.size =
            activeBlock.rect.size;

        activeBody.bodyType =
            RigidbodyType2D.Dynamic;

        activeBody.gravityScale =
            gravityScale;

        activeBody.linearVelocity =
            Vector2.zero;

        activeBody.angularVelocity =
            0f;

        activeBody.constraints =
            RigidbodyConstraints2D.FreezeRotation;

        hasTouchedSupport = false;
        landingTimer = 0f;

        activeBlock.SetAsLastSibling();
    }

    private void MoveBlockHorizontally()
    {
        if (playArea == null ||
            activeBlock == null ||
            activeBody == null)
        {
            return;
        }

        Camera uiCamera = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                parentCanvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                Input.mousePosition,
                uiCamera,
                out Vector2 mousePosition))
        {
            return;
        }

        float halfWidth =
            activeBlock.rect.width * 0.5f;

        float targetX =
            Mathf.Clamp(
                mousePosition.x,
                playArea.rect.xMin + halfWidth,
                playArea.rect.xMax - halfWidth
            );

        Vector3 currentLocal =
            playArea.InverseTransformPoint(
                activeBody.position
            );

        Vector3 targetWorld =
            playArea.TransformPoint(
                new Vector3(
                    targetX,
                    currentLocal.y,
                    0f
                )
            );

        float difference =
            targetWorld.x -
            activeBody.position.x;

        float worldMaxSpeed =
            playArea.TransformVector(
                Vector3.right *
                currentHorizontalSpeed
            ).magnitude;

        float desiredXVelocity =
            Mathf.Clamp(
                difference *
                horizontalFollowStrength,
                -worldMaxSpeed,
                worldMaxSpeed
            );

        Vector2 velocity =
            activeBody.linearVelocity;

        velocity.x =
            desiredXVelocity;

        activeBody.linearVelocity =
            velocity;
    }

    private void FastDrop()
    {
        if (activeBody == null)
            return;

        float worldSpeed =
            fastDropSpeed;

        if (playArea != null)
        {
            worldSpeed =
                playArea.TransformVector(
                    Vector3.up *
                    fastDropSpeed
                ).magnitude;
        }

        Vector2 velocity =
            activeBody.linearVelocity;

        velocity.y =
            -Mathf.Abs(worldSpeed);

        activeBody.linearVelocity =
            velocity;
    }

    private void CheckActiveBlockStability()
    {
        if (activeBody == null ||
            activeCollider == null)
        {
            return;
        }

        if (!activeCollider.IsTouchingLayers())
        {
            landingTimer = 0f;
            return;
        }

        bool velocityStable =
            activeBody.linearVelocity.magnitude
            <= stableVelocityThreshold;

        bool rotationStable =
            Mathf.Abs(activeBody.angularVelocity)
            <= stableAngularThreshold;

        if (velocityStable &&
            rotationStable)
        {
            landingTimer +=
                Time.deltaTime;
        }
        else
        {
            landingTimer = 0f;
        }

        if (landingTimer >=
            landingStableTime)
        {
            AcceptCurrentBlock();
        }
    }

    private void AcceptCurrentBlock()
    {
        if (activeBlock == null)
            return;

        placedBlocks.Add(activeBlock);

        activeBlock = null;
        activeBody = null;
        activeCollider = null;

        currentBlockIndex++;

        hasTouchedSupport = false;
        landingTimer = 0f;

        if (currentBlockIndex >=
            letterBlocks.Length)
        {
            stabilityCoroutine =
                StartCoroutine(
                    FinalStabilityRoutine()
                );

            return;
        }

        StartCoroutine(
            SpawnNextBlockRoutine()
        );
    }

    private IEnumerator SpawnNextBlockRoutine()
    {
        yield return
            new WaitForSeconds(0.25f);

        SpawnCurrentBlock();
    }

    private void OnActiveBlockFallen()
    {
        if (activeBlock == null ||
            isRespawning)
        {
            return;
        }

        RectTransform failedBlock =
            activeBlock;

        activeBlock = null;
        activeBody = null;
        activeCollider = null;

        DisablePhysicsBlock(
            failedBlock
        );

        fallCount++;

        UpdateFallUI();

        ShowMessage(
            "블록이 떨어졌습니다. 같은 블록이 다시 제공됩니다."
        );

        ApplyFallAssist();

        StartCoroutine(
            RespawnCurrentBlock()
        );
    }

    private IEnumerator RespawnCurrentBlock()
    {
        isRespawning = true;

        yield return
            new WaitForSeconds(
                respawnDelay
            );

        isRespawning = false;

        if (isGameActive &&
            !towerCompleted)
        {
            SpawnCurrentBlock();
        }
    }

    private bool CheckPlacedTowerCollapsed()
    {
        foreach (RectTransform block
                 in placedBlocks)
        {
            if (block == null)
                continue;

            if (IsBlockBelowFailLine(block))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleTowerCollapse()
    {
        if (isRespawning)
            return;

        if (stabilityCoroutine != null)
        {
            StopCoroutine(
                stabilityCoroutine
            );

            stabilityCoroutine = null;
        }

        stabilityChecking = false;

        fallCount++;

        UpdateFallUI();

        ShowMessage(
            "탑이 무너졌습니다. 처음부터 다시 쌓습니다."
        );

        ApplyFallAssist();

        ResetTowerOnly();

        StartCoroutine(
            RespawnTowerRoutine()
        );
    }

    private IEnumerator RespawnTowerRoutine()
    {
        isRespawning = true;

        yield return
            new WaitForSeconds(
                respawnDelay
            );

        isRespawning = false;

        SpawnCurrentBlock();
    }

    private bool IsBlockBelowFailLine(
        RectTransform block)
    {
        if (block == null ||
            playArea == null)
        {
            return false;
        }

        Vector3 local =
            playArea.InverseTransformPoint(
                block.position
            );

        return
            local.y <
            playArea.rect.yMin -
            fallMargin;
    }

    private void ApplyFallAssist()
    {
        if (fallCount >= 2 &&
            !centerHintShown)
        {
            ShowCenterHint();

            ShowMessage(
                "[ HINT ] 블록의 중심 위치가 표시됩니다."
            );
        }

        if (fallCount >= 4 &&
            !speedAssistApplied)
        {
            speedAssistApplied = true;

            currentHorizontalSpeed =
                horizontalMoveSpeed *
                horizontalSlowMultiplier;

            ShowMessage(
                "[ HINT ] 블록의 좌우 이동 속도가 감소했습니다."
            );
        }
    }

    private void CheckCenterHint()
    {
        if (centerHintShown)
            return;

        if (elapsedTime <
            centerHintTime)
        {
            return;
        }

        ShowCenterHint();

        ShowMessage(
            "[ HINT ] 블록의 중심 위치가 표시됩니다."
        );
    }

    private void ShowCenterHint()
    {
        centerHintShown = true;

        if (centerHintLine != null)
        {
            centerHintLine.gameObject
                .SetActive(true);

            UpdateCenterHintLine();
        }
    }

    private void UpdateCenterHintLine()
    {
        if (centerHintLine == null ||
            playArea == null)
        {
            return;
        }

        float targetX = 0f;

        if (placedBlocks.Count > 0)
        {
            targetX =
                placedBlocks[
                    placedBlocks.Count - 1
                ].anchoredPosition.x;
        }
        else if (floor != null)
        {
            targetX =
                floor.anchoredPosition.x;
        }

        Vector2 position =
            centerHintLine.anchoredPosition;

        position.x = targetX;
        position.y = 0f;

        centerHintLine.anchoredPosition =
            position;

        Vector2 size =
            centerHintLine.sizeDelta;

        size.y =
            playArea.rect.height;

        centerHintLine.sizeDelta =
            size;
    }

    private IEnumerator FinalStabilityRoutine()
    {
        stabilityChecking = true;

        float timer =
            stabilityCheckTime;

        while (timer > 0f)
        {
            if (!isGameActive)
            {
                yield break;
            }

            if (AreAllPlacedBlocksStable())
            {
                timer -=
                    Time.deltaTime;
            }
            else
            {
                timer =
                    stabilityCheckTime;
            }

            if (hintText != null)
            {
                hintText.text =
                    $"구조 안정성 확인 중... {Mathf.Max(0f, timer):0.0}";
            }

            yield return null;
        }

        stabilityChecking = false;
        stabilityCoroutine = null;

        ShowFinalStartScreen();
    }

    private bool AreAllPlacedBlocksStable()
    {
        if (placedBlocks.Count == 0)
            return false;

        foreach (RectTransform block
                 in placedBlocks)
        {
            if (block == null)
                return false;

            Rigidbody2D body =
                block.GetComponent<Rigidbody2D>();

            BoxCollider2D collider =
                block.GetComponent<BoxCollider2D>();

            if (body == null ||
                collider == null)
            {
                return false;
            }

            if (!collider.IsTouchingLayers())
            {
                return false;
            }

            if (body.linearVelocity.magnitude >
                stableVelocityThreshold)
            {
                return false;
            }

            if (Mathf.Abs(
                    body.angularVelocity
                ) >
                stableAngularThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowFinalStartScreen()
    {
        towerCompleted = true;

        foreach (RectTransform block
                 in placedBlocks)
        {
            if (block == null)
                continue;

            Rigidbody2D body =
                block.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                body.linearVelocity =
                    Vector2.zero;

                body.angularVelocity =
                    0f;

                body.bodyType =
                    RigidbodyType2D.Kinematic;
            }
        }

        if (gameplayBackground != null)
        {
            gameplayBackground.enabled =
                false;
        }

        if (playArea != null)
        {
            playArea.gameObject
                .SetActive(false);
        }

        if (letterBlocks != null)
        {
            foreach (RectTransform block
                     in letterBlocks)
            {
                if (block != null)
                {
                    block.gameObject
                        .SetActive(false);
                }
            }
        }

        if (centerHintLine != null)
        {
            centerHintLine.gameObject
                .SetActive(false);
        }

        if (hintText != null)
        {
            hintText.gameObject
                .SetActive(false);
        }

        if (timerText != null)
        {
            timerText.gameObject
                .SetActive(false);
        }

        if (fallText != null)
        {
            fallText.gameObject
                .SetActive(false);
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            completionPanel.transform.SetAsLastSibling();
        }

        if (completedStartButton != null)
        {
            completedStartButton.gameObject
                .SetActive(true);

            completedStartButton.interactable =
                true;

            RectTransform buttonRect =
                completedStartButton.transform
                    as RectTransform;

            if (buttonRect != null)
            {
                buttonRect.anchorMin =
                    new Vector2(
                        0.5f,
                        0.5f
                    );

                buttonRect.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f
                    );

                buttonRect.pivot =
                    new Vector2(
                        0.5f,
                        0.5f
                    );

                buttonRect.anchoredPosition =
                    Vector2.zero;

                buttonRect.localRotation =
                    Quaternion.identity;

                buttonRect.localScale =
                    Vector3.one;
            }

            Image buttonImage =
                completedStartButton
                    .GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.enabled = true;
                buttonImage.raycastTarget = true;
            }

            TMP_Text buttonText =
                completedStartButton
                    .GetComponentInChildren<TMP_Text>(
                        true
                    );

            if (buttonText != null)
            {
                buttonText.gameObject.SetActive(true);
                buttonText.raycastTarget = false;
            }

            CanvasGroup canvasGroup =
                completedStartButton
                    .GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    completedStartButton
                        .gameObject
                        .AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            completedStartButton.transform
                .SetAsLastSibling();
        }
    }

    private void SetupCompletedStartButton()
    {
        if (completedStartButton == null)
            return;

        completedStartButton.onClick
            .RemoveAllListeners();

        completedStartButton.onClick
            .AddListener(
                OnCompletedStartClicked
            );

        completedStartButton.interactable =
            true;

        completedStartButton.gameObject
            .SetActive(false);
    }

    private void OnCompletedStartClicked()
    {
        if (!isGameActive)
            return;

        if (!towerCompleted)
            return;

        Debug.Log(
            "미니게임15 완료"
        );

        completedStartButton.interactable =
            false;

        Success();
    }

    private void ResetTowerOnly()
    {
        if (letterBlocks != null)
        {
            foreach (RectTransform block
                     in letterBlocks)
            {
                if (block == null)
                    continue;

                DisablePhysicsBlock(
                    block
                );
            }
        }

        placedBlocks.Clear();

        activeBlock = null;
        activeBody = null;
        activeCollider = null;

        currentBlockIndex = 0;
        hasTouchedSupport = false;
        landingTimer = 0f;
    }

    private void ResetLetterBlocks()
    {
        if (letterBlocks == null)
            return;

        foreach (RectTransform block
                 in letterBlocks)
        {
            if (block == null)
                continue;

            DisablePhysicsBlock(
                block
            );
        }
    }

    private void DisablePhysicsBlock(
        RectTransform block)
    {
        if (block == null)
            return;

        Rigidbody2D body =
            block.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity =
                Vector2.zero;

            body.angularVelocity =
                0f;

            body.gravityScale =
                0f;

            body.bodyType =
                RigidbodyType2D.Kinematic;

            body.constraints =
                RigidbodyConstraints2D.FreezeRotation;
        }

        block.localRotation =
            Quaternion.identity;

        block.gameObject
            .SetActive(false);
    }

    private void UpdateFallUI()
    {
        if (fallText != null)
        {
            fallText.text =
                $"FALL {fallCount}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        float displayTime =
            Mathf.Max(
                0f,
                currentTimer
            );

        int minutes =
            Mathf.FloorToInt(
                displayTime / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                displayTime % 60f
            );

        timerText.text =
            $"TIME {minutes:00}:{seconds:00}";
    }

    private void ShowMessage(
        string message)
    {
        if (hintText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }

        hintText.text =
            message;

        messageCoroutine =
            StartCoroutine(
                ClearMessageRoutine()
            );
    }

    private IEnumerator ClearMessageRoutine()
    {
        yield return
            new WaitForSeconds(2f);

        if (hintText != null &&
            isGameActive &&
            !towerCompleted &&
            !stabilityChecking)
        {
            hintText.text =
                instruction;
        }

        messageCoroutine = null;
    }

    protected override void GiveHint()
    {
        ShowCenterHint();
    }

    protected override void RestartGame()
    {
        StopAllCoroutines();
        StartMinigame();
    }
}