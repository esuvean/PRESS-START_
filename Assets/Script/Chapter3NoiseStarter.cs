using System.Collections;
using UnityEngine;

public class Chapter3NoiseStarter : MonoBehaviour
{
    private ChapterNoiseController noiseController;

    private IEnumerator Start()
    {
        noiseController =
            FindFirstObjectByType<ChapterNoiseController>();

        if (noiseController == null)
        {
            Debug.LogWarning(
                "[Chapter3] ChapterNoiseController를 찾지 못했습니다."
            );

            yield break;
        }

        // 첫 노이즈는 바로 나오지 않게
        yield return new WaitForSeconds(
            Random.Range(30f, 45f)
        );

        while (true)
        {
            yield return StartCoroutine(
                noiseController.PlayLightNoiseAndWait()
            );

            // 다음 노이즈까지 25~45초 대기
            yield return new WaitForSeconds(
                Random.Range(25f, 45f)
            );
        }
    }
}