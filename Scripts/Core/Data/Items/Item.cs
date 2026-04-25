using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;

namespace QuestFantasy.Core.Data.Items
{
    public enum ItemType { None, Potion, Equipment, Weapon, Misc }

    /// <summary>
    /// Base class for all items in the game.
    /// Handles item properties, pricing, and usage.
    /// </summary>
    public class Item : NameAndDescription
    {
        public int Price { get; set; }
        public ItemType ItemType { get; protected set; }
        public int Quantity { get; set; } = 1;

        public event Action<Item> OnItemUsed;

        /// <summary>
        /// Use this item. Override in derived classes to implement specific behavior.
        /// </summary>
        public virtual void Use(Player player)
        {
            if (player == null)
            {
                GD.PrintErr($"[Item] {Name}: Cannot use item on null player");
                return;
            }

            GD.Print($"[Item] {Name} used by {player.EntityName}");
            OnItemUsed?.Invoke(this);
        }

        /// <summary>
        /// Validate if this item can be used
        /// </summary>
        public virtual bool CanUse(Player player)
        {
            return player != null;
        }
    }
}