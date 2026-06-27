using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;

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
        DifficultyLevel mapDiff = DifficultyLevel.Normal;
        if (parent is Map parentMap) mapDiff = parentMap.Difficulty;

        if (Main.Instance != null)
        {
            string token = Main.Instance.GetAuthToken();
            if (Main.Instance.PlayerDataApiClient != null && !string.IsNullOrEmpty(token))
            {
                Main.Instance.PlayerDataApiClient.GenerateDrops(token, playerLevel, "chest", mapDiff.ToString(), result => {
                    if (result.NetworkOk && result.ResponseCode == 200 && result.ArrayData != null)
                    {
                        SpawnServerDrops(parent, centerPosition, manager, result.ArrayData);
                    }
                    else
                    {
                        GD.PrintErr("[TreasureChest] Failed to generate secure drops from server. Falling back to local generation...");
                        SpawnLocalDrops(parent, centerPosition, manager, playerLevel, mapDiff);
                    }
                });
                return new Godot.Collections.Array();
            }
        }

        SpawnLocalDrops(parent, centerPosition, manager, playerLevel, mapDiff);
        return new Godot.Collections.Array();
    }

    private void SpawnServerDrops(Node parent, Vector2 centerPosition, EquipmentManager manager, Godot.Collections.Array drops)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < drops.Count; i++)
        {
            if (!(drops[i] is Godot.Collections.Dictionary drop))
            {
                continue;
            }

            string instanceId = drop.Contains("instance_id") ? drop["instance_id"]?.ToString() : string.Empty;
            string itemType = drop.Contains("item_type") ? drop["item_type"]?.ToString() : string.Empty;

            if (itemType == "gold")
            {
                int goldAmount = drop.Contains("gold_amount") ? Convert.ToInt32(drop["gold_amount"]) : 0;
                var player = FindPlayerRecursive(parent);
                var coinDrop = new QuestFantasy.Items.CoinDrop();
                coinDrop.InitializeSecure(instanceId, goldAmount, player);
                coinDrop.Position = centerPosition + new Vector2(rng.Randf() * 40f - 20f, rng.Randf() * 40f - 20f);
                parent.AddChild(coinDrop);
                GD.PrintS($"[TreasureChest] Spawned Secure Coin drop of value {goldAmount} at {coinDrop.Position}");
            }
            else if (drop.Contains("item_data") && drop["item_data"] is Godot.Collections.Dictionary itemDataDict)
            {
                itemDataDict["instance_id"] = instanceId;
                Item item = PlayerItemSnapshotCodec.Decode(itemDataDict);
                if (item != null)
                {
                    float pscale = manager != null ? manager.PickupSpriteScale : 0.5f;
                    var offset = new Vector2(rng.Randf() * 200f - 100f, rng.Randf() * 200f - 100f);
                    var itemPos = centerPosition + offset;
                    LootItemFactory.SpawnPickup(parent, item, itemPos, pscale, "secure_chest");
                    GD.PrintS($"[TreasureChest] Spawned secure pickup: {item.Name} at {itemPos}");
                }
            }
        }
    }

    private void SpawnLocalDrops(Node parent, Vector2 centerPosition, EquipmentManager manager, int playerLevel, DifficultyLevel mapDiff)
    {
        int minD = Math.Max(0, MinDrops);
        int maxD = Math.Max(minD, MaxDrops);
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int drops = rng.RandiRange(minD, maxD);

        var options = manager != null ? manager.GetEquipmentSet(OptionCount, playerLevel, LevelOffset) : new System.Collections.Generic.List<Item>();
        var optList = new System.Collections.Generic.List<object>();
        foreach (var o in options)
        {
            optList.Add(o);
        }

        var shuffled = new System.Collections.Generic.List<object>(optList);
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
            pickup.SpriteScale = manager != null ? manager.PickupSpriteScale : 0.1f;
            var offset = new Vector2(rng.Randf() * 200f - 100f, rng.Randf() * 200f - 100f);
            pickup.Position = centerPosition + offset;

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
        }

        Item potionDrop = LootItemFactory.RollPotion(rng, 0.18f);
        if (potionDrop != null)
        {
            var potionPos = centerPosition + new Vector2(rng.Randf() * 180f - 90f, rng.Randf() * 180f - 90f);
            float pscale = manager != null ? manager.PickupSpriteScale : 0.5f;
            LootItemFactory.SpawnPickup(parent, potionDrop, potionPos, pscale, "chest_potion");
        }

        Item ticketDrop = LootItemFactory.RollTicket(rng, mapDiff, 2f);
        if (ticketDrop != null)
        {
            var ticketPos = centerPosition + new Vector2(rng.Randf() * 180f - 90f, rng.Randf() * 180f - 90f);
            float tscale = manager != null ? manager.PickupSpriteScale : 0.5f;
            LootItemFactory.SpawnPickup(parent, ticketDrop, ticketPos, tscale, "chest_ticket");
        }

        var player = FindPlayerRecursive(parent);
        var coinDrop = new QuestFantasy.Items.CoinDrop();
        coinDrop.Initialize(playerLevel, mapDiff, 1.0f, player);
        coinDrop.Position = centerPosition + new Vector2(rng.Randf() * 40f - 20f, rng.Randf() * 40f - 20f);
        parent.AddChild(coinDrop);
    }
}