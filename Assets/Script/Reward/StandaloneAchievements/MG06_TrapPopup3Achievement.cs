
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 업적 22 - 창 좀 그만 열어!
// 조건: MG06에서 팝업 생성 함정 3회 작동
// 기존 Minigame6_PopupStart.cs 수정 불필요

[DisallowMultipleComponent]
public sealed class MG06_TrapPopup3Achievement
    : AchievementStandaloneWatcher
{
    protected override int AchievementId => 22;

    private int trapCount;

    private readonly HashSet<Button> registeredButtons =
        new HashSet<Button>();

    protected override void ResetWatch()
    {
        trapCount = 0;

        Debug.Log(
            "[MG06 Achievement] 함정 업적 감지 시작"
        );
    }

    protected override void Poll()
    {
        if (game == null)
            return;

        DraggablePopup[] popups =
            game.GetComponentsInChildren<DraggablePopup>(true);

        foreach (DraggablePopup popup in popups)
        {
            if (popup == null)
                continue;

            // 함정 유형 1:
            // 닫기 버튼을 누르면 팝업 2개 생성
            if (popup.closable &&
                popup.spawnTwoOnClose &&
                popup.nuisancePopupPrefab != null)
            {
                RegisterTrapButton(popup.closeButton);
            }

            // 함정 유형 2:
            // DuplicatePopupTwice()가 연결된 버튼
            Button[] buttons =
                popup.GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                for (int i = 0;
                     i < button.onClick.GetPersistentEventCount();
                     i++)
                {
                    if (button.onClick.GetPersistentMethodName(i)
                        == "DuplicatePopupTwice")
                    {
                        RegisterTrapButton(button);
                        break;
                    }
                }
            }
        }
    }

    private void RegisterTrapButton(Button button)
    {
        if (button == null)
            return;

        // 같은 버튼에 여러 번 연결하지 않음
        if (!registeredButtons.Add(button))
            return;

        button.onClick.AddListener(
            () => OnTrapTriggered(button)
        );
    }

    private void OnTrapTriggered(Button button)
    {
        if (game == null ||
            !game.IsGameActive ||
            button == null)
        {
            return;
        }

        trapCount++;

        Debug.Log(
            $"[MG06 Achievement] 팝업 함정 " +
            $"{trapCount}/3회 작동"
        );

        if (trapCount >= 3)
        {
            Grant();

            Debug.Log(
                "[MG06 Achievement] 업적 22번 달성!"
            );
        }
    }
}
