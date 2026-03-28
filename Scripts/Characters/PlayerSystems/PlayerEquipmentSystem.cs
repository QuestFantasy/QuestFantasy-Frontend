using System;

using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Manages player equipment system:
    /// - Equipping and unequipping weapons
    /// - Managing armor/equipment slots
    /// - Calculating total attribute bonuses from equipment
    /// </summary>
    public class PlayerEquipmentSystem
    {
        private Weapon _equippedWeapon;
        public Weapon EquippedWeapon => _equippedWeapon;

        private readonly EquippedItems _equipped;
        public EquippedItems EquippedItems => _equipped;

        public event Action<Weapon> OnWeaponEquipped;
        public event Action<Weapon> OnWeaponUnequipped;
        public event Action OnEquipmentChanged;

        public PlayerEquipmentSystem()
        {
            _equipped = new EquippedItems();
        }

        /// <summary>
        /// Equip a weapon
        /// </summary>
        public void EquipWeapon(Weapon weapon)
        {
            if (weapon == null)
            {
                GD.PrintErr("[PlayerEquipmentSystem] Cannot equip null weapon");
                return;
            }

            // Unequip previous weapon
            if (_equippedWeapon != null)
            {
                _equippedWeapon = null;
                OnWeaponUnequipped?.Invoke(_equippedWeapon);
            }

            _equippedWeapon = weapon;
            GD.Print($"[PlayerEquipmentSystem] Equipped weapon: {weapon.Name}");
            OnWeaponEquipped?.Invoke(weapon);
            OnEquipmentChanged?.Invoke();
        }

        /// <summary>
        /// Unequip weapon
        /// </summary>
        public void UnequipWeapon()
        {
            if (_equippedWeapon == null)
            {
                GD.Print("[PlayerEquipmentSystem] No weapon equipped");
                return;
            }

            var previousWeapon = _equippedWeapon;
            _equippedWeapon = null;
            GD.Print($"[PlayerEquipmentSystem] Unequipped weapon: {previousWeapon.Name}");
            OnWeaponUnequipped?.Invoke(previousWeapon);
            OnEquipmentChanged?.Invoke();
        }

        /// <summary>
        /// Get total attack bonus from all equipped items
        /// </summary>
        public int GetTotalAtkBonus()
        {
            int bonus = _equipped?.TotalAtk() ?? 0;
            bonus += _equippedWeapon?.WeaponAbilities?.Atk ?? 0;
            return bonus;
        }

        /// <summary>
        /// Get total defense bonus from all equipped items
        /// </summary>
        public int GetTotalDefBonus()
        {
            int bonus = _equipped?.TotalDef() ?? 0;
            bonus += _equippedWeapon?.WeaponAbilities?.Def ?? 0;
            return bonus;
        }

        /// <summary>
        /// Get total speed bonus from all equipped items
        /// </summary>
        public int GetTotalSpdBonus()
        {
            int bonus = _equipped?.TotalSpd() ?? 0;
            bonus += _equippedWeapon?.WeaponAbilities?.Spd ?? 0;
            return bonus;
        }

        /// <summary>
        /// Get total vitality bonus from all equipped items
        /// </summary>
        public int GetTotalVitBonus()
        {
            int bonus = _equipped?.TotalVit() ?? 0;
            bonus += _equippedWeapon?.WeaponAbilities?.Vit ?? 0;
            return bonus;
        }

        /// <summary>
        /// Get all attribute bonuses as a single object
        /// </summary>
        public Abilities GetAllEquipmentBonuses()
        {
            return new Abilities
            {
                Atk = GetTotalAtkBonus(),
                Def = GetTotalDefBonus(),
                Spd = GetTotalSpdBonus(),
                Vit = GetTotalVitBonus()
            };
        }
    }
}
