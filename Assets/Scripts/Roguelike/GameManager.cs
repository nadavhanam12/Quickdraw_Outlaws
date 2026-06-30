using System.Collections;
using UnityEngine;

public enum GameState { Map, Combat, Loot, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState   State       { get; private set; }
    public PlayerStats Player      { get; private set; }
    public PathData[]  CurrentPaths { get; private set; }

    public int LastLootHp   { get; private set; }
    public int LastLootGold { get; private set; }
    public PathData LastPath { get; private set; }

    private int battleNumber;   // 0-based, increments after each win
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

            // HP heal is determined by enemy difficulty, hard-capped at 100
            LastLootHp   = Mathf.Min(LastPath.LootHp, 100);
            LastLootGold = rng.Next(LastPath.LootGoldMin, LastPath.LootGoldMax + 1);

            Player.hp   = Mathf.Min(Player.hp + LastLootHp, Player.maxHp);
            Player.gold += LastLootGold;

            State = GameState.Loot;
            UIManager.Instance?.ShowLoot();
        }
        else
        {
            State = GameState.GameOver;
            UIManager.Instance?.ShowGameOver();
        }
    }

    public void ContinueFromLoot()
    {
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
