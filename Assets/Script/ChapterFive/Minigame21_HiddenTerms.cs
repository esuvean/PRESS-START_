
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

    [Header("Hint Timing (초)")]
    [Tooltip("1단계: 끝까지 내려도 없다는 힌트")]
    public float firstHintTime = 15f;
    [Tooltip("2단계: 약관 중간을 보라는 힌트 + 숨은 단어가 가끔 깜빡임")]
    public float secondHintTime = 30f;
    [Tooltip("3단계: 몇 조인지 알려 줌 + 더 자주 깜빡임")]
    public float thirdHintTime = 40f;
    [Tooltip("마지막: 숨은 단어가 노랗게 계속 빛남")]
    public float finalHintTime = 50f;

    [Header("Hint Texts")]
    public string firstHint = "힌트: 끝까지 내린다고 정답이 나오지는 않습니다.";
    public string secondHint = "힌트: 약관 중간의 조항을 천천히 다시 읽어 보세요.";
    public string thirdHint = "힌트: 제12조 (실행 조건) 안에서 깜빡이는 단어를 찾으세요.";
    public string finalHint = "힌트: 노랗게 빛나는 단어를 클릭하세요!";

    private const string HiddenLinkId = "hidden_start";

    private const string HiddenMarkup =
        "<link=\"hidden_start\">START</link>";

    private float elapsedTime;

    // 약관 클릭 판정 (스크롤하려고 끈 건 제외, 제자리 클릭만)
    private bool pressInsideTerms;
    private Vector2 pressPosition;

    private bool firstHintShown;
    private bool secondHintShown;
    private bool thirdHintShown;
    private bool finalHintShown;
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

        // 제한 시간은 프리팹 Inspector의 Time Limit 값 사용 (챕터5 도구가 60초로 맞춤)

        base.StartMinigame();

        StopAllCoroutines();

        elapsedTime = 0f;
        firstHintShown = false;
        secondHintShown = false;
        thirdHintShown = false;
        finalHintShown = false;
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
            statusText.text = "숨은 데이터 검색 중...";
        }

        UpdateTimeText();

        StartCoroutine(ScrollToTopNextFrame());
    }

    private IEnumerator ScrollToTopNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        // 약관이 길어도 끝까지 스크롤되도록 글 길이에 맞춰 높이 조절
        FitTermsHeight();

        if (termsScrollRect != null && isGameActive)
        {
            termsScrollRect.StopMovement();

            termsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void FitTermsHeight()
    {
        if (termsText == null || termsScrollRect == null || termsScrollRect.content == null)
            return;

        termsText.ForceMeshUpdate();
        float textHeight = termsText.preferredHeight + 40f;

        RectTransform textRect = termsText.rectTransform;
        textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textHeight);

        RectTransform content = termsScrollRect.content;
        float top = Mathf.Abs(textRect.anchoredPosition.y);
        content.sizeDelta = new Vector2(content.sizeDelta.x, textHeight + top + 40f);

        Canvas.ForceUpdateCanvases();
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

        // 1단계 힌트
        if (!firstHintShown &&
            elapsedTime >= firstHintTime)
        {
            firstHintShown = true;

            if (hintText != null)
            {
                hintText.text = firstHint;
            }
        }

        // 2단계 힌트
        if (!secondHintShown &&
            elapsedTime >= secondHintTime)
        {
            secondHintShown = true;

            if (hintText != null)
            {
                hintText.text = secondHint;
            }

            if (statusText != null)
            {
                statusText.text =
                    "숨은 데이터 신호 감지";
            }
        }

        // 3단계 힌트
        if (!thirdHintShown &&
            elapsedTime >= thirdHintTime)
        {
            thirdHintShown = true;

            if (hintText != null)
            {
                hintText.text = thirdHint;
            }

            if (statusText != null)
            {
                statusText.text =
                    "숨은 데이터 신호 강함 (제12조)";
            }
        }

        // 마지막 힌트
        if (!finalHintShown &&
            elapsedTime >= finalHintTime)
        {
            finalHintShown = true;

            if (hintText != null)
            {
                hintText.text = finalHint;
            }
        }

        // 약관 속 START 위치 확인
        int hoveredLinkIndex = FindHoveredHiddenLink();

        bool hovering = hoveredLinkIndex >= 0;

        // 2단계부터 START 부분이 주기적으로 흔들림 (3단계부터는 더 자주)
        bool noiseFlash =
            secondHintShown &&
            Mathf.Repeat(
                elapsedTime - secondHintTime,
                thirdHintShown ? 1.4f : 3.5f
            ) < (thirdHintShown ? 0.35f : 0.18f);

        AnimateHiddenStart(hovering, noiseFlash, finalHintShown);

        // 숨겨진 START 클릭
        if (hovering && Input.GetMouseButtonDown(0))
        {
            RevealStartButton();
            return;
        }

        // 약관의 다른 곳을 '클릭'하면 틀린 것 (챕터5 틀린 횟수 +1)
        if (Input.GetMouseButtonDown(0))
        {
            pressInsideTerms = IsMouseInsideTerms();
            pressPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && pressInsideTerms)
        {
            pressInsideTerms = false;
            bool wasClick = ((Vector2)Input.mousePosition - pressPosition).sqrMagnitude < 12f * 12f;
            if (wasClick && FindHoveredHiddenLink() < 0)
            {
                ReportMistake();
                if (statusText != null)
                    statusText.text = "잘못된 데이터입니다.";
            }
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

    private bool IsMouseInsideTerms()
    {
        if (termsScrollRect == null)
            return false;

        RectTransform viewport =
            termsScrollRect.viewport != null
                ? termsScrollRect.viewport
                : termsScrollRect.GetComponent<RectTransform>();

        Camera eventCamera =
            uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? uiCanvas.worldCamera
                : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            viewport, Input.mousePosition, eventCamera);
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
        bool noiseFlash,
        bool glow)
    {
        if (termsText == null)
        {
            return;
        }

        // 원래 글자 메시를 매번 다시 구성
        termsText.ForceMeshUpdate();

        if (!hovering && !noiseFlash && !glow)
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

                // 마우스를 올리면 민트색 / 마지막 힌트에서는 노란색으로 계속 빛남
                Color32 color = hovering
                    ? new Color32(145, 255, 204, 255)
                    : glow
                        ? new Color32(255, 232, 26, 255)
                        : new Color32(220, 240, 255, 255);

                // 글자가 살짝 흔들리는 효과
                float shake = hovering
                    ? Mathf.Sin(
                        Time.unscaledTime * 19f +
                        i * 1.6f
                    ) * 1.2f
                    : glow
                        ? Mathf.Sin(Time.unscaledTime * 6f) * 1.5f
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
                "숨은 데이터를 찾았습니다! 나타난 버튼을 누르세요.";
        }

        if (statusText != null)
        {
            statusText.text = "숨은 데이터 발견";
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
        // MG21은 15초/30초/40초/50초에 직접 힌트 표시
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
