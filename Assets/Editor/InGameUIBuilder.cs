using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 인게임 UI 1회 생성 도구 — 에디터 API로 조립하므로 직렬화가 항상 올바르고,
// 결과물은 순수 씬 오브젝트(Inspector 튜닝 가능). 실행 후 씬 저장하면 끝, 빌드엔 포함 안 됨.
// 손 YAML 조립은 Slider/TMP 같은 복잡 컴포넌트에서 파서 불일치 사고가 나서 이 방식으로 확정 (2026-07-06).
public static class InGameUIBuilder
{
    private static TMP_FontAsset font;

    // 조준선을 점선+끝점 조준점으로 (원작 관찰) — 기존 파츠 눈튜닝을 건드리지 않는 별도 메뉴
    [MenuItem("Tools/InGame UI 빌더/Build Aim Line (점선+조준점)")]
    public static void BuildAimLine()
    {
        var shooter = Object.FindFirstObjectByType<Shooter>(FindObjectsInactive.Include);
        if (shooter == null) { Debug.LogError("Shooter 없음"); return; }

        // 점선 머티리얼 (dash 텍스처 + Tile 모드)
        var dashTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Sprites/UI/aim_dash.png");
        if (dashTex == null) { Debug.LogError("aim_dash.png 없음"); return; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AimDashed.mat");
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = dashTex };
            AssetDatabase.CreateAsset(mat, "Assets/Materials/AimDashed.mat");
        }
        mat.mainTexture = dashTex;
        mat.mainTextureScale = new Vector2(14f, 1f);   // 반복 밀도 ↑ = 짧은 점이 촘촘히 (값 키울수록 촘촘)
        EditorUtility.SetDirty(mat);

        var line = shooter.GetComponent<LineRenderer>();
        line.sharedMaterial = mat;
        line.textureMode = LineTextureMode.Tile;   // 길이 따라 dash 반복 = 점선
        line.startColor = line.endColor = new Color(0.72f, 0.72f, 0.7f, 0.9f);    // 원작: 회색
        line.startWidth = line.endWidth = 0.045f;                                  // 가는 선 (원작 #52)

        // 끝점 조준 레티클 (원작 #53: 끊긴 링 + 빨간 중심점) — 재실행 시 스프라이트 갱신
        Transform dot = shooter.transform.Find("AimDot");
        if (dot == null)
        {
            var dotGo = new GameObject("AimDot");
            dot = dotGo.transform;
            dot.SetParent(shooter.transform, false);
        }
        dot.localScale = Vector3.one * 1.2f;
        var reticleSr = dot.GetComponent<SpriteRenderer>() ?? dot.gameObject.AddComponent<SpriteRenderer>();
        reticleSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/UI/aim_reticle.png");
        reticleSr.color = Color.white;
        reticleSr.sortingOrder = 5;
        var so = new SerializedObject(shooter);
        so.FindProperty("aimDot").objectReferenceValue = dot;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 점선 조준선 + 조준점 완료 — 씬 저장하세요");
    }

    // 플레이어 월드 HP바 조립 (원작 #84 발밑 캡슐 — 몬스터 HpBar와 동일 문법) + 캔버스 슬라이더 은퇴
    [MenuItem("Tools/InGame UI 빌더/Build Player HpBar (월드)")]
    public static void BuildPlayerWorldHpBar()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        var shooter = GameObject.Find("Shooter");
        if (shooter == null || font == null) { Debug.LogError("Shooter/폰트 확인"); return; }
        var white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/UI/white.png");

        Transform old = shooter.transform.Find("HpBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("HpBar").transform;
        root.SetParent(shooter.transform, false);
        root.localPosition = new Vector3(0f, -0.55f, 0f);   // 발밑 [눈튜닝]

        SpriteRenderer Bar(string name, Color c, int order, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = white; sr.color = c; sr.sortingOrder = order;
            return sr;
        }
        Bar("Background", new Color(0.08f, 0.08f, 0.08f, 0.9f), 6, new Vector3(1.1f, 0.22f, 1f));
        var fill = Bar("Fill", new Color(0.35f, 0.9f, 0.3f), 7, new Vector3(1.04f, 0.16f, 1f));

        var textGo = new GameObject("Value");
        textGo.transform.SetParent(root, false);
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.font = font; tmp.fontSize = 1.6f; tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
        tmp.sortingOrder = 8;
        tmp.text = "300";

        var bar = shooter.GetComponent<PlayerWorldHpBar>() ?? shooter.AddComponent<PlayerWorldHpBar>();
        Assign(bar, ("fill", fill.transform), ("valueText", tmp));

        // 캔버스 슬라이더 은퇴 (오브젝트 비활성 — enum/리스트는 무해하게 유지)
        var canvasBar = GameObject.Find("PlayerHpSlider");
        if (canvasBar != null) canvasBar.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 플레이어 월드 HP바 조립 완료 (캔버스 슬라이더 비활성) — 씬 저장하세요");
    }

    // 전투 정보 창 조립 (원작 #57) — SafeAreaPanel 아래 패널 생성 + 행 7개 + 참조 자동 할당
    [MenuItem("Tools/InGame UI 빌더/Build CombatInfo Panel")]
    public static void BuildCombatInfoPanel()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        var safeArea = GameObject.Find("SafeAreaPanel")?.GetComponent<RectTransform>();
        if (safeArea == null || font == null) { Debug.LogError("SafeAreaPanel/폰트 확인"); return; }

        // 패널 루트 (재실행 시 기존 것 제거 후 재조립)
        var oldPanel = Object.FindFirstObjectByType<CombatInfoPanel>(FindObjectsInactive.Include);
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
        var root = Child(safeArea, "CombatInfoPanel", C, Vector2.zero, Vector2.zero);
        Stretch(root);
        var panel = root.gameObject.AddComponent<CombatInfoPanel>();

        var overlay = Overlay(root, 0.92f);
        var closeBtn = overlay.gameObject.AddComponent<Button>();   // 전체 터치 = 닫기
        closeBtn.targetGraphic = overlay.GetComponent<UnityEngine.UI.Image>();
        closeBtn.transition = Selectable.Transition.None;

        // 스테이지 라벨·진행도 없음 — 무한 모드라 스테이지 개념이 없고, 진행도는 HUD가 이미 표시 (2026-08-03 유저 확정)
        Text(overlay, "Title", "전투 정보", 56, F(0.90f), Vector2.zero, new(640, 80), color: new Color(1f, 0.85f, 0.5f));
        Text(overlay, "CloseHint", "터치하여 닫기", 26, F(0.05f), Vector2.zero, new(400, 40), color: new Color(0.6f, 0.6f, 0.6f));

        const int RowCount = 7;
        var rows = new GameObject[RowCount];
        var icons = new UnityEngine.UI.Image[RowCount];
        var levels = new TMP_Text[RowCount];
        var names = new TMP_Text[RowCount];
        var totals = new TMP_Text[RowCount];
        var ratios = new Slider[RowCount];
        var dpss = new TMP_Text[RowCount];
        for (int i = 0; i < RowCount; i++)
        {
            float y = 0.68f - i * 0.085f;   // 위에서부터 행 배치
            var row = Child(overlay, $"Row{i}", F(y), Vector2.zero, new(900, 90));
            rows[i] = row.gameObject;

            icons[i] = Image(row, "Icon", Color.white, C, new(-380, 8), new(80, 80), sprite: false);
            icons[i].preserveAspect = true;
            levels[i] = Text(row, "Level", "◆x1", 26, C, new(-380, -38), new(130, 32), color: new Color(1f, 0.85f, 0.4f));
            names[i] = Text(row, "Name", "", 34, C, new(-88, 26), new(420, 44), bold: true);   // x=-88: 슬라이더 왼끝 정렬 (유저 눈튜닝)
            names[i].horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;   // 원작: 아이콘 옆 좌측 정렬
            totals[i] = Text(row, "Total", "0", 38, C, new(-88, -14), new(420, 48), bold: true, color: new Color(0.95f, 0.9f, 0.7f));
            totals[i].horizontalAlignment = TMPro.HorizontalAlignmentOptions.Left;
            ratios[i] = SliderGauge(row, "Ratio", new Color(0.85f, 0.22f, 0.18f), C, new(-70, -44), new(460, 24));   // 원작 두께
            Text(row, "DpsLabel", "DPS", 30, C, new(330, 26), new(150, 38), bold: true, color: new Color(0.9f, 0.88f, 0.8f));
            dpss[i] = Text(row, "Dps", "0", 36, C, new(330, -14), new(150, 44), bold: true);
        }

        Assign(panel, ("overlay", overlay.gameObject), ("closeButton", closeBtn));
        AssignArray(panel, "rows", rows);
        AssignArray(panel, "icons", icons);
        AssignArray(panel, "levels", levels);
        AssignArray(panel, "names", names);
        AssignArray(panel, "totals", totals);
        AssignArray(panel, "ratios", ratios);
        AssignArray(panel, "dpsTexts", dpss);
        overlay.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] CombatInfoPanel 조립 완료 — Generate UIElement Enum 실행 후 씬 저장");
    }

    // 캐릭터 파츠 조립(조준 연출용)
    [MenuItem("Tools/InGame UI 빌더/Build Shooter Parts + Move HpBar")]
    public static void BuildShooterParts()
    {
        var shooter = Object.FindFirstObjectByType<Shooter>(FindObjectsInactive.Include);
        if (shooter == null) { Debug.LogError("Shooter 없음"); return; }
        Transform visual = shooter.transform.Find("Visual");
        if (visual == null) { Debug.LogError("Shooter/Visual 없음"); return; }

        // 통짜 스프라이트 제거 → 파츠 3장 (배치값은 시작점 — Inspector에서 눈튜닝)
        var oldSr = visual.GetComponent<SpriteRenderer>();
        if (oldSr != null) Object.DestroyImmediate(oldSr);
        for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);

        Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/Characters/{name}.png");
        SpriteRenderer Part(string name, Sprite sprite, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(visual, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        var body = Part("Body", Load("Character_Main_body"), Vector3.zero, 1);

        // 머리: "목 위치"의 빈 피벗 기준 회전 — 자기 중심 회전이면 목에서 분리돼 보임
        var headPivot = new GameObject("HeadPivot").transform;
        headPivot.SetParent(visual, false);
        headPivot.localPosition = new Vector3(0f, 0.35f, 0f);   // 목 위치 [눈튜닝]
        var head = Part("Head", Load("Character_Main_head"), Vector3.zero, 2);
        head.transform.SetParent(headPivot, false);
        head.transform.localPosition = new Vector3(0f, 0.18f, 0f);   // 목→머리 중심 거리 [눈튜닝]

        var pivot = new GameObject("WeaponPivot").transform;
        pivot.SetParent(visual, false);
        pivot.localPosition = new Vector3(0.25f, 0.1f, 0f);   // 손잡이 위치 [눈튜닝]
        var weapon = Part("Weapon", Load("Character_main_weapon"), Vector3.zero, 0);   // 지팡이는 머리·몸 뒤 (원작)
        weapon.transform.SetParent(pivot, false);
        weapon.transform.localPosition = new Vector3(0f, 0.3f, 0f);   // 피벗에서 지팡이 몸통까지 [눈튜닝]

        var sv = visual.gameObject.GetComponent<ShooterVisual>();
        if (sv == null) sv = visual.gameObject.AddComponent<ShooterVisual>();
        var svSo = new SerializedObject(sv);
        svSo.FindProperty("body").objectReferenceValue = body;
        svSo.FindProperty("headPivot").objectReferenceValue = headPivot;
        svSo.FindProperty("weaponPivot").objectReferenceValue = pivot;
        svSo.ApplyModifiedProperties();

        var shooterSo = new SerializedObject(shooter);
        shooterSo.FindProperty("visual").objectReferenceValue = sv;
        shooterSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 캐릭터 파츠 조립 완료 — Head/WeaponPivot 위치를 Scene 뷰에서 눈튜닝 후 저장");
    }

    // 데미지 팝업 프리팹 생성 + 씬 MonsterManager에 연결 (TMP는 손 YAML 금지 규약 → 에디터 API로)
    [MenuItem("Tools/InGame UI 빌더/Build DamagePopup Prefab")]
    public static void BuildDamagePopupPrefab()
    {
        var kostar = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        if (kostar == null) { Debug.LogError("Kostar SDF 2.asset 없음"); return; }

        var go = new GameObject("DamagePopup");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = kostar;
        tmp.fontSize = 2.4f;                               // 월드 TMP 크기 — 2차 축소 (유저 확정 2026-07-07) [눈튜닝]
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "99";
        tmp.GetComponent<MeshRenderer>().sortingOrder = 10;   // 몬스터/볼 위
        go.AddComponent<DamagePopup>();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/UI/DamagePopup.prefab");
        Object.DestroyImmediate(go);

        var monsterManager = Object.FindFirstObjectByType<MonsterManager>(FindObjectsInactive.Include);
        if (monsterManager != null)
        {
            var so = new SerializedObject(monsterManager);
            so.FindProperty("damagePopupPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        Debug.Log("[InGameUIBuilder] DamagePopup.prefab 생성 + MonsterManager 연결 완료 — 씬 저장하세요");
    }

    [MenuItem("Tools/InGame UI 빌더/Build InGame UI (1회 실행)")]
    public static void Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        if (font == null) { Debug.LogError("Kostar SDF 2.asset 없음 — Assets/Font 확인"); return; }

        var topBar = GameObject.Find("TopBar")?.GetComponent<RectTransform>();
        var bottomBar = GameObject.Find("BottomBar")?.GetComponent<RectTransform>();
        var hud = Object.FindFirstObjectByType<InGameHud>(FindObjectsInactive.Include);
        var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
        var result = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
        var cards = Object.FindFirstObjectByType<SkillSelectionPanel>(FindObjectsInactive.Include);
        if (topBar == null || hud == null || pause == null || result == null || cards == null)
        { Debug.LogError("씬 구조가 예상과 다름 (TopBar/패널들 확인)"); return; }

        BuildHud(hud, topBar);
        BuildPause(pause);
        BuildResult(result);
        BuildCards(cards);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 완료 — 씬 저장(Cmd+S)하면 확정됩니다");
    }

    // HUD만 재조립 (다른 패널의 눈튜닝 보존용 별도 메뉴)
    [MenuItem("Tools/InGame UI 빌더/Build HUD Only (게이지 Slider 통일)")]
    public static void BuildHudOnly()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        var topBar = GameObject.Find("TopBar")?.GetComponent<RectTransform>();
        var hud = Object.FindFirstObjectByType<InGameHud>(FindObjectsInactive.Include);
        if (topBar == null || hud == null || font == null) { Debug.LogError("TopBar/InGameHud/폰트 확인"); return; }
        BuildHud(hud, topBar);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] HUD 재조립 완료 (Slider 게이지) — 씬 저장하세요");
    }

    // ── HUD (TopBar 안) — 게이지는 전부 UGUI Slider (플레이어 HP와 방식 통일) ──
    private static void BuildHud(InGameHud hud, RectTransform topBar)
    {
        Clear(hud.transform);
        hud.transform.SetParent(topBar, false);
        Stretch((RectTransform)hud.transform);

        Text(hud.transform, "StageName", "1. 깊은 숲", 42, new(0.5f, 1f), new(0, -32), new(520, 54), bold: true);

        var pSlider = SliderGauge(hud.transform, "ProgressSlider", new Color(0.85f, 0.22f, 0.18f),
                                  new(0.5f, 1f), new(0, -66), new(400, 22));
        var pText = Text(hud.transform, "ProgressText", "0%", 22, new(0.5f, 1f), new(0, -78), new(460, 30), bold: true);

        // 레벨 게이지: 중앙 고정폭 + 굵게 + 우측 끝 배지 (원작 #35 — 스트레치 앵커 폐기: 화면 폭 따라 길어지던 문제)
        var lSlider = SliderGauge(hud.transform, "LevelSlider", new Color(0.95f, 0.6f, 0.15f),
                                  new(0.5f, 1f), new(-35, -117), new(560, 30));
        var badge = Image(hud.transform, "LevelBadge", new Color(0.95f, 0.6f, 0.15f), new(0.5f, 1f), new(265, -117), new(40, 40));
        badge.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        var lText = Text(hud.transform, "LevelText", "Lv.1", 28, new(0.5f, 1f), new(330, -117), new(110, 40), bold: true, color: new Color(1f, 0.9f, 0.6f));

        // 일시정지 버튼 확대 (유저 튜닝)
        var pauseBtn = ButtonBox(hud.transform, "PauseButton", "II", 38, new Color(0.15f, 0.16f, 0.2f, 0.9f), new(1, 1), new(-52, -48), new(84, 84));

        Assign(hud, ("progressSlider", pSlider), ("progressText", pText), ("levelSlider", lSlider),
                    ("levelBadge", badge.gameObject), ("levelText", lText), ("pauseButton", pauseBtn));
    }

    // 핸들 없는 게이지 Slider (표시 전용)
    private static Slider SliderGauge(Transform parent, string name, Color fillColor, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var root = Child(parent, name, anchor, pos, size);
        var slider = root.gameObject.AddComponent<Slider>();
        var bg = Image(root, "Background", new Color(0.08f, 0.08f, 0.08f, 0.95f), C, Vector2.zero, Vector2.zero);
        Stretch(bg.rectTransform);

        var fillArea = Child(root, "Fill Area", C, Vector2.zero, Vector2.zero);
        Stretch(fillArea); fillArea.offsetMin = new(2, 2); fillArea.offsetMax = new(-2, -2);
        var fill = Image(fillArea, "Fill", fillColor, C, Vector2.zero, Vector2.zero);   // 폭 여분 0 — 값 0이면 완전히 빈 바
        fill.rectTransform.anchorMin = new(0, 0); fill.rectTransform.anchorMax = new(0, 1);

        slider.fillRect = fill.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0; slider.maxValue = 1; slider.value = 0;
        return slider;
    }

    // ── 일시정지 (원작 #36 비율) ─────────────────────────────────
    // 퍼즈 "슬롯 6개만" 새 구조로 교체 — 위치는 기존(유저 튜닝) 그대로, 다른 오브젝트(유저 제작 포함) 불변
    [MenuItem("Tools/InGame UI 빌더/Build Pause Only (슬롯만 교체)")]
    public static void BuildPauseOnly()
    {
        var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
        if (pause == null) { Debug.LogError("PausePanel 없음"); return; }
        Transform overlay = pause.transform.Find("Overlay");
        if (overlay == null) { Debug.LogError("Overlay 없음"); return; }

        var active = new Image[4]; var passive = new Image[2];
        for (int i = 0; i < 4; i++) active[i] = SwapSlot(overlay, $"ActiveSlot{i}", new Color(0.55f, 0.2f, 0.18f));
        for (int i = 0; i < 2; i++) passive[i] = SwapSlot(overlay, $"PassiveSlot{i}", new Color(0.16f, 0.45f, 0.42f));

        AssignArray(pause, "activeIcons", active);
        AssignArray(pause, "passiveIcons", passive);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 퍼즈 슬롯 6개 교체 완료 (위치 보존) — 씬 저장하세요");
    }

    // 기존 슬롯의 앵커/위치/크기를 그대로 물려받는 교체
    private static Image SwapSlot(Transform overlay, string name, Color frame)
    {
        Transform old = overlay.Find(name);
        Vector2 anchorMin = new(0.5f, 0.6f), anchorMax = new(0.5f, 0.6f), pos = Vector2.zero, size = new(116, 116);
        int sibling = -1;
        if (old != null)
        {
            var r = (RectTransform)old;
            anchorMin = r.anchorMin; anchorMax = r.anchorMax; pos = r.anchoredPosition; size = r.sizeDelta;
            sibling = old.GetSiblingIndex();
            Object.DestroyImmediate(old.gameObject);
        }
        Image frameImg = Image(overlay, name, frame, anchorMin, pos, size);
        var fr = frameImg.rectTransform; fr.anchorMin = anchorMin; fr.anchorMax = anchorMax;
        if (sibling >= 0) frameImg.transform.SetSiblingIndex(sibling);
        Image inner = Image(frameImg.transform, "Inner", new Color(0.06f, 0.05f, 0.05f, 0.98f), C, Vector2.zero, size - new Vector2(12, 12));
        var icon = Image(inner.transform, "Icon", Color.white, C, Vector2.zero, size - new Vector2(22, 22), sprite: false);
        icon.preserveAspect = true;
        icon.enabled = false;
        return icon;
    }

    private static void BuildPause(PausePanel pause)
    {
        Clear(pause.transform);
        var overlay = Overlay(pause.transform, 0.9f);

        // Stage 라벨 없음 — 갱신 코드 없는 정적 문구가 "Stage 1" 거짓 표시를 하던 것 제거 (2026-08-03 유저 확정)
        Text(overlay, "Title", "일시정지", 76, F(0.87f), Vector2.zero, new(640, 90), color: new Color(1f, 0.9f, 0.6f));
        var combat = ButtonBox(overlay, "CombatInfoButton", "il", 26, new Color(0.3f, 0.32f, 0.38f), F(0.78f), new(240, 0), new(52, 52));

        Text(overlay, "ActiveLabel", "Active Skill", 28, F(0.665f), new(-250, 0), new(400, 40), color: new Color(1f, 0.6f, 0.55f));
        Text(overlay, "PassiveLabel", "Passive Skill", 28, F(0.665f), new(275, 0), new(400, 40), color: new Color(0.55f, 0.9f, 0.85f));

        var active = new Image[4]; var passive = new Image[2];
        for (int i = 0; i < 4; i++) active[i] = Slot(overlay, $"ActiveSlot{i}", new Color(0.55f, 0.2f, 0.18f), new(-430 + i * 128, 0));
        for (int i = 0; i < 2; i++) passive[i] = Slot(overlay, $"PassiveSlot{i}", new Color(0.16f, 0.45f, 0.42f), new(160 + i * 128, 0));

        Text(overlay, "DropLabel", "+ 현재 스테이지 드랍", 28, F(0.50f), new(-250, 0), new(500, 40), color: new Color(0.75f, 0.75f, 0.9f));
        Image(overlay, "DropFrame", new Color(0.09f, 0.09f, 0.12f, 0.95f), F(0.35f), Vector2.zero, new(920, 440));

        var resume = ButtonBox(overlay, "ResumeButton", "이어하기", 40, new Color(0.95f, 0.65f, 0.2f), F(0.12f), Vector2.zero, new(380, 100), boldLabel: true);

        Assign(pause, ("overlay", overlay.gameObject), ("resumeButton", resume), ("combatInfoButton", combat));
        AssignArray(pause, "activeIcons", active);
        AssignArray(pause, "passiveIcons", passive);
        overlay.gameObject.SetActive(false);
    }

    // ── 결과 ─────────────────────────────────────────────────────
    private static void BuildResult(ResultPanel result)
    {
        Clear(result.transform);
        var overlay = Overlay(result.transform, 0.75f);
        var panel = Image(overlay, "Panel", new Color(0.13f, 0.12f, 0.16f, 0.97f), C, Vector2.zero, new(520, 480)).transform;

        // 플레이스홀더도 런타임과 동일한 "Game Over"로 — Stage 라벨은 무한모드라 제거 (검수 v5 #9와 정합)
        var title = Text(panel, "Title", "Game Over", 60, C, new(0, 140), new(480, 80), color: new Color(1f, 0.85f, 0.4f));
        var info = Text(panel, "Info", "", 34, C, new(0, -10), new(480, 60));
        var restart = ButtonBox(panel, "RestartButton", "다시 시작", 36, new Color(0.95f, 0.65f, 0.2f), C, new(0, -150), new(300, 88), boldLabel: true);

        Assign(result, ("overlay", overlay.gameObject), ("titleText", title), ("infoText", info), ("restartButton", restart));
        overlay.gameObject.SetActive(false);
    }

    // 선택창만 재조립 (다른 패널 눈튜닝 보존용 별도 메뉴)
    [MenuItem("Tools/InGame UI 빌더/Build Selection Panel (원작 레이아웃)")]
    public static void BuildSelectionOnly()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Kostar SDF 2.asset");
        var panel = Object.FindFirstObjectByType<SkillSelectionPanel>(FindObjectsInactive.Include);
        if (panel == null || font == null) { Debug.LogError("SkillSelectionPanel/폰트 확인"); return; }
        BuildCards(panel);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InGameUIBuilder] 선택창 재조립 완료 (세로 카드+레벨 바+보유 슬롯) — 씬 저장하세요");
    }

    // ── 3택지 선택창 (원작 #67: 레벨 업 타이틀 + 빨간 레벨 바 + 보유 슬롯 + 세로 카드 3장) ──
    private static void BuildCards(SkillSelectionPanel panel)
    {
        Clear(panel.transform);
        Stretch((RectTransform)panel.transform);   // 루트도 강제 리셋 — 손튜닝/옛 오프셋 잔재가 남지 않게
        var overlay = Overlay(panel.transform, 0.8f);

        // 상단: 레벨 업 + 꽉 찬 빨간 바 + 레벨 배지 — HUD 게이지와 같은 데이터(레벨)의 다른 뷰.
        // 열리는 순간 = 게이지가 꽉 찬 순간이므로 값은 고정 1 (유저 확정 2026-07-07)
        // 위치는 HUD 레벨바와 "같은 top 픽셀 앵커" — 비율 앵커(F)를 쓰면 화면 높이에 따라 어긋난다 (실사례)
        Vector2 topAnchor = new(0.5f, 1f);
        Text(overlay, "Title", "레벨 업", 64, topAnchor, new(0, -60), new(500, 80), bold: true, color: new Color(1f, 0.92f, 0.8f));
        var bar = SliderGauge(overlay, "LevelBar", new Color(0.9f, 0.25f, 0.15f), topAnchor, new(-30, -125), new(560, 30));   // HUD 바와 동일 좌표 (겹침)
        bar.value = 1f;
        var badgeBg = Image(overlay, "LevelBadgeBg", new Color(0.75f, 0.15f, 0.1f), topAnchor, new(285, -125), new(56, 56));
        badgeBg.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        var badge = Text(overlay, "LevelBadge", "2", 34, topAnchor, new(285, -125), new(70, 50), bold: true);

        // 보유 스킬 슬롯 — Active 4 + Passive 2 (원작 배치)
        Text(overlay, "ActiveLabel", "Active Skill", 28, topAnchor, new(-330, -240), new(260, 36), bold: true, color: new Color(1f, 0.7f, 0.6f));
        Text(overlay, "PassiveLabel", "Passive Skill", 28, topAnchor, new(300, -240), new(260, 36), bold: true, color: new Color(0.6f, 0.95f, 0.9f));
        var actives = new Image[4]; var passives = new Image[2];
        for (int i = 0; i < 4; i++) actives[i] = SlotIcon(overlay, $"ActiveSlot{i}", new Color(0.45f, 0.2f, 0.2f, 0.9f), new(-390 + i * 122, -330));
        for (int i = 0; i < 2; i++) passives[i] = SlotIcon(overlay, $"PassiveSlot{i}", new Color(0.2f, 0.52f, 0.25f, 0.9f), new(235 + i * 122, -330));   // 패시브 = 초록 (유저 확정)

        // 세로 카드 3장 (원작 비율)
        var buttons = new Button[3]; var icons = new Image[3];
        var names = new TMP_Text[3]; var descs = new TMP_Text[3];
        var damages = new TMP_Text[3]; var damageBadges = new GameObject[3];
        var diamonds = new Image[9]; var inners = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var card = Image(overlay, $"Card{i}", new Color(0.22f, 0.11f, 0.11f, 0.98f), C, new((i - 1) * 330, -30), new(310, 720));
            card.raycastTarget = true;
            buttons[i] = card.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = card;
            // 안쪽 마감: 밝은 안판 (에셋 없이 투톤 — 원작 #76의 테두리+면 구조 근사)
            inners[i] = Image(card.transform, "Inner", new Color(0.38f, 0.22f, 0.21f, 1f), C, Vector2.zero, new(286, 696));

            names[i] = Text(card.transform, "Name", "", 40, C, new(0, 280), new(290, 52), bold: true);
            icons[i] = Image(card.transform, "Icon", Color.white, C, new(0, 130), new(170, 170), sprite: false);
            icons[i].preserveAspect = true;

            // 데미지 뱃지 (원작 #73: 설명 바로 위 ★숫자) — 패시브 카드는 코드에서 숨김
            var badgeBg2 = Image(card.transform, "DamageBadge", new Color(0.16f, 0.09f, 0.09f, 0.95f), C, new(0, 10), new(150, 46));
            damages[i] = Text(badgeBg2.transform, "Value", "★ 0", 30, C, Vector2.zero, new(150, 46), bold: true, color: new Color(1f, 0.85f, 0.4f));
            damageBadges[i] = badgeBg2.gameObject;

            descs[i] = Text(card.transform, "Description", "", 28, C, new(0, -140), new(260, 240), color: new Color(0.95f, 0.9f, 0.85f));

            // 하단 레벨 다이아 3개 (원작 #73: 현재 레벨만큼 노랑)
            for (int k = 0; k < 3; k++)
            {
                var dia = Image(card.transform, $"Dia{k}", new Color(0.12f, 0.08f, 0.08f), C, new((k - 1) * 56, -310), new(34, 34));
                dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                diamonds[i * 3 + k] = dia;
            }
        }

        Assign(panel, ("overlay", overlay.gameObject), ("levelBadge", badge));
        AssignArray(panel, "activeSlots", actives);
        AssignArray(panel, "passiveSlots", passives);
        AssignArray(panel, "buttons", buttons);
        AssignArray(panel, "icons", icons);
        AssignArray(panel, "names", names);
        AssignArray(panel, "descriptions", descs);
        AssignArray(panel, "damages", damages);
        AssignArray(panel, "damageBadges", damageBadges);
        AssignArray(panel, "diamonds", diamonds);
        AssignArray(panel, "cardInners", inners);
        overlay.gameObject.SetActive(false);
    }

    // 보유 슬롯 한 칸 — 색은 "테두리만", 안쪽 면은 어둡게 (원작 #74, 유저 확정)
    private static Image SlotIcon(Transform parent, string name, Color frame, Vector2 pos)
    {
        Image frameImg = Image(parent, name, frame, new Vector2(0.5f, 1f), pos, new(116, 116));   // 퍼즈 슬롯과 동일 크기 (유저 확정)
        Image inner = Image(frameImg.transform, "Inner", new Color(0.06f, 0.05f, 0.05f, 0.98f), C, Vector2.zero, new(104, 104));
        var icon = Image(inner.transform, "Icon", Color.white, C, Vector2.zero, new(94, 94), sprite: false);
        icon.preserveAspect = true;
        icon.enabled = false;
        return icon;
    }

    // ── 조립 헬퍼 ────────────────────────────────────────────────
    private static readonly Vector2 C = new(0.5f, 0.5f);
    private static Vector2 F(float y) => new(0.5f, y);

    private static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static RectTransform Child(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private static RectTransform Overlay(Transform parent, float dim)
    {
        var r = Child(parent, "Overlay", C, Vector2.zero, Vector2.zero);
        Stretch(r);
        var img = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, dim);
        img.raycastTarget = true;
        return r;
    }

    private static Image Image(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool sprite = true)
    {
        var r = Child(parent, name, anchor, pos, size);
        var img = r.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }
        return img;
    }

    // 선택창 SlotIcon과 동일 문법 (2026-07-07 통일): 색은 테두리만, 안은 어둡게 (#85)
    private static Image Slot(Transform parent, string name, Color frame, Vector2 pos)
    {
        var frameImg = Image(parent, name, frame, F(0.60f), pos, new(116, 116));
        Image inner = Image(frameImg.transform, "Inner", new Color(0.06f, 0.05f, 0.05f, 0.98f), C, Vector2.zero, new(104, 104));
        var icon = Image(inner.transform, "Icon", Color.white, C, Vector2.zero, new(94, 94), sprite: false);
        icon.enabled = false;
        return icon;
    }

    private static TMP_Text Text(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 pos, Vector2 rect,
                                 bool bold = false, Color? color = null)
    {
        var r = Child(parent, name, anchor, pos, rect);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color ?? Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Button ButtonBox(Transform parent, string name, string label, int labelSize, Color bg,
                                    Vector2 anchor, Vector2 pos, Vector2 size, bool boldLabel = false)
    {
        var img = Image(parent, name, bg, anchor, pos, size);
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = Text(img.transform, "Label", label, labelSize, C, Vector2.zero, size, bold: boldLabel);
        return btn;
    }

    // SerializedObject로 private [SerializeField]에 참조 할당 (에디터 전용 정석)
    private static void Assign(Object target, params (string field, Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"{target.name}: 필드 {field} 없음"); continue; }
            prop.objectReferenceValue = value;
        }
        so.ApplyModifiedProperties();
    }

    private static void AssignArray<T>(Object target, string field, T[] values) where T : Object
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"{target.name}: 필드 {field} 없음"); return; }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedProperties();
    }
}
