using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 메뉴: 상단 메뉴바 > PRESS START > 챕터5 UI 새 디자인 적용
// 챕터5 미니게임(MG21~MG25) 프리팹을 네온 픽셀 디자인으로 바꿔 준다.
//  - 팀원들이 이미 만든 아트(배경 프레임, 네온 패널, 버튼, S/T/A/R 글자, 로딩바, 팝업창, X버튼)만 사용
//  - 스크립트가 이름으로 찾는 오브젝트는 이름을 바꾸거나 지우지 않는다 (위치/색/그림만 바꿈)
//  - 처음 실행할 때 원래 프리팹을 Assets/Prefab/_Ch5Backup 폴더에 복사해 둔다
//  - 여러 번 눌러도 결과는 같다 (C5_ 로 시작하는 꾸밈 오브젝트를 지우고 다시 만듦)
public static class Chapter5SkinTool
{
    const string PrefabDir = "Assets/Prefab/";
    const string BackupDir = "Assets/Prefab/_Ch5Backup";
    const string Prefix = "C5_";

    // ---------- 사용하는 아트 ----------
    const string ArtBg = "Assets/1. Team Folder/Subin/NEW ART/설정배경2.png";
    const string ArtFrame = "Assets/1. Team Folder/Dawon/no11_outline.png";
    const string ArtButton = "Assets/1. Team Folder/Hyunmin/Art/Chapter5/24_button.png";
    const string ArtBarFrame = "Assets/1. Team Folder/Hyunmin/Art/Chapter5/24_loading_bar_white.png";
    const string ArtBarFill = "Assets/1. Team Folder/Hyunmin/Art/Chapter5/24_loading_prograss__nottrim.png";
    const string ArtWindow = "Assets/1. Team Folder/Hyunmin/Art/06_window.png";
    const string ArtX = "Assets/1. Team Folder/Hyunmin/Art/Xbutton.png";
    const string ArtSlot = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/find_blank.png";
    const string ArtS = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/S.png";      // 파란 S
    const string ArtTBlue = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/T2.png"; // 파란 T
    const string ArtA = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/A.png";      // 분홍 A
    const string ArtR = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/R.png";      // 분홍 R
    const string ArtTYellow = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/T.png"; // 노란 T

    const string FontBoldPath = "Assets/Fonts/Galmuri11_Bold SDF.asset";
    const string FontRegularPath = "Assets/Fonts/Galmuri11_Regular SDF.asset";

    // ---------- 색 ----------
    static readonly Color White = Color.white;
    static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
    static readonly Color Pink = new Color32(255, 45, 149, 255);
    static readonly Color Cyan = new Color32(25, 200, 240, 255);
    static readonly Color Yellow = new Color32(255, 232, 26, 255);
    static readonly Color Navy = new Color32(11, 26, 58, 255);
    static readonly Color Pale = new Color32(169, 188, 224, 255);
    const string PinkHex = "#FF2D95";
    const string YellowHex = "#FFE81A";

    // ---------- 공통 배치 (1920x1080 캔버스 기준, 가운데가 0,0) ----------
    static readonly Vector2 BadgePos = new Vector2(-770f, 395f);
    static readonly Vector2 BadgeSize = new Vector2(200f, 74f);
    const float TitleLeftX = -640f;
    const float TitleY = 410f;
    const float SubtitleY = 360f;
    const float HintY = -424f;
    const float FootnoteY = -478f;

    static TMP_FontAsset fontBold, fontRegular;
    static readonly List<string> missing = new List<string>();

    [MenuItem("PRESS START/챕터5 UI 새 디자인 적용")]
    public static void Apply()
    {
        missing.Clear();
        fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldPath);
        fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRegularPath);
        if (fontBold == null) missing.Add(FontBoldPath);
        if (fontRegular == null) missing.Add(FontRegularPath);

        var done = new List<string>();
        for (int n = 21; n <= 25; n++)
        {
            string path = PrefabDir + "Minigame_" + n + "Panel.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                missing.Add(path);
                continue;
            }

            Backup(path, n);

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Cleanup(root.transform);
                switch (n)
                {
                    case 21: Skin21(root.transform); break;
                    case 22: Skin22(root.transform); break;
                    case 23: Skin23(root.transform); break;
                    case 24: Skin24(root.transform); break;
                    case 25: Skin25(root.transform); break;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                done.Add("MG" + n);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();

        string msg = "적용 완료: " + string.Join(", ", done) +
                     "\n\n원래 프리팹은 " + BackupDir + " 폴더에 백업되어 있어요.";
        if (missing.Count > 0)
            msg += "\n\n찾지 못한 파일 (이 부분은 건너뜀):\n- " + string.Join("\n- ", missing);
        EditorUtility.DisplayDialog("챕터5 UI", msg, "확인");
    }

    // =====================================================
    // MG21 손상된 약관 데이터 탐색 검사
    // =====================================================
    static void Skin21(Transform root)
    {
        Common(root, 21,
            "손상된 약관 데이터 탐색 검사",
            "약관을 확인하고 숨은 START를 찾으세요.",
            "끝이 정답이라는 보장은 없습니다.");

        AddFrame(root, new Vector2(0f, -25f), new Vector2(1320f, 640f));

        RectTransform scroll = Find(root, "TermsScrollView");
        if (scroll != null)
        {
            Place(scroll, new Vector2(0f, 5f), new Vector2(1180f, 470f));
            SetImage(scroll, null, Clear);
        }

        RectTransform bar = Find(root, "Scrollbar Vertical");
        if (bar != null) SetImage(bar, null, new Color32(20, 40, 85, 255));
        RectTransform handle = Find(root, "Handle");
        if (handle != null) SetImage(handle, null, Cyan);

        TMP_Text terms = Text(root, "TremText");
        if (terms != null) terms.color = Pale;

        NewImage(root, Prefix + "Divider", null, new Color(Pale.r, Pale.g, Pale.b, 0.6f),
            new Vector2(0f, -262f), new Vector2(1150f, 3f));

        TMP_Text status = Text(root, "STATUSTEXT");
        if (status != null)
        {
            Place(status.rectTransform, new Vector2(0f, -300f), new Vector2(1000f, 44f));
            StyleText(status, fontRegular, 28f, Pale, TextAlignmentOptions.Center);
            status.transform.SetAsLastSibling();
        }

        StyleButton(Find(root, "RevealedStartButton"), "START", new Vector2(300f, 90f), 40f, false);

        // 스크립트가 바꾸는 힌트 문구(HINTTEXT)를 아래쪽 >> << 줄에 넣는다
        HintBar(root, Text(root, "HINTTEXT"), null);
    }

    // =====================================================
    // MG22 설정 데이터 문자 복구 검사
    // =====================================================
    static void Skin22(Transform root)
    {
        Common(root, 22,
            "설정 데이터 문자 복구 검사",
            "SETTINGS 화면 곳곳의 START 문자를 복구하세요.",
            "마지막 T는 나중에 나타납니다.");

        AddFrame(root, new Vector2(0f, 40f), new Vector2(1320f, 540f));

        RectTransform window = Find(root, "SettingsWindow");
        if (window != null) SetImage(window, null, Clear);

        TMP_Text settingsTitle = Text(root, "SettingsTitle");
        if (settingsTitle != null)
            StyleText(settingsTitle, fontBold, 34f, Cyan, settingsTitle.alignment);

        // 아래쪽 안내 문구 (스크립트가 바꾸지 않는 문구라 내용도 시안대로)
        TMP_Text instruction = Text(root, "InstructionText");
        if (instruction != null)
        {
            instruction.text = "숨어 있는 글자를 모아 <color=" + YellowHex + ">START</color>를 완성하세요.";
            HintBar(root, instruction, null);
        }

        // ----- START 조합 화면 -----
        RectTransform assembly = Find(root, "AssemblyPanel");
        if (assembly != null)
        {
            // AssemblyPanel 바로 아래의 전체 배경 Image
            foreach (Transform c in assembly)
            {
                Image img = c.GetComponent<Image>();
                RectTransform rt = c as RectTransform;
                if (img != null && c.name == "Image" && rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                    SetImage(rt, Load(ArtBg), White);
            }

            TMP_Text recTitle = Text(assembly, "START DATA RECOVERY");
            if (recTitle != null)
                StyleText(recTitle, fontBold, 44f, White, recTitle.alignment);
        }

        for (int i = 0; i < 5; i++)
        {
            RectTransform slot = Find(root, "TargetSlot_" + i);
            if (slot != null)
            {
                slot.sizeDelta = new Vector2(120f, 140f);
                SetImage(slot, Load(ArtSlot), White);
            }
        }

        LetterArt(Find(root, "LetterBlock_S"), ArtS, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_T1"), ArtTBlue, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_A"), ArtA, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_R"), ArtR, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_T2"), ArtTYellow, new Vector2(120f, 140f));

        if (assembly != null)
            StyleButton(Find(assembly, "FinalStartButton"), "START", new Vector2(300f, 80f), 36f, false);
    }

    // =====================================================
    // MG23 실행 문자 추적 검사
    // =====================================================
    static void Skin23(Transform root)
    {
        Common(root, 23,
            "실행 문자 추적 검사",
            "움직이는 글자를 S-T-A-R-T 순서로 잡으세요.",
            null);

        AddFrame(root, new Vector2(0f, -25f), new Vector2(1640f, 690f));

        RectTransform play = Find(root, "PlayArea");
        if (play != null) Place(play, new Vector2(0f, -30f), new Vector2(1540f, 520f));

        // CURRENT TARGET 상자
        TMP_Text target = Text(root, "CurrentTargetText");
        if (target != null)
        {
            Image box = NewImage(root, Prefix + "TargetBox", Load(ArtButton), White,
                new Vector2(0f, 262f), new Vector2(620f, 76f));
            box.transform.SetAsLastSibling();
            Place(target.rectTransform, new Vector2(0f, 262f), new Vector2(580f, 60f));
            StyleText(target, fontBold, 32f, White, TextAlignmentOptions.Center);
            target.transform.SetAsLastSibling();
        }

        TMP_Text startText = Text(root, "StartText");
        if (startText != null) StyleText(startText, fontBold, startText.fontSize, White, startText.alignment);

        LetterArt(Find(root, "Letter_S"), ArtS, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_T1"), ArtTBlue, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_A"), ArtA, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_R"), ArtR, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_T2"), ArtTYellow, new Vector2(120f, 146f));

        StyleButton(Find(root, "FinalStartButton"), "START", new Vector2(380f, 110f), 52f, false);

        TMP_Text progress = Text(root, "ProgressText");
        if (progress != null)
        {
            Place(progress.rectTransform, new Vector2(0f, -300f), new Vector2(900f, 60f));
            StyleText(progress, fontBold, 36f, Yellow, TextAlignmentOptions.Center);
            progress.transform.SetAsLastSibling();
        }

        HintBar(root, null, "순서가 틀리면 <color=" + YellowHex + ">처음부터</color> 다시 시작합니다.");
    }

    // =====================================================
    // MG24 실행 대기 판단 검사
    // =====================================================
    static void Skin24(Transform root)
    {
        Common(root, 24,
            "실행 대기 판단 검사",
            "99%에서 멈춘 화면에서 올바른 조건을 기다리세요.",
            null);

        Vector2 windowPos = new Vector2(0f, 23f);
        AddFrame(root, windowPos, new Vector2(1320f, 600f));

        RectTransform window = Find(root, "LoadingWindow");
        if (window != null)
        {
            Place(window, windowPos, new Vector2(1320f, 600f));
            SetImage(window, null, Clear);
        }

        // "LOADING..." + "99%" 를 한 줄로
        TMP_Text loading = Text(root, "LoadingText");
        if (loading != null)
        {
            Place(loading.rectTransform, new Vector2(110f, 175f), new Vector2(700f, 76f), new Vector2(1f, 0.5f));
            StyleText(loading, fontBold, 58f, White, TextAlignmentOptions.Right);
        }
        TMP_Text percent = Text(root, "PercentText");
        if (percent != null)
        {
            Place(percent.rectTransform, new Vector2(130f, 175f), new Vector2(260f, 76f), new Vector2(0f, 0.5f));
            StyleText(percent, fontBold, 58f, White, TextAlignmentOptions.Left);
        }

        RectTransform barBg = Find(root, "LoadingBarBG");
        if (barBg != null)
        {
            Place(barBg, new Vector2(0f, 92f), new Vector2(980f, 68f));
            SetImage(barBg, Load(ArtBarFrame), White);
        }
        RectTransform fill = Find(root, "LoadingBarFill");
        if (fill != null)
        {
            // 테두리 그림과 같은 크기로 겹쳐서, 노란 부분만 왼쪽부터 차오르게
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0.5f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image img = SetImage(fill, Load(ArtBarFill), White);
            if (img != null)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = 0;
                img.fillAmount = 0.99f;
            }
        }

        TMP_Text fake = Text(root, "FakeInstructionText");
        if (fake != null)
        {
            Place(fake.rectTransform, new Vector2(0f, 24f), new Vector2(1100f, 40f));
            StyleText(fake, fontRegular, 26f, Pale, TextAlignmentOptions.Center);
        }

        StyleButton(Find(root, "CompleteButton"), "COMPLETE", new Vector2(580f, 120f), 64f, true, new Vector2(0f, -75f));

        TMP_Text small = Text(root, "SmallHintText");
        if (small != null)
        {
            Place(small.rectTransform, new Vector2(0f, -175f), new Vector2(1000f, 40f));
            StyleText(small, fontRegular, small.fontSize, White, TextAlignmentOptions.Center);
        }

        TMP_Text error = Text(root, "ErrorText");
        if (error != null)
        {
            Place(error.rectTransform, new Vector2(0f, -75f), new Vector2(1100f, 130f));
            StyleText(error, fontBold, 28f, Pink, TextAlignmentOptions.Center);
        }

        // 진짜 START는 창 아래쪽에 작게
        StyleButton(Find(root, "RealStartButton"), "START", new Vector2(260f, 72f), 36f, true, new Vector2(0f, -367f));

        HintBar(root, null, "입력이 <color=" + YellowHex + ">항상</color> 필요한 것은 아닙니다.");
    }

    // =====================================================
    // MG25 복합 실행 경로 탐색 검사
    // =====================================================
    static void Skin25(Transform root)
    {
        Common(root, 25,
            "복합 실행 경로 탐색 검사",
            "팝업을 정리하고 START를 GOAL까지 이동하세요.",
            null);

        RectTransform play = Find(root, "PlayArea");
        if (play != null)
        {
            Place(play, new Vector2(0f, -22f), new Vector2(1560f, 700f));
            SetImage(play, Load(ArtFrame), White);
        }

        RectTransform board = Find(root, "MazeBoard");
        if (board != null) SetImage(board, null, Clear); // 투명(클릭은 그대로 받음)

        RectTransform layer = Find(root, "PopupLayer");
        if (layer != null) layer.sizeDelta = new Vector2(1500f, 660f);

        // 미로 벽 색 (스크립트 변수 wallColor)
        Component maze = root.GetComponent("Minigame25_PopupMaze");
        if (maze != null)
        {
            var so = new SerializedObject(maze);
            SerializedProperty wall = so.FindProperty("wallColor");
            if (wall != null)
            {
                wall.colorValue = new Color32(240, 246, 255, 255);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        RectTransform start = Find(root, "StartButton");
        if (start != null)
        {
            SetImage(start, null, Pink);
            foreach (TMP_Text t in start.GetComponentsInChildren<TMP_Text>(true))
            {
                t.text = "START";
                StyleText(t, fontBold, t.fontSize, White, TextAlignmentOptions.Center);
            }
        }

        RectTransform goal = Find(root, "GoalZone");
        if (goal != null)
        {
            SetImage(goal, Load(ArtButton), White);
            TMP_Text goalText = Text(goal, "GoalText");
            if (goalText != null)
            {
                Place(goalText.rectTransform, Vector2.zero, goal.sizeDelta);
                StyleText(goalText, fontBold, 30f, Yellow, TextAlignmentOptions.Center);
            }
        }

        foreach (string popupName in new[] { "NormalPopup", "MovePopup", "TrapPopup" })
        {
            RectTransform popup = Find(root, popupName);
            if (popup == null) continue;

            SetImage(popup, Load(ArtWindow), White);

            RectTransform titleBar = Find(popup, "TitleBar");
            if (titleBar != null)
            {
                titleBar.anchorMin = titleBar.anchorMax = new Vector2(0f, 1f);
                titleBar.pivot = new Vector2(0.5f, 0.5f);
                titleBar.anchoredPosition = new Vector2(popup.sizeDelta.x * 0.5f, -28f);
                titleBar.sizeDelta = new Vector2(popup.sizeDelta.x - 14f, 44f);
                SetImage(titleBar, null, new Color32(8, 18, 40, 255));
            }

            TMP_Text title = Text(popup, "PopupTitle");
            if (title != null) StyleText(title, fontRegular, 26f, White, TextAlignmentOptions.Center);

            TMP_Text body = Text(popup, "PopupBodyText");
            if (body != null) StyleText(body, fontRegular, 22f, White, TextAlignmentOptions.Center);

            RectTransform close = Find(popup, "CloseButton");
            if (close != null)
            {
                close.sizeDelta = new Vector2(34f, 34f);
                close.anchoredPosition = new Vector2(close.anchoredPosition.x, 0f);
                SetImage(close, Load(ArtX), White);
                foreach (Transform c in close) c.gameObject.SetActive(false); // 글자 X 숨김 (그림 X 사용)
            }
        }

        HintBar(root, null, "팝업을 치우고 <color=" + YellowHex + ">안전한 길</color>을 찾으세요.");
    }

    // =====================================================
    // 공통: 배경 프레임 + 위쪽 제목줄 + 맨 아래 작은 글씨
    // =====================================================
    static void Common(Transform root, int n, string title, string subtitle, string footnote)
    {
        // 1) 전체 배경 → 네온 프레임 그림
        RectTransform bg = FindBackground(root);
        if (bg != null) SetImage(bg, Load(ArtBg), White);

        // 2) 예전 제목 글자("MG2x | ...") 숨기기
        foreach (Transform c in root)
        {
            TMP_Text t = c.GetComponent<TMP_Text>();
            if (t != null && t.text.Contains("MG" + n))
                c.gameObject.SetActive(false);
        }

        // 3) 새 제목줄
        RectTransform header = NewRect(root, Prefix + "Header", Vector2.zero, Vector2.zero);
        header.anchorMin = Vector2.zero;
        header.anchorMax = Vector2.one;
        header.offsetMin = header.offsetMax = Vector2.zero;
        header.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);

        NewImage(header, Prefix + "Badge", Load(ArtButton), White, BadgePos, BadgeSize);
        TMP_Text badge = NewText(header, Prefix + "BadgeText", "MG" + n, fontBold, 44f, White,
            TextAlignmentOptions.Center, BadgePos, BadgeSize - new Vector2(10f, 6f));
        badge.characterSpacing = 4f;

        NewImage(header, Prefix + "TitleBar", null, Pink, new Vector2(-660f, 388f), new Vector2(6f, 66f));

        NewText(header, Prefix + "Title", title, fontBold, 48f, White, TextAlignmentOptions.Left,
            new Vector2(TitleLeftX, TitleY), new Vector2(1150f, 64f), new Vector2(0f, 0.5f));
        NewText(header, Prefix + "Subtitle", subtitle, fontRegular, 28f, White, TextAlignmentOptions.Left,
            new Vector2(TitleLeftX, SubtitleY), new Vector2(1150f, 40f), new Vector2(0f, 0.5f));

        // 4) 맨 아래 작은 회색 글씨
        if (!string.IsNullOrEmpty(footnote))
        {
            TMP_Text foot = NewText(root, Prefix + "Footnote", footnote, fontRegular, 24f,
                new Color(Pale.r, Pale.g, Pale.b, 0.85f), TextAlignmentOptions.Center,
                new Vector2(0f, FootnoteY), new Vector2(1200f, 36f));
            foot.transform.SetAsLastSibling();
        }
    }

    // 프레임 안쪽 네온 패널 (배경 바로 위에 깔림)
    static void AddFrame(Transform root, Vector2 pos, Vector2 size)
    {
        Image frame = NewImage(root, Prefix + "Frame", Load(ArtFrame), White, pos, size);
        frame.raycastTarget = false;
        RectTransform bg = FindBackground(root);
        frame.transform.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
    }

    // 아래쪽 ">>  안내 문구  <<" 줄
    // existing: 스크립트가 쓰는 기존 글자(있으면 그걸 가운데에 넣음), staticText: 새로 만들 문구
    static void HintBar(Transform root, TMP_Text existing, string staticText)
    {
        RectTransform bar = NewRect(root, Prefix + "HintBar", new Vector2(0f, HintY), new Vector2(1700f, 60f));
        var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 26f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        NewText(bar, Prefix + "ArrowL", ">>", fontBold, 40f, Pink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(60f, 60f));

        TMP_Text middle = existing;
        if (middle == null)
        {
            middle = NewText(bar, Prefix + "HintText", staticText, fontRegular, 36f, White,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(800f, 60f));
        }
        else
        {
            middle.transform.SetParent(bar, false);
            middle.gameObject.SetActive(true);
            StyleText(middle, fontRegular, 36f, White, TextAlignmentOptions.Center);
        }
        middle.richText = true;
        middle.transform.SetSiblingIndex(1);

        NewText(bar, Prefix + "ArrowR", "<<", fontBold, 40f, Pink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(60f, 60f));

        bar.SetAsLastSibling();
    }

    // 버튼: 24_button 그림 + 흰 글자
    static void StyleButton(RectTransform button, string label, Vector2 size, float fontSize, bool setPos, Vector2 pos = default)
    {
        if (button == null) return;
        if (setPos) Place(button, pos, size);
        else button.sizeDelta = size;

        SetImage(button, Load(ArtButton), White);
        foreach (TMP_Text t in button.GetComponentsInChildren<TMP_Text>(true))
        {
            t.text = label;
            StyleText(t, fontBold, fontSize, White, TextAlignmentOptions.Center);
        }
    }

    // 글자 블록: S/T/A/R 그림을 보이게 하고 안쪽 글자는 숨김
    static void LetterArt(RectTransform block, string artPath, Vector2 size)
    {
        if (block == null) return;
        block.sizeDelta = size;
        Image img = SetImage(block, Load(artPath), White);
        if (img != null) img.preserveAspect = true;
        foreach (TMP_Text t in block.GetComponentsInChildren<TMP_Text>(true))
            t.gameObject.SetActive(false);
    }

    // =====================================================
    // 도우미
    // =====================================================
    static void Backup(string path, int n)
    {
        if (!AssetDatabase.IsValidFolder(BackupDir))
            AssetDatabase.CreateFolder("Assets/Prefab", "_Ch5Backup");

        string backup = BackupDir + "/Minigame_" + n + "Panel_원본.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(backup) == null)
            AssetDatabase.CopyAsset(path, backup);
    }

    // 예전에 이 도구가 만든 C5_ 오브젝트 지우기 (안에 넣어 둔 원래 오브젝트는 밖으로 꺼냄)
    static void Cleanup(Transform root)
    {
        var made = new List<Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name.StartsWith(Prefix)) made.Add(t);

        foreach (Transform t in made)
        {
            if (t == null) continue;
            var keep = new List<Transform>();
            foreach (Transform c in t)
                if (!c.name.StartsWith(Prefix)) keep.Add(c);
            foreach (Transform c in keep) c.SetParent(root, false);
        }
        foreach (Transform t in made)
            if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static RectTransform FindBackground(Transform root)
    {
        foreach (Transform c in root)
        {
            if (c.name != "BG" && c.name != "Image") continue;
            RectTransform rt = c as RectTransform;
            if (rt != null && c.GetComponent<Image>() != null) return rt;
        }
        return null;
    }

    static Sprite Load(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null && !missing.Contains(path)) missing.Add(path);
        return s;
    }

    static RectTransform Find(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim() == name) return t as RectTransform;
        return null;
    }

    static TMP_Text Text(Transform parent, string name)
    {
        RectTransform rt = Find(parent, name);
        return rt != null ? rt.GetComponent<TMP_Text>() : null;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image SetImage(RectTransform rt, Sprite sprite, Color color)
    {
        Image img = rt.GetComponent<Image>();
        if (img == null) return null;
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = color;
        return img;
    }

    static void StyleText(TMP_Text t, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        if (font != null)
        {
            t.font = font;
            t.fontSharedMaterial = font.material;
        }
        t.fontSize = size;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
    }

    static RectTransform NewRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        Place(rt, pos, size);
        return rt;
    }

    static Image NewImage(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = NewRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text NewText(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions align, Vector2 pos, Vector2 boxSize, Vector2? pivot = null)
    {
        RectTransform rt = NewRect(parent, name, pos, boxSize);
        if (pivot.HasValue) Place(rt, pos, boxSize, pivot);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.richText = true;
        t.raycastTarget = false;
        StyleText(t, font, size, color, align);
        return t;
    }
}
