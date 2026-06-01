using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Items
{
    public class ConsumableItem : Item
    {
        public string ItemId { get; set; } = string.Empty;
        public string SpritePath { get; set; } = string.Empty;
        public Texture Sprite { get; set; }
        public int HealAmount { get; set; } = 0;
        public bool RemovesBurn { get; set; } = false;

        public ConsumableItem()
        {
            ItemType = ItemType.Potion;
        }

        public override bool CanUse(Player player)
        {
            if (!base.CanUse(player) || player.Attributes?.HP == null || !player.Attributes.HP.IsAlive)
            {
                return false;
            }

            if (HealAmount > 0)
            {
                return true;
            }

            return RemovesBurn && player.EffectManager?.HasEffect(StatusEffectType.Burn) == true;
        }

        public override void Use(Player player)
        {
            if (!CanUse(player))
            {
                GD.Print($"[ConsumableItem] {Name} cannot be used right now.");
                return;
            }

            if (HealAmount > 0)
            {
                int before = player.Attributes.HP.CurrentHP;
                player.Attributes.HP.Heal(HealAmount);
                int healed = player.Attributes.HP.CurrentHP - before;
                GD.Print($"[ConsumableItem] {Name} healed {healed} HP ({player.Attributes.HP.CurrentHP}/{player.Attributes.HP.MaxHP}).");
            }

            if (RemovesBurn)
            {
                player.EffectManager?.RemoveEffect(StatusEffectType.Burn, player);
                GD.Print($"[ConsumableItem] {Name} cured burn on {player.EntityName}.");
            }

            base.Use(player);
        }
    }

    public class TicketItem : Item
    {
        public string ItemId { get; set; } = string.Empty;
        public string SpritePath { get; set; } = string.Empty;
        public Texture Sprite { get; set; }
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

        public TicketItem()
        {
            ItemType = ItemType.Misc;
        }
    }

    public static class ItemCatalog
    {
        public const string HpPotionS = "hp_potion_s";
        public const string HpPotionM = "hp_potion_m";
        public const string HpPotionL = "hp_potion_l";
        public const string BurnPotion = "burn_potion";
        public const string TicketNormal = "ticket_normal";
        public const string TicketHard = "ticket_hard";
        public const string TicketNightmare = "ticket_nightmare";

        public static ConsumableItem CreatePotion(string itemId)
        {
            switch ((itemId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case BurnPotion:
                    return CreatePotion(BurnPotion, "Burn Remedy", "Immediately cures Burn.", "res://Assets/items/burn_potion.png", 0, removesBurn: true, price: 30);
                case HpPotionL:
                    return CreatePotion(HpPotionL, "Large HP Potion", "Restores 20 HP.", "res://Assets/items/hp_potion_L.png", 20);
                case HpPotionM:
                    return CreatePotion(HpPotionM, "Medium HP Potion", "Restores 12 HP.", "res://Assets/items/hp_potion_M.png", 12);
                default:
                    return CreatePotion(HpPotionS, "Small HP Potion", "Restores 5 HP.", "res://Assets/items/hp_potion_S.png", 5);
            }
        }

        public static TicketItem CreateTicket(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Nightmare:
                    return CreateTicket(TicketNightmare, "Nightmare Ticket", "Allows one Nightmare dungeon entry.", "res://Assets/items/ticket_nightmare.png", DifficultyLevel.Nightmare);
                case DifficultyLevel.Hard:
                    return CreateTicket(TicketHard, "Hard Ticket", "Allows one Hard dungeon entry.", "res://Assets/items/ticket_hard.png", DifficultyLevel.Hard);
                default:
                    return CreateTicket(TicketNormal, "Normal Ticket", "Allows one Normal dungeon entry.", "res://Assets/items/ticket_normal.png", DifficultyLevel.Normal);
            }
        }

        public static bool IsUsableFromBackpack(Item item)
        {
            return item is ConsumableItem;
        }

        public static bool IsToolPanelItem(Item item)
        {
            return item is ConsumableItem || item is TicketItem;
        }

        public static bool IsTicketForDifficulty(Item item, DifficultyLevel difficulty)
        {
            if (difficulty == DifficultyLevel.Easy)
            {
                return false;
            }

            if (item is TicketItem ticket)
            {
                return ticket.Difficulty == difficulty;
            }

            string name = (item?.Name ?? string.Empty).Trim().ToLowerInvariant();
            string id = GetStoredItemId(item);
            string expected = GetTicketItemId(difficulty);
            return string.Equals(id, expected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, expected, StringComparison.OrdinalIgnoreCase)
                || name.Contains(expected);
        }

        public static string GetTicketItemId(DifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case DifficultyLevel.Nightmare:
                    return TicketNightmare;
                case DifficultyLevel.Hard:
                    return TicketHard;
                case DifficultyLevel.Normal:
                    return TicketNormal;
                default:
                    return string.Empty;
            }
        }

        public static string GetStoredItemId(Item item)
        {
            if (item is ConsumableItem consumable)
            {
                return consumable.ItemId;
            }

            if (item is TicketItem ticket)
            {
                return ticket.ItemId;
            }

            return string.Empty;
        }

        private static ConsumableItem CreatePotion(string itemId, string name, string description, string spritePath, int healAmount, bool removesBurn = false, int price = -1)
        {
            var item = new ConsumableItem
            {
                ItemId = itemId,
                Name = name,
                Description = description,
                SpritePath = spritePath,
                HealAmount = healAmount,
                RemovesBurn = removesBurn,
                Price = price >= 0 ? price : healAmount * 2,
            };
            item.Sprite = GD.Load<Texture>(spritePath);
            return item;
        }

        private static TicketItem CreateTicket(string itemId, string name, string description, string spritePath, DifficultyLevel difficulty)
        {
            var item = new TicketItem
            {
                ItemId = itemId,
                Name = name,
                Description = description,
                SpritePath = spritePath,
                Difficulty = difficulty,
                Price = 0,
            };
            item.Sprite = GD.Load<Texture>(spritePath);
            return item;
        }
    }
}