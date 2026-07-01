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
    public MapData       CurrentMap      { get; private set; }
    public int           CurrentFloor    { get; private set; }
    public UpgradeData[] CurrentUpgrades { get; private set; }

    public int      LastLootHp   { get; private set; }
    public int      LastLootGold { get; private set; }
    public PathData LastPath     { get; private set; }

    public const int MAP_ROWS = 8;

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
        CurrentFloor = 0;
        State = GameState.Map;
        GenerateMap();
    }

    void GenerateMap()
    {
        var rows  = new PathData[MAP_ROWS][];
        var conns = new System.Collections.Generic.List<int>[MAP_ROWS][];

        // Pass 1: generate all rows first so connections can see both ends
        for (int row = 0; row < MAP_ROWS; row++)
        {
            rows[row] = new PathData[3];
            if (row == 0)
            {
                rows[0][1] = PathData.Generate(rng, 0); // single centre start
            }
            else
            {
                for (int col = 0; col < 3; col++)
                    rows[row][col] = PathData.Generate(rng, row);
                // 50% chance: drop one random node → 2 nodes that row
                if (rng.Next(2) == 0)
                    rows[row][rng.Next(3)] = null;
            }
        }

        // Pass 2: generate connections now that all rows are known
        var emptyConns = new System.Collections.Generic.List<int>[]
            { new System.Collections.Generic.List<int>(),
              new System.Collections.Generic.List<int>(),
              new System.Collections.Generic.List<int>() };

        for (int row = 0; row < MAP_ROWS; row++)
        {
            conns[row] = row < MAP_ROWS - 1
                ? (row == 0
                    ? MapData.GenerateSingleSourceConnections(rng)
                    : MapData.GenerateConnections(rng, rows[row], rows[row + 1]))
                : emptyConns;
        }

        CurrentMap = new MapData(rows, conns);
    }

    public void StartBattle(int col)
    {
        CurrentMap.chosenColumns[CurrentFloor] = col;
        LastPath = CurrentMap.rows[CurrentFloor][col];
        State = GameState.Combat;

        int enemyHp  = Mathf.Max(LastPath.GetEnemyHp(CurrentFloor) - Player.enemyStartHpPenalty, 1);
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
        yield return new WaitForSeconds(3.5f);
        if (playerWon)
        {
            CurrentFloor++;

            if (Player.winHeal > 0)
                Player.hp = Mathf.Min(Player.hp + Player.winHeal, Player.maxHp);

            LastLootHp   = Mathf.Min(LastPath.LootHp, 100);
            LastLootGold = rng.Next(LastPath.LootGoldMin, LastPath.LootGoldMax + 1) + Player.goldBonus + Player.goldPerWin;
            Player.hp   = Mathf.Min(Player.hp + LastLootHp, Player.maxHp);
            Player.gold += LastLootGold;

            if (CurrentFloor >= MAP_ROWS)
            {
                State = GameState.GameOver;
                UIManager.Instance?.ShowGameOver();
            }
            else
            {
                CurrentUpgrades = PickRandomUpgrades(3);
                State = GameState.Loot;
                UIManager.Instance?.ShowLoot();
            }
        }
        else
        {
            State = GameState.GameOver;
            UIManager.Instance?.ShowGameOver();
        }
    }

    UpgradeData[] PickRandomUpgrades(int count)
    {
        var pool   = UpgradeData.All.ToList();
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
            // Original
            case  1: Player.maxHp += 20; Player.hp = Mathf.Min(Player.hp + 20, Player.maxHp); break;
            case  2: Player.startingBullets      += 1;  break;
            case  3: Player.fireDamageBonus      += 10; break;
            case  4: Player.reloadBonus          += 1;  break;
            case  5: Player.maxBulletsBonus      += 1;  break;
            case  6: Player.defendHeal           += 5;  break;
            case  7: Player.firstShotDamageBonus += 20; break;
            case  8: Player.winHeal              += 15; break;
            case  9: Player.enemyStartHpPenalty  += 10; break;
            // New
            case 10: Player.deadEyeBonus         += 20; break;
            case 11: Player.damageReduction      += 5;  break;
            case 12: Player.battleStartHeal      += 10; break;
            case 13: Player.goldBonus            += 10; break;
            case 14: Player.reloadHeal           += 5;  break;
            case 15: Player.ambushBonus          += 25; break;
            case 16: Player.lastStandBonus       += 20; break;
            case 17: Player.maxHp                += 15; break;
            case 18: Player.maxBulletsBonus      += 2;  break;
            case 19: Player.fireDamageBonus      += 15; break;
            case 20: Player.enemyStartHpPenalty  += 25; break;
            // Aim upgrades
            case 21: Player.aimMin   = Mathf.Max(Player.aimMin, 2); break;
            case 22: Player.aimMax   += 3;    break;
            case 23: Player.aimBonus += 2;    break;
            case 24: Player.aimReroll = true; break;
            case 25: Player.aimBonus += 1;    break;
            // Gold upgrades
            case 26: Player.goldPerWin += 20; break;
            case 27: Player.goldPerWin += 35; break;
        }
        State = GameState.Map;
        UIManager.Instance?.ShowMap();
    }

    public void RestartGame()
    {
        InitNewRun();
        UIManager.Instance?.ShowMap();
    }
}
