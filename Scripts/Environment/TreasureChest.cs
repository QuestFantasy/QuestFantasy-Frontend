using System;

using Godot;

using QuestFantasy.Characters;

public class TreasureChest : Node
{
    [Export]
    public NodePath EquipmentManagerPath;

    [Export(PropertyHint.Range, "1,10,1")]
    public int OptionCount = 3;

    [Export]
    public int LevelOffset = 1; // +/- levels from player level

    [Export]
    public int MinDrops = 1;

    [Export]
    public int MaxDrops = 4;

    private EquipmentManager _manager;

    public override void _Ready()
    {
        if (EquipmentManagerPath != null && EquipmentManagerPath != "")
        {
            _manager = GetNode<EquipmentManager>(EquipmentManagerPath);
        }
    }

    // Handle map's BoxOpened signal directly and spawn drops.
    // This allows Map -> TreasureChest wiring without Main as intermediary.
    public void HandleMapBoxOpened(Vector2 worldPosition)
    {
        var manager = _manager ?? FindEquipmentManagerRecursive(GetTree().Root);
        var player = FindPlayerRecursive(GetTree().Root);
        int playerLevel = 1;
        if (player != null)
            playerLevel = (int)player.Level;

        Node parent = GetParent() ?? GetTree().Root;
        OpenChest(parent, worldPosition, manager, playerLevel);
    }

    private EquipmentManager FindEquipmentManagerRecursive(Node node)
    {
        if (node is EquipmentManager em) return em;
        foreach (Node child in node.GetChildren())
        {
            var found = FindEquipmentManagerRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    private Player FindPlayerRecursive(Node node)
    {
        if (node is Player p) return p;
        foreach (Node child in node.GetChildren())
        {
            var found = FindPlayerRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    // Return a set of equipment choices for the given player level.
    public Godot.Collections.Array GetEquipmentSet(int playerLevel)
    {
        var list = new Godot.Collections.Array();
        if (_manager == null)
            return list;
        var set = _manager.GetEquipmentSet(OptionCount, playerLevel, LevelOffset);
        foreach (var it in set)
        {
            if (it is QuestFantasy.Core.Data.Items.Equipment eq)
            {
                eq.Source = "TreasureChest";
                list.Add(eq);
            }
            else if (it is QuestFantasy.Core.Data.Items.Weapon w)
            {
                w.Source = "TreasureChest";
                list.Add(w);
            }
            else
            {
                list.Add(it);
            }
        }
        return list;
    }

    // Convenience: return single equipment (random from a set based on player level)
    public object OpenAndGetEquipment(int playerLevel = 1)
    {
        var set = GetEquipmentSet(playerLevel);
        if (set.Count == 0)
            return null;
        int idx = (int)GD.RandRange(0, set.Count);
        if (idx < 0) idx = 0;
        if (idx >= set.Count) idx = set.Count - 1;
        return set[idx];
    }

    // Open the chest and spawn pickups under the given parent node around centerPosition.
    // Returns the list of spawned EquipmentPickup nodes.
    public Godot.Collections.Array OpenChest(Node parent, Vector2 centerPosition, EquipmentManager manager, int playerLevel)
    {
        var spawned = new Godot.Collections.Array();
        if (manager == null || parent == null)
            return spawned;

        int minD = Math.Max(0, MinDrops);
        int maxD = Math.Max(minD, MaxDrops);
        // Use RandomNumberGenerator to avoid casting overflow from GD.Randi
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int drops = rng.RandiRange(minD, maxD);

        GD.PrintS($"[TreasureChest] drop range min={minD} max={maxD} -> drops={drops}");

        // Use the provided manager to get equipment options (avoid relying on _manager field)
        var options = manager.GetEquipmentSet(OptionCount, playerLevel, LevelOffset);
        GD.PrintS($"[TreasureChest] Opening chest: drops={drops}, options={options.Count}");

        // Convert options to a typed list
        var optList = new System.Collections.Generic.List<object>();
        foreach (var o in options)
        {
            optList.Add(o);
        }

        // Shuffle optList and take unique items up to available count
        var shuffled = new System.Collections.Generic.List<object>(optList);
        // Fisher-Yates shuffle
        for (int s = shuffled.Count - 1; s > 0; s--)
        {
            int j = rng.RandiRange(0, s);
            var tmp = shuffled[s];
            shuffled[s] = shuffled[j];
            shuffled[j] = tmp;
        }

        int take = Math.Min(drops, shuffled.Count);
        for (int i = 0; i < take; i++)
        {
            var it = shuffled[i];
            if (it == null)
                continue;

            var pickup = new EquipmentPickup();
            pickup.ItemData = it;
            pickup.SpriteScale = manager.PickupSpriteScale;
            var offset = new Vector2(rng.Randf() * 200f - 100f, rng.Randf() * 200f - 100f);
            pickup.Position = centerPosition + offset;

            // deterministic node name based on sprite/resource name
            string baseName = "equipment";
            var spriteTex = (pickup.ItemData is QuestFantasy.Core.Data.Items.Equipment pe) ? pe.Sprite : (pickup.ItemData is QuestFantasy.Core.Data.Items.Weapon pw ? pw.Sprite : null);
            if (spriteTex != null)
            {
                var rp = spriteTex.ResourcePath;
                if (!string.IsNullOrEmpty(rp))
                {
                    baseName = System.IO.Path.GetFileNameWithoutExtension(rp).Replace(' ', '_');
                }
            }
            pickup.Name = $"Pickup_{baseName}_{i}";
            parent.AddChild(pickup);
            spawned.Add(pickup);
            GD.PrintS($"[TreasureChest] Spawned pickup: {pickup.Name} at {pickup.Position}");
            if (pickup.ItemData == null || (pickup.ItemData is QuestFantasy.Core.Data.Items.Equipment e2 && e2.Sprite == null) || (pickup.ItemData is QuestFantasy.Core.Data.Items.Weapon w2 && w2.Sprite == null))
            {
                GD.PrintS($"[TreasureChest] WARNING: pickup {pickup.Name} has no sprite or item is null");
            }
        }

        return spawned;
    }
}