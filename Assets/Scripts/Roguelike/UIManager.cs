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
    public Button[] pathButtons;
    public TextMeshProUGUI mapGoldText;

    [Header("Combat UI")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerBulletsText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyBulletsText;
    public TextMeshProUGUI selectedActionText;
    public Button fireButton;
    public Button defendButton;
    public Button reloadButton;
    public TextMeshProUGUI combatLogText;

    [Header("Loot UI")]
    public TextMeshProUGUI lootTitleText;
    public TextMeshProUGUI lootInfoText;
    public Button continueButton;

    [Header("Game Over UI")]
    public TextMeshProUGUI gameOverText;
    public Button restartButton;

    private List<string> logLines = new List<string>();
    private bool battleEnded;

    void Awake() => Instance = this;

    void Start()
    {
        fireButton.onClick.AddListener(() => OnActionButton(CombatAction.Fire));
        defendButton.onClick.AddListener(() => OnActionButton(CombatAction.Defend));
        reloadButton.onClick.AddListener(() => OnActionButton(CombatAction.Reload));

        for (int i = 0; i < pathButtons.Length; i++)
        {
            int idx = i;
            pathButtons[i].onClick.AddListener(() => GameManager.Instance.StartBattle(idx));
        }

        continueButton.onClick.AddListener(() => GameManager.Instance.ContinueFromLoot());
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

        // Stamp path descriptions onto buttons
        var paths = gm.CurrentPaths;
        for (int i = 0; i < pathButtons.Length && i < paths.Length; i++)
        {
            var tmp = pathButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = paths[i].ButtonLabel;
        }
    }

    public void ShowCombat()
    {
        SetScreen(combatScreen);
        battleEnded = false;
        logLines.Clear();
        combatLogText.text = "";
        selectedActionText.text = "Select an action";
        SetActionButtonsInteractable(true);

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
        if (lootInfoText  != null)
            lootInfoText.text =
                $"+{gm.LastLootHp} HP\n" +
                $"+{gm.LastLootGold} Gold\n\n" +
                $"Total Gold: {gm.Player.gold}";
    }

    public void ShowGameOver()
    {
        UnsubscribeCombat();
        SetScreen(gameOverScreen);
        gameOverText.text = "YOU DIED\nBetter luck next time";
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

        playerHpText.text     = $"HP: {p.hp} / {p.maxHp}";
        playerBulletsText.text = $"Bullets: {p.bullets} / {p.maxBullets}";
        enemyHpText.text      = $"HP: {e.hp} / {e.maxHp}";
        enemyBulletsText.text  = $"Bullets: {e.bullets} / {e.maxBullets}";

        fireButton.interactable = !battleEnded && (!p.disableFireWhenEmpty || p.bullets > 0);
    }

    void AddLog(string msg)
    {
        logLines.Add(msg);
        if (logLines.Count > 12) logLines.RemoveAt(0);
        combatLogText.text = string.Join("\n", logLines);
    }

    void SetActionButtonsInteractable(bool value)
    {
        fireButton.interactable   = value;
        defendButton.interactable = value;
        reloadButton.interactable = value;
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
