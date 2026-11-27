using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 메뉴: 상단 메뉴바 > PRESS START > 챕터5 UI 새 디자인 적용 (MG26~30)
// 챕터5 미니게임 MG26~MG30 프리팹 디자인 (MG21~25 와 같은 방식)
//  - 화면 틀은 기존 미니게임과 똑같이: 배경 + 왼쪽 위 MG 배지/이름/설명/별/아이콘 + 오른쪽 위 설정 버튼 (MG07에서 복사)
//  - 가운데 게임 화면(네온 판, 문자 슬롯, 컨베이어, 사다리, 벽돌...)은 각 미니게임 스크립트가 실행할 때 만든다.
//    이 도구는 그때 쓸 현민님 Chapter5 그림과 글꼴을 스크립트 칸에 넣어 준다.
//  - 적용 전 지금 프리팹을 Assets/Prefab/_Ch5Backup 폴더에 '_원본' 으로 복사해 둔다 (이미 있으면 그대로)
//  - 여러 번 눌러도 결과는 같다 (C5_ 로 시작하는 꾸밈 오브젝트를 지우고 다시 만듦)
//  - 자동 적용: 유니티가 이 스크립트를 처음 읽을 때 한 번 자동으로 실행됨
//    (Assets/Editor/Chapter5SkinTool2630_applied.txt 에 버전을 적어 두고, 같은 버전이면 다시 안 함)
[InitializeOnLoad]
public static class Chapter5SkinTool2630
{
    const string SkinVersion = "2026-11-27-v12";
    const string MarkerPath = "Assets/Editor/Chapter5SkinTool2630_applied.txt";

    static Chapter5SkinTool2630()
    {
        EditorApplication.delayCall += AutoApply;
    }

    static void AutoApply()
    {
        if (Application.isBatchMode)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += AutoApply;
            return;
        }

        string done = System.IO.File.Exists(MarkerPath) ? System.IO.File.ReadAllText(MarkerPath).Trim() : "";
        if (done == SkinVersion)
            return;

        Debug.Log("[Chapter5] MG26~30 UI 자동 적용 시작 (" + SkinVersion + ")");
        Apply(false);
        System.IO.File.WriteAllText(MarkerPath, SkinVersion);
        AssetDatabase.ImportAsset(MarkerPath);
    }

    const string PrefabDir = "Assets/Prefab/";
    const string BackupDir = "Assets/Prefab/_Ch5Backup";
    const string Prefix = "C5_";
    const string RefPrefabPath = "Assets/Prefab/Minigame_7Panel.prefab";

    // ---------- 그림 ----------
    const string H = "Assets/1. Team Folder/Hyunmin/Art/";
    const string GuidPanelBg = "51757ff398d5c0149a148417c6d738c7";   // Subin/NEW ART/배경 1.PNG (기존 미니게임 공통 배경)

    // 왼쪽 위 능력 아이콘 (기존 미니게임과 같은 아이콘 세트)
    const string IconReaction = "6ee370cbc4b0f624bbe4c8e3502b693a";   // 반응속도
    const string IconBrain = "8236e7ce8f45d9748a60cf7460b5e286";      // 지능
    const string IconSpatial = "b3235ce21ef5a5a45aa0faae83f07cdc";    // 공간감각
    const string IconMusclePath = "Assets/1. Team Folder/Youngwoo/art/11/icon/Muscle_icon.png";   // 피지컬

    static readonly Color White = Color.white;

    static readonly List<string> missing = new List<string>();
    static GameObject refRoot;
    static TMP_Text refTitle;

    [MenuItem("PRESS START/챕터5 UI 새 디자인 적용 (MG26~30)")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    public static void Apply(bool showDialog)
    {
        missing.Clear();

        refRoot = AssetDatabase.LoadAssetAtPath<GameObject>(RefPrefabPath);
        if (refRoot == null)
        {
            Debug.LogError("[Chapter5] 기준 프리팹을 찾지 못했어요: " + RefPrefabPath);
            if (showDialog)
                EditorUtility.DisplayDialog("챕터5 UI", "기준 프리팹을 찾지 못했어요:\n" + RefPrefabPath, "확인");
            return;
        }
        refTitle = Text(refRoot.transform, "TitleText");

        var done = new List<string>();
        for (int n = 26; n <= 30; n++)
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
                EnsureBackground(root.transform);
                switch (n)
                {
                    case 26:
                        Common(root.transform, 26, "실행 문자 수신 검사", "S-T-A-R-T 순서대로 문자를 받으세요.",
                            "★★★★★", LoadGuid(IconReaction));
                        Art26(root);
                        break;
                    case 27:
                        Common(root.transform, 27, "실행 장치 복구 검사", "올바른 부품을 골라 START 버튼을 완성하세요.",
                            "★★★★★", LoadGuid(IconBrain));
                        Art27(root);
                        break;
                    case 28:
                        Common(root.transform, 28, "문자 중력 이동 검사", "판을 기울여 문자를 GOAL까지 이동시키세요. (드래그 / 방향키)",
                            "★★★★★", LoadGuid(IconSpatial));
                        Art28(root);
                        break;
                    case 29:
                        Common(root.transform, 29, "실행 경로 복구 검사", "숨은 경로를 찾아 START 순서를 복구하세요.",
                            "★★★★★", LoadGuid(IconBrain));
                        Art29(root);
                        break;
                    case 30:
                        Common(root.transform, 30, "실행 문자 순서 제거 검사", "벽돌을 깨서 S-T-A-R-T 문자를 모두 모으세요.",
                            "★★★★★", Load(IconMusclePath));
                        Art30(root);
                        break;
                }
                SetTimeLimit(root, n);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                done.Add("MG" + n + " (" + TimeLimitFor(n) + "초)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        EnsureMergeArt();
        AssetDatabase.SaveAssets();

        string msg = "적용 완료: " + string.Join(", ", done) +
                     "\n\n화면 틀은 기존 미니게임과 같게, 게임 화면은 현민님 Chapter5 그림으로 바뀌었어요." +
                     "\n바뀌기 전 모습은 " + BackupDir + " 폴더에 '_원본' 이름으로 백업되어 있어요.";
        if (missing.Count > 0)
            msg += "\n\n찾지 못한 것 (이 부분은 건너뜀):\n- " + string.Join("\n- ", missing);

        Debug.Log("[Chapter5] " + msg);
        if (showDialog)
            EditorUtility.DisplayDialog("챕터5 UI (MG26~30)", msg, "확인");
    }

    // =====================================================
    // 모든 미니게임 성공 때 나오는 S·T·A·R·T 모이는 연출용 그림 (Resources 에 저장)
    // =====================================================
    const string MergeArtPath = "Assets/Resources/C5StartMergeArt.asset";

    static void EnsureMergeArt()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        C5StartMergeArt art = AssetDatabase.LoadAssetAtPath<C5StartMergeArt>(MergeArtPath);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<C5StartMergeArt>();
            AssetDatabase.CreateAsset(art, MergeArtPath);
        }

        art.letters = new[]
        {
            Load(H + "Chapter5/26_pink_S.png"), Load(H + "Chapter5/26_pink_T.png"), Load(H + "Chapter5/26_pink_A.png"),
            Load(H + "Chapter5/26_pink_R.png"), Load(H + "Chapter5/26_pink_T.png")
        };
        EditorUtility.SetDirty(art);
    }

    // =====================================================
    // 미니게임별 그림 넣기 (스크립트의 Sprite 칸)
    // =====================================================
    static SerializedObject Game(GameObject root, string typeName)
    {
        Component c = root.GetComponent(typeName);
        if (c == null)
        {
            missing.Add(root.name + " 에 " + typeName + " 스크립트가 없음");
            return null;
        }
        return new SerializedObject(c);
    }

    static void CommonArt(SerializedObject so, bool plate, bool slots)
    {
        SetFont(so, "uiFont");
        SetSprite(so, "panelSprite", H + "Chapter5/23_line_back.png");
        SetSprite(so, "startButtonSprite", H + "normal_button.png");
        if (plate)
            SetSprite(so, "plateSprite", H + "Chapter5/26_upline.png");
        if (slots)
        {
            SetSprite(so, "slotFrameSprite", H + "Chapter5/26_miniline.png");
            SetSprite(so, "slotEmptySprite", H + "Chapter5/26_blueblank.png");
            SetSprite(so, "slotArrowLeft", H + "Chapter5/26_arrow_left.png");
            SetSprite(so, "slotArrowRight", H + "Chapter5/26_arrow_right.png");
            SetSprites(so, "slotLetterSprites", new[]
            {
                H + "Chapter5/26_pink_S.png", H + "Chapter5/26_pink_T.png", H + "Chapter5/26_pink_A.png",
                H + "Chapter5/26_pink_R.png", H + "Chapter5/26_pink_T.png"
            });
        }
    }

    static void Art26(GameObject root)
    {
        SerializedObject so = Game(root, "Minigame26_FallingLetters");
        if (so == null) return;
        CommonArt(so, true, true);

        string[] alphabet = new string[26];
        for (int i = 0; i < 26; i++)
            alphabet[i] = H + "alpabet/block_" + (char)('A' + i) + ".png";
        SetSprites(so, "alphabetSprites", alphabet);

        SetSprites(so, "stackBlockSprites", new[]
        {
            H + "Chapter5/30_block_S.png", H + "Chapter5/30_block_T.png", H + "Chapter5/30_block_A.png",
            H + "Chapter5/30_block_R.png", H + "Chapter5/30_block_T.png"
        });
        SetSprite(so, "stackBaseSprite", H + "Chapter5/24_loading_prograss__trim.png");
        SetNumber(so, "spawnInterval", 0.4f);
        SetNumber(so, "targetChance", 0.35f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Art27(GameObject root)
    {
        SerializedObject so = Game(root, "Minigame27_DamagedCircuit");
        if (so == null) return;
        SetFont(so, "uiFont");
        SetSprite(so, "panelSprite", H + "Chapter5/23_line_back.png");
        SetSprite(so, "labelSprite", H + "Chapter5/26_upline.png");
        SetSprite(so, "railSprite", H + "Chapter5/27_rail.png");
        SetSprite(so, "assemblyPlateSprite", H + "Chapter5/27_assembly_line.png");
        SetSprite(so, "slotSprite", H + "Chapter5/27_big_blank.png");
        SetSprite(so, "arrowSprite", H + "Chapter5/26_arrow_left.png");
        SetSprite(so, "powerSprite", H + "Chapter5/27_power.png");
        SetSprite(so, "inputSprite", H + "Chapter5/27_input.png");
        SetSprite(so, "displaySprite", H + "Chapter5/27_display.png");
        SetSprite(so, "startSprite", H + "Chapter5/27_start.png");
        SetSprite(so, "brokenPowerSprite", H + "Chapter5/27_brokenpower.png");
        SetSprite(so, "wrongInputSprite", H + "Chapter5/27_wronginput.png");
        SetSprite(so, "brokenDisplaySprite", H + "Chapter5/27_brokendisplay.png");
        SetSprite(so, "exitSprite", H + "Chapter5/27_exit.png");
        SetNumber(so, "partSize", 124f);
        SetNumber(so, "beltSpeed", 120f);
        SetNumber(so, "spawnInterval", 1.6f);
        SetNumber(so, "minPartGap", 70f);
        SetNumber(so, "neededPartChance", 0.4f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Art28(GameObject root)
    {
        SerializedObject so = Game(root, "Minigame28_TiltedStart");
        if (so == null) return;
        CommonArt(so, true, true);
        // MG26 떨어지는 문자와 같은 알파벳 블록
        SetSprites(so, "letterSprites", new[]
        {
            H + "alpabet/block_S.png", H + "alpabet/block_T.png", H + "alpabet/block_A.png",
            H + "alpabet/block_R.png", H + "alpabet/block_T.png"
        });
        SetSprite(so, "obstacleSprite", H + "Chapter2/08_Xwall.png");
        SetSprite(so, "goalSprite", H + "Chapter2/08_goal.png");
        // MG08 기울어진 창과 같은 조작감 (속도만 살짝 낮춤)
        SetNumber(so, "dragRange", 250f);
        SetNumber(so, "deadZone", 0.1f);
        SetNumber(so, "moveSpeed", 300f);
        SetNumber(so, "movementSmooth", 6f);
        SetNumber(so, "brakeSmooth", 6f);
        SetNumber(so, "tiltSmoothTime", 0.18f);
        SetNumber(so, "maxVisualAngle", 7f);
        SetNumber(so, "countMistakes", 1f);   // 떨어지거나 장애물에 닿으면 ERROR +1
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Art29(GameObject root)
    {
        SerializedObject so = Game(root, "Minigame29_DamagedStartRoute");
        if (so == null) return;
        CommonArt(so, false, false);
        SetSprite(so, "runButtonSprite", H + "Chapter5/24_button.png");
        SetSprites(so, "letterSprites", new[]
        {
            H + "Chapter1/S.png", H + "Chapter1/T.png", H + "Chapter1/A.png", H + "Chapter1/R.png",
            H + "Chapter1/T2.png"
        });
        SetSprites(so, "numberSprites", new[]
        {
            H + "Chapter5/29_1.png", H + "Chapter5/29_2.png", H + "Chapter5/29_3.png", H + "Chapter5/29_4.png",
            H + "Chapter5/29_5.png"
        });
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Art30(GameObject root)
    {
        SerializedObject so = Game(root, "Minigame30_SequenceBreakout");
        if (so == null) return;
        CommonArt(so, true, true);
        SetSprites(so, "letterBrickSprites", new[]
        {
            H + "Chapter5/30_block_S.png", H + "Chapter5/30_block_T.png", H + "Chapter5/30_block_A.png",
            H + "Chapter5/30_block_R.png", H + "Chapter5/30_block_T.png"
        });
        SetSprite(so, "normalBrickSprite", H + "Chapter5/30_blank.png");
        SetNumber(so, "distractorCount", 0f);
        SetNumber(so, "ballSpeed", 430f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetSprite(SerializedObject so, string field, string path)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            missing.Add("스크립트 칸 " + field);
            return;
        }
        p.objectReferenceValue = Load(path);
    }

    // 스크립트에서 바꾼 숫자는 프리팹에 저장된 옛날 값이 우선이라, 여기서 직접 넣어 줌
    static void SetNumber(SerializedObject so, string field, float value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            missing.Add("스크립트 칸 " + field);
            return;
        }
        if (p.propertyType == SerializedPropertyType.Integer)
            p.intValue = Mathf.RoundToInt(value);
        else if (p.propertyType == SerializedPropertyType.Boolean)
            p.boolValue = value != 0f;
        else
            p.floatValue = value;
    }

    static void SetSprites(SerializedObject so, string field, string[] paths)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null || !p.isArray)
        {
            missing.Add("스크립트 칸 " + field);
            return;
        }
        p.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = Load(paths[i]);
    }

    static void SetFont(SerializedObject so, string field)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null && refTitle != null)
            p.objectReferenceValue = refTitle.font;
    }

    // =====================================================
    // 공통: 배경 + MG 배지/제목/설명/별/아이콘 + 설정 버튼 (MG07에서 복사)
    // =====================================================
    static void Common(Transform root, int n, string title, string guide, string stars, Sprite icon)
    {
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
            WidenLeft(t.rectTransform, 760f);
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
            if (img != null && icon != null) img.sprite = icon;
        }

        CopyFromRef(root, "Setting");
    }

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

    // 배경 (기존 미니게임 공통 배경 그림). 없으면 만들고, 있으면 그림만 맞춤
    static void EnsureBackground(Transform root)
    {
        Sprite bgSprite = LoadGuid(GuidPanelBg);
        RectTransform bg = null;
        foreach (Transform c in root)
        {
            if (c.name == "BG" && c.GetComponent<Image>() != null)
            {
                bg = (RectTransform)c;
                break;
            }
        }

        if (bg == null)
        {
            GameObject go = new GameObject("BG", typeof(RectTransform));
            go.layer = root.gameObject.layer;
            go.transform.SetParent(root, false);
            bg = (RectTransform)go.transform;
            Image img = go.AddComponent<Image>();
            img.raycastTarget = true;   // 뒤쪽 클릭 막기
        }

        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;
        bg.SetAsFirstSibling();

        Image image = bg.GetComponent<Image>();
        image.sprite = bgSprite;
        image.type = Image.Type.Simple;
        image.color = bgSprite != null ? White : new Color32(5, 8, 20, 255);

        // 루트에 붙은 회색 Image 는 투명하게 (배경 그림이 보이게)
        Image rootImage = root.GetComponent<Image>();
        if (rootImage != null && rootImage.sprite == null)
            rootImage.color = new Color(0f, 0f, 0f, 0f);
    }

    // 제한 시간
    static float TimeLimitFor(int n)
    {
        switch (n)
        {
            case 26: return 60f;    // 문자 받기 (탑 쌓기 뺌)
            case 27: return 90f;    // 컨베이어 부품 고르기
            case 28: return 150f;   // 기울이기 5판
            case 29: return 150f;   // 숨은 경로 관찰 + 사다리 (2:30)
            case 30: return 120f;   // 벽돌깨기
            default: return 60f;
        }
    }

    static void SetTimeLimit(GameObject root, int n)
    {
        MinigameBase game = root.GetComponent<MinigameBase>();
        if (game == null) return;
        var so = new SerializedObject(game);
        SerializedProperty p = so.FindProperty("timeLimit");
        if (p != null) p.floatValue = TimeLimitFor(n);
        so.ApplyModifiedPropertiesWithoutUndo();
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
    }

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
}
