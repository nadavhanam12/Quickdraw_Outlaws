[System.Serializable]
public class EnemyStats
{
    public int hp;
    public int maxHp;
    public int bullets;
    public int maxBullets;
    public int damage;

    public void Initialize(int maxHp, int damage = 30)
    {
        this.maxHp  = maxHp;
        this.hp     = maxHp;
        this.damage = damage;
        maxBullets  = 6;
        bullets     = 0;
    }
}
