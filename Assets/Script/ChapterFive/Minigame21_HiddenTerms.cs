
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Minigame21_HiddenTerms : MinigameBase
{
    [Header("Terms UI")]
    public ScrollRect termsScrollRect;
    public TextMeshProUGUI termsText;
    public Button revealedStartButton;

    [Header("Message UI (optional)")]
    public TMP_Text hintText;
    public TMP_Text statusText;
    public TMP_Text timeText;

    [Header("Hint Timing")]
    public float firstHintTime = 40f;
    public float secondHintTime = 60f;

    private const string HiddenLinkId = "hidden_start";

    private const string HiddenMarkup =
        "<link=\"hidden_start\">START</link>";

    private float elapsedTime;

    private bool firstHintShown;
    private bool secondHintShown;
    private bool startRevealed;
    private bool originalTextSaved;

    private string originalTermsText;

    private Canvas uiCanvas;

    private void Start()
    {
        if (!isGameActive)
        {
            StartMinigame();
        }
    }

    public override void StartMinigame()
    {
        gameName = "약관 속에 숨은 시작";
        instruction = "약관을 확인하십시오. 시작을 찾으십시오.";

        // MG21은 시간제한 없이 진행
        timeLimit = 999999f;

        base.StartMinigame();

        StopAllCoroutines();

        elapsedTime = 0f;
        firstHintShown = false;
        secondHintShown = false;
        startRevealed = false;

        if (termsScrollRect == null ||
            termsText == null ||
            revealedStartButton == null)
        {
            Debug.LogError(
                "MG21: Inspector UI 연결을 확인하세요.",
                this
            );

            isGameActive = false;
            return;
        }

        // 최초 약관 내용 저장
        if (!originalTextSaved)
        {
            originalTermsText = termsText.text;
            originalTextSaved = true;
        }
        else
        {
            // 재시작할 때 원래 약관으로 복구
            termsText.text = originalTermsText;
        }

        if (!termsText.text.Contains(HiddenMarkup))
        {
            Debug.LogWarning(
                "MG21: 약관에 숨겨진 START 태그가 없습니다.",
                this
            );
        }

        termsText.richText = true;
        termsText.raycastTarget = true;

        uiCanvas = termsText.GetComponentInParent<Canvas>();

        termsScrollRect.enabled = true;
        termsScrollRect.vertical = true;
        termsScrollRect.horizontal = false;
        termsScrollRect.StopMovement();

        // 버튼 클릭 이벤트 연결
        revealedStartButton.onClick.RemoveListener(
            OnRevealedStartClicked
        );

        revealedStartButton.onClick.AddListener(
            OnRevealedStartClicked
        );

        // 처음에는 START 버튼 숨기기
        revealedStartButton.gameObject.SetActive(false);
        revealedStartButton.interactable = true;

        if (hintText != null)
        {
            hintText.text = instruction;
        }

        if (statusText != null)
        {
            statusText.text = "START DATA : SEARCHING";
        }

        UpdateTimeText();

        StartCoroutine(ScrollToTopNextFrame());
    }

    private IEnumerator ScrollToTopNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (termsScrollRect != null && isGameActive)
        {
            termsScrollRect.StopMovement();

            termsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!isGameActive || termsText == null)
        {
            return;
        }

        // 플레이 시간 증가
        elapsedTime += Time.deltaTime;

        UpdateTimeText();

        if (startRevealed)
        {
            return;
        }

        // 40초 힌트
        if (!firstHintShown &&
            elapsedTime >= firstHintTime)
        {
            firstHintShown = true;

            if (hintText != null)
            {
                hintText.text =
                    "힌트: 끝이 정답이라는 보장은 없습니다.";
            }
        }

        // 60초 힌트
        if (!secondHintShown &&
            elapsedTime >= secondHintTime)
        {
            secondHintShown = true;

            if (hintText != null)
            {
                hintText.text =
                    "힌트: 약관 중간의 데이터를 살펴보세요.";
            }

            if (statusText != null)
            {
                statusText.text =
                    "START DATA : SIGNAL DETECTED";
            }
        }

        // 약관 속 START 위치 확인
        int hoveredLinkIndex = FindHoveredHiddenLink();

        bool hovering = hoveredLinkIndex >= 0;

        // 60초 이후 START 부분이 주기적으로 흔들림
        bool noiseFlash =
            secondHintShown &&
            Mathf.Repeat(
                elapsedTime - secondHintTime,
                3.5f
            ) < 0.18f;

        AnimateHiddenStart(hovering, noiseFlash);

        // 숨겨진 START 클릭
        if (hovering && Input.GetMouseButtonDown(0))
        {
            RevealStartButton();
        }
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
        {
            return;
        }

        int seconds = Mathf.FloorToInt(elapsedTime);

        int minutes = seconds / 60;
        int remainder = seconds % 60;

        // 예: TIME 00:42
        timeText.text =
            $"TIME {minutes:00}:{remainder:00}";
    }

    private int FindHoveredHiddenLink()
    {
        if (termsScrollRect == null ||
            termsText == null)
        {
            return -1;
        }

        RectTransform viewport =
            termsScrollRect.viewport != null
                ? termsScrollRect.viewport
                : termsScrollRect.GetComponent<RectTransform>();

        Camera eventCamera = null;

        if (uiCanvas != null &&
            uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = uiCanvas.worldCamera;
        }

        // 마우스가 약관 창 밖에 있으면 무시
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            viewport,
            Input.mousePosition,
            eventCamera))
        {
            return -1;
        }

        termsText.ForceMeshUpdate();

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                termsText,
                Input.mousePosition,
                eventCamera
            );

        if (linkIndex < 0 ||
            linkIndex >= termsText.textInfo.linkCount)
        {
            return -1;
        }

        TMP_LinkInfo link =
            termsText.textInfo.linkInfo[linkIndex];

        if (link.GetLinkID() == HiddenLinkId)
        {
            return linkIndex;
        }

        return -1;
    }

    private void AnimateHiddenStart(
        bool hovering,
        bool noiseFlash)
    {
        if (termsText == null)
        {
            return;
        }

        // 원래 글자 메시를 매번 다시 구성
        termsText.ForceMeshUpdate();

        if (!hovering && !noiseFlash)
        {
            return;
        }

        TMP_TextInfo info = termsText.textInfo;

        bool changed = false;

        for (int linkIndex = 0;
             linkIndex < info.linkCount;
             linkIndex++)
        {
            TMP_LinkInfo link =
                info.linkInfo[linkIndex];

            if (link.GetLinkID() != HiddenLinkId)
            {
                continue;
            }

            int first =
                link.linkTextfirstCharacterIndex;

            int end =
                first + link.linkTextLength;

            for (int i = first;
                 i < end && i < info.characterCount;
                 i++)
            {
                TMP_CharacterInfo character =
                    info.characterInfo[i];

                if (!character.isVisible)
                {
                    continue;
                }

                int meshIndex =
                    character.materialReferenceIndex;

                int vertexIndex =
                    character.vertexIndex;

                TMP_MeshInfo mesh =
                    info.meshInfo[meshIndex];

                // 마우스를 올리면 민트색으로 변경
                Color32 color = hovering
                    ? new Color32(145, 255, 204, 255)
                    : new Color32(220, 240, 255, 255);

                // 글자가 살짝 흔들리는 효과
                float shake = hovering
                    ? Mathf.Sin(
                        Time.unscaledTime * 19f +
                        i * 1.6f
                    ) * 1.2f
                    : (Random.value - 0.5f) * 2.2f;

                for (int v = 0; v < 4; v++)
                {
                    mesh.colors32[vertexIndex + v] = color;

                    mesh.vertices[vertexIndex + v] +=
                        new Vector3(0f, shake, 0f);
                }

                changed = true;
            }
        }

        if (changed)
        {
            termsText.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices |
                TMP_VertexDataUpdateFlags.Colors32
            );
        }
    }

    private void RevealStartButton()
    {
        if (startRevealed)
        {
            return;
        }

        startRevealed = true;

        // 스크롤 정지
        termsScrollRect.StopMovement();
        termsScrollRect.enabled = false;

        // 약관 속 START 문구 변경
        termsText.text = termsText.text.Replace(
            HiddenMarkup,
            "[데이터 추출 완료]"
        );

        if (hintText != null)
        {
            hintText.text =
                "START 데이터를 발견했습니다. 버튼을 누르세요.";
        }

        if (statusText != null)
        {
            statusText.text = "START DATA : FOUND";
        }

        // 숨겨 놓은 최종 START 버튼 표시
        revealedStartButton.gameObject.SetActive(true);

        revealedStartButton.interactable = true;

        revealedStartButton.transform.SetAsLastSibling();
    }

    private void OnRevealedStartClicked()
    {
        if (!isGameActive || !startRevealed)
        {
            return;
        }

        revealedStartButton.interactable = false;

        // ChapterManager의 성공 처리로 연결
        Success();
    }

    protected override void GiveHint()
    {
        // MG21은 40초, 60초에 직접 힌트 표시
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }

    private void OnDestroy()
    {
        if (revealedStartButton != null)
        {
            revealedStartButton.onClick.RemoveListener(
                OnRevealedStartClicked
            );
        }
    }
}
