using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using CRTFilter;

public class StartMenuController : MonoBehaviour
{
    [Header("기존 패널")]
    public GameObject titlePanel;
    public GameObject chapterSelectPanel;
    public GameObject settingsPanel;

    [Header("인트로 연출 패널")]
    public GameObject noiseBackgroundPanel;
    public GameObject systemCheckPanel;
    public GameObject rebootPanel;

    [Header("인트로 텍스트")]
    public TMP_Text systemCheckTitle;
    public TMP_Text systemCheckText;
    public TMP_Text rebootText;

    [Header("타이핑 속도")]
    public float titleTypingSpeed = 0.08f;
    public float typingSpeed = 0.05f;

    [Header("CRT 노이즈 시간")]
    public float firstNoiseDuration = 3f;
    public float secondNoiseBeforeText = 0.5f;
    public float finalNoiseDuration = 1.5f;

    public ChapterProgressUI chapterProgressUI;

    [Header("챕터 씬 이름")]
    public string chapter1SceneName = "Chapter1";
    public string chapter2SceneName = "Chapter2";
    public string chapter3SceneName = "Chapter3";
    public string chapter4SceneName = "Chapter4";

    private bool introFinished = false;
    private bool sequencePlaying = false;

    // CRT Filter
    private CRTRendererFeature crtFilter;


    private void Start()
    {
        // CRT Renderer Feature 찾기
        crtFilter = Resources
            .FindObjectsOfTypeAll<CRTRendererFeature>()
            .FirstOrDefault(x => x.name == "CRT Filter");

        if (crtFilter == null)
        {
            Debug.LogWarning("CRT Filter를 찾지 못했습니다.");
        }
        else
        {
            TurnNoiseOff();
        }


        // 처음에는 연출용 패널 전부 OFF
        if (noiseBackgroundPanel != null)
            noiseBackgroundPanel.SetActive(false);

        if (systemCheckPanel != null)
            systemCheckPanel.SetActive(false);

        if (rebootPanel != null)
            rebootPanel.SetActive(false);


        // 챕터를 끝내고 돌아온 경우
        if (SessionProgress.openChapterSelectOnLoad)
        {
            ShowChapterSelect();

            SessionProgress.openChapterSelectOnLoad = false;
        }
        else
        {
            ShowTitle();
        }
    }


    private void ShowTitle()
    {
        if (titlePanel != null)
            titlePanel.SetActive(true);

        if (chapterSelectPanel != null)
            chapterSelectPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }


    private void ShowChapterSelect()
    {
        if (titlePanel != null)
            titlePanel.SetActive(false);

        if (chapterSelectPanel != null)
            chapterSelectPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (chapterProgressUI != null)
            chapterProgressUI.RefreshProgress();
    }


    // ==============================
    // START 버튼
    // ==============================

    public void OnClickStart()
    {
        if (sequencePlaying)
            return;

        // 첫 START
        if (!introFinished)
        {
            StartCoroutine(PlayIntroSequence());
        }
        // 두 번째 START
        else
        {
            ShowChapterSelect();
        }
    }


    // ==============================
    // 인트로 연출
    // ==============================

    private IEnumerator PlayIntroSequence()
    {
        sequencePlaying = true;

        // 시작 화면 숨기기
        if (titlePanel != null)
            titlePanel.SetActive(false);


        // ==================================
        // 1. 첫 번째 노이즈 3초
        // ==================================

        if (noiseBackgroundPanel != null)
            noiseBackgroundPanel.SetActive(true);

        TurnNoiseOn();

        yield return new WaitForSeconds(2f);

        // 노이즈 완전히 종료
        TurnNoiseOff();

        if (noiseBackgroundPanel != null)
            noiseBackgroundPanel.SetActive(false);

        yield return new WaitForSeconds(0.2f);


        // ==================================
        // 2. SYSTEM CHECK
        // 노이즈 없음
        // ==================================

        if (systemCheckPanel != null)
            systemCheckPanel.SetActive(true);

        if (systemCheckTitle != null)
            systemCheckTitle.text = "";

        if (systemCheckText != null)
            systemCheckText.text = "";


        // SYSTEM CHECK 타이핑
        if (systemCheckTitle != null)
        {
            yield return StartCoroutine(
                TypeText(
                    systemCheckTitle,
                    "SYSTEM CHECK",
                    titleTypingSpeed
                )
            );
        }

        yield return new WaitForSeconds(0.5f);


        // 첫 번째 문장
        if (systemCheckText != null)
        {
            yield return StartCoroutine(
                AppendText(
                    systemCheckText,
                    "게임을 시작할 수 없습니다.",
                    typingSpeed
                )
            );
        }

        yield return new WaitForSeconds(0.7f);


        // 두 번째 문장
        if (systemCheckText != null)
        {
            yield return StartCoroutine(
                AppendText(
                    systemCheckText,
                    "\n\nSTART 명령이 정상적으로 인식되지 않습니다.",
                    typingSpeed
                )
            );
        }

        // 읽을 시간
        yield return new WaitForSeconds(2f);


        // SYSTEM CHECK 종료
        if (systemCheckPanel != null)
            systemCheckPanel.SetActive(false);


        // ==================================
        // 3. 두 번째 노이즈 1.5초
        // ==================================

        if (noiseBackgroundPanel != null)
            noiseBackgroundPanel.SetActive(true);

        TurnNoiseOn();

        yield return new WaitForSeconds(1.5f);

        // 노이즈 완전히 끄기
        TurnNoiseOff();

        if (noiseBackgroundPanel != null)
            noiseBackgroundPanel.SetActive(false);

        yield return new WaitForSeconds(0.2f);


        // ==================================
        // 4. 정상화 메시지
        // 노이즈 없음
        // ==================================

        if (rebootPanel != null)
            rebootPanel.SetActive(true);

        if (rebootText != null)
            rebootText.text = "";


        if (rebootText != null)
        {
            yield return StartCoroutine(
                AppendText(
                    rebootText,
                    "게임이 정상화되었습니다.",
                    0.06f
                )
            );
        }

        yield return new WaitForSeconds(0.5f);


        if (rebootText != null)
        {
            yield return StartCoroutine(
                AppendText(
                    rebootText,
                    "\n다시 시작합니다.",
                    0.06f
                )
            );
        }


        // 메시지 읽을 시간
        yield return new WaitForSeconds(1.5f);


        // 정상화 메시지 종료
        if (rebootPanel != null)
            rebootPanel.SetActive(false);


        // ==================================
        // 5. PRESS START 화면으로 복귀
        // ==================================

        ShowTitle();

        introFinished = true;
        sequencePlaying = false;
    }

    // ==============================
    // CRT 노이즈 ON
    // ==============================

    private void TurnNoiseOn()
    {
        if (crtFilter == null)
            return;

        // CRT 필터 자체 ON
        crtFilter.SetActive(true);

        // 노이즈
        crtFilter.noiseSize = 25f;
        crtFilter.noiseSpeed = 10f;
        crtFilter.noiseAlpha = 1f;

        // VHS / Glitch
        crtFilter.glitchFrequency = 1f;
        crtFilter.glitchBands = 6;
        crtFilter.glitchPosition = 0f;
        crtFilter.glitchPositionFlicker = 0.45f;
        crtFilter.glitchMovementSpeed = 0f;
        crtFilter.glitchStrength = 0.7f;
        crtFilter.glitchNoise = 1.45f;
        crtFilter.glitchNoiseSpeed = 6f;
    }


    // ==============================
    // CRT 노이즈 OFF
    // ==============================

    private void TurnNoiseOff()
    {
        if (crtFilter == null)
            return;

        // CRT 필터 자체를 완전히 OFF
        crtFilter.SetActive(false);
    }


    // ==============================
    // 타이핑 함수
    // ==============================

    private IEnumerator TypeText(
        TMP_Text target,
        string message,
        float speed
    )
    {
        target.text = "";

        foreach (char letter in message)
        {
            target.text += letter;

            yield return new WaitForSeconds(speed);
        }
    }


    private IEnumerator AppendText(
        TMP_Text target,
        string message,
        float speed
    )
    {
        foreach (char letter in message)
        {
            target.text += letter;

            yield return new WaitForSeconds(speed);
        }
    }


    // ==============================
    // 기존 챕터 버튼
    // ==============================

    public void OnClickChapter1()
    {
        Debug.Log("Chapter1 Scene으로 이동");

        SceneManager.LoadScene(chapter1SceneName);
    }


    public void OnClickChapter2()
    {
        Debug.Log("Chapter2 Scene으로 이동");

        SceneManager.LoadScene(chapter2SceneName);
    }


    public void OnClickChapter3()
    {
        Debug.Log("Chapter3 Scene으로 이동");

        SceneManager.LoadScene(chapter3SceneName);
    }


    public void OnClickChapter4()
    {
        Debug.Log("Chapter4 Scene으로 이동");

        SceneManager.LoadScene(chapter4SceneName);
    }


    // ==============================
    // 설정
    // ==============================

    public void OnClickSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }


    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }


    // ==============================
    // 종료
    // ==============================

    public void OnClickQuit()
    {
        Application.Quit();
    }
}