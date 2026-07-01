public class UpgradeData
{
    public int id;
    public string name;
    public string description;

    public static readonly UpgradeData[] All = new UpgradeData[]
    {
        // ── Original 9 ───────────────────────────────────────────────────────
        new UpgradeData { id =  1, name = "Iron Will",       description = "+20 Max HP and heal 20" },
        new UpgradeData { id =  2, name = "Quick Draw",      description = "Start each battle with 1 bullet" },
        new UpgradeData { id =  3, name = "Hot Lead",        description = "Fire damage +10" },
        new UpgradeData { id =  4, name = "Speed Loader",    description = "Reload gives +1 extra bullet" },
        new UpgradeData { id =  5, name = "Extended Mag",    description = "Max bullets +1" },
        new UpgradeData { id =  6, name = "Steel Guard",     description = "Defend heals 5 HP" },
        new UpgradeData { id =  7, name = "First Blood",     description = "First shot each battle deals +20 damage" },
        new UpgradeData { id =  8, name = "Bounty",          description = "Win a battle: heal 15 HP" },
        new UpgradeData { id =  9, name = "Intimidation",    description = "Enemy starts with -10 HP" },
        // ── New 11 ───────────────────────────────────────────────────────────
        new UpgradeData { id = 10, name = "Dead Eye",        description = "Shoot an unarmed enemy (0 bullets): +20 damage" },
        new UpgradeData { id = 11, name = "Thick Skin",      description = "Reduce all incoming damage by 5" },
        new UpgradeData { id = 12, name = "War Paint",       description = "Heal 10 HP at the start of each battle" },
        new UpgradeData { id = 13, name = "Gold Rush",       description = "Earn +10 bonus gold from every loot" },
        new UpgradeData { id = 14, name = "Quick Hands",     description = "Reloading also heals 5 HP" },
        new UpgradeData { id = 15, name = "Ambush",          description = "Deal +25 damage when enemy reloads" },
        new UpgradeData { id = 16, name = "Last Stand",      description = "Below 33% HP: fire deals +20 extra damage" },
        new UpgradeData { id = 17, name = "Hired Muscle",    description = "+15 Max HP" },
        new UpgradeData { id = 18, name = "Extra Clip",      description = "Max bullets +2" },
        new UpgradeData { id = 19, name = "Desperado",       description = "Fire damage +15" },
        new UpgradeData { id = 20, name = "Outlaw Legend",   description = "Enemy starts with -25 HP" },
        // ── Aim upgrades ──────────────────────────────────────────────────────
        new UpgradeData { id = 21, name = "Steady Hand",     description = "Aim roll minimum is 2" },
        new UpgradeData { id = 22, name = "Sniper's Eye",    description = "Aim roll maximum +3" },
        new UpgradeData { id = 23, name = "Gunslinger",      description = "+2 to every aim roll" },
        new UpgradeData { id = 24, name = "Lucky Shot",      description = "Aim rolls twice, takes the higher result" },
        new UpgradeData { id = 25, name = "Eagle Eye",       description = "+1 to every aim roll" },
        // ── Gold upgrades ─────────────────────────────────────────────────────
        new UpgradeData { id = 26, name = "Wanted Poster",   description = "Earn +20 bonus gold per victory" },
        new UpgradeData { id = 27, name = "Treasure Map",    description = "Earn +35 bonus gold per victory" },
    };
}
