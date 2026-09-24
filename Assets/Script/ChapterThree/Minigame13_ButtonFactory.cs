using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Minigame13_ButtonFactory : MinigameBase
{
    [Header("Conveyor")]
    public RectTransform conveyorArea;

    [Header("Good Parts")]
    public Button[] goodParts;

    [Header("Bad Parts")]
    public Button[] badParts;

    [Header("Conveyor Settings")]
    public float conveyorSpeed = 180f;
    public float wrapPadding = 100f;
    public float partSpacing = 160f;

    [Header("Assembly")]
    public GameObject[] assemblySlots;
    public Button completedStartButton;
    public RectTransform badButtonPreview;

    [Header("UI")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI assemblyCountText;

    [Header("Error 2 Hint")]
    public float glowDuration = 1.5f;
    public Color glowColor = Color.green;

    [Header("Error 4 Assist")]
    [Range(0.1f, 1f)]
    public float conveyorSlowMultiplier = 0.6f;

    [Header("Error 6 Assist")]
    public int badPartsToRemove = 2;

    [Header("Bad Part Feedback")]
    public float badClickCooldown = 0.6f;
    public float badPreviewWaitTime = 0.2f;
    public float badPreviewEjectDuration = 0.45f;
    public float badPreviewEjectDistance = 700f;

    [Header("Message")]
    public float messageDuration = 2f;

    private bool[] goodCollected;

    private Vector2[] goodInitialPositions;
    private Vector2[] badInitialPositions;

    private Vector2 badPreviewInitialPosition;

    private int collectedCount = 0;
    private int errorCount = 0;

    private float currentConveyorSpeed;

    private bool error2Applied = false;
    private bool error4Applied = false;
    private bool error6Applied = false;

    private bool assemblyCompleted = false;

    private bool initialPositionsCached = false;

    private Coroutine messageCoroutine;
    private Coroutine badPreviewCoroutine;


    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }


    // =====================================================
    // 게임 시작
    // =====================================================

    public override void StartMinigame()
    {
        gameName = "실행 버튼 생산 검사";

        instruction =
            "컨베이어 벨트에서 정상 부품만 선택하여 START 버튼을 완성하세요.";

        base.StartMinigame();

        StopAllCoroutines();

        CacheInitialPositions();

        collectedCount = 0;
        errorCount = 0;

        error2Applied = false;
        error4Applied = false;
        error6Applied = false;

        assemblyCompleted = false;

        currentConveyorSpeed =
            conveyorSpeed;

        goodCollected =
            new bool[
                goodParts != null
                    ? goodParts.Length
                    : 0
            ];

        ResetParts();

        ResetAssembly();

        SetupPartButtons();

        SetupCompletedButton();

        SetGoodOutlines(false);

        UpdateUI();
        UpdateTimerUI();

        if (hintText != null)
        {
            hintText.text = instruction;
        }
    }


    // =====================================================
    // 초기 위치 저장
    // =====================================================

    private void CacheInitialPositions()
    {
        if (initialPositionsCached)
            return;

        if (goodParts != null)
        {
            goodInitialPositions =
                new Vector2[goodParts.Length];

            for (int i = 0;
                 i < goodParts.Length;
                 i++)
            {
                if (goodParts[i] != null)
                {
                    RectTransform rect =
                        goodParts[i]
                            .GetComponent<RectTransform>();

                    goodInitialPositions[i] =
                        rect.anchoredPosition;
                }
            }
        }

        if (badParts != null)
        {
            badInitialPositions =
                new Vector2[badParts.Length];

            for (int i = 0;
                 i < badParts.Length;
                 i++)
            {
                if (badParts[i] != null)
                {
                    RectTransform rect =
                        badParts[i]
                            .GetComponent<RectTransform>();

                    badInitialPositions[i] =
                        rect.anchoredPosition;
                }
            }
        }

        if (badButtonPreview != null)
        {
            badPreviewInitialPosition =
                badButtonPreview.anchoredPosition;
        }

        initialPositionsCached = true;
    }


    // =====================================================
    // Update
    // =====================================================

    protected override void Update()
    {
        base.Update();

        if (!isGameActive)
            return;

        if (!assemblyCompleted)
        {
            MoveConveyor();
        }

        UpdateTimerUI();
    }


    // =====================================================
    // 컨베이어 이동
    // =====================================================

    private void MoveConveyor()
    {
        MovePartArray(goodParts);
        MovePartArray(badParts);
    }


    private void MovePartArray(
        Button[] parts)
    {
        if (parts == null ||
            conveyorArea == null)
        {
            return;
        }

        foreach (Button part in parts)
        {
            if (part == null ||
                !part.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform rect =
                part.GetComponent<RectTransform>();

            rect.anchoredPosition +=
                Vector2.right *
                currentConveyorSpeed *
                Time.deltaTime;

            float halfWidth =
                rect.rect.width * 0.5f;

            if (rect.anchoredPosition.x -
                halfWidth >
                conveyorArea.rect.xMax +
                wrapPadding)
            {
                Vector2 pos =
                    rect.anchoredPosition;

                pos.x =
                    GetNextWrapPosition(rect);

                rect.anchoredPosition = pos;
            }
        }
    }


    private float GetNextWrapPosition(
        RectTransform ignore)
    {
        float leftMost =
            float.PositiveInfinity;

        FindLeftMost(
            goodParts,
            ignore,
            ref leftMost
        );

        FindLeftMost(
            badParts,
            ignore,
            ref leftMost
        );

        float defaultX =
            conveyorArea.rect.xMin -
            wrapPadding;

        if (float.IsPositiveInfinity(
            leftMost))
        {
            return defaultX;
        }

        return Mathf.Min(
            defaultX,
            leftMost - partSpacing
        );
    }


    private void FindLeftMost(
        Button[] parts,
        RectTransform ignore,
        ref float leftMost)
    {
        if (parts == null)
            return;

        foreach (Button part in parts)
        {
            if (part == null ||
                !part.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform rect =
                part.GetComponent<RectTransform>();

            if (rect == ignore)
                continue;

            if (rect.anchoredPosition.x <
                leftMost)
            {
                leftMost =
                    rect.anchoredPosition.x;
            }
        }
    }


    // =====================================================
    // 버튼 이벤트 연결
    // =====================================================

    private void SetupPartButtons()
    {
        if (goodParts != null)
        {
            for (int i = 0;
                 i < goodParts.Length;
                 i++)
            {
                if (goodParts[i] == null)
                    continue;

                int index = i;

                goodParts[i]
                    .onClick
                    .RemoveAllListeners();

                goodParts[i]
                    .onClick
                    .AddListener(
                        () =>
                            OnGoodPartClicked(
                                index
                            )
                    );
            }
        }

        if (badParts != null)
        {
            for (int i = 0;
                 i < badParts.Length;
                 i++)
            {
                if (badParts[i] == null)
                    continue;

                int index = i;

                badParts[i]
                    .onClick
                    .RemoveAllListeners();

                badParts[i]
                    .onClick
                    .AddListener(
                        () =>
                            OnBadPartClicked(
                                index
                            )
                    );
            }
        }
    }


    // =====================================================
    // 정상 부품
    // =====================================================

    private void OnGoodPartClicked(
        int index)
    {
        if (!isGameActive ||
            assemblyCompleted)
        {
            return;
        }

        if (index < 0 ||
            index >= goodParts.Length)
        {
            return;
        }

        if (goodCollected[index])
            return;

        goodCollected[index] = true;

        collectedCount++;

        goodParts[index]
            .gameObject
            .SetActive(false);

        if (assemblySlots != null &&
            index < assemblySlots.Length &&
            assemblySlots[index] != null)
        {
            assemblySlots[index]
                .SetActive(true);
        }

        UpdateUI();

        if (collectedCount >=
            goodParts.Length)
        {
            CompleteAssembly();
        }
    }


    // =====================================================
    // 불량 부품
    // =====================================================

    private void OnBadPartClicked(
        int index)
    {
        if (!isGameActive ||
            assemblyCompleted)
        {
            return;
        }

        if (badParts == null ||
            index < 0 ||
            index >= badParts.Length)
        {
            return;
        }

        Button badPart =
            badParts[index];

        if (badPart == null ||
            !badPart.interactable)
        {
            return;
        }

        errorCount++;

        UpdateUI();

        ShowMessage(
            "불량 부품입니다."
        );

        StartCoroutine(
            BadPartCooldown(
                badPart
            )
        );

        PlayBadButtonPreview();

        CheckErrorAssists();
    }


    private IEnumerator BadPartCooldown(
        Button part)
    {
        part.interactable = false;

        yield return new WaitForSeconds(
            badClickCooldown
        );

        if (part != null &&
            part.gameObject.activeSelf &&
            !assemblyCompleted)
        {
            part.interactable = true;
        }
    }


    // =====================================================
    // 불량 버튼 생성 → 밖으로 튕기기
    // =====================================================

    private void PlayBadButtonPreview()
    {
        if (badButtonPreview == null)
            return;

        if (badPreviewCoroutine != null)
        {
            StopCoroutine(
                badPreviewCoroutine
            );
        }

        badPreviewCoroutine =
            StartCoroutine(
                BadButtonPreviewRoutine()
            );
    }


    private IEnumerator
        BadButtonPreviewRoutine()
    {
        badButtonPreview
            .gameObject
            .SetActive(true);

        badButtonPreview
            .anchoredPosition =
            badPreviewInitialPosition;

        yield return new WaitForSeconds(
            badPreviewWaitTime
        );

        Vector2 start =
            badPreviewInitialPosition;

        Vector2 target =
            start +
            Vector2.right *
            badPreviewEjectDistance;

        float timer = 0f;

        while (timer <
               badPreviewEjectDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    badPreviewEjectDuration
                );

            // 뒤로 살짝 당겼다가
            // 빠르게 튕겨나가는 느낌
            float curvedT =
                t * t;

            badButtonPreview
                .anchoredPosition =
                Vector2.Lerp(
                    start,
                    target,
                    curvedT
                );

            yield return null;
        }

        badButtonPreview
            .gameObject
            .SetActive(false);

        badButtonPreview
            .anchoredPosition =
            badPreviewInitialPosition;

        badPreviewCoroutine = null;
    }


    // =====================================================
    // 오류 힌트
    // =====================================================

    private void CheckErrorAssists()
    {
        // 오류 2회
        if (errorCount >= 2 &&
            !error2Applied)
        {
            error2Applied = true;

            StartCoroutine(
                FlashGoodPartsRoutine()
            );
        }

        // 오류 4회
        if (errorCount >= 4 &&
            !error4Applied)
        {
            error4Applied = true;

            currentConveyorSpeed =
                conveyorSpeed *
                conveyorSlowMultiplier;

            ShowMessage(
                "[ HINT ] 컨베이어 벨트의 속도가 감소했습니다."
            );
        }

        // 오류 6회
        if (errorCount >= 6 &&
            !error6Applied)
        {
            error6Applied = true;

            RemoveSomeBadParts();

            ShowMessage(
                "[ HINT ] 일부 불량 부품이 제거되었습니다."
            );
        }
    }


    // =====================================================
    // 오류 2회 - 정상 부품 외곽선
    // =====================================================

    private IEnumerator
        FlashGoodPartsRoutine()
    {
        SetGoodOutlines(true);

        ShowMessage(
            "[ HINT ] 정상 부품의 외곽선을 확인하세요."
        );

        yield return new WaitForSeconds(
            glowDuration
        );

        SetGoodOutlines(false);
    }


    private void SetGoodOutlines(
        bool visible)
    {
        if (goodParts == null)
            return;

        foreach (Button part
                 in goodParts)
        {
            if (part == null)
                continue;

            Outline outline =
                part.GetComponent<Outline>();

            if (outline == null)
            {
                outline =
                    part.gameObject
                        .AddComponent<Outline>();

                outline.effectColor =
                    glowColor;

                outline.effectDistance =
                    new Vector2(
                        4f,
                        -4f
                    );
            }

            outline.effectColor =
                glowColor;

            outline.enabled =
                visible &&
                part.gameObject.activeSelf;
        }
    }


    // =====================================================
    // 오류 6회 - 불량 부품 일부 제거
    // =====================================================

    private void RemoveSomeBadParts()
    {
        if (badParts == null)
            return;

        int removed = 0;

        for (int i =
             badParts.Length - 1;
             i >= 0;
             i--)
        {
            if (removed >=
                badPartsToRemove)
            {
                break;
            }

            if (badParts[i] == null ||
                !badParts[i]
                    .gameObject
                    .activeSelf)
            {
                continue;
            }

            badParts[i]
                .gameObject
                .SetActive(false);

            removed++;
        }
    }


    // =====================================================
    // 조립 완료
    // =====================================================

    private void CompleteAssembly()
    {
        assemblyCompleted = true;

        SetAllPartInteractable(false);

        if (completedStartButton != null)
        {
            completedStartButton
                .gameObject
                .SetActive(true);
        }

        if (hintText != null)
        {
            hintText.text =
                "START 버튼이 완성되었습니다. 완성된 버튼을 클릭하세요.";
        }
    }


    private void SetupCompletedButton()
    {
        if (completedStartButton == null)
            return;

        completedStartButton
            .gameObject
            .SetActive(false);

        completedStartButton
            .onClick
            .RemoveAllListeners();

        completedStartButton
            .onClick
            .AddListener(
                OnCompletedButtonClicked
            );
    }


    private void
        OnCompletedButtonClicked()
    {
        if (!isGameActive ||
            !assemblyCompleted)
        {
            return;
        }

        Success();
    }


    // =====================================================
    // 리셋
    // =====================================================

    private void ResetParts()
    {
        if (goodParts != null)
        {
            for (int i = 0;
                 i < goodParts.Length;
                 i++)
            {
                if (goodParts[i] == null)
                    continue;

                goodParts[i]
                    .gameObject
                    .SetActive(true);

                goodParts[i]
                    .interactable = true;

                if (goodInitialPositions != null &&
                    i <
                    goodInitialPositions.Length)
                {
                    goodParts[i]
                        .GetComponent<
                            RectTransform>()
                        .anchoredPosition =
                        goodInitialPositions[i];
                }
            }
        }

        if (badParts != null)
        {
            for (int i = 0;
                 i < badParts.Length;
                 i++)
            {
                if (badParts[i] == null)
                    continue;

                badParts[i]
                    .gameObject
                    .SetActive(true);

                badParts[i]
                    .interactable = true;

                if (badInitialPositions != null &&
                    i <
                    badInitialPositions.Length)
                {
                    badParts[i]
                        .GetComponent<
                            RectTransform>()
                        .anchoredPosition =
                        badInitialPositions[i];
                }
            }
        }
    }


    private void ResetAssembly()
    {
        if (assemblySlots != null)
        {
            foreach (GameObject slot
                     in assemblySlots)
            {
                if (slot != null)
                {
                    slot.SetActive(false);
                }
            }
        }

        if (completedStartButton != null)
        {
            completedStartButton
                .gameObject
                .SetActive(false);
        }

        if (badButtonPreview != null)
        {
            badButtonPreview
                .gameObject
                .SetActive(false);

            badButtonPreview
                .anchoredPosition =
                badPreviewInitialPosition;
        }
    }


    private void SetAllPartInteractable(
        bool value)
    {
        if (goodParts != null)
        {
            foreach (Button part
                     in goodParts)
            {
                if (part != null)
                    part.interactable =
                        value;
            }
        }

        if (badParts != null)
        {
            foreach (Button part
                     in badParts)
            {
                if (part != null)
                    part.interactable =
                        value;
            }
        }
    }


    // =====================================================
    // UI
    // =====================================================

    private void UpdateUI()
    {
        if (errorText != null)
        {
            errorText.text =
                $"ERROR {errorCount}";
        }

        if (assemblyCountText != null)
        {
            int total =
                goodParts != null
                    ? goodParts.Length
                    : 5;

            assemblyCountText.text =
                $"ASSEMBLY {collectedCount} / {total}";
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
            string.Format(
                "TIME {0:00}:{1:00}",
                minutes,
                seconds
            );
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

        hintText.text = message;

        messageCoroutine =
            StartCoroutine(
                ClearMessageRoutine()
            );
    }


    private IEnumerator
        ClearMessageRoutine()
    {
        yield return new WaitForSeconds(
            messageDuration
        );

        if (hintText != null &&
            isGameActive &&
            !assemblyCompleted)
        {
            hintText.text =
                instruction;
        }

        messageCoroutine = null;
    }


    protected override void GiveHint()
    {
    }


    protected override void RestartGame()
    {
        StartMinigame();
    }
}