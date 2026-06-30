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
    public Transform playerAmmoDisplay;
    public Transform enemyAmmoDisplay;

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
        if (shieldOverlay      != null) shieldOverlay.gameObject.SetActive(false);

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

    // ── Turn sequence coroutine ───────────────────────────────────────────────

    IEnumerator TurnSequence(CombatAction playerAction)
    {
        _animating = true;
        var cm = CombatManager.Instance;

        // Decide actions (no state change yet)
        cm.DecideActions(playerAction);
        var pAction = cm.LastPlayerAction.Value;
        var eAction = cm.LastEnemyAction.Value;

        // ── Phase 1: Action texts pop in ──────────────────────────────────
        if (playerActionText != null)
        {
            playerActionText.text = ActionLabel(pAction);
            playerActionText.transform.localScale = Vector3.zero;
        }
        if (enemyActionText != null)
        {
            enemyActionText.text = ActionLabel(eAction);
            enemyActionText.transform.localScale = Vector3.zero;
        }
        if (selectedActionText != null) selectedActionText.text = $"► {ActionLabel(pAction)}";

        StartCoroutine(ScalePunch(playerActionText?.transform));
        StartCoroutine(ScalePunch(enemyActionText?.transform));
        yield return new WaitForSeconds(0.45f);

        // ── Phase 2: Action animations ────────────────────────────────────
        var playerSpr = battleVisuals?.playerSprite;
        var enemySpr  = battleVisuals?.enemySprite;

        // Fire — flash shooter orange, then shake the target
        if (pAction == CombatAction.Fire && playerSpr != null)
            StartCoroutine(FlashColor(playerSpr, new Color(1f, 0.55f, 0.15f)));
        if (eAction == CombatAction.Fire && enemySpr != null)
            StartCoroutine(FlashColor(enemySpr, new Color(1f, 0.55f, 0.15f)));

        yield return new WaitForSeconds(0.18f);

        if (pAction == CombatAction.Fire && enemySpr != null)
            StartCoroutine(Shake(enemySpr.rectTransform));
        if (eAction == CombatAction.Fire && playerSpr != null)
            StartCoroutine(Shake(playerSpr.rectTransform));

        // Defend — show shield overlay + flash blue
        if (pAction == CombatAction.Defend)
        {
            if (shieldOverlay != null) StartCoroutine(ShowShield());
            if (playerSpr != null) StartCoroutine(FlashColor(playerSpr, new Color(0.35f, 0.65f, 1f), 0.55f));
        }
        if (eAction == CombatAction.Defend && enemySpr != null)
            StartCoroutine(FlashColor(enemySpr, new Color(0.35f, 0.65f, 1f), 0.55f));

        // Reload — bounce the ammo display
        if (pAction == CombatAction.Reload && playerAmmoDisplay != null)
            StartCoroutine(BounceScale(playerAmmoDisplay));
        if (eAction == CombatAction.Reload && enemyAmmoDisplay != null)
            StartCoroutine(BounceScale(enemyAmmoDisplay));

        yield return new WaitForSeconds(0.6f);

        // ── Phase 3: Apply + show results ─────────────────────────────────
        cm.ApplyTurn();

        if (playerHpText  != null) playerHpText.text  = $"HP: {cm.Player.hp} / {cm.Player.maxHp}";
        if (enemyHpText   != null) enemyHpText.text   = $"HP: {cm.Enemy.hp} / {cm.Enemy.maxHp}";
        if (blockUsesText != null) blockUsesText.text = $"Shield: {cm.Player.blockUses} / {cm.Player.maxBlockUses}";
        battleVisuals?.Refresh(cm.Player, cm.Enemy);

        yield return new WaitForSeconds(0.25f);

        _animating = false;
        if (!battleEnded) SetActionButtonsInteractable(true);
    }

    // ── Animation helpers ─────────────────────────────────────────────────────

    IEnumerator ScalePunch(Transform t, float duration = 0.35f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, f / half); yield return null; }
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
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            rt.anchoredPosition = orig + new Vector2(
                Random.Range(-1f, 1f) * magnitude,
                Random.Range(-0.3f, 0.3f) * magnitude);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }

    IEnumerator BounceScale(Transform t, float duration = 0.32f)
    {
        if (t == null) yield break;
        float half = duration * 0.5f;
        for (float f = 0; f < half; f += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(1f, 1.28f, f / half); yield return null; }
        for (float f = 0; f < half; f += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(1.28f, 1f, f / half); yield return null; }
        t.localScale = Vector3.one;
    }

    IEnumerator ShowShield(float duration = 0.75f)
    {
        if (shieldOverlay == null) yield break;
        shieldOverlay.gameObject.SetActive(true);
        Color c = shieldOverlay.color;

        float fadeIn = duration * 0.35f;
        for (float f = 0; f < fadeIn; f += Time.deltaTime)
        { c.a = f / fadeIn; shieldOverlay.color = c; yield return null; }
        c.a = 1f; shieldOverlay.color = c;

        yield return new WaitForSeconds(duration * 0.3f);

        float fadeOut = duration * 0.35f;
        for (float f = 0; f < fadeOut; f += Time.deltaTime)
        { c.a = 1f - f / fadeOut; shieldOverlay.color = c; yield return null; }
        c.a = 0f; shieldOverlay.color = c;
        shieldOverlay.gameObject.SetActive(false);
    }
}
