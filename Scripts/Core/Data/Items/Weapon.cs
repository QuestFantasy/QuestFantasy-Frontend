using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Items
{
    public enum WeaponType { Sword, Bow, Staff }

    /// <summary>
    /// Weapon item class. Provides combat bonuses when equipped.
    /// </summary>
    public class Weapon : Item
    {
        public WeaponType WeaponType { get; set; }
        public Abilities WeaponAbilities { get; set; }
        public int Rarity { get; set; } = 1;
        public int LevelRequirement { get; set; } = 1;
        public string SpritePath { get; set; }
        public string Source { get; set; }
        public Texture Sprite { get; set; }

        public Weapon()
        {
            ItemType = ItemType.Weapon;
        }

        public override void Use(Player player)
        {
            if (player == null)
            {
                GD.PrintErr($"[Weapon] {Name}: Cannot use weapon on null player");
                return;
            }

            GD.Print($"[Weapon] {player.EntityName} equipped {Name} (+{WeaponAbilities?.Atk ?? 0} ATK)");
            base.Use(player);
        }

        public override Item Clone()
        {
            var clone = (Weapon)base.Clone();
            if (WeaponAbilities != null)
            {
                clone.WeaponAbilities = WeaponAbilities.Clone();
            }
            return clone;
        }
    }
}