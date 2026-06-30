using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject mapScreen;
    public GameObject combatScreen;
    public GameObject lootScreen;
    public GameObject gameOverScreen;

    [Header("Map UI")]
    public MapRenderer mapRenderer;
    public TextMeshProUGUI mapGoldText;
    public TextMeshProUGUI mapFloorText;

    [Header("Combat UI")]
    public TextMeshProUGUI playerActionText;
    public TextMeshProUGUI enemyActionText;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerBulletsText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyBulletsText;
    public TextMeshProUGUI selectedActionText;
    public Button fireButton;
    public Button defendButton;
    public Button reloadButton;
    public TextMeshProUGUI blockUsesText;
    public TextMeshProUGUI combatLogText;
    public BattleVisuals battleVisuals;

    [Header("Combat Animation")]
    public Image shieldOverlay;
    public Image enemyShieldOverlay;
    public Transform playerAmmoDisplay;
    public Transform enemyAmmoDisplay;

    [Header("Bullet FX")]
    public Image         playerBulletImage;
    public Image         enemyBulletImage;
    public RectTransform playerGunTipMarker;
    public RectTransform enemyGunTipMarker;

    [Header("Loot UI")]
    public TextMeshProUGUI lootTitleText;
    public TextMeshProUGUI lootInfoText;
    public Button[] upgradeButtons;
    public TextMeshProUGUI[] upgradeButtonTexts;

    [Header("Game Over UI")]
    public TextMeshProUGUI gameOverText;
    public Button restartButton;

    private List<string> logLines = new List<string>();
    private bool battleEnded;
    private bool _animating;

    void Awake() => Instance = this;

    void Start()
    {
        if (mapRenderer != null)
            mapRenderer.OnColumnChosen += col => GameManager.Instance.StartBattle(col);

        fireButton.onClick.AddListener(() => OnActionButton(CombatAction.Fire));
        defendButton.onClick.AddListener(() => OnActionButton(CombatAction.Defend));
        reloadButton.onClick.AddListener(() => OnActionButton(CombatAction.Reload));

        restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    // ── Screens ───────────────────────────────────────────────────────────────

    public void ShowMap()
    {
        UnsubscribeCombat();
        SetScreen(mapScreen);
        var gm = GameManager.Instance;
        if (mapGoldText  != null) mapGoldText.text  = $"Gold: {gm.Player.gold}";
        if (mapFloorText != null) mapFloorText.text = $"Battle {gm.CurrentFloor + 1} / {GameManager.MAP_ROWS}";
        if (mapRenderer  != null) mapRenderer.DrawMap(gm.CurrentMap, gm.CurrentFloor);
    }

    public void ShowCombat()
    {
        SetScreen(combatScreen);
        battleEnded = false;
        _animating  = false;
        logLines.Clear();

        if (combatLogText      != null) combatLogText.text      = "";
        if (selectedActionText != null) selectedActionText.text = "Select an action";
        if (playerActionText   != null) { playerActionText.text = ""; playerActionText.transform.localScale = Vector3.one; }
        if (enemyActionText    != null) { enemyActionText.text  = ""; enemyActionText.transform.localScale  = Vector3.one; }
        if (shieldOverlay       != null) shieldOverlay.gameObject.SetActive(false);
        if (enemyShieldOverlay  != null) enemyShieldOverlay.gameObject.SetActive(false);
        if (playerBulletImage  != null) playerBulletImage.gameObject.SetActive(false);
        if (enemyBulletImage   != null) enemyBulletImage.gameObject.SetActive(false);

        SetActionButtonsInteractable(true);

        if (battleVisuals != null && GameManager.Instance.LastPath != null)
            battleVisuals.SetEnemyTier(GameManager.Instance.LastPath.enemyTier);

        var cm = CombatManager.Instance;
        cm.OnCombatLog    += AddLog;
        cm.OnTurnResolved += RefreshCombatUI;
        cm.OnBattleEnd    += OnBattleEndUI;

        RefreshCombatUI();
    }

    public void ShowLoot()
    {
        UnsubscribeCombat();
        SetScreen(lootScreen);
        var gm = GameManager.Instance;
        string tierName = PathData.EnemyLabel(gm.LastPath.enemyTier);
        if (lootTitleText != null) lootTitleText.text = $"LOOT  —  {tierName} Enemy";
        if (lootInfoText  != null) lootInfoText.text  = $"+{gm.LastLootHp} HP  →  {gm.Player.hp}/{gm.Player.maxHp}    +{gm.LastLootGold} Gold  (Total: {gm.Player.gold})";
        var upgrades = gm.CurrentUpgrades;
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (i < upgrades.Length)
            {
                var u = upgrades[i];
                upgradeButtonTexts[i].text = $"<b>{u.name}</b>\n{u.description}";
                upgradeButtons[i].gameObject.SetActive(true);
                int id = u.id;
                upgradeButtons[i].onClick.RemoveAllListeners();
                upgradeButtons[i].onClick.AddListener(() => GameManager.Instance.ApplyUpgrade(id));
            }
            else upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        UnsubscribeCombat();
        SetScreen(gameOverScreen);
        var gm = GameManager.Instance;
        bool won = gm.CurrentFloor >= GameManager.MAP_ROWS;
        gameOverText.text = won ? "YOU WON!\nThe Outlaw rides into the sunset" : "YOU DIED\nBetter luck next time";
    }

    // ── Combat helpers ────────────────────────────────────────────────────────

    void OnActionButton(CombatAction action)
    {
        if (battleEnded || _animating) return;
        SetActionButtonsInteractable(false);
        StartCoroutine(TurnSequence(action));
    }

    void OnBattleEndUI(bool playerWon)
    {
        battleEnded = true;
        AddLog(playerWon ? "=== VICTORY! ===" : "=== DEFEAT ===");
        SetActionButtonsInteractable(false);
    }

    void RefreshCombatUI()
    {
        if (_animating) return;
        var cm = CombatManager.Instance;
        var p  = cm.Player;
        var e  = cm.Enemy;
        if (p == null || e == null) return;

        if (playerHpText      != null) playerHpText.text      = $"HP: {p.hp} / {p.maxHp}";
        if (playerBulletsText != null) playerBulletsText.text = $"Bullets: {p.bullets} / {p.maxBullets}";
        if (enemyHpText       != null) enemyHpText.text       = $"HP: {e.hp} / {e.maxHp}";
        if (enemyBulletsText  != null) enemyBulletsText.text  = $"Bullets: {e.bullets} / {e.maxBullets}";
        if (blockUsesText     != null) blockUsesText.text     = $"Shield: {p.blockUses} / {p.maxBlockUses}";

        fireButton.interactable   = !battleEnded;
        defendButton.interactable = !battleEnded && p.blockUses > 0;
        battleVisuals?.Refresh(p, e);
    }

    void AddLog(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > 12) logLines.RemoveAt(0);
        if (combatLogText != null) combatLogText.text = string.Join("\n", logLines);
    }

    void SetActionButtonsInteractable(bool value)
    {
        fireButton.interactable   = value;
        reloadButton.interactable = value;
        if (!value) defendButton.interactable = false;
    }

    void SetScreen(GameObject screen)
    {
        if (mapScreen)      mapScreen.SetActive(false);
        if (combatScreen)   combatScreen.SetActive(false);
        if (lootScreen)     lootScreen.SetActive(false);
        if (gameOverScreen) gameOverScreen.SetActive(false);
        screen?.SetActive(true);
    }

    void UnsubscribeCombat()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnCombatLog    -= AddLog;
        cm.OnTurnResolved -= RefreshCombatUI;
        cm.OnBattleEnd    -= OnBattleEndUI;
    }

    static string ActionLabel(CombatAction action) => action switch
    {
        CombatAction.Fire   => "BANG!",
        CombatAction.Reload => "RELOADING...",
        CombatAction.Defend => "DEFENDING!",
        _                   => ""
    };

    // ── Turn sequence ─────────────────────────────────────────────────────────

    IEnumerator TurnSequence(CombatAction playerAction)
    {
        _animating = true;
        var cm = CombatManager.Instance;

        cm.DecideActions(playerAction);
        var pAction = cm.LastPlayerAction.Value;
        var eAction = cm.LastEnemyAction.Value;

        // ── Phase 1: Action texts pop in ──────────────────────────────────
        if (playerActionText != null) { playerActionText.text = ActionLabel(pAction); playerActionText.transform.localScale = Vector3.zero; }
        if (enemyActionText  != null) { enemyActionText.text  = ActionLabel(eAction); enemyActionText.transform.localScale  = Vector3.zero; }
        if (selectedActionText != null) selectedActionText.text = $"► {ActionLabel(pAction)}";

        StartCoroutine(ScalePunch(playerActionText?.transform));
        StartCoroutine(ScalePunch(enemyActionText?.transform));
        yield return new WaitForSeconds(0.45f);

        // ── Phase 2: Action animations ────────────────────────────────────
        var pSpr = battleVisuals?.playerSprite;
        var eSpr = battleVisuals?.enemySprite;

        bool pFires = pAction == CombatAction.Fire;
        bool eFires = eAction == CombatAction.Fire;

        // Fire: muzzle → bullet → impact + shake (sequenced sub-coroutine)
        if (pFires || eFires)
            StartCoroutine(FireFX(pFires, eFires, pSpr, eSpr));

        // Defend: shield overlay + blue flash
        if (pAction == CombatAction.Defend)
        {
            if (shieldOverlay != null) StartCoroutine(ShowShieldOverlay(shieldOverlay));
            if (pSpr != null) StartCoroutine(FlashColor(pSpr, new Color(0.35f, 0.65f, 1f), 0.55f));
        }
        if (eAction == CombatAction.Defend)
        {
            if (enemyShieldOverlay != null) StartCoroutine(ShowShieldOverlay(enemyShieldOverlay));
            if (eSpr != null) StartCoroutine(FlashColor(eSpr, new Color(0.35f, 0.65f, 1f), 0.55f));
        }

        // Reload: bounce the ammo display
        if (pAction == CombatAction.Reload && playerAmmoDisplay != null)
            StartCoroutine(BounceScale(playerAmmoDisplay));
        if (eAction == CombatAction.Reload && enemyAmmoDisplay != null)
            StartCoroutine(BounceScale(enemyAmmoDisplay));

        yield return new WaitForSeconds(0.78f);

        // ── Phase 3: Apply + show results ─────────────────────────────────
        cm.ApplyTurn();

        if (playerHpText  != null) playerHpText.text  = $"HP: {cm.Player.hp} / {cm.Player.maxHp}";
        if (enemyHpText   != null) enemyHpText.text   = $"HP: {cm.Enemy.hp} / {cm.Enemy.maxHp}";
        if (blockUsesText != null) blockUsesText.text = $"Shield: {cm.Player.blockUses} / {cm.Player.maxBlockUses}";
        battleVisuals?.Refresh(cm.Player, cm.Enemy);

        yield return new WaitForSeconds(0.25f);

        _animating = false;
        if (!battleEnded)
        {
            SetActionButtonsInteractable(true);
            RefreshCombatUI(); // re-evaluates defend button against current blockUses
        }
    }

    // ── Fire FX sub-sequence ──────────────────────────────────────────────────

    IEnumerator FireFX(bool pFires, bool eFires, Image pSpr, Image eSpr)
    {
        Vector2 pTip = playerGunTipMarker != null ? playerGunTipMarker.anchoredPosition : new Vector2(-120f, -125f);
        Vector2 eTip = enemyGunTipMarker  != null ? enemyGunTipMarker.anchoredPosition  : new Vector2( 120f,  120f);

        // Muzzle flash at shooter + sprite flash orange (simultaneous)
        if (pFires) { StartCoroutine(MuzzleFlash(pTip)); if (pSpr) StartCoroutine(FlashColor(pSpr, new Color(1f, 0.55f, 0.15f))); }
        if (eFires) { StartCoroutine(MuzzleFlash(eTip)); if (eSpr) StartCoroutine(FlashColor(eSpr, new Color(1f, 0.55f, 0.15f))); }

        yield return new WaitForSeconds(0.13f);

        // Each shooter uses their own bullet image so both can fly simultaneously
        if (pFires) StartCoroutine(BulletFly(playerBulletImage, pTip, eTip));
        if (eFires) StartCoroutine(BulletFly(enemyBulletImage,  eTip, pTip));

        yield return new WaitForSeconds(0.3f);

        // Impact burst + shake at the hit tip
        var impactColor = new Color(1f, 0.75f, 0.2f);
        if (pFires) { StartCoroutine(ImpactBurst(eTip, impactColor)); if (eSpr) StartCoroutine(Shake(eSpr.rectTransform)); }
        if (eFires) { StartCoroutine(ImpactBurst(pTip, impactColor)); if (pSpr) StartCoroutine(Shake(pSpr.rectTransform)); }

        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator BulletFly(Image img, Vector2 from, Vector2 to, float duration = 0.28f)
    {
        if (img == null) yield break;
        img.gameObject.SetActive(true);
        var rt = img.rectTransform;

        Vector2 dir   = (to - from).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        rt.anchoredPosition = from;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, f / duration);
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    IEnumerator MuzzleFlash(Vector2 pos)
    {
        var go  = new GameObject("MuzzleFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(combatScreen.transform, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = Vector2.one * 0.5f;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();

        float dur = 0.14f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            rt.sizeDelta = Vector2.one * Mathf.Lerp(18f, 55f, t);
            img.color    = new Color(1f, 0.9f, 0.3f, 1f - t);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ImpactBurst(Vector2 pos, Color color)
    {
        int   count = 7;
        var   rts   = new RectTransform[count];
        var   imgs  = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("ImpactParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(combatScreen.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = Vector2.one * 0.5f;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = Vector2.one * (i == 0 ? 44f : 11f);
            imgs[i] = go.GetComponent<Image>();
            imgs[i].color = color;
            rts[i] = rt;
        }

        // Precompute outward directions for the ring particles
        var dirs = new Vector2[count - 1];
        for (int i = 0; i < dirs.Length; i++)
        {
            float a = i * (360f / dirs.Length) * Mathf.Deg2Rad;
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        float dur = 0.38f;
        for (float f = 0; f < dur; f += Time.deltaTime)
        {
            float t = f / dur;
            // Center flash: expand + fade
            rts[0].sizeDelta = Vector2.one * Mathf.Lerp(44f, 88f, t);
            imgs[0].color    = new Color(color.r, color.g, color.b, 1f - t);
            // Ring particles: fly outward + fade
            for (int i = 1; i < count; i++)
            {
                rts[i].anchoredPosition = pos + dirs[i - 1] * 95f * t;
                imgs[i].color = new Color(color.r, color.g * (1f - t * 0.5f), 0f, 1f - t);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++) Destroy(rts[i].gameObject);
    }

    // ── Animation helpers ─────────────────────────────────────────────────────

    IEnumerator ScalePunch(Transform t, float duration = 0.35f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(0f,    1.15f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f,    f / half); yield return null; }
        t.localScale = Vector3.one;
    }

    IEnumerator FlashColor(Image img, Color flash, float duration = 0.2f)
    {
        if (img == null) yield break;
        Color orig = img.color;
        img.color = flash;
        yield return new WaitForSeconds(duration);
        img.color = orig;
    }

    IEnumerator Shake(RectTransform rt, float duration = 0.42f, float magnitude = 10f)
    {
        if (rt == null) yield break;
        Vector2 orig = rt.anchoredPosition;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            rt.anchoredPosition = orig + new Vector2(Random.Range(-1f, 1f) * magnitude, Random.Range(-0.3f, 0.3f) * magnitude);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }

    IEnumerator BounceScale(Transform t, float duration = 0.32f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1f,    1.28f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1.28f, 1f,    f / half); yield return null; }
        t.localScale = Vector3.one;
    }

    IEnumerator ShowShieldOverlay(Image overlay, float duration = 0.75f)
    {
        if (overlay == null) yield break;
        overlay.gameObject.SetActive(true);
        Color c = overlay.color;

        float fadeIn = duration * 0.35f;
        for (float f = 0; f < fadeIn; f += Time.deltaTime)  { c.a = f / fadeIn;       overlay.color = c; yield return null; }
        c.a = 1f; overlay.color = c;
        yield return new WaitForSeconds(duration * 0.3f);

        float fadeOut = duration * 0.35f;
        for (float f = 0; f < fadeOut; f += Time.deltaTime) { c.a = 1f - f / fadeOut; overlay.color = c; yield return null; }
        c.a = 0f; overlay.color = c;
        overlay.gameObject.SetActive(false);
    }
}
