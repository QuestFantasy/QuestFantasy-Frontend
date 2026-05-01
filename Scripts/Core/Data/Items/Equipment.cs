using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Items
{
    public enum EquipmentType { None, Head, Body, Arms, Legs, Shoes, Other }

    /// <summary>
    /// Manages all equipped items on a character.
    /// Provides methods to equip/unequip items and calculate total stat bonuses.
    /// </summary>
    public class EquippedItems
    {
        public Equipment Head { get; private set; }
        public Equipment Body { get; private set; }
        public Equipment Arms { get; private set; }
        public Equipment Legs { get; private set; }
        public Equipment Shoes { get; private set; }

        /// <summary>
        /// Equips an item to the appropriate slot. Returns the previously equipped item or null.
        /// </summary>
        public Equipment Equipped(Equipment newEquipment)
        {
            if (newEquipment == null)
                return null;

            Equipment oldEquipment = null;
            switch (newEquipment.EquipmentType)
            {
                case EquipmentType.Head:
                    oldEquipment = Head;
                    Head = newEquipment;
                    break;
                case EquipmentType.Body:
                    oldEquipment = Body;
                    Body = newEquipment;
                    break;
                case EquipmentType.Arms:
                    oldEquipment = Arms;
                    Arms = newEquipment;
                    break;
                case EquipmentType.Legs:
                    oldEquipment = Legs;
                    Legs = newEquipment;
                    break;
                case EquipmentType.Shoes:
                    oldEquipment = Shoes;
                    Shoes = newEquipment;
                    break;
            }

            return oldEquipment;
        }

        /// <summary>
        /// Unequip an item from the given slot. Returns the removed item or null.
        /// </summary>
        public Equipment Unequip(EquipmentType slot)
        {
            Equipment removed = null;
            switch (slot)
            {
                case EquipmentType.Head:
                    removed = Head;
                    Head = null;
                    break;
                case EquipmentType.Body:
                    removed = Body;
                    Body = null;
                    break;
                case EquipmentType.Arms:
                    removed = Arms;
                    Arms = null;
                    break;
                case EquipmentType.Legs:
                    removed = Legs;
                    Legs = null;
                    break;
                case EquipmentType.Shoes:
                    removed = Shoes;
                    Shoes = null;
                    break;
            }
            return removed;
        }

        /// <summary>
        /// Get the equipment in a specific slot.
        /// </summary>
        public Equipment GetBySlot(EquipmentType slot)
        {
            switch (slot)
            {
                case EquipmentType.Head: return Head;
                case EquipmentType.Body: return Body;
                case EquipmentType.Arms: return Arms;
                case EquipmentType.Legs: return Legs;
                case EquipmentType.Shoes: return Shoes;
                default: return null;
            }
        }

        /// <summary>
        /// Generic method to calculate total stat contribution from all equipped items.
        /// </summary>
        private int CalculateTotalStat(System.Func<Abilities, int> statSelector)
        {
            if (statSelector == null)
                return 0;

            var slots = new[] { Head, Body, Arms, Legs, Shoes };
            int total = 0;

            foreach (var equipment in slots)
            {
                if (equipment?.EquipmentAbilities != null)
                    total += statSelector(equipment.EquipmentAbilities);
            }

            return total;
        }

        public int TotalAtk() => CalculateTotalStat(a => a.Atk);
        public int TotalDef() => CalculateTotalStat(a => a.Def);
        public int TotalSpd() => CalculateTotalStat(a => a.Spd);
        public int TotalVit() => CalculateTotalStat(a => a.Vit);
    }

    /// <summary>
    /// Equipment item class. Provides stat bonuses when equipped.
    /// </summary>
    public class Equipment : Item
    {
        public EquipmentType EquipmentType { get; set; }
        public Abilities EquipmentAbilities { get; set; }
        public int Rarity { get; set; } = 1;
        public int LevelRequirement { get; set; } = 1;
        public string SpritePath { get; set; }
        public string Source { get; set; }
        public Texture Sprite { get; set; }

        public Equipment()
        {
            ItemType = ItemType.Equipment;
        }

        public override void Use(Player player)
        {
            if (player == null)
            {
                GD.PrintErr($"[Equipment] {Name}: Cannot equip on null player");
                return;
            }

            int atkBonus = EquipmentAbilities?.Atk ?? 0;
            int defBonus = EquipmentAbilities?.Def ?? 0;
            GD.Print($"[Equipment] {player.EntityName} equipped {Name} (+{atkBonus} ATK, +{defBonus} DEF)");
            base.Use(player);
        }

        public override Item Clone()
        {
            var clone = (Equipment)base.Clone();
            if (EquipmentAbilities != null)
            {
                clone.EquipmentAbilities = EquipmentAbilities.Clone();
            }
            return clone;
        }
    }
}