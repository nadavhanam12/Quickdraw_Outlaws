public class UpgradeData
{
    public int id;
    public string name;
    public string description;

    public static readonly UpgradeData[] All = new UpgradeData[]
    {
        new UpgradeData { id = 1,  name = "Iron Will",    description = "+20 Max HP and heal 20" },
        new UpgradeData { id = 2,  name = "Quick Draw",   description = "Start each battle with 1 bullet" },
        new UpgradeData { id = 3,  name = "Hot Lead",     description = "Fire damage +10" },
        new UpgradeData { id = 4,  name = "Speed Loader", description = "Reload gives +1 extra bullet" },
        new UpgradeData { id = 5,  name = "Extended Mag", description = "Max bullets +1" },
        new UpgradeData { id = 6,  name = "Steel Guard",  description = "Defend heals 5 HP" },
        new UpgradeData { id = 7,  name = "First Blood",  description = "First shot each battle deals +20 damage" },
        new UpgradeData { id = 8,  name = "Bounty",       description = "Win battle: heal 15 HP" },
        new UpgradeData { id = 9,  name = "Intimidation", description = "Enemy starts with -10 HP" },
        new UpgradeData { id = 10, name = "Precision",    description = "Fire button disabled when out of bullets" },
    };
}
