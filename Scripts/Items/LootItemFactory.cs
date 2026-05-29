using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Items;

public static class LootItemFactory
{
    // Global multiplier for drop rates used during testing. Set to 5 for 5x drops.
    public static float DropRateMultiplier = 5f;

    public static Item RollPotion(RandomNumberGenerator rng, float chance)
    {
        if (rng == null)
        {
            return null;
        }

        // Apply global multiplier (clamped to 1.0 to avoid weird comparisons)
        float effectiveChance = Mathf.Min(1f, chance * Mathf.Max(0f, DropRateMultiplier));
        if (rng.Randf() >= effectiveChance)
        {
            return null;
        }

        float roll = rng.Randf();
        if (roll < 0.52f)
        {
            return ItemCatalog.CreatePotion(ItemCatalog.HpPotionS);
        }

        if (roll < 0.76f)
        {
            return ItemCatalog.CreatePotion(ItemCatalog.HpPotionM);
        }

        if (roll < 0.90f)
        {
            return ItemCatalog.CreatePotion(ItemCatalog.BurnPotion);
        }

        return ItemCatalog.CreatePotion(ItemCatalog.HpPotionL);
    }

    public static Item RollTicket(RandomNumberGenerator rng, DifficultyLevel currentDifficulty, float chanceScale = 1f)
    {
        if (rng == null)
        {
            return null;
        }

        var weighted = new List<Tuple<DifficultyLevel, float>>
        {
            Tuple.Create(DifficultyLevel.Normal, AdjustTicketWeight(DifficultyLevel.Normal, currentDifficulty, 0.018f)),
            Tuple.Create(DifficultyLevel.Hard, AdjustTicketWeight(DifficultyLevel.Hard, currentDifficulty, 0.010f)),
            Tuple.Create(DifficultyLevel.Nightmare, AdjustTicketWeight(DifficultyLevel.Nightmare, currentDifficulty, 0.005f)),
        };

        float total = 0f;
        foreach (var entry in weighted)
        {
            total += entry.Item2;
        }

        // Apply global drop multiplier to the overall ticket chance scale
        float effectiveThreshold = Mathf.Min(1f, total * Mathf.Max(0f, chanceScale * DropRateMultiplier));
        if (rng.Randf() >= effectiveThreshold)
        {
            return null;
        }

        float pick = rng.Randf() * total;
        foreach (var entry in weighted)
        {
            pick -= entry.Item2;
            if (pick <= 0f)
            {
                return ItemCatalog.CreateTicket(entry.Item1);
            }
        }

        return ItemCatalog.CreateTicket(DifficultyLevel.Normal);
    }

    public static EquipmentPickup SpawnPickup(Node parent, Item item, Vector2 position, float spriteScale, string nameSuffix)
    {
        if (parent == null || item == null)
        {
            return null;
        }

        var pickup = new EquipmentPickup
        {
            ItemData = item,
            SpriteScale = ResolvePickupScale(item, spriteScale),
            Position = position,
            Name = $"Pickup_{ResolvePickupName(item)}_{nameSuffix}",
        };
        parent.AddChild(pickup);
        return pickup;
    }

    private static float AdjustTicketWeight(DifficultyLevel ticketDifficulty, DifficultyLevel currentDifficulty, float baseWeight)
    {
        if (ticketDifficulty == currentDifficulty)
        {
            return baseWeight * 1.8f;
        }

        if (currentDifficulty == DifficultyLevel.Easy && ticketDifficulty == DifficultyLevel.Normal)
        {
            return baseWeight * 1.4f;
        }

        return baseWeight;
    }

    private static string ResolvePickupName(Item item)
    {
        string itemId = ItemCatalog.GetStoredItemId(item);
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return itemId.Replace(' ', '_');
        }

        if (item is Equipment eq && !string.IsNullOrWhiteSpace(eq.SpritePath))
        {
            return System.IO.Path.GetFileNameWithoutExtension(eq.SpritePath).Replace(' ', '_');
        }

        if (item is Weapon w && !string.IsNullOrWhiteSpace(w.SpritePath))
        {
            return System.IO.Path.GetFileNameWithoutExtension(w.SpritePath).Replace(' ', '_');
        }

        return (item.Name ?? "item").Trim().Replace(' ', '_');
    }

    private static float ResolvePickupScale(Item item, float baseScale)
    {
        // Make consumables (potions) and tickets use the same visual scale as equipment
        // so pickups on the map appear consistent in size.
        if (item is ConsumableItem || item is TicketItem)
        {
            return baseScale;
        }

        return baseScale;
    }
}