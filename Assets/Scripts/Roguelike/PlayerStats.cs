using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerStats
{
    public int hp;
    public int maxHp;
    public int bullets;
    public int maxBullets;

    // Upgrade modifiers
    public int startingBullets;       // upgrade 2
    public int fireDamageBonus;       // upgrade 3
    public int reloadBonus;           // upgrade 4: extra bullet per reload
    public int maxBulletsBonus;       // upgrade 5
    public int defendHeal;            // upgrade 6
    public int firstShotDamageBonus;  // upgrade 7
    public int winHeal;               // upgrade 8
    public int enemyStartHpPenalty;   // upgrade 9
    public bool disableFireWhenEmpty; // upgrade 10

    public int gold;

    public bool firstShotFired;
    public List<int> acquiredUpgrades = new List<int>();

    public void Initialize()
    {
        maxHp = 100;
        hp = maxHp;
        maxBullets = 6 + maxBulletsBonus;
        bullets = startingBullets;
        firstShotFired = false;
    }

    public void StartBattle()
    {
        maxBullets = 6 + maxBulletsBonus;
        bullets = Mathf.Min(startingBullets, maxBullets);
        firstShotFired = false;
    }

    public int GetFireDamage()
    {
        int dmg = 30 + fireDamageBonus;
        if (!firstShotFired && firstShotDamageBonus > 0)
            dmg += firstShotDamageBonus;
        return dmg;
    }

    public void MarkFirstShot() => firstShotFired = true;
}
