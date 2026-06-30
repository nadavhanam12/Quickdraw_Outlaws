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

        // Remove stale objects from previous runs
        foreach (var n in new[] { "Canvas", "EventSystem", "GameManager", "TestChild" })
        {
            var old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }

        // EventSystem — use InputSystem module to match project Input settings
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
            es.AddComponent(inputModuleType);
        else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var ct = canvasGO.transform;

        // ── SCREENS ──────────────────────────────────────────────────
        var mapGO      = FullPanel(ct, "MapScreen",      new Color(0.08f, 0.08f, 0.14f, 1f));
        var combatGO   = FullPanel(ct, "CombatScreen",   new Color(0.07f, 0.07f, 0.12f, 1f));
        var upgradeGO  = FullPanel(ct, "UpgradeScreen",  new Color(0.07f, 0.10f, 0.08f, 1f));
        var gameOverGO = FullPanel(ct, "GameOverScreen", new Color(0.12f, 0.04f, 0.04f, 1f));

        // ── MAP ──────────────────────────────────────────────────────
        Txt(mapGO.transform, "Title", "~ THE OUTLAWS ~",
            42f, V2(0, 235), V2(700, 90), Color.white, TextAlignmentOptions.Center);
        Txt(mapGO.transform, "Subtitle", "Choose a path to start a battle",
            22f, V2(0, 158), V2(700, 50), new Color(0.85f, 0.85f, 0.6f, 1f), TextAlignmentOptions.Center);
        var pathBtn1 = Btn(mapGO.transform, "PathButton1", "Path 1  —  Dusty Road",   V2(0, 55),   V2(440, 65), new Color(0.32f, 0.22f, 0.10f, 1f));
        var pathBtn2 = Btn(mapGO.transform, "PathButton2", "Path 2  —  Ghost Town",   V2(0, -30),  V2(440, 65), new Color(0.32f, 0.22f, 0.10f, 1f));
        var pathBtn3 = Btn(mapGO.transform, "PathButton3", "Path 3  —  Desert Pass",  V2(0, -115), V2(440, 65), new Color(0.32f, 0.22f, 0.10f, 1f));

        // ── COMBAT ───────────────────────────────────────────────────
        var ep = SzPanel(combatGO.transform, "EnemyPanel",
            new Color(0.35f, 0.08f, 0.08f, 0.85f), V2(0, 298), V2(800, 78));
        Txt(ep.transform, "EL", "ENEMY", 12f, V2(0, 22), V2(780, 22),
            new Color(1f, 0.5f, 0.5f, 1f), TextAlignmentOptions.Center);
        var enemyHpTMP  = Txt(ep.transform, "EnemyHP",      "HP: 100 / 100", 22f, V2(-178, -4), V2(330, 44), Color.white, TextAlignmentOptions.Center);
        var enemyBulTMP = Txt(ep.transform, "EnemyBullets", "Bullets: 0 / 6",22f, V2(178,  -4), V2(280, 44), new Color(1f, 0.9f, 0.3f, 1f), TextAlignmentOptions.Center);

        var lp = SzPanel(combatGO.transform, "LogPanel",
            new Color(0f, 0f, 0f, 0.55f), V2(0, 65), V2(800, 268));
        var logGO = new GameObject("CombatLog");
        logGO.transform.SetParent(lp.transform, false);
        var logRT = logGO.AddComponent<RectTransform>();
        logRT.anchorMin = Vector2.zero; logRT.anchorMax = Vector2.one;
        logRT.offsetMin = V2(10, 8);   logRT.offsetMax = V2(-10, -8);
        var logTMP = logGO.AddComponent<TextMeshProUGUI>();
        logTMP.fontSize = 15f;
        logTMP.color = new Color(0.88f, 0.88f, 0.88f, 1f);
        logTMP.alignment = TextAlignmentOptions.TopLeft;
        logTMP.textWrappingMode = TextWrappingModes.Normal;
        logTMP.overflowMode = TextOverflowModes.Truncate;

        var selTMP  = Txt(combatGO.transform, "SelectedAction", "Select an action",
            20f, V2(0, -95), V2(600, 40), new Color(0.8f, 0.9f, 1f, 1f), TextAlignmentOptions.Center);
        var fireBtn   = Btn(combatGO.transform, "FireButton",   "FIRE",   V2(-245, -168), V2(205, 58), new Color(0.55f, 0.13f, 0.13f, 1f));
        var defendBtn = Btn(combatGO.transform, "DefendButton", "DEFEND", V2(0,    -168), V2(205, 58), new Color(0.13f, 0.33f, 0.55f, 1f));
        var reloadBtn = Btn(combatGO.transform, "ReloadButton", "RELOAD", V2(245,  -168), V2(205, 58), new Color(0.13f, 0.45f, 0.13f, 1f));

        var pp = SzPanel(combatGO.transform, "PlayerPanel",
            new Color(0.08f, 0.30f, 0.08f, 0.85f), V2(0, -298), V2(800, 78));
        Txt(pp.transform, "PL", "PLAYER", 12f, V2(0, 22), V2(780, 22),
            new Color(0.5f, 1f, 0.5f, 1f), TextAlignmentOptions.Center);
        var playerHpTMP  = Txt(pp.transform, "PlayerHP",      "HP: 100 / 100", 22f, V2(-178, -4), V2(330, 44), Color.white, TextAlignmentOptions.Center);
        var playerBulTMP = Txt(pp.transform, "PlayerBullets", "Bullets: 0 / 6",22f, V2(178,  -4), V2(280, 44), new Color(1f, 0.9f, 0.3f, 1f), TextAlignmentOptions.Center);

        // ── LOOT SCREEN ───────────────────────────────────────────────
        var lootTitleTMP = Txt(upgradeGO.transform, "LootTitle", "LOOT",
            42f, V2(0, 290), V2(700, 70), new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center);
        var lootInfoTMP = Txt(upgradeGO.transform, "LootInfo", "+?? HP    +?? Gold    (Total: 0)",
            22f, V2(0, 230), V2(700, 45), Color.white, TextAlignmentOptions.Center);

        // 3 upgrade choice buttons (tall enough for 2 lines of text)
        var upgBtn1 = Btn(upgradeGO.transform, "UpgradeButton1", "Upgrade 1",  V2(0, 120),  V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));
        var upgBtn2 = Btn(upgradeGO.transform, "UpgradeButton2", "Upgrade 2",  V2(0,   0),  V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));
        var upgBtn3 = Btn(upgradeGO.transform, "UpgradeButton3", "Upgrade 3",  V2(0, -120), V2(620, 90), new Color(0.22f, 0.18f, 0.38f, 1f));

        // Grab the TMP components inside each button for runtime text updates
        var upgTxt1 = upgBtn1.GetComponentInChildren<TextMeshProUGUI>();
        var upgTxt2 = upgBtn2.GetComponentInChildren<TextMeshProUGUI>();
        var upgTxt3 = upgBtn3.GetComponentInChildren<TextMeshProUGUI>();
        foreach (var t in new[] { upgTxt1, upgTxt2, upgTxt3 })
            t.fontSize = 18f;

        // ── GAME OVER ─────────────────────────────────────────────────
        var goTMP = Txt(gameOverGO.transform, "GameOverText",
            "YOU DIED\nBetter luck next time",
            52f, V2(0, 90), V2(800, 220), new Color(1f, 0.3f, 0.3f, 1f), TextAlignmentOptions.Center);
        var restartBtn = Btn(gameOverGO.transform, "RestartButton", "PLAY AGAIN",
            V2(0, -90), V2(300, 70), new Color(0.25f, 0.25f, 0.45f, 1f));

        // ── MANAGERS ─────────────────────────────────────────────────
        var mgrsGO = new GameObject("GameManager");
        mgrsGO.AddComponent<GameManager>();
        mgrsGO.AddComponent<CombatManager>();
        var uiMgr = mgrsGO.AddComponent<UIManager>();

        SetF(uiMgr, "mapScreen",          mapGO);
        SetF(uiMgr, "combatScreen",       combatGO);
        SetF(uiMgr, "lootScreen",         upgradeGO);
        SetF(uiMgr, "gameOverScreen",     gameOverGO);
        SetF(uiMgr, "pathButtons",        new Button[] { pathBtn1, pathBtn2, pathBtn3 });
        SetF(uiMgr, "playerHpText",       playerHpTMP);
        SetF(uiMgr, "playerBulletsText",  playerBulTMP);
        SetF(uiMgr, "enemyHpText",        enemyHpTMP);
        SetF(uiMgr, "enemyBulletsText",   enemyBulTMP);
        SetF(uiMgr, "selectedActionText", selTMP);
        SetF(uiMgr, "fireButton",         fireBtn);
        SetF(uiMgr, "defendButton",       defendBtn);
        SetF(uiMgr, "reloadButton",       reloadBtn);
        SetF(uiMgr, "combatLogText",      logTMP);
        SetF(uiMgr, "lootTitleText",      lootTitleTMP);
        SetF(uiMgr, "lootInfoText",       lootInfoTMP);
        SetF(uiMgr, "upgradeButtons",     new Button[] { upgBtn1, upgBtn2, upgBtn3 });
        SetF(uiMgr, "upgradeButtonTexts", new TextMeshProUGUI[] { upgTxt1, upgTxt2, upgTxt3 });
        SetF(uiMgr, "gameOverText",       goTMP);
        SetF(uiMgr, "restartButton",      restartBtn);

        // Also add a gold display on the map screen
        var mapGoldTMP = Txt(mapGO.transform, "GoldText", "Gold: 0",
            22f, V2(0, -210), V2(400, 45), new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center);
        SetF(uiMgr, "mapGoldText", mapGoldTMP);

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
}
