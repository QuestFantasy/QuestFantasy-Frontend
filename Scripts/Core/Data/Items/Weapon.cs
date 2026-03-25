using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Items
{
    public enum WeaponType { Sword, Bow, Staff }

    public class Weapon : Item
    {
        public WeaponType WeaponType { get; private set; }
        public Abilities WeaponAbilities { get; private set; }
    }
}
