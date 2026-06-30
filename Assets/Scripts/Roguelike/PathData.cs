using UnityEngine;

public class PathData
{
    public enum EnemyTier { Weak, Normal, Strong, Elite }
    public enum LootTier  { Small, Normal, Big }

    public EnemyTier enemyTier;
    public LootTier  lootTier;

    // ── Button label ──────────────────────────────────────────────────────────
    public string ButtonLabel => $"{SimpleEnemyLabel(enemyTier)} Enemy";

    // ── Enemy scaling ─────────────────────────────────────────────────────────
    public int GetEnemyHp(int battleNumber)
    {
        int baseHp = 50 + battleNumber * 20; // 50, 70, 90, 110, …
        int mod;
        switch (enemyTier)
        {
            case EnemyTier.Weak:   mod = -20; break;
            case EnemyTier.Normal: mod =   0; break;
            case EnemyTier.Strong: mod =  40; break;
            case EnemyTier.Elite:  mod =  90; break;
            default:               mod =   0; break;
        }
        return Mathf.Max(baseHp + mod, 20);
    }

    public int EnemyDamage
    {
        get
        {
            switch (enemyTier)
            {
                case EnemyTier.Weak:   return 20;
                case EnemyTier.Normal: return 30;
                case EnemyTier.Strong: return 40;
                case EnemyTier.Elite:  return 50;
                default:               return 30;
            }
        }
    }

    // ── Loot ──────────────────────────────────────────────────────────────────
    // HP heal is tied to enemy difficulty (harder fight → bigger reward), max 100
    public int LootHp
    {
        get
        {
            switch (enemyTier)
            {
                case EnemyTier.Weak:   return 15;
                case EnemyTier.Normal: return 40;
                case EnemyTier.Strong: return 70;
                case EnemyTier.Elite:  return 100;
                default:               return 40;
            }
        }
    }

    public int LootGoldMin
    {
        get
        {
            switch (lootTier)
            {
                case LootTier.Small:  return  5;
                case LootTier.Normal: return 15;
                case LootTier.Big:    return 35;
                default:              return 15;
            }
        }
    }

    public int LootGoldMax
    {
        get
        {
            switch (lootTier)
            {
                case LootTier.Small:  return 15;
                case LootTier.Normal: return 30;
                case LootTier.Big:    return 55;
                default:              return 30;
            }
        }
    }

    // ── Generation ────────────────────────────────────────────────────────────
    public static PathData Generate(System.Random rng, int battleNumber)
    {
        var pd = new PathData();

        // Enemy tier: weighted toward harder as battles progress
        int roll = rng.Next(10);
        if (battleNumber <= 2)
        {
            if      (roll < 5) pd.enemyTier = EnemyTier.Weak;
            else if (roll < 9) pd.enemyTier = EnemyTier.Normal;
            else               pd.enemyTier = EnemyTier.Strong;
        }
        else if (battleNumber <= 5)
        {
            if      (roll < 2) pd.enemyTier = EnemyTier.Weak;
            else if (roll < 6) pd.enemyTier = EnemyTier.Normal;
            else if (roll < 9) pd.enemyTier = EnemyTier.Strong;
            else               pd.enemyTier = EnemyTier.Elite;
        }
        else
        {
            if      (roll < 1) pd.enemyTier = EnemyTier.Weak;
            else if (roll < 3) pd.enemyTier = EnemyTier.Normal;
            else if (roll < 7) pd.enemyTier = EnemyTier.Strong;
            else               pd.enemyTier = EnemyTier.Elite;
        }

        // Loot tier: independent of enemy, pure random
        int loot = rng.Next(3);
        if      (loot == 0) pd.lootTier = LootTier.Small;
        else if (loot == 1) pd.lootTier = LootTier.Normal;
        else                pd.lootTier = LootTier.Big;

        return pd;
    }

    // ── Labels ────────────────────────────────────────────────────────────────
    public static string EnemyLabel(EnemyTier t)
    {
        switch (t)
        {
            case EnemyTier.Weak:   return "Weak";
            case EnemyTier.Normal: return "Normal";
            case EnemyTier.Strong: return "Strong";
            case EnemyTier.Elite:  return "Elite";
            default:               return "Normal";
        }
    }

    // Simplified 3-tier display label used on path buttons
    public static string SimpleEnemyLabel(EnemyTier t)
    {
        switch (t)
        {
            case EnemyTier.Weak:   return "Weak";
            case EnemyTier.Normal: return "Medium";
            case EnemyTier.Strong: return "Strong";
            case EnemyTier.Elite:  return "Strong";
            default:               return "Medium";
        }
    }

    public static string LootLabel(LootTier t)
    {
        switch (t)
        {
            case LootTier.Small:  return "Small";
            case LootTier.Normal: return "Normal";
            case LootTier.Big:    return "Big";
            default:              return "Normal";
        }
    }
}
