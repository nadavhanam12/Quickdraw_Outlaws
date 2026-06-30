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

        if (mapGoldText != null)
            mapGoldText.text = $"Gold: {gm.Player.gold}";

        if (mapFloorText != null)
            mapFloorText.text = $"Battle {gm.CurrentFloor + 1} / {GameManager.MAP_ROWS}";

        if (mapRenderer != null)
            mapRenderer.DrawMap(gm.CurrentMap, gm.CurrentFloor);
    }

    public void ShowCombat()
    {
        SetScreen(combatScreen);
        battleEnded = false;
        logLines.Clear();
        if (combatLogText != null) combatLogText.text = "";
        if (selectedActionText != null) selectedActionText.text = "Select an action";
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

        if (lootTitleText != null)
            lootTitleText.text = $"LOOT  —  {tierName} Enemy";

        if (lootInfoText != null)
            lootInfoText.text =
                $"+{gm.LastLootHp} HP  →  {gm.Player.hp}/{gm.Player.maxHp}" +
                $"    +{gm.LastLootGold} Gold  (Total: {gm.Player.gold})";

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
            else
                upgradeButtons[i].gameObject.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        UnsubscribeCombat();
        SetScreen(gameOverScreen);
        var gm = GameManager.Instance;
        bool won = gm.CurrentFloor >= GameManager.MAP_ROWS;
        gameOverText.text = won
            ? "YOU WON!\nThe Outlaw rides into the sunset"
            : "YOU DIED\nBetter luck next time";
    }

    // ── Combat helpers ────────────────────────────────────────────────────────

    void OnActionButton(CombatAction action)
    {
        if (battleEnded) return;
        selectedActionText.text = $"Action: {action}";
        AddLog($"--- Player chose {action} ---");
        CombatManager.Instance.ResolveTurn(action);
    }

    void OnBattleEndUI(bool playerWon)
    {
        battleEnded = true;
        AddLog(playerWon ? "=== VICTORY! ===" : "=== DEFEAT ===");
        SetActionButtonsInteractable(false);
    }

    void RefreshCombatUI()
    {
        var p = CombatManager.Instance.Player;
        var e = CombatManager.Instance.Enemy;
        if (p == null || e == null) return;

        if (playerHpText != null)      playerHpText.text      = $"HP: {p.hp} / {p.maxHp}";
        if (playerBulletsText != null) playerBulletsText.text = $"Bullets: {p.bullets} / {p.maxBullets}";
        if (enemyHpText != null)       enemyHpText.text       = $"HP: {e.hp} / {e.maxHp}";
        if (enemyBulletsText != null)  enemyBulletsText.text  = $"Bullets: {e.bullets} / {e.maxBullets}";
        if (blockUsesText != null)     blockUsesText.text     = $"Shield: {p.blockUses} / {p.maxBlockUses}";

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
        // defendButton interactability is managed by RefreshCombatUI (block uses gate)
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
}
