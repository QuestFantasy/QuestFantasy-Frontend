using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;

public class PlayerSkillSnapshot
{
    public string SkillId { get; set; } = "basic_attack";
    public string Name { get; set; } = "Basic Attack";
    public float CooldownSeconds { get; set; } = 1f;
    public float RemainingCooldownSeconds { get; set; } = 0f;
    public int DisplayOrder { get; set; } = 0;
}

public static class PlayerItemSnapshotCodec
{
    public static Godot.Collections.Array EncodeMany(IEnumerable<Item> items)
    {
        var array = new Godot.Collections.Array();
        if (items == null)
        {
            return array;
        }

        foreach (Item item in items)
        {
            var encoded = Encode(item);
            if (encoded != null)
            {
                array.Add(encoded);
            }
        }

        return array;
    }

    public static List<Item> DecodeMany(Godot.Collections.Array source)
    {
        var result = new List<Item>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (!(source[i] is Godot.Collections.Dictionary dict))
            {
                continue;
            }

            Item item = Decode(dict);
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    public static Godot.Collections.Dictionary Encode(Item item)
    {
        if (item == null)
        {
            return null;
        }

        var baseDict = new Godot.Collections.Dictionary
        {
            ["instance_id"] = item.InstanceId ?? string.Empty,
            ["name"] = item.Name ?? "Unnamed",
            ["description"] = item.Description ?? string.Empty,
            ["item_type"] = item.ItemType.ToString(),
            ["quantity"] = Math.Max(1, item.Quantity),
            ["price"] = Math.Max(0, item.Price),
        };

        if (item is Equipment equipment)
        {
            baseDict["equipment_type"] = equipment.EquipmentType.ToString();
            baseDict["rarity"] = Math.Max(1, equipment.Rarity);
            baseDict["level_requirement"] = Math.Max(1, equipment.LevelRequirement);
            baseDict["source"] = equipment.Source ?? string.Empty;
            baseDict["sprite_path"] = NormalizeSpritePathForStorage(equipment.SpritePath, equipment.Sprite);
            baseDict["abilities"] = EncodeAbilities(equipment.EquipmentAbilities);
            return baseDict;
        }

        if (item is Weapon weapon)
        {
            baseDict["weapon_type"] = weapon.WeaponType.ToString();
            baseDict["rarity"] = Math.Max(1, weapon.Rarity);
            baseDict["level_requirement"] = Math.Max(1, weapon.LevelRequirement);
            baseDict["source"] = weapon.Source ?? string.Empty;
            baseDict["sprite_path"] = NormalizeSpritePathForStorage(weapon.SpritePath, weapon.Sprite);
            baseDict["abilities"] = EncodeAbilities(weapon.WeaponAbilities);
            return baseDict;
        }

        return baseDict;
    }

    public static Item Decode(Godot.Collections.Dictionary data)
    {
        if (data == null)
        {
            return null;
        }

        string itemType = ReadString(data, "item_type", "misc");
        if (string.Equals(itemType, "equipment", StringComparison.OrdinalIgnoreCase))
        {
            var equipment = new Equipment
            {
                InstanceId = ReadString(data, "instance_id", string.Empty),
                Name = ReadString(data, "name", "Equipment"),
                Description = ReadString(data, "description", string.Empty),
                Quantity = ReadInt(data, "quantity", 1, 1),
                Price = ReadInt(data, "price", 0, 0),
                EquipmentType = ReadEnum(ReadString(data, "equipment_type", "Other"), EquipmentType.Other),
                Rarity = ReadInt(data, "rarity", 1, 1),
                LevelRequirement = ReadInt(data, "level_requirement", 1, 1),
                Source = ReadString(data, "source", string.Empty),
                SpritePath = NormalizeSpritePathForRuntime(ReadString(data, "sprite_path", string.Empty)),
                EquipmentAbilities = DecodeAbilities(data),
            };
            equipment.Sprite = LoadTextureOrNull(equipment.SpritePath);
            return equipment;
        }

        if (string.Equals(itemType, "weapon", StringComparison.OrdinalIgnoreCase))
        {
            var weapon = new Weapon
            {
                InstanceId = ReadString(data, "instance_id", string.Empty),
                Name = ReadString(data, "name", "Weapon"),
                Description = ReadString(data, "description", string.Empty),
                Quantity = ReadInt(data, "quantity", 1, 1),
                Price = ReadInt(data, "price", 0, 0),
                WeaponType = ReadEnum(ReadString(data, "weapon_type", "Sword"), WeaponType.Sword),
                Rarity = ReadInt(data, "rarity", 1, 1),
                LevelRequirement = ReadInt(data, "level_requirement", 1, 1),
                Source = ReadString(data, "source", string.Empty),
                SpritePath = NormalizeSpritePathForRuntime(ReadString(data, "sprite_path", string.Empty)),
                WeaponAbilities = DecodeAbilities(data),
            };
            weapon.Sprite = LoadTextureOrNull(weapon.SpritePath);
            return weapon;
        }

        var generic = new Item
        {
            InstanceId = ReadString(data, "instance_id", string.Empty),
            Name = ReadString(data, "name", "Item"),
            Description = ReadString(data, "description", string.Empty),
            Quantity = ReadInt(data, "quantity", 1, 1),
            Price = ReadInt(data, "price", 0, 0),
        };
        return generic;
    }

    private static Godot.Collections.Dictionary EncodeAbilities(Abilities abilities)
    {
        return new Godot.Collections.Dictionary
        {
            ["atk"] = Math.Max(0, abilities?.Atk ?? 0),
            ["def"] = Math.Max(0, abilities?.Def ?? 0),
            ["spd"] = Math.Max(0, abilities?.Spd ?? 0),
            ["vit"] = Math.Max(0, abilities?.Vit ?? 0),
        };
    }

    private static Abilities DecodeAbilities(Godot.Collections.Dictionary data)
    {
        var abilities = new Abilities();
        if (data != null && data.Contains("abilities") && data["abilities"] is Godot.Collections.Dictionary ad)
        {
            abilities.Set(
                ReadInt(ad, "atk", 0, 0),
                ReadInt(ad, "def", 0, 0),
                ReadInt(ad, "spd", 0, 0),
                ReadInt(ad, "vit", 0, 0));
            return abilities;
        }

        abilities.Set(0, 0, 0, 0);
        return abilities;
    }

    private static Texture LoadTextureOrNull(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            return null;
        }

        string normalized = NormalizeSpritePathForRuntime(spritePath);
        Texture tex = GD.Load<Texture>(normalized);
        if (tex != null)
        {
            return tex;
        }

        string fileName = System.IO.Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        Texture fallback = GD.Load<Texture>("res://Assets/Equipments/" + fileName);
        if (fallback != null)
        {
            return fallback;
        }

        return GD.Load<Texture>("res://Assets/" + fileName);
    }

    private static string NormalizeSpritePathForStorage(string spritePath, Texture sprite)
    {
        if (!string.IsNullOrWhiteSpace(spritePath))
        {
            return NormalizeSpritePathForRuntime(spritePath);
        }

        string resourcePath = sprite?.ResourcePath;
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            return NormalizeSpritePathForRuntime(resourcePath);
        }

        return string.Empty;
    }

    private static string NormalizeSpritePathForRuntime(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            return string.Empty;
        }

        string path = spritePath.Trim().Replace('\\', '/');
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + path;
        }

        if (path.StartsWith("/"))
        {
            return "res://" + path.TrimStart('/');
        }

        if (!path.Contains("/"))
        {
            return "res://Assets/Equipments/" + path;
        }

        return "res://" + path;
    }

    private static string ReadString(Godot.Collections.Dictionary data, string key, string fallback)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return fallback;
        }

        string text = data[key].ToString();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static int ReadInt(Godot.Collections.Dictionary data, string key, int fallback, int min = int.MinValue)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return Math.Max(min, fallback);
        }

        object raw = data[key];
        int value;
        if (raw is int intValue)
        {
            value = intValue;
        }
        else if (raw is long longValue)
        {
            value = (int)longValue;
        }
        else if (!int.TryParse(raw.ToString(), out value))
        {
            value = fallback;
        }

        return Math.Max(min, value);
    }

    private static TEnum ReadEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (Enum.TryParse(value, true, out TEnum parsed))
        {
            return parsed;
        }

        return fallback;
    }
}

public class PlayerProfileSnapshot
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int HpMax { get; set; } = 100;
    public int HpCurrent { get; set; } = 100;
    public int Gold { get; set; } = 0;
    public bool Ignored { get; set; } = false;
    public string IgnoreReason { get; set; } = string.Empty;
    public bool HasInventoryItemsPayload { get; set; } = false;
    public bool HasDiscardedItemsPayload { get; set; } = false;
    public Godot.Collections.Array InventoryItems { get; set; } = new Godot.Collections.Array();
    public Godot.Collections.Array DiscardedItems { get; set; } = new Godot.Collections.Array();

    public List<PlayerSkillSnapshot> Skills { get; set; } = new List<PlayerSkillSnapshot>();

    public static PlayerProfileSnapshot FromDictionary(Godot.Collections.Dictionary data)
    {
        var snapshot = new PlayerProfileSnapshot();
        if (data == null)
        {
            return snapshot;
        }

        snapshot.Level = ReadInt(data, "level", 1, min: 1);
        snapshot.Experience = ReadInt(data, "experience", 0, min: 0);
        snapshot.HpMax = ReadInt(data, "hp_max", 100, min: 1);
        snapshot.HpCurrent = ReadInt(data, "hp_current", snapshot.HpMax, min: 0, max: snapshot.HpMax);
        snapshot.Gold = ReadInt(data, "gold", 0, min: 0);
        snapshot.Ignored = ReadBool(data, "ignored", false);
        snapshot.IgnoreReason = ReadString(data, "reason", string.Empty);

        if (data.Contains("inventory_items") && data["inventory_items"] is Godot.Collections.Array inventory)
        {
            snapshot.HasInventoryItemsPayload = true;
            snapshot.InventoryItems = inventory;
        }

        if (data.Contains("discarded_items") && data["discarded_items"] is Godot.Collections.Array discarded)
        {
            snapshot.HasDiscardedItemsPayload = true;
            snapshot.DiscardedItems = discarded;
        }

        if (data.Contains("skills") && data["skills"] is Godot.Collections.Array rawSkills)
        {
            for (int i = 0; i < rawSkills.Count; i++)
            {
                if (!(rawSkills[i] is Godot.Collections.Dictionary skillDict))
                {
                    continue;
                }

                snapshot.Skills.Add(new PlayerSkillSnapshot
                {
                    SkillId = ReadString(skillDict, "skill_id", "basic_attack"),
                    Name = ReadString(skillDict, "name", "Unnamed Skill"),
                    CooldownSeconds = ReadFloat(skillDict, "cooldown_seconds", 1f),
                    DisplayOrder = ReadInt(skillDict, "display_order", i, min: 0),
                });
            }
        }

        if (snapshot.Skills.Count == 0)
        {
            snapshot.Skills.Add(new PlayerSkillSnapshot());
        }

        return snapshot;
    }

    public Godot.Collections.Dictionary ToUpdatePayload(string sessionId, int sequence)
    {
        return new Godot.Collections.Dictionary
        {
            ["session_id"] = sessionId ?? string.Empty,
            ["sequence"] = Math.Max(0, sequence),
            ["level"] = Math.Max(1, Level),
            ["experience"] = Math.Max(0, Experience),
            ["hp_max"] = Math.Max(1, HpMax),
            ["hp_current"] = Math.Max(0, Math.Min(HpCurrent, HpMax)),
            ["gold"] = Math.Max(0, Gold),
            ["inventory_items"] = InventoryItems ?? new Godot.Collections.Array(),
            ["discarded_items"] = DiscardedItems ?? new Godot.Collections.Array(),
        };
    }

    private static string ReadString(Godot.Collections.Dictionary data, string key, string fallback)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return fallback;
        }

        string raw = data[key].ToString();
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    private static bool ReadBool(Godot.Collections.Dictionary data, string key, bool fallback)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return fallback;
        }

        if (data[key] is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(data[key].ToString(), out bool parsedBool))
        {
            return parsedBool;
        }

        return fallback;
    }

    private static int ReadInt(Godot.Collections.Dictionary data, string key, int fallback, int min = int.MinValue, int max = int.MaxValue)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return ClampInt(fallback, min, max);
        }

        object raw = data[key];
        int value;
        if (raw is int intValue)
        {
            value = intValue;
        }
        else if (raw is long longValue)
        {
            value = (int)longValue;
        }
        else if (!int.TryParse(raw.ToString(), out value))
        {
            value = fallback;
        }

        return ClampInt(value, min, max);
    }

    private static float ReadFloat(Godot.Collections.Dictionary data, string key, float fallback)
    {
        if (data == null || !data.Contains(key) || data[key] == null)
        {
            return fallback;
        }

        object raw = data[key];
        if (raw is float floatValue)
        {
            return floatValue;
        }

        if (raw is double doubleValue)
        {
            return (float)doubleValue;
        }

        if (float.TryParse(raw.ToString(), out float parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}