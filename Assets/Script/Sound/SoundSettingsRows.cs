using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 화면 SOUND 탭에 "BGM"(배경음악) / "SFX"(효과음) 조절 바를 자동으로 추가.
///
/// 기존 Volume 줄(글자 + 바 + 퍼센트)을 그대로 복사해서 만들기 때문에
/// 모양·글꼴·색이 원래 Volume 바와 똑같이 나온다. (씬/프리팹을 따로 고칠 필요 없음)
///
/// 배치 (위에서부터)
///   Volume - 전체 소리 (원래 있던 바)
///   BGM    - 배경음악
///   SFX    - 효과음 (클릭, 버튼, 타이핑, 노이즈, START 성공음 등)
///
/// SettingsController.Start()에서 Build()를 한 번 불러 준다.
/// </summary>
public static class SoundSettingsRows
{
    // 줄 사이 간격 (위아래 거리). 줄이 겹치거나 너무 멀면 이 숫자만 바꾸면 됨
    private const float RowGap = 80f;

    // 미니게임 10 "설정 속 숨은 START" 자리 — 바와 겹치지 않게 SETTINGS 글자 위 가운데(별 아래 검은 공간)로 옮김
    private static readonly Vector2 StartSlotPosition = new Vector2(0f, 320f);

    // 설정 속 숨은 START 버튼 크기 (1 = 원래 크기)
    // 가로·세로를 따로 키워서 눌려 보이지 않게 (원래 미니게임 화면의 START 비율과 비슷하게)
    private const float StartButtonScaleX = 1.3f;
    private const float StartButtonScaleY = 1.65f;

    public static void Build(Slider volumeSlider, TMP_Text volumeValueText)
    {
        if (volumeSlider == null) return;

        RectTransform parent = volumeSlider.transform.parent as RectTransform;
        if (parent == null) return;

        // 이미 만들어져 있으면 다시 만들지 않음
        if (parent.Find("BgmSlider") != null) return;

        RectTransform sliderRT = (RectTransform)volumeSlider.transform;
        TMP_Text label = FindLabel(parent, sliderRT, volumeValueText);

        // 원래 Volume 줄은 한 칸 위로
        MoveRow(sliderRT, label, volumeValueText, RowGap);

        // 숨은 START 자리(StartSlot_Sound)가 있으면 옆으로 치움
        foreach (Transform child in parent)
        {
            if (child.name.Contains("StartSlot") && child is RectTransform slot)
                slot.anchoredPosition = StartSlotPosition;
        }

        // 설정 화면 모든 탭의 숨은 START 자리를 조금 크게 (DISPLAY / SOUND / INFO / EXIT)
        Transform pages = parent.parent;
        if (pages != null)
        {
            foreach (RectTransform slot in pages.GetComponentsInChildren<RectTransform>(true))
            {
                if (slot.name.Contains("StartSlot"))
                    slot.localScale = new Vector3(StartButtonScaleX, StartButtonScaleY, 1f);
            }
        }

        // BGM 줄 (원래 Volume 자리) / SFX 줄 (한 칸 아래)
        CreateRow(parent, volumeSlider, label, volumeValueText, "Bgm", "BGM", 0f,
            GameBgm.BgmVolumeKey, v => GameBgm.SetMusicVolume(v));

        CreateRow(parent, volumeSlider, label, volumeValueText, "Sfx", "SFX", -RowGap,
            GameSfx.SfxVolumeKey, v => GameSfx.SetSfxVolume(v));
    }

    // ------------------------------

    private static void CreateRow(RectTransform parent, Slider srcSlider, TMP_Text srcLabel, TMP_Text srcValue,
                                  string idName, string title, float offsetFromOriginal,
                                  string prefsKey, System.Action<float> apply)
    {
        // 1) 바 복사
        Slider slider = Object.Instantiate(srcSlider, parent);
        slider.name = idName + "Slider";
        // 복사하면 원래 바의 "전체 볼륨 바꾸기" 연결까지 따라오므로 지워 줌
        slider.onValueChanged = new Slider.SliderEvent();
        Shift(slider.transform, offsetFromOriginal - RowGap); // 원래 줄은 이미 위로 옮겨졌으므로 보정

        // 2) 글자(BGM / SFX) 복사
        if (srcLabel != null)
        {
            TMP_Text label = Object.Instantiate(srcLabel, parent);
            label.name = idName + "Text";
            label.text = title;
            Shift(label.transform, offsetFromOriginal - RowGap);
        }

        // 3) 퍼센트 글자 복사
        TMP_Text valueText = null;
        if (srcValue != null)
        {
            valueText = Object.Instantiate(srcValue, parent);
            valueText.name = idName + "ValueText";
            Shift(valueText.transform, offsetFromOriginal - RowGap);
        }

        // 저장된 값 불러오기 (0 ~ 100, 처음엔 100)
        float saved = Mathf.Clamp(PlayerPrefs.GetFloat(prefsKey, 100f), 0f, 100f);
        float max = slider.maxValue > 1f ? slider.maxValue : 100f;
        slider.SetValueWithoutNotify(saved / 100f * max);
        SetPercent(valueText, saved);

        slider.onValueChanged.AddListener(value =>
        {
            float percent = Mathf.Clamp(value / max * 100f, 0f, 100f);
            apply(percent / 100f);
            SetPercent(valueText, percent);
            PlayerPrefs.SetFloat(prefsKey, percent);
            PlayerPrefs.Save();
        });
    }

    private static void SetPercent(TMP_Text t, float percent)
    {
        if (t != null) t.text = Mathf.RoundToInt(percent) + "%";
    }

    private static void MoveRow(RectTransform slider, TMP_Text label, TMP_Text value, float dy)
    {
        Shift(slider, dy);
        if (label != null) Shift(label.transform, dy);
        if (value != null) Shift(value.transform, dy);
    }

    private static void Shift(Transform t, float dy)
    {
        RectTransform rt = t as RectTransform;
        if (rt != null) rt.anchoredPosition += new Vector2(0f, dy);
    }

    // 바 왼쪽에 같은 높이로 있는 글자("Volume")를 찾음
    private static TMP_Text FindLabel(RectTransform parent, RectTransform slider, TMP_Text valueText)
    {
        TMP_Text best = null;
        float bestDist = float.MaxValue;
        foreach (Transform child in parent)
        {
            TMP_Text t = child.GetComponent<TMP_Text>();
            if (t == null || t == valueText) continue;
            RectTransform rt = (RectTransform)child;
            if (rt.anchoredPosition.x >= slider.anchoredPosition.x) continue;
            float dy = Mathf.Abs(rt.anchoredPosition.y - slider.anchoredPosition.y);
            if (dy < 40f && dy < bestDist)
            {
                best = t;
                bestDist = dy;
            }
        }
        return best;
    }
}
