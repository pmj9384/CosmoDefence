using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 아웃게임 UI 우주 톤 배선 — 1회 실행 빌더 (반복 조립은 에디터 메뉴 도구 관례).
// 스프라이트 참조 할당은 MCP update_component가 조용히 실패하는 영역이라 에디터 API로 직행한다.
public static class OutGameUISkinner
{
    const string SpriteDir = "Assets/Resources/Sprites/UI/";

    [MenuItem("Tools/OutGame/Apply Space UI Skin")]
    public static void Apply()
    {
        var bg = Load("Background_Lobby");
        var panel = Load("Panel_Common");
        var card = Load("Card_Gacha");
        var coin = Load("Icon_Coin");
        var bolt = Load("Icon_Stamina");

        // ── 1. SafeAreaPanel.prefab: TopBar 아이콘 2개 + 스태미나 팝업 패널 ──
        const string safeAreaPath = "Assets/Prefabs/UI/SafeAreaPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(safeAreaPath);
        AddIconLeftOf(root.transform.Find("TopBar/CurrencyDisplay"), "CoinIcon", coin);
        AddIconLeftOf(root.transform.Find("TopBar/EnergyDisplay"), "EnergyIcon", bolt);
        SetPanel(root.transform.Find("ContentArea/LobbyScreen/StaminaEmptyPopup/Panel"), panel);
        PrefabUtility.SaveAsPrefabAsset(root, safeAreaPath);
        PrefabUtility.UnloadPrefabContents(root);

        // ── 2. SkinItemUI.prefab: 셀 배경 ──
        const string skinItemPath = "Assets/Prefabs/UI/SkinItemUI.prefab";
        var cell = PrefabUtility.LoadPrefabContents(skinItemPath);
        SetPanel(cell.transform, panel);
        PrefabUtility.SaveAsPrefabAsset(cell, skinItemPath);
        PrefabUtility.UnloadPrefabContents(cell);

        // ── 3. LobbyScene: 배경 + 타이틀 + 가챠 팝업/카드 ──
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
        var canvas = GameObject.Find("Canvas");

        var bgTr = (RectTransform)canvas.transform.Find("Background");
        if (bgTr == null)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgTr = (RectTransform)go.transform;
            bgTr.SetParent(canvas.transform, false);
        }
        bgTr.SetSiblingIndex(0);                       // 최배면
        bgTr.anchorMin = Vector2.zero; bgTr.anchorMax = Vector2.one;
        bgTr.offsetMin = Vector2.zero; bgTr.offsetMax = Vector2.zero;
        var bgImg = bgTr.GetComponent<Image>();
        bgImg.sprite = bg;
        bgImg.raycastTarget = false;

        foreach (var tr in canvas.GetComponentsInChildren<Transform>(true))
        {
            switch (tr.name)
            {
                case "TitleText":
                    tr.GetComponent<TMP_Text>().text = "COSMO DEFENCE";
                    break;
                case "DrawOneButton":
                case "DrawTenButton":
                    var img = tr.GetComponent<Image>();
                    img.sprite = card;
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                    break;
                case "Panel":
                    if (tr.parent.name.StartsWith("Gacha")) SetPanel(tr, panel);
                    break;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OutGameUISkinner] 우주 톤 배선 완료 — SafeAreaPanel/SkinItemUI 프리팹 + LobbyScene");
    }

    // ── 2단계: 하단 탭바(클래시 로얄식 전폭 인접 분할) + 상점 카드 내부 구성 ──
    static readonly Color Teal = new Color(0f, 0.83f, 1f, 1f);   // #00D4FF — 무채색 공용 스프라이트에 틴트

    [MenuItem("Tools/OutGame/Build Bottom Bar + Shop")]
    public static void BuildBottomBarAndShop()
    {
        var panel = Load("Panel_Common");
        var coin = Load("Icon_Coin");
        var tab = Load("Button_Tab");
        var main = Load("Button_Main");
        var cellBg = Load("Cell_Item");
        var pill = LoadAt("Assets/Art/Space/ui/bar_round_small.png");
        var crate1 = Load("Crate_Common");
        var crate2 = Load("Crate_Gold");

        // 1) SafeAreaPanel.prefab — BottomBar 전폭 3분할 (탭=Button_Tab, PLAY=Button_Main+틸)
        const string safeAreaPath = "Assets/Prefabs/UI/SafeAreaPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(safeAreaPath);
        var bar = root.transform.Find("BottomBar");

        var barImg = bar.GetComponent<Image>() ?? bar.gameObject.AddComponent<Image>();
        barImg.sprite = panel; barImg.type = Image.Type.Sliced;
        barImg.color = new Color(1f, 1f, 1f, 0.95f);
        barImg.raycastTarget = false;

        Segment(bar.Find("ShopButton"), 0.00f, 0.32f, 0f, tab, Color.white);
        Segment(bar.Find("HomeButton"), 0.32f, 0.68f, 30f, main, Teal);   // PLAY 돌출+강조
        Segment(bar.Find("SkinButton"), 0.68f, 1.00f, 0f, tab, Color.white);

        var closeBtn = root.transform.Find("ContentArea/LobbyScreen/StaminaEmptyPopup/Panel/CloseButton");
        var closeImg = closeBtn.GetComponent<Image>();
        closeImg.sprite = main; closeImg.type = Image.Type.Sliced; closeImg.color = Teal;

        PrefabUtility.SaveAsPrefabAsset(root, safeAreaPath);
        PrefabUtility.UnloadPrefabContents(root);

        // 1-1) SkinItemUI.prefab — 셀=Cell_Item, 장착 버튼=Button_Main
        const string skinItemPath = "Assets/Prefabs/UI/SkinItemUI.prefab";
        var cell = PrefabUtility.LoadPrefabContents(skinItemPath);
        var cellImg = cell.GetComponent<Image>();
        cellImg.sprite = cellBg; cellImg.type = Image.Type.Sliced; cellImg.color = Color.white;
        var equipImg = cell.transform.Find("EquipButton").GetComponent<Image>();
        equipImg.sprite = main; equipImg.type = Image.Type.Sliced; equipImg.color = Teal;
        PrefabUtility.SaveAsPrefabAsset(cell, skinItemPath);
        PrefabUtility.UnloadPrefabContents(cell);

        // 2) LobbyScene — ShopScreen 타이틀 + 뽑기 카드 내부(함선 아트·가격 필)
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
        var canvas = GameObject.Find("Canvas");
        Transform shop = null, one = null, ten = null;
        foreach (var tr in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name == "ShopScreen") shop = tr;
            else if (tr.name == "DrawOneButton") one = tr;
            else if (tr.name == "DrawTenButton") ten = tr;
        }
        var fontSrc = one.Find("Text").GetComponent<TMP_Text>();   // 폰트/머티리얼 복제 원본 (Kostar)

        AddTitle(shop, fontSrc, "ShopTitle", "PILOT GACHA");
        // 가격은 GachaService 상수가 SSOT — 리터럴로 박으면 비용 변경 시 씬 텍스트가 거짓말 (검수 v6).
        // 런타임에도 ShopScreen.Awake가 같은 상수로 덮어써 이중 방어
        BuildCard(one, fontSrc, "뽑기 1회", GachaService.DrawOneCost.ToString(), crate1, coin, pill, new Vector2(-170f, -30f), null);
        BuildCard(ten, fontSrc, "뽑기 10회", GachaService.DrawTenCost.ToString(), crate2, coin, pill, new Vector2(170f, -30f), "×10");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OutGameUISkinner] BottomBar 탭바 + Shop 카드 구성 완료");
    }

    // ── 원복: 하단 탭바 개편 취소 (2026-07-16 유저 — 버튼 스프라이트가 잘려 보임) + 1뽑 카드 ×1 뱃지 ──
    [MenuItem("Tools/OutGame/Revert Bottom Bar + Fix Badge")]
    public static void RevertBottomBarAndFixBadge()
    {
        var circle = LoadAt("Assets/Sprites/Circle.png");

        const string safeAreaPath = "Assets/Prefabs/UI/SafeAreaPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(safeAreaPath);
        var bar = root.transform.Find("BottomBar");

        var barImg = bar.GetComponent<Image>();
        if (barImg != null) Object.DestroyImmediate(barImg);

        RestoreButton(bar.Find("ShopButton"), new Vector2(0f, 0.5f), new Vector2(30f, 0f), 100f, null, Color.white);
        RestoreButton(bar.Find("HomeButton"), new Vector2(0.5f, 0.5f), Vector2.zero, 140f, circle, Color.white);
        RestoreButton(bar.Find("SkinButton"), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), 100f, null, Color.white);

        var closeImg = root.transform.Find("ContentArea/LobbyScreen/StaminaEmptyPopup/Panel/CloseButton").GetComponent<Image>();
        closeImg.sprite = null; closeImg.color = new Color(0.3f, 0.5f, 0.9f, 1f);

        PrefabUtility.SaveAsPrefabAsset(root, safeAreaPath);
        PrefabUtility.UnloadPrefabContents(root);

        const string skinItemPath = "Assets/Prefabs/UI/SkinItemUI.prefab";
        var cell = PrefabUtility.LoadPrefabContents(skinItemPath);
        var equipImg = cell.transform.Find("EquipButton").GetComponent<Image>();
        equipImg.sprite = null; equipImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        PrefabUtility.SaveAsPrefabAsset(cell, skinItemPath);
        PrefabUtility.UnloadPrefabContents(cell);

        // 1뽑 카드에 ×1 뱃지 (10뽑과 동형)
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
        var canvas = GameObject.Find("Canvas");
        Transform one = null;
        foreach (var tr in canvas.GetComponentsInChildren<Transform>(true))
            if (tr.name == "DrawOneButton") one = tr;
        var fontSrc = one.Find("Text").GetComponent<TMP_Text>();
        var oldBadge = one.Find("Badge");
        if (oldBadge != null) Object.DestroyImmediate(oldBadge.gameObject);
        var badge = NewText(one, fontSrc, "Badge", "×1", 44f);
        var brt = (RectTransform)badge.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.55f);
        brt.anchoredPosition = new Vector2(0f, -110f);
        brt.sizeDelta = new Vector2(200f, 60f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OutGameUISkinner] 탭바 원복 + ×1 뱃지 완료");
    }

    // 표준 탭바 구조: 사각 버튼 배경 + 중앙 아이콘 (위치 정렬은 인스펙터 몫 — feedback-inspector-over-builder)
    [MenuItem("Tools/OutGame/Iconize Bottom Buttons")]
    public static void IconizeBottomButtons()
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var navy = new Color(0.07f, 0.11f, 0.22f, 0.95f);

        const string safeAreaPath = "Assets/Prefabs/UI/SafeAreaPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(safeAreaPath);
        var bar = root.transform.Find("BottomBar");

        Iconize(bar.Find("ShopButton"), uiSprite, navy, Load("Icon_Shop"));
        Iconize(bar.Find("HomeButton"), uiSprite, navy, Load("Icon_Home"));
        Iconize(bar.Find("SkinButton"), uiSprite, navy, Load("Icon_Skin"));

        PrefabUtility.SaveAsPrefabAsset(root, safeAreaPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[OutGameUISkinner] 하단 버튼 아이콘화 완료 — 위치/크기는 인스펙터에서 조정");
    }

    static void Iconize(Transform btn, Sprite bg, Color bgColor, Sprite icon)
    {
        var img = btn.GetComponent<Image>();
        img.sprite = bg; img.type = Image.Type.Sliced; img.color = bgColor;

        var text = btn.Find("Text");
        if (text != null) text.gameObject.SetActive(false);   // 아이콘 온리 (필요 시 인스펙터에서 재활성)

        var old = btn.Find("Icon");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(btn, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(64f, 64f);
        var iconImg = go.GetComponent<Image>();
        iconImg.sprite = icon; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
    }

    // 설정 버튼 텍스트 → 톱니 아이콘 (TopBar는 아이콘 온리 표준 — 2026-07-16 유저)
    [MenuItem("Tools/OutGame/Iconize Options Button")]
    public static void IconizeOptionsButton()
    {
        const string safeAreaPath = "Assets/Prefabs/UI/SafeAreaPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(safeAreaPath);
        var btn = root.transform.Find("TopBar/OptionsButton");

        var img = btn.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);   // 배경 투명 — 히트 영역은 유지(레이캐스트는 알파 무관)

        var text = btn.Find("Text");
        if (text != null) text.gameObject.SetActive(false);

        var old = btn.Find("Icon");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(btn, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
        var iconImg = go.GetComponent<Image>();
        iconImg.sprite = Load("Icon_Settings");
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        PrefabUtility.SaveAsPrefabAsset(root, safeAreaPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[OutGameUISkinner] 설정 버튼 톱니 아이콘화 완료");
    }

    // 인게임 PausePanel 설정 버튼에 톱니 아이콘 (텍스트 유지 — 메뉴 형제들과 통일)
    [MenuItem("Tools/OutGame/Iconize InGame Settings Button")]
    public static void IconizeInGameSettingsButton()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/InGameScene.unity");
        Transform btn = null;
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (tr.name == "SettingsButton") btn = tr;

        var old = btn.Find("Icon");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(btn, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.14f, 0.5f);
        rt.sizeDelta = new Vector2(52f, 52f);
        var img = go.GetComponent<Image>();
        img.sprite = Load("Icon_Settings");
        img.preserveAspect = true; img.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OutGameUISkinner] 인게임 설정 버튼 톱니 아이콘 완료");
    }

    // 설정 팝업 표준 패턴 적용: 우상단 X + 행 아이콘 + 두꺼운 틸 슬라이더 (인게임 일시정지 설정과 공유)
    [MenuItem("Tools/OutGame/Restyle Settings Panel")]
    public static void RestyleSettingsPanel()
    {
        const string path = "Assets/Prefabs/SettingsPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var card = root.transform.Find("Card");

        // 닫기 → 우상단 X (Button 오브젝트 이동이라 리스너 배선 그대로)
        var close = (RectTransform)card.Find("CloseButton");
        close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
        close.pivot = new Vector2(1f, 1f);
        close.anchoredPosition = new Vector2(-14f, -14f);
        close.sizeDelta = new Vector2(56f, 56f);
        close.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.22f, 0.9f);
        var closeText = close.Find("Text").GetComponent<TMP_Text>();
        closeText.text = "✕";
        closeText.fontSize = 34f;

        // 행 아이콘 (라벨 왼쪽) + 라벨 자리 조정
        AddRowIcon(card, "BgmIcon", Load("Icon_Bgm"), 0.70f);
        AddRowIcon(card, "SfxIcon", Load("Icon_Sfx"), 0.48f);
        SetAnchors(card.Find("BGMLabel"), new Vector2(0.17f, 0.62f), new Vector2(0.38f, 0.78f));
        SetAnchors(card.Find("SFXLabel"), new Vector2(0.17f, 0.40f), new Vector2(0.38f, 0.56f));

        // 슬라이더: 두께 + 남색 트랙/틸 채움/큰 핸들
        RestyleSlider(card.Find("BGMSlider"), 0.655f, 0.745f);
        RestyleSlider(card.Find("SFXSlider"), 0.435f, 0.525f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[OutGameUISkinner] 설정 팝업 리스타일 완료");
    }

    static void AddRowIcon(Transform card, string name, Sprite sprite, float centerY)
    {
        var old = card.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(card, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.11f, centerY);
        rt.sizeDelta = new Vector2(52f, 52f);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
    }

    static void RestyleSlider(Transform slider, float y0, float y1)
    {
        SetAnchors(slider, new Vector2(0.42f, y0), new Vector2(0.90f, y1));
        var bg = slider.Find("Background").GetComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.24f, 1f);
        var fill = slider.Find("Fill Area/Fill").GetComponent<Image>();
        fill.color = Teal;
        var handle = (RectTransform)slider.Find("Handle Slide Area/Handle");
        handle.sizeDelta = new Vector2(36f, 8f);   // knob 원형 + 위아래로 살짝 돌출
    }

    static void SetAnchors(Transform tr, Vector2 min, Vector2 max)
    {
        var rt = (RectTransform)tr;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    // 1회용: EquippedIndicator 오브젝트 제거 (장착 상태는 버튼 텍스트 "장착중"으로 충분 — 2026-07-16 유저)
    [MenuItem("Tools/OutGame/Remove EquippedIndicator")]
    public static void RemoveEquippedIndicator()
    {
        const string skinItemPath = "Assets/Prefabs/UI/SkinItemUI.prefab";
        var cell = PrefabUtility.LoadPrefabContents(skinItemPath);
        var indicator = cell.transform.Find("EquippedIndicator");
        if (indicator != null) Object.DestroyImmediate(indicator.gameObject);
        PrefabUtility.SaveAsPrefabAsset(cell, skinItemPath);
        PrefabUtility.UnloadPrefabContents(cell);
        Debug.Log("[OutGameUISkinner] EquippedIndicator 제거 완료");
    }

    static void RestoreButton(Transform btn, Vector2 anchor, Vector2 pos, float size, Sprite sprite, Color color)
    {
        var rt = (RectTransform)btn;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        var img = btn.GetComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Simple; img.color = color;
    }

    // 전폭 세그먼트: anchors 가로 분할·세로 스트레치, poke만큼 위로 돌출
    static void Segment(Transform btn, float x0, float x1, float poke, Sprite sprite, Color tint)
    {
        var rt = (RectTransform)btn;
        rt.anchorMin = new Vector2(x0, 0f);
        rt.anchorMax = new Vector2(x1, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, poke);
        var img = btn.GetComponent<Image>();
        img.sprite = sprite; img.type = Image.Type.Sliced; img.color = tint;
    }

    static void AddTitle(Transform parent, TMP_Text src, string name, string text)
    {
        var old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var t = NewText(parent, src, name, text, 56f);
        var rt = (RectTransform)t.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -60f);
        rt.sizeDelta = new Vector2(600f, 80f);
    }

    // 카드 내부: 상단 제목(기존 Text 재배치) + 중앙 함선 + 하단 가격 필(코인 아이콘+숫자)
    static void BuildCard(Transform card, TMP_Text fontSrc, string title, string price,
                          Sprite art, Sprite coin, Sprite pill, Vector2 pos, string badge)
    {
        var rt = (RectTransform)card;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300f, 511f);   // Card_Gacha 원본비 676:1152

        var text = card.Find("Text").GetComponent<TMP_Text>();
        text.text = title;
        text.fontSize = 40f;
        text.alignment = TextAlignmentOptions.Center;
        var trt = (RectTransform)text.transform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -66f);
        trt.sizeDelta = new Vector2(0f, 60f);

        foreach (var n in new[] { "ShipArt", "CrateArt", "PricePill", "Badge" })
        {
            var old = card.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        var artGo = new GameObject("CrateArt", typeof(RectTransform), typeof(Image));
        var art_rt = (RectTransform)artGo.transform;
        art_rt.SetParent(card, false);
        art_rt.anchorMin = art_rt.anchorMax = new Vector2(0.5f, 0.55f);
        art_rt.sizeDelta = new Vector2(210f, 160f);
        var artImg = artGo.GetComponent<Image>();
        artImg.sprite = art; artImg.preserveAspect = true; artImg.raycastTarget = false;

        var pillGo = new GameObject("PricePill", typeof(RectTransform), typeof(Image));
        var pillRt = (RectTransform)pillGo.transform;
        pillRt.SetParent(card, false);
        pillRt.anchorMin = pillRt.anchorMax = new Vector2(0.5f, 0f);
        pillRt.pivot = new Vector2(0.5f, 0f);
        pillRt.anchoredPosition = new Vector2(0f, 60f);
        pillRt.sizeDelta = new Vector2(180f, 52f);
        var pillImg = pillGo.GetComponent<Image>();
        pillImg.sprite = pill;
        pillImg.type = pill != null && pill.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        pillImg.raycastTarget = false;

        var coinGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        var coinRt = (RectTransform)coinGo.transform;
        coinRt.SetParent(pillGo.transform, false);
        coinRt.anchorMin = coinRt.anchorMax = new Vector2(0f, 0.5f);
        coinRt.pivot = new Vector2(0f, 0.5f);
        coinRt.anchoredPosition = new Vector2(12f, 0f);
        coinRt.sizeDelta = new Vector2(36f, 36f);
        var coinImg = coinGo.GetComponent<Image>();
        coinImg.sprite = coin; coinImg.raycastTarget = false;

        var priceTxt = NewText(pillGo.transform, fontSrc, "PriceText", price, 30f);
        var prt = (RectTransform)priceTxt.transform;
        prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(1f, 1f);
        prt.offsetMin = new Vector2(52f, 0f); prt.offsetMax = new Vector2(-12f, 0f);

        if (badge != null)
        {
            var b = NewText(card, fontSrc, "Badge", badge, 44f);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.55f);
            brt.anchoredPosition = new Vector2(0f, -110f);
            brt.sizeDelta = new Vector2(200f, 60f);
        }
    }

    static TMP_Text NewText(Transform parent, TMP_Text src, string name, string text, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = src.font;
        t.fontSharedMaterial = src.fontSharedMaterial;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static Sprite LoadAt(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError($"[OutGameUISkinner] 스프라이트 없음: {path}");
        return s;
    }

    static Sprite Load(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
        if (s == null) Debug.LogError($"[OutGameUISkinner] 스프라이트 없음: {name}");
        return s;
    }

    // 9-slice 패널 적용 — 틴트 제거(스프라이트 자체가 어두운 네이비)
    static void SetPanel(Transform tr, Sprite panel)
    {
        var img = tr.GetComponent<Image>();
        img.sprite = panel;
        img.color = Color.white;
        img.type = Image.Type.Sliced;
    }

    // 텍스트 위젯 왼쪽 바깥에 아이콘 Image 자식 부착
    static void AddIconLeftOf(Transform text, string name, Sprite sprite)
    {
        var old = text.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(text, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);   // 부모 왼쪽 중앙
        rt.pivot = new Vector2(1f, 0.5f);                      // 자기 오른쪽 끝 기준 = 텍스트 왼쪽 바깥으로
        rt.anchoredPosition = new Vector2(-6f, 0f);
        rt.sizeDelta = new Vector2(44f, 44f);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }
}
