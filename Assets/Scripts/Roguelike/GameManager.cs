using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GameState { Map, Combat, Loot, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState     State           { get; private set; }
    public PlayerStats   Player          { get; private set; }
    public PathData[]    CurrentPaths    { get; private set; }
    public UpgradeData[] CurrentUpgrades { get; private set; }

    public int      LastLootHp   { get; private set; }
    public int      LastLootGold { get; private set; }
    public PathData LastPath     { get; private set; }

    private int battleNumber;
    private System.Random rng = new System.Random();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitNewRun();
    }

    void Start() => UIManager.Instance?.ShowMap();

    void InitNewRun()
    {
        Player = new PlayerStats();
        Player.Initialize();
        battleNumber = 0;
        State = GameState.Map;
        GeneratePaths();
    }

    void GeneratePaths()
    {
        CurrentPaths = new PathData[3];
        for (int i = 0; i < 3; i++)
            CurrentPaths[i] = PathData.Generate(rng, battleNumber);
    }

    public void StartBattle(int pathIndex)
    {
        LastPath = CurrentPaths[pathIndex];
        State = GameState.Combat;

        int enemyHp  = Mathf.Max(LastPath.GetEnemyHp(battleNumber) - Player.enemyStartHpPenalty, 1);
        int enemyDmg = LastPath.EnemyDamage;

        CombatManager.Instance.StartBattle(Player, enemyHp, enemyDmg);
        CombatManager.Instance.OnBattleEnd += HandleBattleEnd;
        UIManager.Instance?.ShowCombat();
    }

    void HandleBattleEnd(bool playerWon)
    {
        CombatManager.Instance.OnBattleEnd -= HandleBattleEnd;
        StartCoroutine(DelayedTransition(playerWon));
    }

    IEnumerator DelayedTransition(bool playerWon)
    {
        yield return new WaitForSeconds(1.5f);
        if (playerWon)
        {
            battleNumber++;

            if (Player.winHeal > 0)
                Player.hp = Mathf.Min(Player.hp + Player.winHeal, Player.maxHp);

            LastLootHp   = Mathf.Min(LastPath.LootHp, 100);
            LastLootGold = rng.Next(LastPath.LootGoldMin, LastPath.LootGoldMax + 1);
            Player.hp   = Mathf.Min(Player.hp + LastLootHp, Player.maxHp);
            Player.gold += LastLootGold;

            CurrentUpgrades = PickRandomUpgrades(3);
            State = GameState.Loot;
            UIManager.Instance?.ShowLoot();
        }
        else
        {
            State = GameState.GameOver;
            UIManager.Instance?.ShowGameOver();
        }
    }

    UpgradeData[] PickRandomUpgrades(int count)
    {
        var pool = UpgradeData.All.ToList();
        var result = new List<UpgradeData>();
        while (result.Count < count && pool.Count > 0)
        {
            int i = rng.Next(pool.Count);
            result.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return result.ToArray();
    }

    public void ApplyUpgrade(int upgradeId)
    {
        Player.acquiredUpgrades.Add(upgradeId);
        switch (upgradeId)
        {
            case 1:  Player.maxHp += 20; Player.hp = Mathf.Min(Player.hp + 20, Player.maxHp); break;
            case 2:  Player.startingBullets      += 1;  break;
            case 3:  Player.fireDamageBonus      += 10; break;
            case 4:  Player.reloadBonus          += 1;  break;
            case 5:  Player.maxBulletsBonus      += 1;  break;
            case 6:  Player.defendHeal           += 5;  break;
            case 7:  Player.firstShotDamageBonus += 20; break;
            case 8:  Player.winHeal              += 15; break;
            case 9:  Player.enemyStartHpPenalty  += 10; break;
            case 10: Player.disableFireWhenEmpty  = true; break;
        }
        GeneratePaths();
        State = GameState.Map;
        UIManager.Instance?.ShowMap();
    }

    public void RestartGame()
    {
        InitNewRun();
        UIManager.Instance?.ShowMap();
    }
}
