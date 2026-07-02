using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Reflection;

public static class RoguelikeSceneSetup
{
    [MenuItem("Tools/Roguelike/Setup Scene")]
    public static void SetupScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();

        foreach (var n in new[] { "Canvas", "EventSystem", "GameManager" })
        {
            var old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }

        // ── EventSystem ───────────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
            es.AddComponent(inputModuleType);
        else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var ct = canvasGO.transform;

        // ── SCREENS ───────────────────────────────────────────────────────────
        var mapGO      = FullPanel(ct, "MapScreen",      new Color(0.06f, 0.06f, 0.12f, 1f));
        var combatGO   = FullPanel(ct, "CombatScreen",   new Color(0.07f, 0.07f, 0.12f, 1f));
        var upgradeGO  = FullPanel(ct, "UpgradeScreen",  new Color(0.07f, 0.10f, 0.08f, 1f));
        var gameOverGO = FullPanel(ct, "GameOverScreen", new Color(0.12f, 0.04f, 0.04f, 1f));

        // ── MAP SCREEN ────────────────────────────────────────────────────────
        Txt(mapGO.transform, "Title", "THE OUTLAWS",
            36f, V2(0, 325), V2(700, 58), new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center);

        var mapFloorTMP = Txt(mapGO.transform, "FloorText", "Battle 1 / 8",
            18f, V2(0, 290), V2(500, 34), new Color(0.75f, 0.75f, 0.75f, 1f), TextAlignmentOptions.Center);

        var mapGoldTMP = Txt(mapGO.transform, "GoldText", "Gold: 0",
            20f, V2(0, -325), V2(400, 38), new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center);

        // ── Scroll View (fills between title bar and gold bar) ────────────────
        var scrollGO = new GameObject("MapScrollView", typeof(RectTransform));
        scrollGO.transform.SetParent(mapGO.transform, false);
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = V2(20f, 48f);    // leave room for gold text at bottom
        scrollRT.offsetMax = V2(-20f, -72f);  // leave room for title + floor at top
        scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);  // transparent
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;

        // Viewport
        var vpGO = new GameObject("Viewport", typeof(RectTransform));
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = (RectTransform)vpGO.transform;
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<RectMask2D>();

        // Content (MapRenderer draws into this)
        // Height = 7 rows * 78px spacing + 150px padding = 696px
        var contentGO = new GameObject("MapContent", typeof(RectTransform));
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.anchorMin         = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax         = new Vector2(0.5f, 0.5f);
        contentRT.anchoredPosition  = Vector2.zero;
        contentRT.sizeDelta         = V2(1000f, 1340f);  // 7 * 165 + 185 padding

        scrollRect.viewport = vpRT;
        scrollRect.content  = contentRT;

        // ── COMBAT (Setup A – Classic Duel) ───────────────────────────────────

        // — Player (left) —
        var playerImg = ImgSlot(combatGO.transform, "PlayerSprite",
            V2(-320f, 55f), V2(250f, 330f), new Color(0.25f, 0.35f, 0.25f, 0.5f));
        playerImg.preserveAspect = true;

        Txt(combatGO.transform, "PlayerLabel", "THE OUTLAW",
            16f, V2(-320f, 240f), V2(250f, 28f),
            new Color(0.4f, 1f, 0.4f, 1f), TextAlignmentOptions.Center);

        var playerHpFill = HpBarFill(combatGO.transform, "PlayerHpBar",
            V2(-320f, -120f), V2(250f, 20f));

        var playerHpTMP = Txt(combatGO.transform, "PlayerHP", "HP: 100 / 100",
            11f, V2(-320f, -120f), V2(250f, 20f),
            Color.white, TextAlignmentOptions.Center);

        var playerBullets = IconRow(combatGO.transform, "PlayerBullet",
            V2(-320f, -152f), 6, 35f, 26f, new Color(1f, 0.88f, 0.2f, 1f));

        var playerShields = IconRow(combatGO.transform, "PlayerShield",
            V2(-320f, -188f), 3, 42f, 26f, new Color(0.35f, 0.65f, 1f, 1f));

        var playerBulTMP = Txt(combatGO.transform, "PlayerBulletsText", "",
            8f, V2(-320f, -218f), V2(200f, 18f),
            new Color(0.5f, 0.5f, 0.5f, 0.4f), TextAlignmentOptions.Center);

        // — Enemy (right) —
        var enemyImg = ImgSlot(combatGO.transform, "EnemySprite",
            V2(320f, 55f), V2(250f, 330f), new Color(0.35f, 0.15f, 0.15f, 0.5f));
        enemyImg.preserveAspect = true;
        enemyImg.transform.localScale = new Vector3(-1f, 1f, 1f);

        var enemyTierTMP = Txt(combatGO.transform, "EnemyLabel", "ENEMY",
            16f, V2(320f, 240f), V2(250f, 28f),
            new Color(1f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Center);

        var enemyHpFill = HpBarFill(combatGO.transform, "EnemyHpBar",
            V2(320f, -120f), V2(250f, 20f));

        var enemyHpTMP = Txt(combatGO.transform, "EnemyHP", "HP: 100 / 100",
            11f, V2(320f, -120f), V2(250f, 20f),
            Color.white, TextAlignmentOptions.Center);

        var enemyBullets = IconRow(combatGO.transform, "EnemyBullet",
            V2(320f, -152f), 6, 35f, 26f, new Color(1f, 0.88f, 0.2f, 1f));

        var enemyBlockTMP = Txt(combatGO.transform, "EnemyBlockUsesText", "Shield: 3 / 3",
            14f, V2(320f, -218f), V2(210f, 28f),
            new Color(0.45f, 0.70f, 1f, 1f), TextAlignmentOptions.Center);

        var enemyBulTMP = Txt(combatGO.transform, "EnemyBulletsText", "",
            8f, V2(320f, -188f), V2(200f, 18f),
            new Color(0.5f, 0.5f, 0.5f, 0.4f), TextAlignmentOptions.Center);

        // — Center: combat log —
        var lp = SzPanel(combatGO.transform, "LogPanel",
            new Color(0f, 0f, 0f, 0.55f), V2(0f, 28f), V2(280f, 250f));
        var logGO = new GameObject("CombatLog");
        logGO.transform.SetParent(lp.transform, false);
        var logRT  = logGO.AddComponent<RectTransform>();
        logRT.anchorMin = Vector2.zero; logRT.anchorMax = Vector2.one;
        logRT.offsetMin = V2(10, 8);   logRT.offsetMax  = V2(-10, -8);
        var logTMP = logGO.AddComponent<TextMeshProUGUI>();
        logTMP.fontSize         = 14f;
        logTMP.color            = new Color(0.88f, 0.88f, 0.88f, 1f);
        logTMP.alignment        = TextAlignmentOptions.TopLeft;
        logTMP.textWrappingMode = TextWrappingModes.Normal;
        logTMP.overflowMode     = TextOverflowModes.Truncate;

        var selTMP = Txt(combatGO.transform, "SelectedAction", "Select an action",
            18f, V2(0f, -228f), V2(350f, 30f),
            new Color(0.8f, 0.9f, 1f, 1f), TextAlignmentOptions.Center);

        // — Action buttons (bottom) —
        var fireBtn   = Btn(combatGO.transform, "FireButton",   "FIRE",   V2(-255f, -285f), V2(210f, 55f), new Color(0.55f, 0.13f, 0.13f, 1f));
        var defendBtn = Btn(combatGO.transform, "DefendButton", "DEFEND", V2(0f,    -285f), V2(210f, 55f), new Color(0.13f, 0.33f, 0.55f, 1f));
        var reloadBtn = Btn(combatGO.transform, "ReloadButton", "RELOAD", V2(255f,  -285f), V2(210f, 55f), new Color(0.13f, 0.45f, 0.13f, 1f));

        var blockTMP = Txt(combatGO.transform, "BlockUsesText", "Shield: 3 / 3",
            14f, V2(0f, -330f), V2(210f, 28f),
            new Color(0.45f, 0.70f, 1f, 1f), TextAlignmentOptions.Center);

        // ── LOOT SCREEN ───────────────────────────────────────────────────────
        var lootTitleTMP = Txt(upgradeGO.transform, "LootTitle", "LOOT",
            42f, V2(0, 290), V2(700, 70), new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center);
        var lootInfoTMP = Txt(upgradeGO.transform, "LootInfo", "+?? HP  →  ??/??    +?? Gold  (Total: 0)",
            22f, V2(0, 230), V2(720, 45), Color.white, TextAlignmentOptions.Center);

        var upgBtn1 = Btn(upgradeGO.transform, "UpgradeButton1", "Upgrade 1", V2(0,  120), V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));
        var upgBtn2 = Btn(upgradeGO.transform, "UpgradeButton2", "Upgrade 2", V2(0,    0), V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));
        var upgBtn3 = Btn(upgradeGO.transform, "UpgradeButton3", "Upgrade 3", V2(0, -120), V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));

        var upgTxt1 = upgBtn1.GetComponentInChildren<TextMeshProUGUI>();
        var upgTxt2 = upgBtn2.GetComponentInChildren<TextMeshProUGUI>();
        var upgTxt3 = upgBtn3.GetComponentInChildren<TextMeshProUGUI>();
        foreach (var t in new[] { upgTxt1, upgTxt2, upgTxt3 }) t.fontSize = 18f;

        // ── GAME OVER ─────────────────────────────────────────────────────────
        var goTMP = Txt(gameOverGO.transform, "GameOverText",
            "YOU DIED\nBetter luck next time",
            52f, V2(0, 90), V2(800, 220), new Color(1f, 0.3f, 0.3f, 1f), TextAlignmentOptions.Center);
        var restartBtn = Btn(gameOverGO.transform, "RestartButton", "PLAY AGAIN",
            V2(0, -90), V2(300, 70), new Color(0.25f, 0.25f, 0.45f, 1f));

        // ── MANAGERS ──────────────────────────────────────────────────────────
        var mgrsGO  = new GameObject("GameManager");
        mgrsGO.AddComponent<GameManager>();
        mgrsGO.AddComponent<CombatManager>();
        var uiMgr   = mgrsGO.AddComponent<UIManager>();
        var mapRend = mgrsGO.AddComponent<MapRenderer>();

        SetF(mapRend, "mapContainer", contentRT);
        SetF(mapRend, "scrollRect",   scrollRect);

        SetF(uiMgr, "mapScreen",          mapGO);
        SetF(uiMgr, "combatScreen",       combatGO);
        SetF(uiMgr, "lootScreen",         upgradeGO);
        SetF(uiMgr, "gameOverScreen",     gameOverGO);
        SetF(uiMgr, "mapRenderer",        mapRend);
        SetF(uiMgr, "mapGoldText",        mapGoldTMP);
        SetF(uiMgr, "mapFloorText",       mapFloorTMP);
        SetF(uiMgr, "playerHpText",       playerHpTMP);
        SetF(uiMgr, "playerBulletsText",  playerBulTMP);
        SetF(uiMgr, "enemyHpText",        enemyHpTMP);
        SetF(uiMgr, "enemyBulletsText",   enemyBulTMP);
        SetF(uiMgr, "selectedActionText", selTMP);
        SetF(uiMgr, "fireButton",         fireBtn);
        SetF(uiMgr, "defendButton",       defendBtn);
        SetF(uiMgr, "reloadButton",       reloadBtn);
        SetF(uiMgr, "blockUsesText",      blockTMP);
        SetF(uiMgr, "enemyBlockUsesText", enemyBlockTMP);
        SetF(uiMgr, "combatLogText",      logTMP);
        SetF(uiMgr, "lootTitleText",      lootTitleTMP);
        SetF(uiMgr, "lootInfoText",       lootInfoTMP);
        SetF(uiMgr, "upgradeButtons",     new Button[]          { upgBtn1, upgBtn2, upgBtn3 });
        SetF(uiMgr, "upgradeButtonTexts", new TextMeshProUGUI[] { upgTxt1, upgTxt2, upgTxt3 });
        SetF(uiMgr, "gameOverText",       goTMP);
        SetF(uiMgr, "restartButton",      restartBtn);

        var bv = mgrsGO.AddComponent<BattleVisuals>();
        SetF(bv,    "playerSprite",       playerImg);
        SetF(bv,    "enemySprite",        enemyImg);
        SetF(bv,    "enemyTierLabel",     enemyTierTMP);
        SetF(bv,    "playerHpFill",       playerHpFill);
        SetF(bv,    "enemyHpFill",        enemyHpFill);
        SetF(bv,    "playerBulletIcons",  playerBullets);
        SetF(bv,    "enemyBulletIcons",   enemyBullets);
        SetF(bv,    "shieldIcons",        playerShields);
        SetF(uiMgr, "battleVisuals",      bv);

        mapGO.SetActive(true);
        combatGO.SetActive(false);
        upgradeGO.SetActive(false);
        gameOverGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[Roguelike] Scene setup complete! Press Play to start.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Vector2 V2(float x, float y) => new Vector2(x, y);

    static GameObject FullPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject SzPanel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI Txt(Transform parent, string name, string text, float fontSize,
        Vector2 pos, Vector2 size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.color = color; tmp.alignment = align;
        return tmp;
    }

    static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();

        var tgo = new GameObject("Text (TMP)", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = V2(8, 4); trt.offsetMax = V2(-8, -4);
        var tmp = tgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 20f;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return btn;
    }

    static void SetF(Component c, string field, object value)
    {
        var f = c.GetType().GetField(field,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(c, value);
        else Debug.LogWarning($"[Setup] Field not found: {c.GetType().Name}.{field}");
    }

    static Image ImgSlot(Transform parent, string name, Vector2 pos, Vector2 size, Color placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = placeholder;
        return img;
    }

    static Image HpBarFill(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var bgGO = SzPanel(parent, name, new Color(0.20f, 0.05f, 0.05f, 1f), pos, size);
        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRT = (RectTransform)fillGO.transform;
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        var fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.18f, 0.78f, 0.28f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        return fill;
    }

    static Image[] IconRow(Transform parent, string name, Vector2 center,
        int count, float spacing, float iconSize, Color color)
    {
        var icons = new Image[count];
        float halfSpan = (count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"{name}_{i}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x - halfSpan + i * spacing, center.y);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            var img = go.AddComponent<Image>();
            img.color = color;
            icons[i] = img;
        }
        return icons;
    }
}
