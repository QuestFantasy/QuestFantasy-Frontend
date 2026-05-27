using System;

using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Items
{
    public class ConsumableItem : Item
    {
        public string ItemId { get; set; } = string.Empty;
        public string SpritePath { get; set; } = string.Empty;
        public Texture Sprite { get; set; }
        public int HealAmount { get; set; } = 0;

        public ConsumableItem()
        {
            ItemType = ItemType.Potion;
        }

        public override bool CanUse(Player player)
        {
            return base.CanUse(player) && HealAmount > 0 && player.Attributes?.HP != null && player.Attributes.HP.IsAlive;
        }

        public override void Use(Player player)
        {
            if (!CanUse(player))
            {
                GD.Print($"[ConsumableItem] {Name} cannot be used right now.");
                return;
            }

            int before = player.Attributes.HP.CurrentHP;
            player.Attributes.HP.Heal(HealAmount);
            int healed = player.Attributes.HP.CurrentHP - before;
            GD.Print($"[ConsumableItem] {Name} healed {healed} HP ({player.Attributes.HP.CurrentHP}/{player.Attributes.HP.MaxHP}).");
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
        public const string TicketNormal = "ticket_normal";
        public const string TicketHard = "ticket_hard";
        public const string TicketNightmare = "ticket_nightmare";

        public static ConsumableItem CreatePotion(string itemId)
        {
            switch ((itemId ?? string.Empty).Trim().ToLowerInvariant())
            {
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

        private static ConsumableItem CreatePotion(string itemId, string name, string description, string spritePath, int healAmount)
        {
            var item = new ConsumableItem
            {
                ItemId = itemId,
                Name = name,
                Description = description,
                SpritePath = spritePath,
                HealAmount = healAmount,
                Price = healAmount * 2,
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