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

    // 씬에 미리 배치하지 않고 코드로 만들 때 연결 (Chapter4Glitch에서 사용)
    public void Setup(Canvas canvas, Camera camera)
    {
        gameCanvas = canvas;
        mainCamera = camera;
    }

    private void Start()
    {
        crtFilter = Resources
            .FindObjectsOfTypeAll<CRTRendererFeature>()
            .FirstOrDefault(x => x.name == "CRTRendererFeature");

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

        // 설정창으로 일시정지해도 노이즈가 멈춘 채 계속되지 않게 실제 시간으로 기다림
        yield return new WaitForSecondsRealtime(0.25f);

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
    // 강한 노이즈 (게임이 튕기기 직전 연출)
    // 캔버스를 카메라 모드로 바꾼 채로 끝냄 (바로 씬이 바뀌므로 되돌리지 않음)
    // =========================================
    public IEnumerator PlayHeavyNoise(float duration)
    {
        if (crtFilter == null ||
            gameCanvas == null ||
            mainCamera == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        gameCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        gameCanvas.worldCamera = mainCamera;
        gameCanvas.planeDistance = 10f;

        yield return null;

        GameSfx.StartNoise(0.6f);

        crtFilter.SetActive(true);
        crtFilter.noiseSize = 40f;
        crtFilter.noiseSpeed = 20f;
        crtFilter.glitchFrequency = 1f;
        crtFilter.glitchPosition = 0.5f;
        crtFilter.glitchMovementSpeed = 2f;
        crtFilter.glitchNoise = 2f;
        crtFilter.glitchNoiseSpeed = 8f;

        // 점점 심해지게
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            crtFilter.noiseAlpha = Mathf.Lerp(0.3f, 0.9f, t);
            crtFilter.glitchBands = Mathf.RoundToInt(Mathf.Lerp(2f, 8f, t));
            crtFilter.glitchStrength = Mathf.Lerp(0.2f, 0.8f, t);
            crtFilter.glitchPositionFlicker = Mathf.Lerp(0.15f, 0.6f, t);

            yield return null;
        }
    }

    public void StopAllNoise()
    {
        TurnNoiseOff();
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
        // 짧은 지지직 소리 (작게)
        GameSfx.StartNoise(0.2f);

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
        GameSfx.StopNoise();

        if (crtFilter == null)
            return;

        crtFilter.SetActive(false);
    }
}