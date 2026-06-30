using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleVisuals : MonoBehaviour
{
    [Header("Characters")]
    public Image playerSprite;
    public Image enemySprite;
    public Sprite[] enemyTierSprites; // 0=Weak, 1=Normal, 2=Strong, 3=Elite
    public TextMeshProUGUI enemyTierLabel;

    [Header("HP Bars")]
    public Image playerHpFill;
    public Image enemyHpFill;

    [Header("Bullet Icons")]
    public Image[] playerBulletIcons;
    public Image[] enemyBulletIcons;
    public Sprite bulletFilledSprite;
    public Sprite bulletEmptySprite;

    [Header("Shield Icons")]
    public Image[] shieldIcons;

    static readonly Color ShieldFull  = new Color(0.35f, 0.65f, 1f,    1f);
    static readonly Color ShieldEmpty = new Color(0.15f, 0.22f, 0.35f, 1f);

    public void SetEnemyTier(PathData.EnemyTier tier)
    {
        int idx = (int)tier;
        if (enemySprite != null && enemyTierSprites != null && idx < enemyTierSprites.Length)
        {
            var spr = enemyTierSprites[idx];
            if (spr != null) enemySprite.sprite = spr;
        }

        if (enemyTierLabel != null)
            enemyTierLabel.text = PathData.EnemyLabel(tier).ToUpper();
    }

    public void Refresh(PlayerStats p, EnemyStats e)
    {
        if (playerHpFill != null)
            playerHpFill.fillAmount = p.maxHp > 0 ? (float)p.hp / p.maxHp : 0f;

        if (enemyHpFill != null)
            enemyHpFill.fillAmount = e.maxHp > 0 ? (float)e.hp / e.maxHp : 0f;

        RefreshBulletIcons(playerBulletIcons, p.bullets, p.maxBullets);
        RefreshBulletIcons(enemyBulletIcons,  e.bullets, e.maxBullets);
        RefreshShieldIcons(p.blockUses, p.maxBlockUses);
    }

    void RefreshBulletIcons(Image[] icons, int current, int max)
    {
        if (icons == null) return;
        int show = Mathf.Min(max, icons.Length);
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].gameObject.SetActive(i < show);
            if (i < show)
            {
                bool filled = i < current;
                if (bulletFilledSprite != null && bulletEmptySprite != null)
                    icons[i].sprite = filled ? bulletFilledSprite : bulletEmptySprite;
                else
                    icons[i].color = filled ? new Color(1f, 0.88f, 0.2f) : new Color(0.25f, 0.22f, 0.10f);
            }
        }
    }

    void RefreshShieldIcons(int current, int max)
    {
        if (shieldIcons == null) return;
        int show = Mathf.Min(max, shieldIcons.Length);
        for (int i = 0; i < shieldIcons.Length; i++)
        {
            shieldIcons[i].gameObject.SetActive(i < show);
            if (i < show)
                shieldIcons[i].color = i < current ? ShieldFull : ShieldEmpty;
        }
    }
}
