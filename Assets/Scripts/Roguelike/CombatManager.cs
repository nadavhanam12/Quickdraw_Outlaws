using UnityEngine;
using System;

public enum CombatAction { Fire, Defend, Reload }

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public PlayerStats Player { get; private set; }
    public EnemyStats  Enemy  { get; private set; }

    public event Action<string> OnCombatLog;
    public event Action         OnTurnResolved;
    public event Action<bool>   OnBattleEnd; // true = player won

    private System.Random rng = new System.Random();
    private bool battleOver;

    void Awake() => Instance = this;

    public void StartBattle(PlayerStats player, int enemyMaxHp, int enemyDamage)
    {
        Player = player;
        Player.StartBattle();
        Enemy = new EnemyStats();
        Enemy.Initialize(enemyMaxHp, enemyDamage);
        battleOver = false;
    }

    CombatAction GetEnemyAction()
    {
        if (Enemy.bullets == 0) return CombatAction.Reload;
        if (Enemy.bullets == Enemy.maxBullets) return (CombatAction)rng.Next(2);
        return (CombatAction)rng.Next(3);
    }

    public void ResolveTurn(CombatAction playerAction)
    {
        if (battleOver) return;

        if (playerAction == CombatAction.Fire && Player.bullets <= 0)
            playerAction = CombatAction.Reload;

        CombatAction enemyAction = GetEnemyAction();
        if (enemyAction == CombatAction.Fire && Enemy.bullets <= 0)
            enemyAction = CombatAction.Reload;

        bool playerFires = playerAction == CombatAction.Fire;
        bool enemyFires  = enemyAction  == CombatAction.Fire;

        ApplyAction(playerAction, isPlayer: true);
        ApplyAction(enemyAction,  isPlayer: false);

        if (playerFires)
        {
            int dmg = Player.GetFireDamage();
            Player.MarkFirstShot();
            if (enemyAction == CombatAction.Defend)
                Log("Enemy blocked Player's shot");
            else
            {
                Enemy.hp -= dmg;
                Log($"Player dealt {dmg} damage to Enemy");
            }
        }

        if (enemyFires)
        {
            if (playerAction == CombatAction.Defend)
                Log("Player blocked Enemy's shot");
            else
            {
                Player.hp -= Enemy.damage;
                Log($"Enemy dealt {Enemy.damage} damage to Player");
            }
        }

        Player.hp = Mathf.Max(Player.hp, 0);
        Enemy.hp  = Mathf.Max(Enemy.hp,  0);

        OnTurnResolved?.Invoke();

        if (Player.hp <= 0 || Enemy.hp <= 0)
        {
            battleOver = true;
            bool playerWon = Enemy.hp <= 0 && Player.hp > 0;
            OnBattleEnd?.Invoke(playerWon);
        }
    }

    void ApplyAction(CombatAction action, bool isPlayer)
    {
        if (isPlayer)
        {
            switch (action)
            {
                case CombatAction.Fire:
                    Player.bullets--;
                    break;
                case CombatAction.Reload:
                    Player.bullets = Mathf.Min(Player.bullets + 1 + Player.reloadBonus, Player.maxBullets);
                    Log("Player reloaded");
                    break;
                case CombatAction.Defend:
                    if (Player.defendHeal > 0)
                    {
                        Player.hp = Mathf.Min(Player.hp + Player.defendHeal, Player.maxHp);
                        Log($"Player defended and healed {Player.defendHeal} HP");
                    }
                    else
                        Log("Player defended");
                    break;
            }
        }
        else
        {
            switch (action)
            {
                case CombatAction.Fire:
                    Enemy.bullets--;
                    break;
                case CombatAction.Reload:
                    Enemy.bullets = Mathf.Min(Enemy.bullets + 1, Enemy.maxBullets);
                    Log("Enemy reloaded");
                    break;
                case CombatAction.Defend:
                    Log("Enemy defended");
                    break;
            }
        }
    }

    void Log(string msg) => OnCombatLog?.Invoke(msg);
}
