using System;

using Godot;

using QuestFantasy.Core.Data.Items;
using QuestFantasy.Systems.Inventory;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Manages player inventory-related functionality:
    /// - Experience points and leveling
    /// - Gold/currency
    /// - Item collection and management
    /// </summary>
    public class PlayerInventorySystem
    {
        private int _experience;
        public int Experience => _experience;
        public event Action<int> OnExperienceChanged;

        private int _gold;
        public int Gold => _gold;
        public event Action<int> OnGoldChanged;

        private readonly Bag _inventory;
        public Bag Inventory => _inventory;
        public event Action<Item> OnInventoryChanged;

        public PlayerInventorySystem(int initialGold = 0, int maxInventorySlots = 20)
        {
            _experience = 0;
            _gold = initialGold;
            _inventory = new Bag { MaxSlots = maxInventorySlots };
        }

        /// <summary>
        /// Add experience points (with validation)
        /// </summary>
        public void GainExperience(int amount)
        {
            if (amount < 0)
            {
                GD.PrintErr("[PlayerInventorySystem] Cannot gain negative experience");
                return;
            }

            _experience += amount;
            OnExperienceChanged?.Invoke(_experience);
            GD.Print($"[PlayerInventorySystem] Gained {amount} EXP (Total: {_experience})");
        }

        /// <summary>
        /// Add gold (with validation)
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount == 0)
                return;

            if (amount < 0)
            {
                GD.PrintErr("[PlayerInventorySystem] Cannot add negative gold");
                return;
            }

            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
            GD.Print($"[PlayerInventorySystem] Gained {amount} Gold (Total: {_gold})");
        }

        /// <summary>
        /// Spend gold (returns true if successful)
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0)
            {
                GD.PrintErr("[PlayerInventorySystem] Cannot spend 0 or negative gold");
                return false;
            }

            if (_gold < amount)
            {
                GD.Print($"[PlayerInventorySystem] Not enough gold. Have: {_gold}, Need: {amount}");
                return false;
            }

            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            GD.Print($"[PlayerInventorySystem] Spent {amount} Gold (Remaining: {_gold})");
            return true;
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public bool AddItem(Item item)
        {
            if (item == null)
            {
                GD.PrintErr("[PlayerInventorySystem] Cannot add null item");
                return false;
            }

            bool success = _inventory.AddItem(item);
            if (success)
            {
                OnInventoryChanged?.Invoke(item);
            }
            return success;
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public bool RemoveItem(Item item)
        {
            if (item == null)
                return false;

            return _inventory.RemoveItem(item);
        }

        /// <summary>
        /// Find item by name
        /// </summary>
        public Item FindItem(string itemName)
        {
            return _inventory.FindItem(itemName);
        }

        /// <summary>
        /// Get item count in inventory
        /// </summary>
        public int GetItemCount()
        {
            return _inventory.UsedSlots;
        }

        /// <summary>
        /// Check if inventory has space
        /// </summary>
        public bool HasInventorySpace()
        {
            return _inventory.HasSpace();
        }
    }
}