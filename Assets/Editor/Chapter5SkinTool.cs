using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;

// 메뉴: 상단 메뉴바 > PRESS START > 챕터5 UI 새 디자인 적용
// 챕터5 미니게임(MG21~MG25) 프리팹 디자인
//  - 화면 틀은 기존 미니게임(챕터1~4)과 똑같이:
//      배경, 글꼴, 왼쪽 위 MG 배지/미니게임 이름/설명/별/아이콘, 오른쪽 위 설정 버튼,
//      아래쪽 안내 문구 위치 → MG07 프리팹에서 그대로 복사
//  - 가운데 게임 화면은 챕터5 느낌: 현민님 Chapter5 네온 판/버튼/로딩바/팝업 그림
//  - MG21 약관 내용(길게 + 가짜 START)도 여기서 넣음
//  - 스크립트가 이름으로 찾는 오브젝트는 이름을 바꾸거나 지우지 않는다 (위치/색/그림만 바꿈)
//  - 적용 전 지금 프리팹을 Assets/Prefab/_Ch5Backup 폴더에 복사해 둔다
//  - 여러 번 눌러도 결과는 같다 (C5_ 로 시작하는 꾸밈 오브젝트를 지우고 다시 만듦)
//  - 자동 적용: 유니티가 이 스크립트를 처음 읽을 때(컴파일 끝난 뒤) 한 번 자동으로 실행됨
//    (Assets/Editor/Chapter5SkinTool_applied.txt 에 적용 버전을 적어 두고, 같은 버전이면 다시 안 함)
[InitializeOnLoad]
public static class Chapter5SkinTool
{
    // 디자인을 고칠 때마다 숫자를 올리면 → 다음에 유니티를 열 때 자동으로 다시 적용됨
    const string SkinVersion = "2026-11-16-v14-popups";
    const string MarkerPath = "Assets/Editor/Chapter5SkinTool_applied.txt";

    static Chapter5SkinTool()
    {
        EditorApplication.delayCall += AutoApply;
    }

    static void AutoApply()
    {
        if (Application.isBatchMode)
            return;

        // 컴파일/임포트/플레이 중이면 잠시 뒤에 다시
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += AutoApply;
            return;
        }

        string done = System.IO.File.Exists(MarkerPath) ? System.IO.File.ReadAllText(MarkerPath).Trim() : "";
        if (done == SkinVersion)
            return;

        Debug.Log("[Chapter5] 챕터5 미니게임 UI 자동 적용 시작 (" + SkinVersion + ")");
        Apply();
        System.IO.File.WriteAllText(MarkerPath, SkinVersion);
        AssetDatabase.ImportAsset(MarkerPath);
    }

    const string PrefabDir = "Assets/Prefab/";
    const string BackupDir = "Assets/Prefab/_Ch5Backup";
    const string Prefix = "C5_";

    // 기존 미니게임 화면 틀을 복사해 올 프리팹 (MG07 약관 스크롤)
    const string RefPrefabPath = "Assets/Prefab/Minigame_7Panel.prefab";
    // MG22 설정 화면을 똑같이 맞출 기존 설정창 프리팹
    const string SettingsPrefabPath = "Assets/Prefab/SettingsSystem.prefab";
    const string GuidSettingsBg = "e2e24da9b01966d4ba6873370ac82637";    // Subin/NEW ART/설정배경2.png (기존 설정창 배경)
    const string GuidTabNormal = "0712e11f31e74b62bdbcd488a36a4a0f";     // 기존 설정창 탭 (보통)
    const string GuidTabSelected = "69b40ef393ea4868b4bd870bd45acb83";   // 기존 설정창 탭 (선택 - 노란 테두리)

    // ---------- 사용하는 아트 (파일 이름이 바뀌어도 찾을 수 있게 GUID 로 불러옴) ----------
    const string GuidPanelBg = "51757ff398d5c0149a148417c6d738c7";       // Subin/NEW ART/배경 1.PNG (기존 미니게임 공통 배경)
    // 현민님 아트
    const string GuidPanelLine = "83894b73fb79be7459435a48f9bbcd02";     // Hyunmin/Art/Chapter5/23_line.png (네온 판 테두리)
    const string GuidPanelGrid = "12f91537b161fa642ac045f7aa9226f8";     // Hyunmin/Art/Chapter5/23_line_back.png (점 무늬 네온 판)
    const string Guid24Button = "9f1cfa4cf22b263488fb90350d9be8c7";      // Hyunmin/Art/Chapter5/24_button.png (COMPLETE)
    const string GuidArrowBox = "bf5a4395487b9f4498895636eae92ba5";      // Hyunmin/Art/Chapter5/23_button.png (▶ ◀ 상자)
    const string GuidWinBlue = "a8bb646f9607af942999bf1c82734d4c";       // Hyunmin/Art/Chapter5/25_window_blue.png
    const string GuidWinYellow = "19681b0e49cfa04438a4853df705b623";     // Hyunmin/Art/Chapter5/25_window_yellow.png
    const string GuidWinPink = "ecfc0de88b2681042aff9b7a0bbd22e9";       // Hyunmin/Art/Chapter5/25_window_pink.png
    const string GuidBarFrame = "451e84e5bfa437b42aecc7e2d591ce4a";      // Hyunmin/Art/Chapter5/24_loading_bar_white.png
    const string GuidBarFill = "b2971788f817fbf41946916a255aba20";       // Hyunmin/Art/Chapter5/24_loading_prograss__nottrim.png
    const string GuidNormalButton = "3f33695ed66ec7e43ae5c2de29d20e79";  // Hyunmin/Art/normal_button.png (기존 START 버튼)
    const string GuidXButton = "55c7b5a9b509a9d4591133461be80d30";       // Hyunmin/Art/Xbutton.png
    const string GuidWindow = "a93dfdf18815ddb408d4b23fc0ae2f2f";        // Hyunmin/Art/06_window.png
    const string GuidSlot = "d150ab02426ae12448cd046b6869bafc";          // Hyunmin/Art/Chapter1/find_blank.png
    const string GuidDialogButton = "b5a648175bc2d4c42a6716ac1d2c7183";  // Hyunmin/Art/미니게임버튼.png (MG06 팝업 버튼)

    // S/T/A/R 글자 (현민님 Chapter1 폴더)
    const string ArtS = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/S.png";      // 파란 S
    const string ArtTBlue = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/T2.png"; // 파란 T
    const string ArtA = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/A.png";      // 분홍 A
    const string ArtR = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/R.png";      // 분홍 R
    const string ArtTYellow = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/T.png"; // 노란 T
    // 노란 알파벳 (MG22 숨은 글자 / 모은 글자 칸)
    const string ArtSYellow = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/S_yellow.png";
    const string ArtAYellow = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/A_yellow.png";
    const string ArtRYellow = "Assets/1. Team Folder/Hyunmin/Art/Chapter1/R_yellow.png";

    // 왼쪽 위 능력 아이콘 (기존 미니게임과 같은 아이콘 세트, Youngwoo/art/11/icon)
    const string IconObservation = "b5156f46d4b8fbb49996cd7b91e07452";   // 관찰력 (MG07과 같음)
    const string IconBrain = "8236e7ce8f45d9748a60cf7460b5e286";         // 두뇌
    const string IconReaction = "6ee370cbc4b0f624bbe4c8e3502b693a";      // 반응속도
    const string IconTiming = "7b3d212166746af4da55c8f887c953d7";        // 타이밍
    const string IconSpatial = "b3235ce21ef5a5a45aa0faae83f07cdc";       // 공간지각 (MG05와 같음)

    // ---------- 색 ----------
    static readonly Color White = Color.white;
    static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
    static readonly Color Pink = new Color32(255, 45, 149, 255);
    static readonly Color Cyan = new Color32(25, 200, 240, 255);
    static readonly Color Yellow = new Color32(255, 232, 26, 255);
    static readonly Color Navy = new Color32(11, 26, 58, 255);
    static readonly Color Pale = new Color32(169, 188, 224, 255);
    const string YellowHex = "#FFE81A";

    // ---------- 기존 미니게임 배치 (1920x1080 캔버스 기준, 가운데가 0,0) ----------
    static readonly Vector2 BottomTextPos = new Vector2(0f, -430f);   // MG07 StatusText / MG05 HintText 자리
    static readonly Vector2 BottomTextSize = new Vector2(1400f, 60f);
    static readonly Vector2 TimerPos = new Vector2(640f, 447f);       // 오른쪽 위 (설정 버튼 왼쪽)
    static readonly Vector2 TimerSize = new Vector2(300f, 50f);
    const float WindowHeaderRatio = 0.147f;                            // 현민님 06_window 그림의 제목줄 높이 비율

    static readonly List<string> missing = new List<string>();

    // MG07 기준 오브젝트 (글꼴/색을 여기서 가져옴)
    static GameObject refRoot;
    static TMP_Text refStatus, refTitle, refGuide, refButtonText;

    [MenuItem("PRESS START/챕터5 UI 새 디자인 적용")]
    public static void Apply()
    {
        missing.Clear();

        refRoot = AssetDatabase.LoadAssetAtPath<GameObject>(RefPrefabPath);
        if (refRoot == null)
        {
            EditorUtility.DisplayDialog("챕터5 UI", "기준 프리팹을 찾지 못했어요:\n" + RefPrefabPath, "확인");
            return;
        }
        refStatus = Text(refRoot.transform, "StatusText");
        refTitle = Text(refRoot.transform, "TitleText");
        refGuide = Text(refRoot.transform, "GuideText");
        RectTransform refStartButton = Find(refRoot.transform, "HiddenStartButton");
        refButtonText = refStartButton != null ? refStartButton.GetComponentInChildren<TMP_Text>(true) : null;

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
                SetTimeLimit(root.transform, n);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                done.Add("MG" + n + " (" + TimeLimitFor(n) + "초)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();

        string msg = "적용 완료: " + string.Join(", ", done) +
                     "\n\n화면 틀은 기존 미니게임과 같게, 가운데는 챕터5(현민님 그림) 느낌으로 바꿨어요." +
                     "\n바로 전 모습은 " + BackupDir + " 폴더에 '_시안디자인' 이름으로 백업되어 있어요.";
        if (missing.Count > 0)
            msg += "\n\n찾지 못한 파일 (이 부분은 건너뜀):\n- " + string.Join("\n- ", missing);
        EditorUtility.DisplayDialog("챕터5 UI", msg, "확인");
    }

    // =====================================================
    // MG21 약관 속에 숨은 시작  (MG07 약관 스크롤 참고)
    // =====================================================
    // 약관 안에 'START' 글자는 딱 하나 (제12조 문장 속 숨은 단어)뿐
    const string TermsText =
        "<size=34><b>실행 시스템 이용약관 및 데이터 검사 동의서</b></size>\n" +
        "<size=22>(버전 5.0.21 - 일부 데이터가 손상되었을 수 있습니다)</size>\n\n" +

        "<b>제1조 (목적)</b>\n" +
        "본 약관은 게임 실행 시스템(이하 \"시스템\")의 이용 절차와 실행 데이터 검사 방법을 규정합니다. " +
        "이용자는 약관의 내용을 확인한 뒤에만 다음 단계로 진행할 수 있습니다.\n\n" +

        "<b>제2조 (용어의 정의)</b>\n" +
        "1. \"실행 버튼\"이란 게임을 시작하기 위해 누르는 버튼을 말합니다.\n" +
        "2. \"가짜 버튼\"이란 실행 버튼처럼 보이지만 눌러도 아무 일도 일어나지 않는 표시를 말합니다.\n" +
        "3. \"손상 데이터\"란 오류로 인해 원래 자리에서 벗어나 다른 문장 사이에 섞여 버린 데이터를 말합니다.\n\n" +

        "<b>제3조 (약관의 효력)</b>\n" +
        "본 약관은 화면에 표시된 순간부터 효력이 발생하며, 이용자가 스크롤을 내리는 속도와 관계없이 동일하게 적용됩니다.\n\n" +

        "<b>제4조 (이용자의 의무)</b>\n" +
        "이용자는 화면에 표시된 모든 안내 사항을 직접 읽어야 합니다. " +
        "버튼처럼 보이는 글자를 무작정 누르는 행위는 검사 결과에 아무런 영향을 주지 않습니다.\n\n" +

        "<b>제5조 (정보 처리)</b>\n" +
        "시스템은 이용자의 클릭, 마우스 위치, 머뭇거린 시간을 내부 검사 절차에 사용할 수 있습니다. " +
        "수집된 정보는 검사가 끝나면 즉시 폐기됩니다.\n\n" +

        "<b>제6조 (검사 과정)</b>\n" +
        "모든 검사는 정해진 절차에 따라 진행됩니다. " +
        "단, 챕터 5 이후의 검사는 일부 절차가 손상된 상태로 진행될 수 있습니다.\n\n" +

        "<b>제7조 (오류 현상)</b>\n" +
        "화면이 깜빡이거나 글자가 흔들리는 현상은 정상적인 오류입니다. " +
        "이용자는 이를 신고하지 않아도 되며, 오히려 주의 깊게 살펴보는 것을 권장합니다.\n\n" +

        "<b>제8조 (재시작 정책)</b>\n" +
        "검사에 실패한 이용자는 재시작 절차에 따라 처음부터 다시 시작합니다.\n\n" +

        "<b>제9조 (데이터 분류)</b>\n" +
        "입력 데이터는 검사 결과에 따라 \"정상\", \"보류\", \"손상\"으로 분류될 수 있습니다. " +
        "\"손상\"으로 분류된 데이터는 대기 상태로 보관됩니다.\n\n" +

        "<b>제10조 (처리 순서)</b>\n" +
        "시스템이 제공하는 처리 순서는 사전 안내 없이 변경될 수 있습니다. " +
        "이용자는 화면에 보이는 순서가 항상 올바르다고 가정해서는 안 됩니다.\n\n" +

        "<b>제11조 (데이터 복구)</b>\n" +
        "손상 데이터는 복구되는 과정에서 원래 문장 사이에 끼어들 수 있습니다. " +
        "복구된 데이터는 주변 글자와 똑같은 모양으로 표시되므로 겉모습만으로는 구별하기 어렵습니다.\n\n" +

        "<b>제12조 (실행 조건)</b>\n" +
        "특정 조건이 충족되면 새로운 실행 단계가 제공될 수 있습니다. " +
        "시스템은 <link=\"hidden_start\">START</link> 상태의 검사 기록을 내부 처리 절차에 포함할 수 있으며, " +
        "이 기록은 이용자가 직접 확인한 경우에만 유효합니다.\n\n" +

        "<b>제13조 (버튼의 위치)</b>\n" +
        "실행 버튼은 항상 화면 아래쪽이나 문서의 마지막에 있다고 보장하지 않습니다.\n\n" +

        "<b>제14조 (기록 보관)</b>\n" +
        "검사 기록은 시스템 내부에 임시로 저장될 수 있으며, 이용자는 언제든지 열람을 요청할 수 있습니다. " +
        "단, 열람 요청 버튼은 현재 점검 중입니다.\n\n" +

        "<b>제15조 (책임의 제한)</b>\n" +
        "이용자가 가짜 버튼을 눌러 발생한 시간 낭비에 대해 시스템은 책임지지 않습니다.\n\n" +

        "<b>제16조 (약관의 변경)</b>\n" +
        "시스템은 필요한 경우 약관의 일부 조항을 예고 없이 손상시킬 수 있습니다.\n\n" +

        "<b>제17조 (검사 종료)</b>\n" +
        "문서의 마지막에 도달하는 행위는 검사 완료를 보장하지 않습니다. " +
        "마지막 줄에는 실행 버튼이 없습니다.\n\n" +

        "<b>부칙</b>\n" +
        "본 약관은 2026년 11월부터 시행합니다.\n\n\n" +
        "약관 확인 완료\n" +
        "실행 데이터 : 찾을 수 없음";

    static void Skin21(Transform root)
    {
        Common(root, 21,
            "약관 속 숨은 시작 검사",
            "긴 약관을 읽고, 문장 속에 숨어 있는 시작 단어를 찾아 클릭한다.",
            "★★★★☆", IconObservation);

        // 가운데 약관 판 (남색 + 현민님 네온 테두리)
        AddPanel(root, new Vector2(0f, -10f), new Vector2(1318f, 660f));

        RectTransform scroll = Find(root, "TermsScrollView");
        if (scroll != null)
        {
            Place(scroll, new Vector2(8f, 20f), new Vector2(1150f, 440f));
            SetImage(scroll, null, Clear);
        }

        RectTransform bar = Find(root, "Scrollbar Vertical");
        if (bar != null)
        {
            bar.sizeDelta = new Vector2(18f, bar.sizeDelta.y);
            SetImage(bar, null, new Color32(20, 40, 85, 255));
            Outline o = bar.GetComponent<Outline>();
            if (o != null) Object.DestroyImmediate(o);
        }
        RectTransform handle = Find(root, "Handle");
        if (handle != null) SetImage(handle, null, Cyan);

        // 약관 내용 (길게 + 가짜 START 여러 개 + 진짜는 제12조 문장 속)
        TMP_Text terms = Text(root, "TremText");
        if (terms != null)
        {
            terms.text = TermsText;
            terms.richText = true;
            StyleLike(terms, refGuide, 26f, Pale, TextAlignmentOptions.TopLeft);
            terms.textWrappingMode = TextWrappingModes.Normal;
            terms.lineSpacing = 12f;
            terms.rectTransform.sizeDelta = new Vector2(terms.rectTransform.sizeDelta.x, 3600f);
            RectTransform content = Find(root, "Content");
            if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, 3700f);
        }

        // 약관 아래 가는 줄 + START DATA 상태
        NewImage(root, Prefix + "Divider", null, new Color(Pale.r, Pale.g, Pale.b, 0.7f),
            new Vector2(0f, -232f), new Vector2(1100f, 3f));

        TMP_Text status = Text(root, "STATUSTEXT");
        if (status != null)
        {
            Place(status.rectTransform, new Vector2(0f, -268f), new Vector2(1000f, 40f));
            StyleLike(status, refGuide, 24f, Pale, TextAlignmentOptions.Center);
            status.characterSpacing = 4f;
            status.transform.SetAsLastSibling();
        }

        TMP_Text time = Text(root, "TimeText");
        if (time != null) PlaceTime(time);

        StartButton(Find(root, "RevealedStartButton"), new Vector2(260f, 60f), 28f, false);

        // 힌트 시간/문구 (스크립트 변수)
        Component game = root.GetComponent("Minigame21_HiddenTerms");
        if (game != null)
        {
            var so = new SerializedObject(game);
            SetFloat(so, "firstHintTime", 15f);
            SetFloat(so, "secondHintTime", 30f);
            SetFloat(so, "thirdHintTime", 40f);
            SetFloat(so, "finalHintTime", 50f);
            SetString(so, "firstHint", "힌트: 끝까지 내린다고 정답이 나오지는 않습니다.");
            SetString(so, "secondHint", "힌트: 약관 중간의 조항을 천천히 다시 읽어 보세요.");
            SetString(so, "thirdHint", "힌트: 제12조 (실행 조건) 안에서 깜빡이는 단어를 찾으세요.");
            SetString(so, "finalHint", "힌트: 노랗게 빛나는 단어를 클릭하세요!");
            // 시간 표시는 다른 챕터와 같은 타이머(MinigameTimerUnifier)가 남은 시간으로 보여 줌
            SerializedProperty timeProp = so.FindProperty("timeText");
            if (timeProp != null) timeProp.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 스크립트가 바꾸는 힌트 문구 → 아래쪽 안내 문구 자리
        BottomText(root, Text(root, "HINTTEXT"), null);
    }

    // =====================================================
    // MG22 흩어진 START  (설정 화면)
    // =====================================================
    static void Skin22(Transform root)
    {
        Common(root, 22,
            "설정 데이터 복구 검사",
            "설정 화면 곳곳에 숨어 있는 START 글자를 모두 찾는다.",
            "★★★★☆", IconBrain);

        // ----- 기존 설정창(SettingsSystem)과 똑같이 -----
        // 배경 = 기존 설정창 배경
        RectTransform bg22 = FindBackground(root);
        if (bg22 != null) SetImage(bg22, LoadGuid(GuidSettingsBg), White);

        RectTransform window = Find(root, "SettingsWindow");
        if (window != null) SetImage(window, null, Clear);

        // 기존 설정창의 SETTINGS 제목 / 제목 옆 줄 / 아래 구분선을 그대로 복사
        RectTransform settingsTitle = Find(root, "SettingsTitle");
        if (settingsTitle != null) settingsTitle.gameObject.SetActive(false);

        GameObject settingsRef = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
        if (settingsRef == null) missing.Add(SettingsPrefabPath);
        RectTransform refPanel = settingsRef != null ? Find(settingsRef.transform, "SettingsPanel") : null;

        // 탭 / 페이지 안의 모든 오브젝트를 기존 설정창의 같은 이름 오브젝트와 똑같이 (위치·크기·그림·글꼴)
        RectTransform tabs = Find(root, "Tabs");
        RectTransform pages = Find(root, "Pages");
        if (refPanel != null)
        {
            RectTransform refTabs = Find(refPanel, "Tabs");
            RectTransform refPages = Find(refPanel, "Pages");
            if (tabs != null && refTabs != null)
            {
                tabs.anchoredPosition = refTabs.anchoredPosition;
                MatchChildren(tabs, refTabs);
            }
            if (pages != null && refPages != null)
            {
                pages.anchoredPosition = refPages.anchoredPosition;
                MatchChildren(pages, refPages);
            }
        }

        // 기존 설정창과 같은 탭 그림 (선택된 탭 = 노란 테두리)
        Component game22 = root.GetComponent("Minigame22_ScatteredStart");
        if (game22 != null)
        {
            var so22 = new SerializedObject(game22);
            SerializedProperty tn = so22.FindProperty("tabNormalSprite");
            SerializedProperty ts = so22.FindProperty("tabSelectedSprite");
            if (tn != null) tn.objectReferenceValue = LoadGuid(GuidTabNormal);
            if (ts != null) ts.objectReferenceValue = LoadGuid(GuidTabSelected);
            so22.ApplyModifiedPropertiesWithoutUndo();
        }

        // INFO 글자 상자를 넓힘 (상자가 좁으면 A / 마지막 T 가 상자 밖이라 클릭이 안 됨)
        string[] infoTexts = { "AboutValue", "InfoDetailText" };
        foreach (string infoName in infoTexts)
        {
            TMP_Text info = Text(root, infoName);
            if (info == null) continue;
            RectTransform rt = info.rectTransform;
            float left = rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x;
            rt.sizeDelta = new Vector2(760f, rt.sizeDelta.y);
            rt.anchoredPosition = new Vector2(left + 760f * rt.pivot.x, rt.anchoredPosition.y);
            info.alignment = TextAlignmentOptions.Left;
            info.textWrappingMode = TextWrappingModes.NoWrap;
            info.raycastTarget = true;
        }

        // 숨은 S / T / R → 현민님 노란 알파벳 그림
        LetterImage(Find(root, "HiddenSButton"), ArtSYellow, new Vector2(56f, 68f));
        RectTransform hiddenT = Find(root, "HiddenT1Button");
        if (hiddenT != null) hiddenT.anchoredPosition = new Vector2(hiddenT.anchoredPosition.x, 0f);
        LetterImage(hiddenT, ArtTYellow, new Vector2(56f, 68f));

        // 기존 설정창의 SETTINGS 제목 / 제목 옆 줄 / 아래 구분선 / BACK 버튼을 그대로 복사
        int afterBg = bg22 != null ? bg22.GetSiblingIndex() + 1 : 0;
        string[] copyNames = { "SETTINGS", "TitleLine", "Divider", "BackButton" };
        foreach (string copyName in copyNames)
        {
            Transform c = CopyFrom(settingsRef, root, copyName);
            if (c == null) continue;
            c.SetSiblingIndex(afterBg++);
            // BACK 버튼: 모양만 (미니게임 안에서는 눌러도 아무 일 없음)
            Button back = c.GetComponent<Button>();
            if (back != null) back.onClick = new Button.ButtonClickedEvent();
            // BACK 버튼은 글자 칸과 겹치지 않게 조금 아래로
            if (copyName == "BackButton")
            {
                RectTransform backRt = c as RectTransform;
                if (backRt != null) backRt.anchoredPosition = new Vector2(backRt.anchoredPosition.x, -340f);
            }
        }

        // 모은 글자 칸: 탭과 상관없이 항상 보이는 한 줄 (구분선과 BACK 사이)
        //  - 빈 칸(현민님 find_blank) 5개, 찾은 글자는 현민님 노란 알파벳 그림이 칸에 나타남
        //  - 예전 페이지별 글자 줄(StartSlot_ 글자)은 지움
        string[] slotNames = { "StartSlot_Display", "StartSlot_Sound", "StartSlot_Info", "StartSlot_Exit" };
        foreach (string slotName in slotNames)
        {
            RectTransform slot = Find(root, slotName);
            if (slot == null) continue;
            slot.name = slotName;   // 'StartSlot_Exit ' 처럼 뒤에 붙은 빈칸 제거
            TMP_Text slotText = slot.GetComponent<TMP_Text>();
            if (slotText != null) Object.DestroyImmediate(slotText);
        }

        RectTransform row = NewRect(root, Prefix + "LetterRow", new Vector2(0f, -272f), new Vector2(400f, 52f));
        if (pages != null) row.SetSiblingIndex(pages.GetSiblingIndex() + 1);
        string[] rowArts = { ArtSYellow, ArtTYellow, ArtAYellow, ArtRYellow, ArtTYellow };
        for (int i = 0; i < 5; i++)
        {
            Vector2 p = new Vector2((i - 2) * 66f, 0f);
            Image box = NewImage(row, Prefix + "LetterSlot_" + i, LoadGuid(GuidSlot), White, p, new Vector2(50f, 50f));
            box.preserveAspect = true;
            Image letter = NewImage(row, Prefix + "FoundLetter_" + i, Load(rowArts[i]), White, p, new Vector2(40f, 46f));
            letter.preserveAspect = true;
            letter.gameObject.SetActive(false);   // 찾으면 스크립트가 켬
        }

        Exit22(root);

        // 예전에 만든 TimeText(지난 시간 표시)는 지움 → 다른 챕터와 같은 남은 시간 타이머가 자동으로 생김
        RectTransform oldTime = Find(root, "TimeText");
        if (oldTime != null) Object.DestroyImmediate(oldTime.gameObject);

        // 아래쪽 안내 문구 (조합 화면이 열리면 가려지도록 AssemblyPanel 바로 앞에 둠)
        RectTransform assembly = Find(root, "AssemblyPanel");
        TMP_Text instruction = Text(root, "InstructionText");
        if (instruction != null)
        {
            BottomText(root, instruction, "숨어 있는 글자를 모아 <color=" + YellowHex + ">START</color>를 완성하세요.");
            if (assembly != null && assembly.parent == root)
                instruction.transform.SetSiblingIndex(assembly.GetSiblingIndex());
        }

        // ----- START 조합 화면 -----
        if (assembly != null)
        {
            foreach (Transform c in assembly)
            {
                Image img = c.GetComponent<Image>();
                RectTransform rt = c as RectTransform;
                if (img != null && c.name == "Image" && rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                    SetImage(rt, LoadGuid(GuidPanelBg), White);
            }

            // 글자 칸 판 (남색 + 네온 테두리)
            RectTransform slotPanel = NewRect(assembly, Prefix + "SlotPanel", new Vector2(-35f, -90f), new Vector2(980f, 420f));
            slotPanel.SetSiblingIndex(1);
            NewImage(slotPanel, Prefix + "SlotFill", null, Navy, Vector2.zero, new Vector2(930f, 370f));
            NewImage(slotPanel, Prefix + "SlotLine", LoadGuid(GuidPanelLine), White, Vector2.zero, new Vector2(980f, 420f));

            TMP_Text recTitle = Text(assembly, "START DATA RECOVERY");
            if (recTitle != null)
            {
                Place(recTitle.rectTransform, new Vector2(0f, 200f), new Vector2(1000f, 80f));
                StyleLike(recTitle, refTitle, 44f, White, TextAlignmentOptions.Center);
            }

            TMP_Text recGuide = Text(assembly, "START DATA RECOVERY (1)");
            if (recGuide != null)
            {
                Place(recGuide.rectTransform, BottomTextPos, BottomTextSize);
                StyleLike(recGuide, refStatus, 30f, StatusColor(), TextAlignmentOptions.Center);
            }

            StartButton(Find(assembly, "FinalStartButton"), new Vector2(300f, 70f), 32f, false);
        }

        for (int i = 0; i < 5; i++)
        {
            RectTransform slot = Find(root, "TargetSlot_" + i);
            if (slot != null)
            {
                slot.sizeDelta = new Vector2(120f, 140f);
                SetImage(slot, LoadGuid(GuidSlot), White);
            }
        }

        LetterArt(Find(root, "LetterBlock_S"), ArtS, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_T1"), ArtTBlue, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_A"), ArtA, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_R"), ArtR, new Vector2(120f, 140f));
        LetterArt(Find(root, "LetterBlock_T2"), ArtTYellow, new Vector2(120f, 140f));
    }

    // EXIT 페이지: 진짜 같은 "게임 종료" 팝업창이 메뉴를 가림
    //  → X / NO 를 누르거나 창을 옆으로 밀어 치우면 뒤에 숨은 R 이 보임
    static void Exit22(Transform root)
    {
        RectTransform exitPage = Find(root, "ExitPage");
        if (exitPage == null) return;

        // 메뉴 버튼 (ExitPage 안 기준)
        RectTransform newGame = Find(exitPage, "NewGameButton");
        if (newGame != null && newGame.parent == exitPage) newGame.anchoredPosition = new Vector2(0f, -59f);
        RectTransform quit = Find(exitPage, "QuitButton");
        if (quit != null) quit.anchoredPosition = new Vector2(0f, -139f);

        // 숨은 R: 메뉴 버튼 오른쪽 (팝업이 덮고 있는 자리)
        RectTransform hiddenR = Find(exitPage, "HiddenRButton");
        if (hiddenR != null)
        {
            Place(hiddenR, new Vector2(240f, -99f), new Vector2(64f, 76f));
            LetterImage(hiddenR, ArtRYellow, new Vector2(64f, 76f));
        }

        RectTransform popup = Find(exitPage, "ExitPopup");
        if (popup == null) return;

        // 불투명한 현민님 06_window 창 (제목줄 있는 창)
        Vector2 size = new Vector2(560f, 300f);
        Place(popup, new Vector2(60f, -99f), size);
        Image win = SetImage(popup, LoadGuid(GuidWindow), White);
        if (win != null) win.raycastTarget = true;   // 창을 끌어서 옮길 수 있게

        float headerH = size.y * WindowHeaderRatio;
        float headerY = size.y * 0.5f - headerH * 0.5f;

        // 원래 글자 → 질문만 가운데에
        foreach (Transform c in popup)
        {
            TMP_Text t = c.GetComponent<TMP_Text>();
            if (t == null || c.name.StartsWith(Prefix)) continue;
            t.text = "게임을 종료하시겠습니까?";
            Place(t.rectTransform, new Vector2(0f, 5f), new Vector2(size.x - 60f, 60f));
            StyleLike(t, refGuide, 32f, White, TextAlignmentOptions.Center);
            t.raycastTarget = false;
        }

        // 제목줄
        TMP_Text title = NewText(popup, Prefix + "PopupTitle", "시스템 알림", 24f, White, TextAlignmentOptions.Center,
            new Vector2(0f, headerY), new Vector2(size.x - 120f, headerH));
        StyleLike(title, refGuide, 24f, White, TextAlignmentOptions.Center);

        // X 버튼 → 창 닫기
        Button close = NewButton(popup, Prefix + "CloseButton", LoadGuid(GuidXButton), null,
            new Vector2(size.x * 0.5f - 32f, headerY), new Vector2(30f, 31f));
        UnityEventTools.AddBoolPersistentListener(close.onClick, new UnityAction<bool>(popup.gameObject.SetActive), false);

        // YES (아무 일도 안 일어남 - 가짜) / NO (창 닫기)
        Button yes = NewButton(popup, Prefix + "YesButton", LoadGuid(GuidDialogButton), "예",
            new Vector2(-110f, -80f), new Vector2(170f, 44f));
        // '예'는 가짜 → 누르면 틀린 것 (챕터5 틀린 횟수 +1)
        Minigame22_ScatteredStart game22 = root.GetComponent<Minigame22_ScatteredStart>();
        if (game22 != null)
            UnityEventTools.AddPersistentListener(yes.onClick, new UnityAction(game22.ReportWrongClick));
        Button no = NewButton(popup, Prefix + "NoButton", LoadGuid(GuidDialogButton), "아니요",
            new Vector2(110f, -80f), new Vector2(170f, 44f));
        UnityEventTools.AddBoolPersistentListener(no.onClick, new UnityAction<bool>(popup.gameObject.SetActive), false);
    }

    // mine 의 자식들을 ref 의 같은 이름 자식과 똑같이 맞춤 (글자 내용은 그대로 둠)
    static void MatchChildren(Transform mine, Transform reference)
    {
        var used = new HashSet<Transform>();
        foreach (Transform child in mine)
        {
            Transform match = null;
            foreach (Transform r in reference)
            {
                if (!used.Contains(r) && r.name.Trim() == child.name.Trim()) { match = r; break; }
            }
            if (match == null) continue;   // 미니게임에만 있는 것(숨은 글자 버튼, 팝업 등)은 그대로
            used.Add(match);

            RectTransform a = child as RectTransform;
            RectTransform b = match as RectTransform;
            if (a != null && b != null)
            {
                a.anchorMin = b.anchorMin;
                a.anchorMax = b.anchorMax;
                a.pivot = b.pivot;
                a.anchoredPosition = b.anchoredPosition;
                a.sizeDelta = b.sizeDelta;
                a.localScale = b.localScale;
            }

            Image ia = child.GetComponent<Image>();
            Image ib = match.GetComponent<Image>();
            if (ia != null && ib != null)
            {
                ia.sprite = ib.sprite;
                ia.color = ib.color;
                ia.type = ib.type;
                ia.preserveAspect = ib.preserveAspect;
            }

            TMP_Text ta = child.GetComponent<TMP_Text>();
            TMP_Text tb = match.GetComponent<TMP_Text>();
            if (ta != null && tb != null)
            {
                ta.font = tb.font;
                ta.fontSharedMaterial = tb.fontSharedMaterial;
                ta.fontSize = tb.fontSize;
                ta.fontStyle = tb.fontStyle;
                ta.color = tb.color;
                ta.alignment = tb.alignment;
                ta.enableAutoSizing = tb.enableAutoSizing;
                ta.characterSpacing = tb.characterSpacing;
                ta.textWrappingMode = tb.textWrappingMode;
                ta.overflowMode = tb.overflowMode;
            }

            MatchChildren(child, match);
        }
    }

    // 다른 프리팹의 오브젝트를 통째로 복사 (이름 앞에 C5_ 를 붙여 다시 적용할 때 지워지게)
    static Transform CopyFrom(GameObject refPrefab, Transform root, string refName)
    {
        if (refPrefab == null) return null;
        RectTransform src = Find(refPrefab.transform, refName);
        if (src == null)
        {
            missing.Add(refPrefab.name + " 프리팹의 " + refName);
            return null;
        }
        GameObject copy = Object.Instantiate(src.gameObject, root, false);
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
            t.name = Prefix + (t == copy.transform ? refName : t.name);
        copy.SetActive(true);
        return copy.transform;
    }

    static Button NewButton(Transform parent, string name, Sprite sprite, string label, Vector2 pos, Vector2 size)
    {
        Image img = NewImage(parent, name, sprite, White, pos, size);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text t = NewText(img.transform, Prefix + "Label", label, 22f, White, TextAlignmentOptions.Center,
                Vector2.zero, size);
            StyleLike(t, refGuide, 22f, White, TextAlignmentOptions.Center);
        }
        return b;
    }

    static TMP_Text NewText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, Vector2 pos, Vector2 boxSize)
    {
        RectTransform rt = NewRect(parent, name, pos, boxSize);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.richText = true;
        t.raycastTarget = false;
        StyleLike(t, refGuide, size, color, align);
        return t;
    }

    // =====================================================
    // MG23 제멋대로 START
    // =====================================================
    static void Skin23(Transform root)
    {
        Common(root, 23,
            "실행 문자 추적 검사",
            "제멋대로 움직이는 글자를 S-T-A-R-T 순서대로 클릭한다.",
            "★★★★☆", IconReaction);

        // 점 무늬 네온 판 (현민님 23_line_back)
        Image grid = NewImage(root, Prefix + "Panel", LoadGuid(GuidPanelGrid), White,
            new Vector2(0f, -15f), new Vector2(1560f, 660f));
        PutAfterBackground(root, grid.transform);

        RectTransform play = Find(root, "PlayArea");
        if (play != null) Place(play, new Vector2(0f, -60f), new Vector2(1480f, 480f));

        // CURRENT TARGET: 위에 작은 글씨 + 아래 작은 ▶ 알파벳 ◀ 상자 (상자 안에는 알파벳만)
        TMP_Text target = Text(root, "CurrentTargetText");
        if (target != null)
        {
            TMP_Text label = NewText(root, Prefix + "TargetLabel", "—  CURRENT TARGET  —", 22f, White,
                TextAlignmentOptions.Center, new Vector2(0f, 268f), new Vector2(400f, 30f));
            StyleLike(label, refTitle, 22f, White, TextAlignmentOptions.Center);
            label.characterSpacing = 2f;
            label.transform.SetAsLastSibling();

            Image box = NewImage(root, Prefix + "TargetBox", LoadGuid(GuidArrowBox), White,
                new Vector2(0f, 222f), new Vector2(170f, 60f));
            box.preserveAspect = true;
            box.transform.SetAsLastSibling();
            // 상자 안 알파벳 그림 (스크립트가 다음 글자 그림으로 바꿔 줌)
            Image targetLetter = NewImage(root, Prefix + "TargetLetter", Load(ArtS), White,
                new Vector2(0f, 222f), new Vector2(40f, 48f));
            targetLetter.preserveAspect = true;
            targetLetter.transform.SetAsLastSibling();

            Place(target.rectTransform, new Vector2(0f, 222f), new Vector2(80f, 50f));
            StyleLike(target, refTitle, 38f, White, TextAlignmentOptions.Center);
            target.richText = true;
            target.transform.SetAsLastSibling();
        }

        TMP_Text startText = Text(root, "StartText");
        if (startText != null) StyleLike(startText, refTitle, startText.fontSize, White, startText.alignment);

        LetterArt(Find(root, "Letter_S"), ArtS, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_T1"), ArtTBlue, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_A"), ArtA, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_R"), ArtR, new Vector2(120f, 146f));
        LetterArt(Find(root, "Letter_T2"), ArtTYellow, new Vector2(120f, 146f));

        StartButton(Find(root, "FinalStartButton"), new Vector2(340f, 80f), 36f, false);

        // 아래 "S - T - A - _ - _" 진행 글자는 숨김 (스크립트는 계속 써도 화면엔 안 보임)
        TMP_Text progress = Text(root, "ProgressText");
        if (progress != null) progress.gameObject.SetActive(false);

        BottomText(root, null, "순서가 틀리면 <color=" + YellowHex + ">처음부터</color> 다시 시작합니다.");
    }

    // =====================================================
    // MG24 99%에서 기다리기
    // =====================================================
    static void Skin24(Transform root)
    {
        Common(root, 24,
            "실행 대기 판단 검사",
            "99%에서 멈춘 로딩 화면에서 올바른 때를 기다렸다가 START를 누른다.",
            "★★★★☆", IconTiming);

        Vector2 windowPos = new Vector2(0f, 0f);
        Vector2 windowSize = new Vector2(1318f, 620f);
        AddPanel(root, windowPos, windowSize);

        RectTransform window = Find(root, "LoadingWindow");
        if (window != null)
        {
            Place(window, windowPos, windowSize);
            SetImage(window, null, Clear);
        }

        // 아래 위치들은 LoadingWindow 안 기준 (창 가운데 = 0,0)
        TMP_Text loading = Text(root, "LoadingText");
        if (loading != null)
        {
            Place(loading.rectTransform, new Vector2(110f, 140f), new Vector2(700f, 76f), new Vector2(1f, 0.5f));
            StyleLike(loading, refTitle, 56f, White, TextAlignmentOptions.Right);
        }
        TMP_Text percent = Text(root, "PercentText");
        if (percent != null)
        {
            Place(percent.rectTransform, new Vector2(130f, 140f), new Vector2(260f, 76f), new Vector2(0f, 0.5f));
            StyleLike(percent, refTitle, 56f, White, TextAlignmentOptions.Left);
        }

        RectTransform barBg = Find(root, "LoadingBarBG");
        if (barBg != null)
        {
            Place(barBg, new Vector2(0f, 55f), new Vector2(1100f, 76f));
            SetImage(barBg, LoadGuid(GuidBarFrame), White);
        }
        RectTransform fill = Find(root, "LoadingBarFill");
        if (fill != null)
        {
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0.5f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image img = SetImage(fill, LoadGuid(GuidBarFill), White);
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
            Place(fake.rectTransform, new Vector2(0f, -8f), new Vector2(1100f, 46f));
            StyleLike(fake, refGuide, 30f, Pale, TextAlignmentOptions.Center);
        }

        // 가짜 버튼 COMPLETE (현민님 24_button)
        RectTransform complete = Find(root, "CompleteButton");
        if (complete != null)
        {
            Place(complete, new Vector2(0f, -110f), new Vector2(560f, 112f));
            SetImage(complete, LoadGuid(Guid24Button), White);
            foreach (TMP_Text t in complete.GetComponentsInChildren<TMP_Text>(true))
            {
                t.text = "COMPLETE";
                StyleLike(t, refTitle, 56f, White, TextAlignmentOptions.Center);
            }
        }

        TMP_Text small = Text(root, "SmallHintText");
        if (small != null)
        {
            Place(small.rectTransform, new Vector2(0f, -218f), new Vector2(1100f, 44f));
            StyleLike(small, refGuide, 28f, Pale, TextAlignmentOptions.Center);
        }

        TMP_Text error = Text(root, "ErrorText");
        if (error != null)
        {
            Place(error.rectTransform, new Vector2(0f, -110f), new Vector2(1100f, 150f));
            StyleLike(error, refTitle, 32f, Pink, TextAlignmentOptions.Center);
        }

        // 진짜 START: 창 아래쪽 (기존 START 버튼 모양)
        StartButton(Find(root, "RealStartButton"), new Vector2(230f, 54f), 26f, true, new Vector2(0f, -355f));

        BottomText(root, null, "입력이 <color=" + YellowHex + ">항상</color> 필요한 것은 아닙니다.");
    }

    // =====================================================
    // MG25 팝업 미로  (MG05 미로 + MG06 팝업 참고)
    // =====================================================
    static void Skin25(Transform root)
    {
        Common(root, 25,
            "복합 실행 경로 탐색 검사",
            "팝업창을 정리하고 START 버튼을 벽에 닿지 않게 GOAL까지 옮긴다.",
            "★★★★★", IconSpatial);

        RectTransform play = Find(root, "PlayArea");
        if (play != null)
        {
            Place(play, new Vector2(0f, -15f), new Vector2(1560f, 660f));
            SetImage(play, null, Navy);

            Image border = NewImage(play, Prefix + "Border", LoadGuid(GuidPanelLine), White,
                Vector2.zero, new Vector2(1560f, 660f));
            border.transform.SetAsLastSibling();
        }

        RectTransform board = Find(root, "MazeBoard");
        if (board != null)
        {
            board.sizeDelta = new Vector2(1440f, 570f);
            SetImage(board, null, Clear); // 투명(클릭은 그대로 받음)
        }

        RectTransform layer = Find(root, "PopupLayer");
        if (layer != null) layer.sizeDelta = new Vector2(1440f, 570f);

        // 미로 벽 색 (스크립트 변수 wallColor) - MG05 레이저처럼 흰 선
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

        // START = MG05와 같은 START 버튼 그림
        RectTransform start = Find(root, "StartButton");
        if (start != null)
        {
            SetImage(start, LoadGuid(GuidNormalButton), White);
            Outline o = start.GetComponent<Outline>();
            if (o != null) Object.DestroyImmediate(o);
            foreach (TMP_Text t in start.GetComponentsInChildren<TMP_Text>(true))
            {
                t.text = "START";
                StyleLike(t, refButtonText, t.fontSize, White, TextAlignmentOptions.Center);
            }
        }

        // GOAL: 노란 테두리 + 노란 글자
        RectTransform goal = Find(root, "GoalZone");
        if (goal != null)
        {
            SetImage(goal, null, Navy);
            Outline o = goal.GetComponent<Outline>();
            if (o == null) o = goal.gameObject.AddComponent<Outline>();
            o.effectColor = Yellow;
            o.effectDistance = new Vector2(3f, -3f);
            o.useGraphicAlpha = false;
            TMP_Text goalText = Text(goal, "GoalText");
            if (goalText != null)
            {
                Place(goalText.rectTransform, Vector2.zero, goal.sizeDelta);
                StyleLike(goalText, refTitle, 30f, Yellow, TextAlignmentOptions.Center);
            }
        }

        // 처음 팝업 개수 줄이기 (함정 1 + 움직이는 창 1 + 일반 2~3)
        if (maze != null)
        {
            var soCount = new SerializedObject(maze);
            SerializedProperty minP = soCount.FindProperty("minPopups");
            SerializedProperty maxP = soCount.FindProperty("maxPopups");
            if (minP != null) minP.intValue = 4;
            if (maxP != null) maxP.intValue = 5;
            SerializedProperty scaleP = soCount.FindProperty("popupScale");
            if (scaleP != null) scaleP.floatValue = 1f;   // 팝업 크게 (원래 0.82배)
            soCount.ApplyModifiedPropertiesWithoutUndo();
        }

        // 움직이는 창도 이제 닫을 수 있으므로 안내 문구 바꿈
        RectTransform movePopup = Find(root, "MovePopup");
        if (movePopup != null)
        {
            TMP_Text moveBody = Text(movePopup, "PopupBodyText");
            if (moveBody != null) moveBody.text = "This window can be moved.";

            // 움직이는 창에는 X 버튼이 없어서 일반 팝업의 X 버튼을 복사해 넣음 (스크립트가 "CloseButton" 이름으로 찾음)
            RectTransform moveTitle = Find(movePopup, "TitleBar");
            RectTransform normalPopup = Find(root, "NormalPopup");
            RectTransform normalClose = normalPopup != null ? Find(normalPopup, "CloseButton") : null;
            if (moveTitle != null && normalClose != null && Find(moveTitle, "CloseButton") == null)
            {
                GameObject closeCopy = Object.Instantiate(normalClose.gameObject, moveTitle, false);
                closeCopy.name = "CloseButton";
            }
        }

        // MG06처럼 제목줄 + X 버튼 있는 팝업창 (현민님 색깔 창: 일반 = 파랑 / 움직이는 팝업 = 노랑 / 함정 = 분홍)
        string[] popupNames = { "NormalPopup", "MovePopup", "TrapPopup" };
        string[] popupArts = { GuidWinBlue, GuidWinYellow, GuidWinPink };
        for (int p = 0; p < popupNames.Length; p++)
        {
            RectTransform popup = Find(root, popupNames[p]);
            if (popup == null) continue;

            Sprite win = LoadGuid(popupArts[p]);
            SetImage(popup, win != null ? win : LoadGuid(GuidWindow), White);

            // 현민님 25_window 그림 비율(510x262)대로 크게, 제목줄은 그림 위쪽 띠(약 20%)에 맞춤
            Vector2 size = new Vector2(480f, 246f);
            popup.sizeDelta = size;
            float scaleY = size.y / 262f;
            float barH = 45f * scaleY;                 // 띠 안쪽 높이
            float barCenter = (7f + 45f * 0.5f) * scaleY;

            RectTransform titleBar = Find(popup, "TitleBar");
            if (titleBar != null)
            {
                titleBar.anchorMin = titleBar.anchorMax = new Vector2(0f, 1f);
                titleBar.pivot = new Vector2(0.5f, 0.5f);
                titleBar.anchoredPosition = new Vector2(size.x * 0.5f, -barCenter);
                titleBar.sizeDelta = new Vector2(size.x - 20f, barH);
                SetImage(titleBar, null, Clear);

                TMP_Text title = Text(titleBar, "PopupTitle");
                if (title != null)
                {
                    Place(title.rectTransform, new Vector2(-10f, 0f), new Vector2(size.x - 100f, barH));
                    StyleLike(title, refGuide, 22f, White, TextAlignmentOptions.Center);
                }

                RectTransform close = Find(titleBar, "CloseButton");
                if (close != null)
                {
                    Place(close, new Vector2((size.x - 20f) * 0.5f - 24f, 0f), new Vector2(30f, 31f));
                    SetImage(close, LoadGuid(GuidXButton), White);
                    foreach (Transform c in close) c.gameObject.SetActive(false); // 글자 X 숨김 (그림 X 사용)
                }
            }

            // 본문: 제목줄 아래 남은 칸 가운데
            TMP_Text body = Text(popup, "PopupBodyText");
            if (body != null)
            {
                float bodyCenter = -(barCenter + barH * 0.5f) * 0.5f;   // 팝업 가운데 기준
                Place(body.rectTransform, new Vector2(0f, bodyCenter), new Vector2(size.x - 60f, size.y - barH - 50f));
                StyleLike(body, refGuide, 26f, White, TextAlignmentOptions.Center);
                body.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        BottomText(root, null, "팝업을 치우고 <color=" + YellowHex + ">안전한 길</color>을 찾으세요.");
    }

    // =====================================================
    // 공통: 기존 미니게임 배경 + 왼쪽 위 MG 배지/이름/설명/별/아이콘 + 설정 버튼 (MG07에서 복사)
    // =====================================================
    static void Common(Transform root, int n, string title, string guide, string stars, string iconGuid)
    {
        RectTransform bg = FindBackground(root);
        if (bg != null) SetImage(bg, LoadGuid(GuidPanelBg), White);

        // 예전 제목 글자("MG2x | ...") 숨기기
        foreach (Transform c in root)
        {
            TMP_Text t = c.GetComponent<TMP_Text>();
            if (t != null && t.text.Contains("MG" + n + " |"))
                c.gameObject.SetActive(false);
        }

        Transform badge = CopyFromRef(root, "MG");
        if (badge != null)
        {
            TMP_Text badgeText = badge.GetComponentInChildren<TMP_Text>(true);
            if (badgeText != null) badgeText.text = "MG" + n;
        }

        Transform titleT = CopyFromRef(root, "TitleText");
        if (titleT != null)
        {
            TMP_Text t = titleT.GetComponent<TMP_Text>();
            t.text = title;
            WidenLeft(t.rectTransform, 760f);   // 왼쪽 시작점은 그대로, 줄바꿈 안 되게 폭만 넓힘
        }

        Transform guideT = CopyFromRef(root, "GuideText");
        if (guideT != null)
        {
            TMP_Text t = guideT.GetComponent<TMP_Text>();
            t.text = guide;
            WidenLeft(t.rectTransform, 1100f);
        }

        Transform starT = CopyFromRef(root, "LevelStar");
        if (starT != null)
        {
            TMP_Text t = starT.GetComponent<TMP_Text>();
            if (t != null) t.text = stars;
        }

        Transform iconT = CopyFromRef(root, "icon");
        if (iconT != null)
        {
            Image img = iconT.GetComponent<Image>();
            Sprite s = LoadGuid(iconGuid);
            if (img != null && s != null) img.sprite = s;
        }

        CopyFromRef(root, "Setting");
    }

    // MG07 프리팹의 오브젝트를 통째로 복사해 root 맨 뒤에 붙임 (이름 앞에 C5_ 를 붙여 다시 적용할 때 지워지게)
    static Transform CopyFromRef(Transform root, string refName)
    {
        RectTransform src = Find(refRoot.transform, refName);
        if (src == null)
        {
            if (!missing.Contains("MG07 프리팹의 " + refName)) missing.Add("MG07 프리팹의 " + refName);
            return null;
        }

        GameObject copy = Object.Instantiate(src.gameObject, root, false);
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
        {
            string baseName = t == copy.transform ? refName : t.name;
            if (!baseName.StartsWith(Prefix)) t.name = Prefix + baseName;
        }
        copy.SetActive(true);
        copy.transform.SetAsLastSibling();
        return copy.transform;
    }

    static void WidenLeft(RectTransform rt, float width)
    {
        float left = rt.anchoredPosition.x - rt.sizeDelta.x * rt.pivot.x;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(left + width * rt.pivot.x, rt.anchoredPosition.y);
    }

    // 오른쪽 위 TIME (기존 미니게임 타이머 자리/글꼴)
    static void PlaceTime(TMP_Text time)
    {
        time.gameObject.SetActive(true);
        Place(time.rectTransform, TimerPos, TimerSize);
        StyleLike(time, refTitle, 30f, White, TextAlignmentOptions.Right);
    }

    // 남색 판 + 현민님 23_line 네온 테두리 (배경 바로 위에 깔림)
    static void AddPanel(Transform root, Vector2 pos, Vector2 size)
    {
        RectTransform group = NewRect(root, Prefix + "Panel", pos, size);
        PutAfterBackground(root, group);
        NewImage(group, Prefix + "PanelFill", null, Navy, Vector2.zero, new Vector2(size.x - 50f, size.y - 50f));
        NewImage(group, Prefix + "PanelLine", LoadGuid(GuidPanelLine), White, Vector2.zero, size);
    }

    static void PutAfterBackground(Transform root, Transform t)
    {
        RectTransform bg = FindBackground(root);
        t.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
    }

    static Color StatusColor()
    {
        return refStatus != null ? refStatus.color : White;
    }

    // 아래쪽 안내 문구 (MG07 StatusText 자리/글꼴/색)
    static void BottomText(Transform root, TMP_Text existing, string text)
    {
        TMP_Text t = existing;
        if (t == null)
        {
            RectTransform rt = NewRect(root, Prefix + "BottomText", BottomTextPos, BottomTextSize);
            var created = rt.gameObject.AddComponent<TextMeshProUGUI>();
            created.raycastTarget = false;
            created.text = text;
            t = created;
        }
        else
        {
            t.transform.SetParent(root, false);
            t.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(text)) t.text = text;
        }

        Place(t.rectTransform, BottomTextPos, BottomTextSize);
        StyleLike(t, refStatus, 30f, StatusColor(), TextAlignmentOptions.Center);
        t.richText = true;
        t.transform.SetAsLastSibling();
    }

    // START 버튼: 기존 미니게임 START 버튼(현민님 normal_button) + 같은 글꼴
    static void StartButton(RectTransform button, Vector2 size, float fontSize, bool setPos, Vector2 pos = default)
    {
        if (button == null) return;
        if (setPos) Place(button, pos, size);
        else button.sizeDelta = size;

        SetImage(button, LoadGuid(GuidNormalButton), White);
        foreach (TMP_Text t in button.GetComponentsInChildren<TMP_Text>(true))
        {
            t.text = "START";
            StyleLike(t, refButtonText, fontSize, White, TextAlignmentOptions.Center);
        }
    }

    // 숨은 글자 버튼: 투명 버튼 → 노란 알파벳 그림 버튼 (안쪽 글자는 숨김)
    static void LetterImage(RectTransform button, string artPath, Vector2 size)
    {
        if (button == null) return;
        button.sizeDelta = size;
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = Load(artPath);
            img.type = Image.Type.Simple;
            img.color = White;
            img.preserveAspect = true;
            img.raycastTarget = true;
        }
        foreach (TMP_Text t in button.GetComponentsInChildren<TMP_Text>(true))
            t.gameObject.SetActive(false);
    }

    static void LetterArt(RectTransform block, string artPath, Vector2 size)
    {
        if (block == null) return;
        block.sizeDelta = size;
        Image img = SetImage(block, Load(artPath), White);
        if (img != null) img.preserveAspect = true;
        foreach (TMP_Text t in block.GetComponentsInChildren<TMP_Text>(true))
            t.gameObject.SetActive(false);
    }

    // 제한 시간: 기본 60초, 22번 70초, 미로(25번) 90초
    static float TimeLimitFor(int n)
    {
        switch (n)
        {
            case 21: return 60f;   // 긴 약관 속 숨은 단어 찾기
            case 22: return 70f;   // 설정 화면 곳곳 숨은 글자 찾기
            case 25: return 90f;   // 팝업 미로
            default: return 60f;   // 23 글자 추적, 24 99% 기다리기
        }
    }

    static void SetTimeLimit(Transform root, int n)
    {
        MinigameBase game = root.GetComponent<MinigameBase>();
        if (game == null) return;
        var so = new SerializedObject(game);
        SetFloat(so, "timeLimit", TimeLimitFor(n));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetString(SerializedObject so, string name, string value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.stringValue = value;
    }

    static void SetFloat(SerializedObject so, string name, float value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null) p.floatValue = value;
    }

    // =====================================================
    // 도우미
    // =====================================================
    static void Backup(string path, int n)
    {
        if (!AssetDatabase.IsValidFolder(BackupDir))
            AssetDatabase.CreateFolder("Assets/Prefab", "_Ch5Backup");

        string original = BackupDir + "/Minigame_" + n + "Panel_원본.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(original) == null)
            AssetDatabase.CopyAsset(path, original);

        // 바로 전 모습 (시안 그대로 버전)
        string previous = BackupDir + "/Minigame_" + n + "Panel_시안디자인.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(previous) == null)
            AssetDatabase.CopyAsset(path, previous);
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

    static Sprite LoadGuid(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            if (!missing.Contains("그림 GUID " + guid)) missing.Add("그림 GUID " + guid);
            return null;
        }
        return Load(path);
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

    // 기준 글자(reference = MG07 글자)와 같은 글꼴로 맞춤
    static void StyleLike(TMP_Text t, TMP_Text reference, float size, Color color, TextAlignmentOptions align)
    {
        if (t == null) return;
        if (reference != null && reference.font != null)
        {
            t.font = reference.font;
            t.fontSharedMaterial = reference.fontSharedMaterial;
        }
        t.fontSize = size;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.characterSpacing = 0f;
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
}
