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
    public event Action<bool>   OnBattleEnd;

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

        if (player.battleStartHeal > 0)
            Log($"War Paint healed {player.battleStartHeal} HP");
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

        if (playerAction == CombatAction.Defend && Player.blockUses <= 0)
            playerAction = CombatAction.Reload;

        CombatAction enemyAction = GetEnemyAction();
        if (enemyAction == CombatAction.Fire && Enemy.bullets <= 0)
            enemyAction = CombatAction.Reload;

        bool playerFires   = playerAction == CombatAction.Fire;
        bool enemyFires    = enemyAction  == CombatAction.Fire;
        bool playerDefends = playerAction == CombatAction.Defend;

        if (playerDefends)
            Player.blockUses--;

        ApplyPlayerAction(playerAction, enemyAction);
        ApplyEnemyAction(enemyAction);

        if (playerFires)
        {
            bool enemyEmpty     = Enemy.bullets == 0 && enemyAction != CombatAction.Fire;
            bool enemyReloading = enemyAction == CombatAction.Reload;
            int dmg = Player.GetFireDamage(enemyEmpty, enemyReloading);
            Player.MarkFirstShot();

            if (enemyAction == CombatAction.Defend)
                Log("Enemy blocked Player's shot");
            else
            {
                Enemy.hp -= dmg;
                string bonusNote = "";
                if (enemyEmpty && Player.deadEyeBonus > 0)
                    bonusNote += $" (Dead Eye +{Player.deadEyeBonus})";
                if (enemyReloading && Player.ambushBonus > 0)
                    bonusNote += $" (Ambush +{Player.ambushBonus})";
                if (Player.lastStandBonus > 0 && Player.hp <= Player.maxHp / 3)
                    bonusNote += $" (Last Stand +{Player.lastStandBonus})";
                Log($"Player dealt {dmg} damage{bonusNote}");
            }
        }

        if (enemyFires)
        {
            if (playerAction == CombatAction.Defend)
                Log("Player blocked Enemy's shot");
            else
            {
                int incoming = Mathf.Max(Enemy.damage - Player.damageReduction, 1);
                Player.hp -= incoming;
                string reductionNote = Player.damageReduction > 0
                    ? $" (Thick Skin -{Player.damageReduction})" : "";
                Log($"Enemy dealt {incoming} damage{reductionNote}");
            }
        }

        // Regenerate 1 block use on turns where player didn't defend
        if (!playerDefends)
            Player.blockUses = Mathf.Min(Player.blockUses + 1, Player.maxBlockUses);

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

    void ApplyPlayerAction(CombatAction action, CombatAction enemyAction)
    {
        switch (action)
        {
            case CombatAction.Fire:
                Player.bullets--;
                break;

            case CombatAction.Reload:
                int gained = 1 + Player.reloadBonus;
                Player.bullets = Mathf.Min(Player.bullets + gained, Player.maxBullets);
                string reloadMsg = $"Player reloaded (+{gained})";
                if (Player.reloadHeal > 0)
                {
                    Player.hp = Mathf.Min(Player.hp + Player.reloadHeal, Player.maxHp);
                    reloadMsg += $", healed {Player.reloadHeal} HP";
                }
                Log(reloadMsg);
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

    void ApplyEnemyAction(CombatAction action)
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

    void Log(string msg) => OnCombatLog?.Invoke(msg);
}
