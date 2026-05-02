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

        /// <summary>
        /// Unique instance identifier assigned by the backend.
        /// Empty string means the item has not yet been synced and does not have a server-side ID.
        /// Never generate this on the client — the backend owns it.
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

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
        /// Clone this item. Override in derived classes to perform deep copies if necessary.
        /// </summary>
        public virtual Item Clone()
        {
            var clone = (Item)this.MemberwiseClone();
            return clone;
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