using System;
using System.Collections.Generic;

using Godot;

public class PlayerSkillSnapshot
{
    public string SkillId { get; set; } = "basic_attack";
    public string Name { get; set; } = "Basic Attack";
    public float CooldownSeconds { get; set; } = 1f;
    public float RemainingCooldownSeconds { get; set; } = 0f;
    public int DisplayOrder { get; set; } = 0;
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