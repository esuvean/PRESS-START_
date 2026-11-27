using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MG28 기울어진 START (문자 중력 이동 검사) - MG08 기울어진 창 방식
///  - 게임판을 마우스로 끌면(또는 방향키/WASD) 판이 기울고, 문자가 기운 쪽으로 미끄러짐
///  - X 장애물은 벽 (막힘). 판 밖으로 떨어지면 이번 판만 처음부터
///  - S → T → A → R → T 총 5판, 판마다 길이 어려워짐 (4판: 흔들림 / 5판: 노이즈, 길은 그대로)
///  - 5판을 모두 넣으면 S·T·A·R·T → START 결합 → START 클릭
/// </summary>
public class Minigame28_TiltedStart : MinigameBase
{
    [Header("챕터5 그림 (에디터 도구가 자동으로 넣음)")]
    public TMP_FontAsset uiFont;
    public Sprite panelSprite;          // 23_line_back (게임판)
    public Sprite plateSprite;          // 26_upline
    public Sprite startButtonSprite;    // normal_button
    public Sprite slotFrameSprite;      // 26_miniline
    public Sprite slotEmptySprite;      // 26_blueblank
    public Sprite slotArrowLeft;
    public Sprite slotArrowRight;
    public Sprite[] slotLetterSprites = new Sprite[5];  // 26_pink_S/T/A/R/T
    public Sprite[] letterSprites = new Sprite[5];      // MG26과 같은 알파벳 블록 (alpabet/block_S/T/A/R/T)
    public Sprite obstacleSprite;       // 08_Xwall (MG08 장애물)
    public Sprite goalSprite;           // 08_goal (MG08 골)

    [Header("규칙")]
    [Tooltip("떨어지거나 장애물에 닿으면 챕터5 오류 횟수에 포함 (기획: 이번 판만 다시 시작)")]
    public bool countMistakes = true;

    [Header("조작 (MG08과 같은 값)")]
    public float dragRange = 250f;          // MG08 프리팹과 같은 값
    public float deadZone = 0.1f;
    public float moveSpeed = 300f;          // MG08(350)보다 살짝 느리게 - 판이 더 좁아서
    public float movementSmooth = 6f;       // MG08과 같음
    public float brakeSmooth = 6f;          // MG08과 같음 (멈출 때도 같은 느낌)
    public float tiltSmoothTime = 0.18f;
    public float maxVisualAngle = 7f;     // BoardTiltVisual(MG08)과 같음

    private const string Word = "START";

    // ---- 배치 ----
    private static readonly Vector2 PanelPos = new Vector2(0f, 30f);
    private static readonly Vector2 PanelSize = new Vector2(1480f, 580f);
    private static readonly Vector2 PlatePos = new Vector2(0f, 300f);
    private static readonly Vector2 BoardPos = new Vector2(0f, 18f);
    private const int Cols = 13;
    private const int Rows = 5;
    private const float CellW = 100f;
    private const float CellH = 92f;
    private static readonly Vector2 BoardSize = new Vector2(Cols * CellW, Rows * CellH);
    private static readonly Vector2 SlotRowPos = new Vector2(0f, -338f);
    private static readonly Vector2 LetterSize = new Vector2(58f, 56f);
    private const float ObstacleSize = 66f;

    // S 시작, G 골, # 장애물 (모든 판 길 있음 확인함)
    private static readonly string[][] Maps =
    {
        new[]
        {
            ".............",
            "......#......",
            ".S....#....G.",
            "......#......",
            ".............",
        },
        new[]
        {
            "....#........",
            "....#...#....",
            ".S......#..G.",
            "....#...#....",
            "....#........",
        },
        new[]
        {
            "...#.........",
            "...#...#.....",
            ".S.#...#...G.",
            ".......#.....",
            ".....#.......",
        },
        new[]
        {
            "...#.....#...",
            "...#..#..#...",
            ".S....#....G.",
            "...#..#..#...",
            "...#.....#...",
        },
        new[]
        {
            "..#...#......",
            "..#...#..#...",
            ".S..#....#.G.",
            "....#.#..#...",
            "..#...#......",
        },
    };

    private static readonly string[] StageNames =
    {
        "기본 장애물", "두 갈래 길", "늘어난 장애물", "지그재그 + 흔들림", "최종 판 + 노이즈"
    };

    private enum Phase
    {
        Playing,
        Falling,
        RoundClear,
        StartReady,
        Completed
    }

    private Phase phase;
    private int stage;
    private bool finished;

    private bool dragging;
    private Vector2 dragStart;
    private Vector2 targetTilt;
    private Vector2 currentTilt;
    private Vector2 tiltVelocity;
    private Vector2 velocity;
    private Vector2 visualTilt;
    private Vector2 startPos;

    private readonly List<Rect> obstacles = new List<Rect>();
    private Rect goalRect;
    private float fxTimer;

    // ---- 만든 UI ----
    private TMP_FontAsset font;
    private RectTransform screen;
    private RectTransform dragZone;      // 누르고 끌 수 있는 영역
    private RectTransform boardHolder;   // 기울어지는 쪽 (보이는 것만)
    private RectTransform board;         // 판정 기준
    private RectTransform mapLayer;
    private RectTransform letter;
    private Image letterImage;
    private TextMeshProUGUI plateText;
    private TextMeshProUGUI bottomText;
    private CanvasGroup banner;
    private TextMeshProUGUI bannerText;
    private C5SlotRow slotRow;
    private Button finalStartButton;

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "문자 중력 이동 검사";
        instruction = "판을 기울여 문자를 GOAL까지 이동시키세요.";

        base.StartMinigame();
        StopAllCoroutines();

        finished = false;
        stage = 0;

        BuildScreen();
        C5Kit.PrepareNoise(transform);
        slotRow.SetCount(0);
        BeginStage();
    }

    // =========================================
    // 화면 만들기
    // =========================================
    private void BuildScreen()
    {
        font = C5Kit.ResolveFont(transform, uiFont);
        C5Kit.HideLegacy(transform);
        screen = C5Kit.NewScreen(transform, "RT_MG28");
        letter = null;
        letterImage = null;

        // 바깥 네온 테두리 판 (고정) - 이 안을 누르고 끌면 판이 기울어짐
        Image frame = C5Kit.Panel(screen, panelSprite, PanelPos, PanelSize);
        frame.color = new Color(1f, 1f, 1f, 0.55f);
        dragZone = frame.rectTransform;

        // 기울어지는 게임판 (MG08처럼 판 자체가 기울어 보임)
        boardHolder = C5Kit.Rect(screen, "RT_BoardHolder", BoardPos, BoardSize);
        Image boardBg = C5Kit.Img(boardHolder, "RT_Board", null, new Color32(10, 22, 52, 255), Vector2.zero,
            BoardSize + new Vector2(24f, 24f));
        C5Kit.AddOutline(boardBg, C5Kit.Cyan, 4f);
        board = boardBg.rectTransform;


        mapLayer = C5Kit.Rect(board, "RT_Map", Vector2.zero, BoardSize);

        plateText = C5Kit.Plate(screen, plateSprite, font, PlatePos, new Vector2(560f, 80f), "");
        slotRow = new C5SlotRow(screen, SlotRowPos, slotFrameSprite, slotEmptySprite, slotLetterSprites,
            slotArrowLeft, slotArrowRight, font);
        bottomText = C5Kit.BottomText(screen, font,
            "문자를 <color=" + C5Kit.YellowHex + ">떨어뜨리지</color> 말고 GOAL까지 이동하세요.");
        banner = C5Kit.Banner(screen, font, new Vector2(0f, 40f), new Vector2(760f, 160f), out bannerText);

        finalStartButton = C5Kit.StartButton(screen, startButtonSprite, font, new Vector2(0f, 40f),
            new Vector2(360f, 84f), OnFinalStartClicked);
        finalStartButton.gameObject.SetActive(false);
    }

    // =========================================
    // 판 시작
    // =========================================
    private void BeginStage()
    {
        phase = Phase.Playing;
        C5Kit.HideBanner(banner);
        BuildMap(Maps[stage]);
        ResetMotion();
        letter.anchoredPosition = startPos;
        letter.localScale = Vector3.one;
        letter.localRotation = Quaternion.identity;
        letterImage.color = C5Kit.White;
        fxTimer = Random.Range(2.5f, 4f);

        plateText.text = "CURRENT : <color=" + C5Kit.YellowHex + "><size=44>" + Word[stage] + "</size></color>" +
                         "   |   STAGE <color=" + C5Kit.YellowHex + ">" + (stage + 1) + "</color> / 5";
        bottomText.text = "판을 <color=" + C5Kit.YellowHex + ">드래그</color>해 기울이세요 · " + StageNames[stage];
    }

    private void BuildMap(string[] map)
    {
        foreach (Transform c in mapLayer)
            Destroy(c.gameObject);
        obstacles.Clear();

        // 점 무늬 바닥
        for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols; c++)
            C5Kit.Img(mapLayer, "Dot", null, new Color(0.35f, 0.75f, 1f, 0.22f), CellCenter(r, c), new Vector2(4f, 4f));

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                char ch = map[r][c];
                Vector2 p = CellCenter(r, c);
                if (ch == '#')
                {
                    // 장애물 = 빨강
                    Image o = C5Kit.Img(mapLayer, "Obstacle", obstacleSprite,
                        new Color32(255, 60, 80, 255), p, new Vector2(ObstacleSize, ObstacleSize), true);
                    if (obstacleSprite == null)
                        C5Kit.AddOutline(o, C5Kit.White, 3f);
                    obstacles.Add(new Rect(p.x - ObstacleSize * 0.5f, p.y - ObstacleSize * 0.5f, ObstacleSize,
                        ObstacleSize));
                }
                else if (ch == 'G')
                {
                    // 골 = 노랑
                    Image g = C5Kit.Img(mapLayer, "Goal", goalSprite, goalSprite != null ? C5Kit.Yellow : C5Kit.Navy, p,
                        new Vector2(92f, 82f), true);
                    if (goalSprite == null)
                    {
                        C5Kit.AddOutline(g, C5Kit.Yellow, 3f);
                        C5Kit.Txt(g.transform, "Text", "GOAL", 24f, C5Kit.Yellow, TextAlignmentOptions.Center,
                            Vector2.zero, new Vector2(92f, 82f), font);
                    }
                    goalRect = new Rect(p.x - 40f, p.y - 36f, 80f, 72f);
                }
                else if (ch == 'S')
                {
                    startPos = p;
                }
            }
        }

        // 문자 (맨 위)
        if (letter == null)
        {
            letterImage = C5Kit.Img(board, "RT_Letter", null, C5Kit.White, startPos, LetterSize, true);
            letter = letterImage.rectTransform;
        }
        Sprite art = letterSprites != null && stage < letterSprites.Length ? letterSprites[stage] : null;
        letterImage.sprite = art;
        letterImage.color = C5Kit.White;
        foreach (Transform c in letter)
            Destroy(c.gameObject);
        if (art == null)
            C5Kit.Txt(letter, "L", Word[stage].ToString(), 54f, C5Kit.Pink, TextAlignmentOptions.Center,
                Vector2.zero, LetterSize, font);
        letter.SetAsLastSibling();
    }

    private static Vector2 CellCenter(int row, int col)
    {
        float x = -BoardSize.x * 0.5f + CellW * (col + 0.5f);
        float y = BoardSize.y * 0.5f - CellH * (row + 0.5f);
        return new Vector2(x, y);
    }

    private void ResetMotion()
    {
        dragging = false;
        targetTilt = Vector2.zero;
        currentTilt = Vector2.zero;
        tiltVelocity = Vector2.zero;
        velocity = Vector2.zero;
        visualTilt = Vector2.zero;
        ApplyTiltVisual();
    }

    // =========================================
    // 매 프레임
    // =========================================
    protected override void Update()
    {
        // 글자를 다 모아 START 버튼만 남았으면 시간은 멈춤 (다 해 놓고 시간 초과로 실패하지 않게)
        bool timerFrozen = phase == Phase.StartReady || phase == Phase.Completed;
        if (!timerFrozen)
            base.Update();
        if (!isGameActive || finished || phase != Phase.Playing)
            return;

        HandleInput();
        currentTilt = Vector2.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);
        ApplyTiltVisual();
        MoveLetter();
        StageEffects();
        if (phase != Phase.Playing)
            return;

        // MG08처럼 문자가 GOAL에 겹치기만 하면 성공
        if (goalRect.Overlaps(LetterRect(letter.anchoredPosition, 0.8f)))
            StartCoroutine(StageClear());
    }

    private void HandleInput()
    {
        Vector2 keys = C5Kit.KeyAxis();

        if (C5Kit.TryMouse(out Vector2 mouse))
        {
            Camera cam = C5Kit.UiCamera(boardHolder);
            if (C5Kit.MousePressedThisFrame())
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(dragZone, mouse, cam))
                {
                    dragging = true;
                    dragStart = mouse;
                }
            }

            if (dragging && C5Kit.MouseHeld())
            {
                // MG08과 똑같이: 마우스를 끈 거리(화면 픽셀) / dragRange
                Vector2 raw = (mouse - dragStart) / dragRange;
                raw = Vector2.ClampMagnitude(raw, 1f);
                if (Mathf.Abs(raw.x) < deadZone) raw.x = 0f;
                if (Mathf.Abs(raw.y) < deadZone) raw.y = 0f;
                targetTilt = raw;
            }
            else if (dragging)
            {
                dragging = false;
                targetTilt = Vector2.zero;
            }
        }

        if (!dragging)
            targetTilt = keys.sqrMagnitude > 0.01f ? keys.normalized : Vector2.zero;
    }

    private void ApplyTiltVisual()
    {
        if (boardHolder == null)
            return;

        // MG08 BoardTiltVisual 과 같은 연출: 따로 부드럽게 따라가며 기울기/눌림/살짝 밀림
        visualTilt = Vector2.Lerp(visualTilt, Vector2.ClampMagnitude(currentTilt, 1f),
            1f - Mathf.Exp(-8f * Time.deltaTime));

        boardHolder.localRotation = Quaternion.Euler(0f, 0f, -visualTilt.x * maxVisualAngle);
        float sx = 1f - Mathf.Abs(visualTilt.x) * 0.06f;
        float sy = 1f - Mathf.Abs(visualTilt.y) * 0.10f;
        boardHolder.localScale = new Vector3(sx, sy, 1f);
        boardHolder.anchoredPosition = BoardPos + visualTilt * 12f;
    }

    private void MoveLetter()
    {
        Vector2 desired = currentTilt * moveSpeed;
        // 속도를 줄이는 중이면(손을 뗐거나 반대로 기울임) 더 빨리 멈춤 → 얼음처럼 미끄러지지 않게
        bool braking = desired.sqrMagnitude < velocity.sqrMagnitude || Vector2.Dot(desired, velocity) < 0f;
        float smooth = braking ? brakeSmooth : movementSmooth;
        velocity = Vector2.Lerp(velocity, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        Vector2 pos = letter.anchoredPosition + velocity * Time.deltaTime;
        letter.anchoredPosition = pos;

        // 장애물에 닿으면 이번 판 처음부터 (MG08과 같음)
        if (HitsObstacle(pos))
        {
            StartCoroutine(ResetRoutine(false));
            return;
        }

        // 판 밖으로 나가면 떨어짐 (문자 가운데 기준)
        float hx = BoardSize.x * 0.5f + 4f;
        float hy = BoardSize.y * 0.5f + 4f;
        if (pos.x < -hx || pos.x > hx || pos.y < -hy || pos.y > hy)
            StartCoroutine(ResetRoutine(true));
    }

    private static Rect LetterRect(Vector2 center, float scale)
    {
        return new Rect(center.x - LetterSize.x * 0.5f * scale, center.y - LetterSize.y * 0.5f * scale,
            LetterSize.x * scale, LetterSize.y * scale);
    }

    private bool HitsObstacle(Vector2 center)
    {
        // 그림과 거의 같은 크기로 판정 (살짝만 여유)
        Rect me = LetterRect(center, 0.82f);
        foreach (Rect o in obstacles)
        {
            Rect shrunk = new Rect(o.x + 4f, o.y + 4f, o.width - 8f, o.height - 8f);
            if (shrunk.Overlaps(me))
                return true;
        }
        return false;
    }

    private void StageEffects()
    {
        if (stage < 3)
            return;

        fxTimer -= Time.deltaTime;
        if (fxTimer > 0f)
            return;

        if (stage == 3)
        {
            // 4판: 가끔 짧은 흔들림 + 글리치
            fxTimer = Random.Range(3f, 5f);
            StartCoroutine(C5Kit.Shake(board, 0.3f, 7f));
            StartCoroutine(C5Kit.Glitch(board, null, 0.2f, 0.6f, false));
        }
        else
        {
            // 5판: 간헐적 CRT 노이즈 (길은 그대로)
            fxTimer = Random.Range(2.5f, 4f);
            StartCoroutine(C5Kit.Glitch(board, null, 0.3f, 0.9f));
            C5Kit.TryCrtNoise(this);
        }
    }

    // =========================================
    // 떨어짐 / 판 성공
    // =========================================
    private IEnumerator ResetRoutine(bool fell)
    {
        phase = Phase.Falling;
        dragging = false;
        if (countMistakes)
            ReportMistake();
        GameSfx.Play("wrong_buzz", 0.5f);

        Vector2 dir = velocity.normalized;
        float e = 0f;
        Vector2 from = letter.anchoredPosition;
        while (e < 0.4f)
        {
            e += Time.deltaTime;
            float k = e / 0.4f;
            if (fell)
            {
                letter.anchoredPosition = from + dir * 50f * k;
                letter.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
                letter.localRotation = Quaternion.Euler(0f, 0f, k * 220f);
            }
            else
            {
                letter.anchoredPosition = from + Random.insideUnitCircle * 6f * (1f - k);
            }
            letterImage.color = fell ? new Color(1f, 1f, 1f, 1f - k) : new Color(1f, 0.4f, 0.6f, 1f - k * 0.5f);
            yield return null;
        }

        C5Kit.SetBanner(banner, bannerText,
            (fell ? "FALL" : "CRASH") + "\n<size=30>이번 판을 다시 시작합니다</size>", C5Kit.Pink);
        ResetMotion();
        yield return new WaitForSeconds(0.7f);

        if (!isGameActive || finished)
            yield break;

        C5Kit.HideBanner(banner);
        letter.anchoredPosition = startPos;
        letter.localScale = Vector3.one;
        letter.localRotation = Quaternion.identity;
        letterImage.color = C5Kit.White;
        phase = Phase.Playing;
    }

    private IEnumerator StageClear()
    {
        phase = Phase.RoundClear;
        dragging = false;
        targetTilt = Vector2.zero;
        GameSfx.Play("correct_ding", 0.55f);

        // 골로 쏙 들어간 뒤 슬롯으로 날아감
        Vector2 from = letter.anchoredPosition;
        float e = 0f;
        while (e < 0.18f)
        {
            e += Time.deltaTime;
            letter.anchoredPosition = Vector2.Lerp(from, goalRect.center, e / 0.18f);
            currentTilt = Vector2.Lerp(currentTilt, Vector2.zero, e / 0.18f);
            ApplyTiltVisual();
            yield return null;
        }
        ResetMotion();

        RectTransform fly = C5Kit.Img(screen, "RT_Fly", letterImage.sprite, C5Kit.White, Vector2.zero, LetterSize,
            true).rectTransform;
        fly.position = letter.position;
        letterImage.color = C5Kit.Clear;
        yield return C5Kit.FlyTo(fly, slotRow.Slot(stage).position, 0.35f, 1.2f);
        Destroy(fly.gameObject);

        slotRow.SetSlot(stage, true);
        yield return C5Kit.Pop(slotRow.Slot(stage), 1.25f);

        stage++;
        if (stage >= Word.Length)
        {
            StartCoroutine(AllClear());
            yield break;
        }

        // 짧은 글리치 후 다음 판
        yield return C5Kit.Glitch(board, board, 0.3f, 0.8f);
        BeginStage();
    }

    private IEnumerator AllClear()
    {
        phase = Phase.StartReady;
        plateText.text = "STAGE <color=" + C5Kit.GreenHex + ">COMPLETE</color>";
        bottomText.text = "";

        boardHolder.gameObject.SetActive(false);

        Transform[] from = new Transform[5];
        for (int i = 0; i < 5; i++)
            from[i] = slotRow.Slot(i);
        yield return C5Kit.MergeLetters(screen, letterSprites, font, new Vector2(0f, 40f), new Vector2(84f, 81f), from);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
        StartCoroutine(C5Kit.Pop(finalStartButton.transform, 1.25f, 0.3f));
        bottomText.text = "<color=" + C5Kit.YellowHex + ">START</color>를 눌러 검사를 완료하세요.";
    }

    private void OnFinalStartClicked()
    {
        if (!isGameActive || finished || phase != Phase.StartReady)
            return;

        finished = true;
        phase = Phase.Completed;
        Success();
    }

    protected override void GiveHint()
    {
        if (bottomText != null && phase == Phase.Playing)
            bottomText.text = "힌트: 마우스를 멀리 끌수록 빨리 움직입니다. 살짝만 끌어 천천히 이동하세요.";
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
