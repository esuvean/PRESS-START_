using TMPro;
using UnityEngine;

// 챕터5 미니게임 오른쪽 위 "TIME 00:00" 표시
// 미니게임이 켜진 뒤 흐른 시간을 보여 준다. (시간제한과는 상관없음, 보여주기용)
// Chapter5SkinTool 이 MG23~MG25 의 C5_Time 글자에 붙여 준다.
public class Chapter5ElapsedTime : MonoBehaviour
{
    private TMP_Text label;
    private float elapsed;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        Refresh();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        Refresh();
    }

    private void Refresh()
    {
        if (label == null)
            return;

        int seconds = Mathf.FloorToInt(elapsed);
        label.text = string.Format("TIME {0:00}:{1:00}", seconds / 60, seconds % 60);
    }
}
