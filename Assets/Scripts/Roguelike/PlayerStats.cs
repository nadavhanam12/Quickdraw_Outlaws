using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public int hp;
    public int maxHp;
    public int bullets;
    public int maxBullets;

    // ── Upgrade modifiers ─────────────────────────────────────────────────────
    public int  startingBullets;       // 2: Quick Draw
    public int  fireDamageBonus;       // 3, 19: Hot Lead / Desperado
    public int  reloadBonus;           // 4: Speed Loader
    public int  maxBulletsBonus;       // 5, 18: Extended Mag / Extra Clip
    public int  defendHeal;            // 6: Steel Guard
    public int  firstShotDamageBonus;  // 7: First Blood
    public int  winHeal;               // 8: Bounty
    public int  enemyStartHpPenalty;   // 9, 20: Intimidation / Outlaw Legend
    public int  deadEyeBonus;          // 10: Dead Eye
    public int  damageReduction;       // 11: Thick Skin
    public int  battleStartHeal;       // 12: War Paint
    public int  goldBonus;             // 13: Gold Rush
    public int  reloadHeal;            // 14: Quick Hands
    public int  ambushBonus;           // 15: Ambush
    public int  lastStandBonus;        // 16: Last Stand
    // 17: Hired Muscle → maxHp += 15  (no new field)

    public int gold;

    public bool firstShotFired;
    public List<int> acquiredUpgrades = new List<int>();

    public void Initialize()
    {
        maxHp      = 100;
        hp         = maxHp;
        maxBullets = 6 + maxBulletsBonus;
        bullets    = startingBullets;
        firstShotFired = false;
    }

    public void StartBattle()
    {
        maxBullets = 6 + maxBulletsBonus;
        bullets    = Mathf.Min(startingBullets, maxBullets);
        firstShotFired = false;
        if (battleStartHeal > 0)
            hp = Mathf.Min(hp + battleStartHeal, maxHp);
    }

    public int GetFireDamage(bool enemyHasNoBullets, bool enemyIsReloading)
    {
        int dmg = 30 + fireDamageBonus;
        if (!firstShotFired && firstShotDamageBonus > 0)
            dmg += firstShotDamageBonus;
        if (enemyHasNoBullets && deadEyeBonus > 0)
            dmg += deadEyeBonus;
        if (enemyIsReloading && ambushBonus > 0)
            dmg += ambushBonus;
        if (lastStandBonus > 0 && hp <= maxHp / 3)
            dmg += lastStandBonus;
        return dmg;
    }

    public void MarkFirstShot() => firstShotFired = true;
}
