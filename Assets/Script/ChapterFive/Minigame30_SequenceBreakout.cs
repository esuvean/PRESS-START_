using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MG30 순서대로 깨는 START (실행 문자 순서 제거 검사) - 기존 벽돌깨기 방식
///  - S / T / A / R / T 문자 벽돌 5개를 순서 상관없이 모두 깨면 됨 (일반 벽돌은 그냥 깨짐)
///  - 공 속도와 튕기는 각도는 MG12 벽돌깨기와 같음
///  - 공을 놓쳐도 진행도는 유지 (새 공만 나옴)
///  - 다 깨면 S·T·A·R·T → START 결합 → START 클릭
/// 벽돌 크기는 시안의 약 2/3, 공과 받침대 사이 공간이 넉넉해서 7줄.
/// </summary>
public class Minigame30_SequenceBreakout : MinigameBase
{
    [Header("챕터5 그림 (에디터 도구가 자동으로 넣음)")]
    public TMP_FontAsset uiFont;
    public Sprite panelSprite;          // 23_line_back
    public Sprite plateSprite;          // 26_upline
    public Sprite startButtonSprite;    // normal_button
    public Sprite slotFrameSprite;      // 26_miniline
    public Sprite slotEmptySprite;      // 26_blueblank
    public Sprite slotArrowLeft;
    public Sprite slotArrowRight;
    public Sprite[] slotLetterSprites = new Sprite[5];    // 26_pink_S/T/A/R/T
    public Sprite[] letterBrickSprites = new Sprite[5];   // 30_block_S/T/A/R/T
    public Sprite normalBrickSprite;                      // 30_blank

    [Header("규칙")]
    [Tooltip("SEQUENCE ERROR 를 챕터5 오류 횟수에 포함 (공이 튀는 게임이라 기본은 끔)")]
    public bool countMistakes = false;

    [Header("벽돌")]
    public int columns = 13;
    public int rows = 7;
    public Vector2 brickSize = new Vector2(96f, 30f);
    public float brickGap = 6f;
    public int distractorCount = 0;   // 방해 문자 없음

    [Header("공 / 받침대")]
    public float ballSpeed = 430f;     // MG12와 같은 속도
    public float ballSize = 22f;
    public float maxBounceAngle = 60f;
    public Vector2 paddleSize = new Vector2(220f, 26f);
    public float paddleMoveSpeed = 1100f;
    public float errorDisplayTime = 1.1f;

    private const string Word = "START";
    private const string Distractors = "ENDXIPLY";

    // ---- 배치 ----
    private static readonly Vector2 PanelPos = new Vector2(0f, 30f);
    private static readonly Vector2 PanelSize = new Vector2(1480f, 580f);
    private static readonly Vector2 PlatePos = new Vector2(0f, 300f);
    private static readonly Vector2 AreaPos = new Vector2(0f, 25f);
    private static readonly Vector2 AreaSize = new Vector2(1420f, 510f);
    private static readonly Vector2 SlotRowPos = new Vector2(0f, -338f);
    private const float TopBrickY = 205f;
    private const float PaddleY = -215f;

    private enum GamePhase
    {
        WaitingLaunch,
        Playing,
        SequenceError,
        StartReady,
        Completed
    }

    private class Brick
    {
        public RectTransform rect;
        public Image image;
        public Outline glow;
        public char character;
        public int targetIndex = -1;   // START 문자면 0~4
        public bool distractor;
        public bool broken;
    }

    private GamePhase phase;
    private bool finished;
    private int currentTargetIndex;          // 지금까지 깬 START 문자 수
    private readonly bool[] slotFilled = new bool[5];
    private float launchTimer;
    private Vector2 ballVelocity;

    private readonly List<Brick> bricks = new List<Brick>();

    // ---- 만든 UI ----
    private TMP_FontAsset font;
    private RectTransform screen;
    private RectTransform panel;
    private RectTransform area;
    private RectTransform paddle;
    private RectTransform ball;
    private TextMeshProUGUI plateText;
    private TextMeshProUGUI bottomText;
    private CanvasGroup banner;
    private TextMeshProUGUI bannerText;
    private C5SlotRow slotRow;
    private Button finalStartButton;
    private Texture2D ballTexture;
    private Sprite ballSprite;
    private Coroutine messageRoutine;

    private void Start()
    {
        if (!isGameActive)
            StartMinigame();
    }

    public override void StartMinigame()
    {
        gameName = "실행 문자 순서 제거 검사";
        instruction = "벽돌을 깨며 S-T-A-R-T를 순서대로 제거하세요.";

        base.StartMinigame();
        StopAllCoroutines();
        messageRoutine = null;

        finished = false;
        currentTargetIndex = 0;
        for (int i = 0; i < slotFilled.Length; i++)
            slotFilled[i] = false;
        bricks.Clear();

        BuildScreen();
        C5Kit.PrepareNoise(transform);
        CreateBricks();
        slotRow.SetCount(0);
        UpdateHud();
        ResetBall();
        SetBottom(DefaultGuide());
    }

    private string DefaultGuide()
    {
        return "<color=" + C5Kit.YellowHex + ">S · T · A · R · T</color> 문자 벽돌을 모두 깨세요. (순서 상관없음)";
    }

    private void OnDestroy()
    {
        if (ballSprite != null) Destroy(ballSprite);
        if (ballTexture != null) Destroy(ballTexture);
    }

    // =========================================
    // 화면 만들기
    // =========================================
    private void BuildScreen()
    {
        font = C5Kit.ResolveFont(transform, uiFont);
        C5Kit.HideLegacy(transform);
        screen = C5Kit.NewScreen(transform, "RT_MG30");

        panel = C5Kit.Panel(screen, panelSprite, PanelPos, PanelSize).rectTransform;
        area = C5Kit.Rect(screen, "RT_Area", AreaPos, AreaSize);

        Image pad = C5Kit.Img(area, "RT_Paddle", null, C5Kit.Cyan, new Vector2(0f, PaddleY), paddleSize);
        C5Kit.Img(pad.transform, "Shine", null, new Color(0.78f, 0.98f, 1f, 1f), new Vector2(0f, paddleSize.y * 0.3f),
            new Vector2(paddleSize.x - 14f, 5f));
        C5Kit.Img(pad.transform, "Shade", null, new Color32(8, 120, 160, 255), new Vector2(0f, -paddleSize.y * 0.38f),
            new Vector2(paddleSize.x, 6f));
        paddle = pad.rectTransform;

        if (ballSprite == null)
            ballSprite = MakeBallSprite();
        ball = C5Kit.Img(area, "RT_Ball", ballSprite, C5Kit.White, Vector2.zero, new Vector2(ballSize, ballSize))
            .rectTransform;

        plateText = C5Kit.Plate(screen, plateSprite, font, PlatePos, new Vector2(480f, 80f), "");
        slotRow = new C5SlotRow(screen, SlotRowPos, slotFrameSprite, slotEmptySprite, slotLetterSprites,
            slotArrowLeft, slotArrowRight, font);
        bottomText = C5Kit.BottomText(screen, font, "");
        banner = C5Kit.Banner(screen, font, new Vector2(0f, 20f), new Vector2(760f, 160f), out bannerText);

        finalStartButton = C5Kit.StartButton(screen, startButtonSprite, font, new Vector2(0f, 40f),
            new Vector2(360f, 84f), OnFinalStartClicked);
        finalStartButton.gameObject.SetActive(false);
    }

    private Sprite MakeBallSprite()
    {
        const int N = 16;
        ballTexture = new Texture2D(N, N, TextureFormat.RGBA32, false);
        ballTexture.filterMode = FilterMode.Point;
        ballTexture.wrapMode = TextureWrapMode.Clamp;
        Color rim = new Color32(25, 220, 245, 255);
        Color core = Color.white;
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float dx = x + 0.5f - N * 0.5f;
                float dy = y + 0.5f - N * 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color c = Color.clear;
                if (d <= N * 0.5f - 0.2f)
                    c = d > N * 0.5f - 2.4f ? rim : core;
                if (d <= 2.2f && dx < 0f && dy > 0f)
                    c = new Color(0.85f, 1f, 1f, 1f);
                ballTexture.SetPixel(x, y, c);
            }
        }
        ballTexture.Apply();
        return Sprite.Create(ballTexture, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    // =========================================
    // 벽돌 배치
    // =========================================
    private void CreateBricks()
    {
        foreach (Brick b in bricks)
            if (b.rect != null) Destroy(b.rect.gameObject);
        bricks.Clear();

        // 문자 칸 정하기 (S-T-A-R-T + 방해 문자, 서로 붙지 않게)
        Dictionary<Vector2Int, int> letterCells = new Dictionary<Vector2Int, int>();   // 값: 0~4 START, 10+ 방해
        List<Vector2Int> used = new List<Vector2Int>();
        for (int i = 0; i < Word.Length; i++)
            letterCells[PickCell(used, 0, rows - 2)] = i;
        int dCount = Mathf.Clamp(distractorCount, 0, Distractors.Length);
        string pool = Shuffle(Distractors);
        for (int i = 0; i < dCount; i++)
            letterCells[PickCell(used, 0, rows - 1)] = 10 + (pool[i] - 'A');

        float pitchX = brickSize.x + brickGap;
        float pitchY = brickSize.y + brickGap;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector2 pos = new Vector2((c - (columns - 1) * 0.5f) * pitchX, TopBrickY - r * pitchY);
                Vector2Int cell = new Vector2Int(c, r);
                Brick b = new Brick();

                if (letterCells.TryGetValue(cell, out int code))
                {
                    if (code < 10)
                    {
                        b.targetIndex = code;
                        b.character = Word[code];
                        Sprite s = letterBrickSprites != null && code < letterBrickSprites.Length
                            ? letterBrickSprites[code] : null;
                        b.image = C5Kit.Img(area, "Brick_" + b.character, s, s != null ? C5Kit.White : C5Kit.Cyan,
                            pos, brickSize);
                        if (s == null)
                            BrickLetter(b.image.transform, b.character, C5Kit.Navy);
                        b.glow = b.image.gameObject.AddComponent<Outline>();
                        b.glow.effectColor = C5Kit.Clear;
                        b.glow.effectDistance = new Vector2(3f, -3f);
                        b.glow.useGraphicAlpha = false;
                    }
                    else
                    {
                        b.distractor = true;
                        b.character = (char)('A' + (code - 10));
                        b.image = C5Kit.Img(area, "Brick_" + b.character, normalBrickSprite,
                            new Color(0.74f, 0.78f, 0.92f, 1f), pos, brickSize);
                        BrickLetter(b.image.transform, b.character, new Color32(30, 36, 70, 255));
                    }
                }
                else
                {
                    b.image = C5Kit.Img(area, "Brick", normalBrickSprite,
                        normalBrickSprite != null ? C5Kit.White : C5Kit.Pale, pos, brickSize);
                }

                b.rect = b.image.rectTransform;
                bricks.Add(b);
            }
        }

        paddle.SetAsLastSibling();
        ball.SetAsLastSibling();
    }

    private Vector2Int PickCell(List<Vector2Int> used, int minRow, int maxRow)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            Vector2Int c = new Vector2Int(Random.Range(0, columns), Random.Range(minRow, maxRow + 1));
            bool ok = true;
            foreach (Vector2Int u in used)
            {
                if (Mathf.Abs(u.x - c.x) <= 1 && Mathf.Abs(u.y - c.y) <= 1)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok) continue;
            used.Add(c);
            return c;
        }

        // 자리가 없으면 겹치지만 않게
        for (int r = minRow; r <= maxRow; r++)
        for (int x = 0; x < columns; x++)
        {
            Vector2Int c = new Vector2Int(x, r);
            if (!used.Contains(c))
            {
                used.Add(c);
                return c;
            }
        }
        return Vector2Int.zero;
    }

    private static string Shuffle(string s)
    {
        char[] a = s.ToCharArray();
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            char t = a[i];
            a[i] = a[j];
            a[j] = t;
        }
        return new string(a);
    }

    private void BrickLetter(Transform parent, char c, Color color)
    {
        C5Kit.Txt(parent, "Letter", c.ToString(), brickSize.y * 0.82f, color, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), brickSize, font);
    }

    private void UpdateHud()
    {
        plateText.text = "START <color=" + C5Kit.YellowHex + "><size=44>" + currentTargetIndex + "</size></color> / 5";
    }

    // =========================================
    // 매 프레임
    // =========================================
    protected override void Update()
    {
        // 글자를 다 모아 START 버튼만 남았으면 시간은 멈춤 (다 해 놓고 시간 초과로 실패하지 않게)
        bool timerFrozen = phase == GamePhase.StartReady || phase == GamePhase.Completed;
        if (!timerFrozen)
            base.Update();
        if (!isGameActive || finished)
            return;

        if (phase == GamePhase.WaitingLaunch || phase == GamePhase.Playing)
            UpdatePaddle();

        if (phase == GamePhase.WaitingLaunch)
        {
            ball.anchoredPosition = new Vector2(paddle.anchoredPosition.x,
                PaddleY + paddleSize.y * 0.5f + ballSize * 0.5f + 2f);
            launchTimer += Time.deltaTime;
            if (launchTimer >= 0.8f || C5Kit.SpacePressedThisFrame() ||
                (C5Kit.MousePressedThisFrame() && MouseInArea()))
                Launch();
        }
        else if (phase == GamePhase.Playing)
        {
            UpdateBall();
        }

        PulseTarget();
    }

    private bool MouseInArea()
    {
        return C5Kit.TryMouse(out Vector2 m) &&
               RectTransformUtility.RectangleContainsScreenPoint(area, m, C5Kit.UiCamera(area));
    }

    private void UpdatePaddle()
    {
        float x = paddle.anchoredPosition.x;
        float key = C5Kit.KeyAxis().x;
        if (Mathf.Abs(key) > 0.01f)
        {
            x += key * paddleMoveSpeed * Time.deltaTime;
        }
        else if (C5Kit.TryMouse(out Vector2 mouse))
        {
            Camera cam = C5Kit.UiCamera(area);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, mouse, cam, out Vector2 local) &&
                RectTransformUtility.RectangleContainsScreenPoint(panel, mouse, cam))
                x = local.x;   // MG12처럼 마우스를 바로 따라감
        }

        float limit = AreaSize.x * 0.5f - paddleSize.x * 0.5f - 6f;
        paddle.anchoredPosition = new Vector2(Mathf.Clamp(x, -limit, limit), PaddleY);
    }

    private void ResetBall()
    {
        phase = GamePhase.WaitingLaunch;
        ball.gameObject.SetActive(true);
        ballVelocity = Vector2.zero;
        launchTimer = 0f;
        ball.anchoredPosition = new Vector2(paddle.anchoredPosition.x, PaddleY + paddleSize.y * 0.5f + ballSize * 0.5f + 2f);
    }

    private void Launch()
    {
        ballVelocity = new Vector2(Random.Range(-0.45f, 0.45f), 1f).normalized * ballSpeed;
        phase = GamePhase.Playing;
        if (messageRoutine == null)
            SetBottom(DefaultGuide());
    }

    private void UpdateBall()
    {
        Vector2 pos = ball.anchoredPosition;
        Vector2 delta = ballVelocity * Time.deltaTime;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 4f));
        Vector2 step = delta / steps;
        float r = ballSize * 0.5f;
        float halfW = AreaSize.x * 0.5f;
        float halfH = AreaSize.y * 0.5f;
        float topWall = TopBrickY + brickSize.y * 0.5f + 14f;   // 위쪽 제목 띠 바로 아래

        for (int i = 0; i < steps; i++)
        {
            pos += step;

            // 벽 (왼쪽/오른쪽/위)
            if (pos.x - r < -halfW && ballVelocity.x < 0f) { pos.x = -halfW + r; ballVelocity.x = -ballVelocity.x; step.x = -step.x; }
            if (pos.x + r > halfW && ballVelocity.x > 0f) { pos.x = halfW - r; ballVelocity.x = -ballVelocity.x; step.x = -step.x; }
            if (pos.y + r > topWall && ballVelocity.y > 0f) { pos.y = topWall - r; ballVelocity.y = -ballVelocity.y; step.y = -step.y; }

            // 받침대
            Rect pad = new Rect(paddle.anchoredPosition.x - paddleSize.x * 0.5f, PaddleY - paddleSize.y * 0.5f,
                paddleSize.x, paddleSize.y);
            if (ballVelocity.y < 0f && CircleHitsRect(pos, r, pad))
            {
                float offset = Mathf.Clamp((pos.x - pad.center.x) / (paddleSize.x * 0.5f), -1f, 1f);
                // MG12와 같은 방식: 맞은 위치만큼 옆으로 (최대 45도)
                ballVelocity = new Vector2(offset, 1f).normalized * ballSpeed;
                pos.y = pad.yMax + r + 0.5f;
                step = ballVelocity * Time.deltaTime / steps;
                GameSfx.Play("piece_place", 0.25f, 0.1f);
            }

            // 벽돌
            Brick hit = null;
            foreach (Brick b in bricks)
            {
                if (b.broken) continue;
                Rect br = new Rect(b.rect.anchoredPosition - brickSize * 0.5f, brickSize);
                if (!CircleHitsRect(pos, r, br)) continue;

                // 덜 파고든 쪽으로 튕김
                float px = Mathf.Min(pos.x + r - br.xMin, br.xMax - (pos.x - r));
                float py = Mathf.Min(pos.y + r - br.yMin, br.yMax - (pos.y - r));
                if (px < py)
                {
                    ballVelocity.x = pos.x < br.center.x ? -Mathf.Abs(ballVelocity.x) : Mathf.Abs(ballVelocity.x);
                    pos.x += pos.x < br.center.x ? -px : px;
                }
                else
                {
                    ballVelocity.y = pos.y < br.center.y ? -Mathf.Abs(ballVelocity.y) : Mathf.Abs(ballVelocity.y);
                    pos.y += pos.y < br.center.y ? -py : py;
                }
                step = ballVelocity * Time.deltaTime / steps;
                hit = b;
                break;
            }

            if (hit != null)
            {
                ball.anchoredPosition = pos;
                HandleBrickHit(hit);
                if (phase != GamePhase.Playing)
                    return;
            }

            // 공이 너무 수평으로 흐르지 않게
            if (Mathf.Abs(ballVelocity.y) < ballSpeed * 0.28f)
            {
                ballVelocity.y = Mathf.Sign(ballVelocity.y == 0f ? 1f : ballVelocity.y) * ballSpeed * 0.28f;
                ballVelocity = ballVelocity.normalized * ballSpeed;
                step = ballVelocity * Time.deltaTime / steps;
            }

            // 아래로 떨어짐
            if (pos.y < -halfH - r)
            {
                StartCoroutine(BallLost());
                return;
            }
        }

        ball.anchoredPosition = pos;
    }

    private static bool CircleHitsRect(Vector2 c, float r, Rect rect)
    {
        float nx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
        float ny = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
        float dx = c.x - nx;
        float dy = c.y - ny;
        return dx * dx + dy * dy <= r * r;
    }

    private void PulseTarget()
    {
        float a = 0.35f + 0.35f * Mathf.Sin(Time.time * 6f);
        foreach (Brick b in bricks)
        {
            if (b.glow == null || b.broken) continue;
            bool on = b.targetIndex >= 0 &&
                      (phase == GamePhase.Playing || phase == GamePhase.WaitingLaunch);
            b.glow.effectColor = on ? new Color(C5Kit.Yellow.r, C5Kit.Yellow.g, C5Kit.Yellow.b, a) : C5Kit.Clear;
        }
    }

    // =========================================
    // 벽돌 맞힘
    // =========================================
    private void HandleBrickHit(Brick b)
    {
        if (b.targetIndex < 0 && !b.distractor)
        {
            BreakBrick(b);
            GameSfx.Play("block_place", 0.35f, 0.12f);
            return;
        }

        if (b.distractor)
        {
            BreakBrick(b);
            return;
        }

        // START 문자: 순서 상관없이 깨지고, 같은 글자 자리 슬롯에 들어감 (T는 비어 있는 T 칸)
        int index = SlotFor(b.character);
        if (index >= 0)
            slotFilled[index] = true;
        BreakBrick(b, false);
        StartCoroutine(FlyToSlot(b, index));
        currentTargetIndex++;
        GameSfx.Play("correct_ding", 0.5f);
        UpdateHud();

        if (currentTargetIndex >= Word.Length)
            StartCoroutine(AllCleared());
    }

    private int SlotFor(char c)
    {
        for (int i = 0; i < Word.Length; i++)
            if (Word[i] == c && !slotFilled[i])
                return i;
        return -1;
    }

    private void BreakBrick(Brick b, bool effect = true)
    {
        b.broken = true;
        if (effect)
            StartCoroutine(BreakEffect(b.rect, b.image.color));
        else
            b.rect.gameObject.SetActive(false);
    }

    private IEnumerator BreakEffect(RectTransform rect, Color color)
    {
        if (rect == null) yield break;
        Image img = rect.GetComponent<Image>();
        TMP_Text label = rect.GetComponentInChildren<TMP_Text>();
        float e = 0f;
        while (e < 0.16f && rect != null)
        {
            e += Time.deltaTime;
            float k = e / 0.16f;
            rect.localScale = new Vector3(1f + k * 0.25f, 1f - k * 0.6f, 1f);
            if (img != null) img.color = new Color(color.r, color.g, color.b, 1f - k);
            if (label != null) label.alpha = 1f - k;
            yield return null;
        }
        if (rect != null)
        {
            rect.gameObject.SetActive(false);
            rect.localScale = Vector3.one;
            if (img != null) img.color = color;
            if (label != null) label.alpha = 1f;
        }
    }

    private IEnumerator FlyToSlot(Brick b, int index)
    {
        Sprite s = slotLetterSprites != null && index >= 0 && index < slotLetterSprites.Length ? slotLetterSprites[index] : null;
        RectTransform fly = C5Kit.Img(screen, "RT_Fly", s, s != null ? C5Kit.White : C5Kit.Pink, Vector2.zero,
            new Vector2(60f, 56f), true).rectTransform;
        fly.position = b.rect.position;
        if (index >= 0)
            yield return C5Kit.FlyTo(fly, slotRow.Slot(index).position, 0.35f, 1.4f);
        Destroy(fly.gameObject);

        if (index >= 0)
        {
            slotRow.SetSlot(index, true);
            yield return C5Kit.Pop(slotRow.Slot(index), 1.25f);
        }
    }

    private IEnumerator BallLost()
    {
        phase = GamePhase.SequenceError;   // 잠깐 멈춤
        ball.gameObject.SetActive(false);
        ShowMessage("공을 놓쳤습니다. 진행도는 <color=" + C5Kit.YellowHex + ">유지</color>됩니다.");
        yield return new WaitForSeconds(0.5f);
        if (!isGameActive || finished)
            yield break;
        ResetBall();
    }

    private IEnumerator AllCleared()
    {
        phase = GamePhase.StartReady;
        ball.gameObject.SetActive(false);
        plateText.text = "SEQUENCE <color=" + C5Kit.GreenHex + ">COMPLETE</color>";
        SetBottom("");
        yield return new WaitForSeconds(0.55f);

        area.gameObject.SetActive(false);

        Transform[] from = new Transform[5];
        for (int i = 0; i < 5; i++)
            from[i] = slotRow.Slot(i);
        yield return C5Kit.MergeLetters(screen, slotLetterSprites, font, new Vector2(0f, 40f), new Vector2(88f, 81f), from);

        finalStartButton.gameObject.SetActive(true);
        finalStartButton.transform.SetAsLastSibling();
        StartCoroutine(C5Kit.Pop(finalStartButton.transform, 1.25f, 0.3f));
        SetBottom("<color=" + C5Kit.YellowHex + ">START</color>를 눌러 검사를 완료하세요.");
    }

    // =========================================
    // 안내 문구
    // =========================================
    private void SetBottom(string text)
    {
        if (bottomText != null)
            bottomText.text = text;
    }

    private void ShowMessage(string text)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text));
    }

    private IEnumerator MessageRoutine(string text)
    {
        SetBottom(text);
        yield return new WaitForSeconds(1.8f);
        if (phase == GamePhase.Playing || phase == GamePhase.WaitingLaunch)
            SetBottom(DefaultGuide());
        messageRoutine = null;
    }

    private void OnFinalStartClicked()
    {
        if (!isGameActive || finished || phase != GamePhase.StartReady)
            return;

        finished = true;
        phase = GamePhase.Completed;
        Success();
    }

    protected override void GiveHint()
    {
        if (phase == GamePhase.Playing || phase == GamePhase.WaitingLaunch)
            ShowMessage("힌트: 노랗게 깜빡이는 벽돌이 지금 깰 문자입니다.");
    }

    protected override void RestartGame()
    {
        StartMinigame();
    }
}
