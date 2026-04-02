using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Systems.Inventory
{
    /// <summary>
    /// Inventory bag system. Manages items and their quantities.
    /// Supports weight/slot limits and item sorting.
    /// </summary>
    public class Bag
    {
        public List<Item> Items { get; set; } = new List<Item>();

        /// <summary>
        /// Maximum number of item slots in this bag. 0 = unlimited.
        /// </summary>
        public int MaxSlots { get; set; } = 20;

        /// <summary>
        /// Current number of occupied slots
        /// </summary>
        public int UsedSlots => Items.Count;

        /// <summary>
        /// Check if bag has space for a new item
        /// </summary>
        public bool HasSpace() => MaxSlots <= 0 || UsedSlots < MaxSlots;

        /// <summary>
        /// Add an item to the bag
        /// </summary>
        public bool AddItem(Item item)
        {
            if (item == null)
            {
                GD.PrintErr("[Bag] Cannot add null item");
                return false;
            }

            if (!HasSpace())
            {
                GD.PrintErr("[Bag] Bag is full!");
                return false;
            }

            Items.Add(item);
            GD.Print($"[Bag] Added {item.Name} (Slots: {UsedSlots}/{MaxSlots})");
            return true;
        }

        /// <summary>
        /// Remove an item from the bag
        /// </summary>
        public bool RemoveItem(Item item)
        {
            if (item == null)
                return false;

            return Items.Remove(item);
        }

        /// <summary>
        /// Find item by name
        /// </summary>
        public Item FindItem(string itemName)
        {
            return Items.FirstOrDefault(i => i.Name == itemName);
        }

        /// <summary>
        /// Get all items of a specific type
        /// </summary>
        public List<Item> GetItemsByType(ItemType type)
        {
            return Items.Where(i => i.ItemType == type).ToList();
        }

        /// <summary>
        /// Clear all items from bag
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            GD.Print("[Bag] Bag cleared");
        }
    }
}