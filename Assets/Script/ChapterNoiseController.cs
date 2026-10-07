using System.Collections;
using System.Linq;
using UnityEngine;
using CRTFilter;

public class ChapterNoiseController : MonoBehaviour
{
    [Header("실제 미니게임들이 들어가는 Canvas")]
    [SerializeField] private Canvas gameCanvas;

    [Header("메인 카메라")]
    [SerializeField] private Camera mainCamera;

    private CRTRendererFeature crtFilter;
    private Coroutine randomNoiseCoroutine;

    private void Start()
    {
        crtFilter = Resources
            .FindObjectsOfTypeAll<CRTRendererFeature>()
            .FirstOrDefault();

        if (crtFilter == null)
        {
            Debug.LogError("[NOISE] CRT Filter를 찾지 못했습니다.");
            return;
        }

        TurnNoiseOff();
    }

    // =========================================
    // Chapter2 종료 직전 노이즈
    // =========================================

    public IEnumerator PlayLightNoiseAndWait()
    {
        if (crtFilter == null ||
            gameCanvas == null ||
            mainCamera == null)
        {
            Debug.LogError("[NOISE] 연결되지 않은 오브젝트가 있습니다.");
            yield break;
        }

        // 평소 Canvas 상태 저장
        RenderMode oldRenderMode =
            gameCanvas.renderMode;

        Camera oldCamera =
            gameCanvas.worldCamera;

        float oldPlaneDistance =
            gameCanvas.planeDistance;

        // -------------------------
        // 노이즈 순간만 Camera 모드
        // -------------------------

        gameCanvas.renderMode =
            RenderMode.ScreenSpaceCamera;

        gameCanvas.worldCamera =
            mainCamera;

        gameCanvas.planeDistance = 10f;

        // 한 프레임 기다려서 Canvas 렌더링 반영
        yield return null;

        TurnLightNoiseOn();

        yield return new WaitForSeconds(0.4f);

        TurnNoiseOff();

        // -------------------------
        // 다시 원래 Overlay로 복구
        // -------------------------

        gameCanvas.renderMode =
            oldRenderMode;

        gameCanvas.worldCamera =
            oldCamera;

        gameCanvas.planeDistance =
            oldPlaneDistance;
    }

    // =========================================
    // Chapter3 랜덤 노이즈
    // =========================================

    public void StartRandomLightNoise()
    {
        if (randomNoiseCoroutine != null)
            StopCoroutine(randomNoiseCoroutine);

        randomNoiseCoroutine =
            StartCoroutine(RandomLightNoiseRoutine());
    }

    private IEnumerator RandomLightNoiseRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(8f, 15f)
            );

            yield return StartCoroutine(
                PlayLightNoiseAndWait()
            );
        }
    }

    public void StopRandomNoise()
    {
        if (randomNoiseCoroutine != null)
        {
            StopCoroutine(randomNoiseCoroutine);
            randomNoiseCoroutine = null;
        }

        TurnNoiseOff();
    }

    private void TurnLightNoiseOn()
    {
        crtFilter.SetActive(true);

        crtFilter.noiseSize = 25f;
        crtFilter.noiseSpeed = 10f;
        crtFilter.noiseAlpha = 0.3f;

        crtFilter.glitchFrequency = 1f;
        crtFilter.glitchBands = 2;
        crtFilter.glitchPosition = 0.5f;
        crtFilter.glitchPositionFlicker = 0.15f;
        crtFilter.glitchMovementSpeed = 0f;
        crtFilter.glitchStrength = 0.15f;
        crtFilter.glitchNoise = 1.15f;
        crtFilter.glitchNoiseSpeed = 4f;
    }

    private void TurnNoiseOff()
    {
        if (crtFilter == null)
            return;

        crtFilter.SetActive(false);
    }
}