using System;
using System.Collections.Generic;
using System.IO;

using Godot;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Core.Data.Attributes;

public class EquipmentManager : Node
{
    [Export]
    public string EquipmentsPath = "res://Assets/Equipments";

    [Export]
    public float PickupSpriteScale = 0.1f;

    [Export]
    public float LevelScalingMultiplier = 1.0f; // scaling factor per equipment level (factor = 1 + level * multiplier)

    // Provide explicit asset lists per equipment category (no automatic discovery).
    [Export]
    public string[] BowAssetPaths = new string[] { "Assets/Equipments/bow/basic-bow.png" };

    [Export]
    public string[] SwordAssetPaths = new string[] { "Assets/Equipments/sword/basic-sword.png" };

    [Export]
    public string[] ChestplateAssetPaths = new string[] { "Assets/Equipments/chestplate/basic-chestplate.png" };

    [Export]
    public string[] GlovesAssetPaths = new string[] { "Assets/Equipments/gloves/basic-gloves.png" };

    [Export]
    public string[] HelmetAssetPaths = new string[] { "Assets/Equipments/helmet/basic-helmet.png" };

    [Export]
    public string[] ShoesAssetPaths = new string[] { "Assets/Equipments/shoes/basic-shoes.png" };

    [Export]
    public string[] StaffAssetPaths = new string[] { "Assets/Equipments/staff/basic-staff.png" };

    // Helper: combine two strings into an equipment asset path.
    // Example: CombineAssetToPath("basic", "chestplate") -> "Assets/Equipments/chestplate/basic.png"
    public string CombineAssetToPath(string baseName, string category)
    {
        if (string.IsNullOrEmpty(baseName) || string.IsNullOrEmpty(category))
            return null;
        var bn = baseName.Trim().Replace(' ', '-');
        var cat = category.Trim().Trim('/').Replace(' ', '-');
        return $"Assets/Equipments/{cat}/{bn}.png";
    }

    // Helper: combine two strings into a display name (e.g. "basic" + "chestplate" -> "Basic Chestplate")
    public string CombineToDisplayName(string partA, string partB)
    {
        string A = string.IsNullOrEmpty(partA) ? "" : partA.Trim();
        string B = string.IsNullOrEmpty(partB) ? "" : partB.Trim();
        if (string.IsNullOrEmpty(A) && string.IsNullOrEmpty(B)) return "";
        string combined = (A + " " + B).Trim();
        // Simple Title Case: capitalize first letter of each word
        var words = combined.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0) continue;
            words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1) : "");
        }
        return string.Join(" ", words);
    }

    // Create item from base name + category using explicit if/else mapping (not filename heuristics).
    // Supports optional rarity: CreateFromParts("basic", "chestplate", 1)
    // Example: CreateFromParts("basic", "chestplate", 1) -> tries Assets/Equipments/chestplate/basic-chestplate.png
    public Item CreateFromParts(string baseName, string category, int rarity = 1)
    {
        if (string.IsNullOrEmpty(baseName) || string.IsNullOrEmpty(category))
            return null;

        var bn = baseName.Trim().ToLower();
        var cat = category.Trim().ToLower();

        // map common aliases to folder names
        string folder;
        if (cat == "chestplate" || cat == "chest" || cat == "body") folder = "chestplate";
        else if (cat == "boots" || cat == "shoes" || cat == "shoe") folder = "shoes";
        else if (cat == "staff") folder = "staff";
        else if (cat == "sword" || cat == "blade" || cat == "dagger") folder = "sword";
        else if (cat == "bow") folder = "bow";
        else if (cat == "gloves" || cat == "gauntlet" || cat == "arms") folder = "gloves";
        else if (cat == "helmet" || cat == "head") folder = "helmet";
        else folder = cat; // fallback: use raw category as folder

        // construct candidate filenames in order of likely patterns
        var candidates = new string[] {
            $"Assets/Equipments/{folder}/{bn}-{folder}.png",
            $"Assets/Equipments/{folder}/{bn}.png",
            $"Assets/Equipments/{folder}/basic-{folder}.png",
            $"Assets/Equipments/{folder}/default.png"
        };

        foreach (var c in candidates)
        {
            var item = CreateFromAssetWithCategory(c, category, rarity);
            if (item != null)
                return item;
        }

        return null;
    }

    // Map rarity to a human-friendly prefix
    public string RarityPrefix(int rarity)
    {
        switch (rarity)
        {
            case 1: return "Basic";
            case 2: return "Fine";
            case 3: return "Rare";
            case 4: return "Epic";
            case 5: return "Legendary";
            default: return "Basic";
        }
    }

    // Map category id to display noun (e.g., shoes -> Boots)
    public string CategoryDisplayName(string category)
    {
        if (string.IsNullOrEmpty(category)) return "Item";
        var c = category.Trim().ToLower();
        if (c == "shoes" || c == "shoe" || c == "boots") return "Boots";
        if (c == "chestplate" || c == "chest" || c == "body") return "Chestplate";
        if (c == "gloves" || c == "gauntlet" || c == "arms") return "Gloves";
        if (c == "helmet" || c == "head") return "Helmet";
        if (c == "sword" || c == "blade" || c == "dagger") return "Sword";
        if (c == "bow") return "Bow";
        if (c == "staff") return "Staff";
        return char.ToUpper(c[0]) + (c.Length > 1 ? c.Substring(1) : "");
    }

    // Create an Item (Equipment or Weapon) from an explicit asset path and a declared category.
    // Uses fixed naming: e.g., rarity 1 + chestplate -> "Basic Chestplate".
    public Item CreateFromAssetWithCategory(string assetPath, string category, int rarity = 1)
    {
        var tex = LoadSprite(assetPath);
        if (tex == null)
            return null;

        var cat = (category ?? "").Trim().ToLower();
        bool isWeaponCat = (cat == "sword" || cat == "bow" || cat == "staff" || cat == "blade" || cat == "dagger");

        string displayCategory = CategoryDisplayName(category);
        string displayName = CombineToDisplayName(RarityPrefix(rarity), displayCategory);

        if (isWeaponCat)
        {
            var w = new Weapon();
            if (cat == "bow") w.WeaponType = WeaponType.Bow;
            else if (cat == "staff") w.WeaponType = WeaponType.Staff;
            else w.WeaponType = WeaponType.Sword;
            w.Rarity = rarity;
            w.WeaponAbilities = new Abilities();
            // Assign weapon-focused stats per type
            switch (w.WeaponType)
            {
                case WeaponType.Sword:
                    // Sword: Attack & Defense
                    w.WeaponAbilities.Set(w.Rarity * 3, w.Rarity * 2, w.Rarity >= 3 ? 1 : 0, 0);
                    break;
                case WeaponType.Bow:
                    // Bow: Attack & Speed
                    w.WeaponAbilities.Set(w.Rarity * 2, w.Rarity, w.Rarity * 3, 0);
                    break;
                case WeaponType.Staff:
                    // Staff: Higher Attack
                    w.WeaponAbilities.Set(w.Rarity * 5, w.Rarity, w.Rarity >= 3 ? 1 : 0, 0);
                    break;
                default:
                    w.WeaponAbilities.Set(w.Rarity * 2, w.Rarity, w.Rarity >= 3 ? 1 : 0, 0);
                    break;
            }
            w.LevelRequirement = Math.Max(1, w.Rarity * 2 - 1);
            w.Sprite = tex;
            w.SpritePath = assetPath;
            w.Source = "Generated";
            w.Price = w.Rarity * 10 + w.LevelRequirement * 5;
            w.Name = displayName;
            return w;
        }

        var eq = new Equipment();
        if (cat == "helmet" || cat == "head") eq.EquipmentType = EquipmentType.Head;
        else if (cat == "chestplate" || cat == "chest" || cat == "body") eq.EquipmentType = EquipmentType.Body;
        else if (cat == "gloves" || cat == "gauntlet" || cat == "arms") eq.EquipmentType = EquipmentType.Arms;
        else if (cat == "shoes" || cat == "boots" || cat == "shoe") eq.EquipmentType = EquipmentType.Shoes;
        else eq.EquipmentType = EquipmentType.Other;

        eq.Rarity = rarity;
        eq.EquipmentAbilities = new Abilities();
        // Assign focused stat bonuses based on equipment slot/type
        switch (eq.EquipmentType)
        {
            case EquipmentType.Head:
                // Head: Vitality focused (more HP)
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity, 0, eq.Rarity * 3);
                break;
            case EquipmentType.Body:
                // Body: Defense focused
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity * 3, 0, eq.Rarity * 2);
                break;
            case EquipmentType.Arms:
                // Arms/Gloves: Attack & Speed focused
                eq.EquipmentAbilities.Set(eq.Rarity * 2, eq.Rarity, eq.Rarity, 0);
                break;
            case EquipmentType.Shoes:
                // Shoes: Speed focused
                eq.EquipmentAbilities.Set(0, eq.Rarity, eq.Rarity * 3, eq.Rarity);
                break;
            default:
                // Fallback: balanced small bonuses
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity, 0, 0);
                break;
        }

        eq.LevelRequirement = Math.Max(1, eq.Rarity * 2 - 1);
        eq.Sprite = tex;
        eq.SpritePath = assetPath;
        eq.Source = "Generated";
        eq.Price = eq.Rarity * 10 + eq.LevelRequirement * 5;
        eq.Name = displayName;

        return eq;
    }

    // Load a texture by filename from the Equipments folder
    public Texture LoadSprite(string fileName)
    {
        string path;
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (fileName.StartsWith("res://"))
            path = fileName;
        else if (fileName.StartsWith("Assets/") || fileName.StartsWith("Assets\\"))
            path = "res://" + fileName.Replace('\\', '/');
        else
            path = EquipmentsPath.TrimEnd('/') + "/" + fileName.Replace('\\', '/');

        var res = ResourceLoader.Load(path);
        return res as Texture;
    }

    // Create a minimal Item (Equipment or Weapon) from a sprite file name.
    // The created Item is an in-memory object (not saved to disk).
    public Item CreateFromSpriteFile(string filePath)
    {
        var tex = LoadSprite(filePath);
        if (tex == null)
            return null;

        // Use the full file path for heuristics (e.g. Assets/Equipments/bow/basic-bow.png)
        var lname = filePath.ToLower();
        var baseName = System.IO.Path.GetFileNameWithoutExtension(filePath).ToLower();

        bool isSword = baseName.Contains("sword") || lname.Contains("/sword/") || baseName.Contains("blade") || baseName.Contains("axe") || baseName.Contains("dagger");
        bool isBow = baseName.Contains("bow") || lname.Contains("/bow/");
        bool isStaff = baseName.Contains("staff") || lname.Contains("/staff/");

            if (isSword || isBow || isStaff)
        {
            var w = new Weapon();
            if (isSword) w.WeaponType = WeaponType.Sword;
            else if (isBow) w.WeaponType = WeaponType.Bow;
            else w.WeaponType = WeaponType.Staff;

            // rarity detection
            w.Rarity = 1;
            for (int i = 5; i >= 1; i--)
            {
                if (lname.Contains("_" + i.ToString()) || lname.Contains(i.ToString() + "star") || lname.Contains(i.ToString() + "-star") || baseName.EndsWith("" + i.ToString()))
                {
                    w.Rarity = i;
                    break;
                }
            }

            w.WeaponAbilities = new Abilities();
            // Assign weapon-focused stats per detected type
            if (isSword)
            {
                // Sword: Attack & Defense
                w.WeaponAbilities.Set(w.Rarity * 3, w.Rarity * 2, w.Rarity >= 3 ? 1 : 0, 0);
            }
            else if (isBow)
            {
                // Bow: Attack & Speed
                w.WeaponAbilities.Set(w.Rarity * 2, w.Rarity, w.Rarity * 3, 0);
            }
            else if (isStaff)
            {
                // Staff: Higher Attack
                w.WeaponAbilities.Set(w.Rarity * 4, w.Rarity, w.Rarity >= 3 ? 1 : 0, 0);
            }
            else
            {
                w.WeaponAbilities.Set(w.Rarity * 2, w.Rarity, w.Rarity >= 3 ? 1 : 0, 0);
            }
            w.LevelRequirement = Math.Max(1, w.Rarity * 2 - 1);
            w.Sprite = tex;
            w.SpritePath = filePath;
            w.Source = "Generated";
            w.Price = w.Rarity * 10 + w.LevelRequirement * 5;
            // use filename (without extension) as default display name
            try { w.Name = System.IO.Path.GetFileNameWithoutExtension(filePath).Replace('-', ' ').Replace('_', ' '); } catch { w.Name = "Weapon"; }
            return w;
        }

        var eq = new Equipment();
        // prefer file base name for type detection when available
        if (baseName.Contains("helmet") || lname.Contains("helmet"))
            eq.EquipmentType = EquipmentType.Head;
        else if (lname.Contains("chestplate") || lname.Contains("chest") || baseName.Contains("chest"))
            eq.EquipmentType = EquipmentType.Body;
        else if (lname.Contains("gloves") || baseName.Contains("gloves") || baseName.Contains("gauntlet"))
            eq.EquipmentType = EquipmentType.Arms;
        else if (lname.Contains("shoe") || lname.Contains("shoes") || baseName.Contains("boot") || baseName.Contains("boots"))
            eq.EquipmentType = EquipmentType.Shoes;
        else
            eq.EquipmentType = EquipmentType.Other;

        eq.Sprite = tex;
        // Default example stats: treat as 1-star unless name contains explicit star count
        eq.Rarity = 1;
        for (int i = 5; i >= 1; i--)
        {
            if (lname.Contains("_" + i.ToString()) || lname.Contains(i.ToString() + "star") || lname.Contains(i.ToString() + "-star") || baseName.EndsWith("" + i.ToString()))
            {
                eq.Rarity = i;
                break;
            }
        }

        eq.EquipmentAbilities = new Abilities();
        // Assign focused stat bonuses based on detected equipment type from filename/folder
        switch (eq.EquipmentType)
        {
            case EquipmentType.Head:
                // Head: Vitality focused
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity, 0, eq.Rarity * 3);
                break;
            case EquipmentType.Body:
                // Body: Defense focused
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity * 3, 0, eq.Rarity * 2);
                break;
            case EquipmentType.Arms:
                // Arms/Gloves: Attack & Speed focused
                eq.EquipmentAbilities.Set(eq.Rarity * 2, eq.Rarity, eq.Rarity, 0);
                break;
            case EquipmentType.Shoes:
                // Shoes: Speed focused
                eq.EquipmentAbilities.Set(0, eq.Rarity, eq.Rarity * 3, eq.Rarity);
                break;
            default:
                // Fallback: balanced small bonuses
                eq.EquipmentAbilities.Set(eq.Rarity, eq.Rarity, 0, 0);
                break;
        }

        eq.LevelRequirement = Math.Max(1, eq.Rarity * 2 - 1);
        eq.SpritePath = filePath;
        eq.Source = "Generated";
        eq.Price = eq.Rarity * 10 + eq.LevelRequirement * 5;
        // default name from filename
        try { eq.Name = System.IO.Path.GetFileNameWithoutExtension(filePath).Replace('-', ' ').Replace('_', ' '); } catch { eq.Name = "Equipment"; }

        return eq;
    }

    // Scan the EquipmentsPath and create Item objects (Equipment or Weapon) for image files.
    public System.Collections.Generic.List<Item> LoadAllFromFolder()
    {
        var list = new System.Collections.Generic.List<Item>();

        // If explicit per-category arrays are provided, create items from them using declared category
        bool hasExplicit = false;
        if (BowAssetPaths != null && BowAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in BowAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "bow", rarity);
                else
                    it = CreateFromParts(left, "bow", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (SwordAssetPaths != null && SwordAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in SwordAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "sword", rarity);
                else
                    it = CreateFromParts(left, "sword", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (StaffAssetPaths != null && StaffAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in StaffAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "staff", rarity);
                else
                    it = CreateFromParts(left, "staff", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (ChestplateAssetPaths != null && ChestplateAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in ChestplateAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "chestplate", rarity);
                else
                    it = CreateFromParts(left, "chestplate", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (GlovesAssetPaths != null && GlovesAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in GlovesAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "gloves", rarity);
                else
                    it = CreateFromParts(left, "gloves", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (HelmetAssetPaths != null && HelmetAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in HelmetAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "helmet", rarity);
                else
                    it = CreateFromParts(left, "helmet", rarity);
                if (it != null) list.Add(it);
            }
        }
        if (ShoesAssetPaths != null && ShoesAssetPaths.Length > 0)
        {
            hasExplicit = true;
            foreach (var p in ShoesAssetPaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var parts = p.Split(':');
                var left = parts[0].Trim();
                int rarity = 1;
                if (parts.Length > 1) int.TryParse(parts[1].Trim(), out rarity);
                Item it = null;
                if (left.Contains("/") || left.StartsWith("Assets/") || left.StartsWith("res://"))
                    it = CreateFromAssetWithCategory(left, "shoes", rarity);
                else
                    it = CreateFromParts(left, "shoes", rarity);
                if (it != null) list.Add(it);
            }
        }

        if (hasExplicit)
            return list;

        // Fallback: scan folder recursively and build paths relative to Assets/Equipments
        var files = new List<string>();
        CollectFilesRecursive("", files);
        foreach (var f in files)
        {
            var full = "Assets/Equipments/" + f.Replace('\\', '/');
                var it = CreateFromSpriteFile(full);
                if (it != null)
                    list.Add(it);
        }

        return list;
    }

    // Recursively collect file paths relative to EquipmentsPath
    private void CollectFilesRecursive(string relativePath, List<string> outFiles)
    {
        var dir = new Godot.Directory();
        var fullPath = string.IsNullOrEmpty(relativePath) ? EquipmentsPath : EquipmentsPath.TrimEnd('/') + "/" + relativePath;
        if (dir.Open(fullPath) != Error.Ok)
            return;

        dir.ListDirBegin(true, true);
        string name = dir.GetNext();
        while (name != "")
        {
            if (dir.CurrentIsDir())
            {
                if (name != "." && name != "..")
                {
                    var childRel = string.IsNullOrEmpty(relativePath) ? name : relativePath + "/" + name;
                    CollectFilesRecursive(childRel, outFiles);
                }
            }
            else
            {
                var lf = name.ToLower();
                if (lf.EndsWith(".png") || lf.EndsWith(".jpg") || lf.EndsWith(".jpeg"))
                {
                    var fileRel = string.IsNullOrEmpty(relativePath) ? name : relativePath + "/" + name;
                    outFiles.Add(fileRel);
                }
            }
            name = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    // Save an Equipment Resource to disk at the specified res:// path.
    // Returns the saved path on success, or null on failure.
    public string SaveEquipmentToFile(Equipment eq, string savePath)
    {
        if (eq == null || string.IsNullOrEmpty(savePath))
            return null;

        var dir = new Godot.Directory();
        var folder = savePath.Substring(0, savePath.LastIndexOf('/'));
        if (!dir.DirExists(folder))
        {
            var err = dir.MakeDirRecursive(folder);
            if (err != Error.Ok)
                return null;
        }

        GD.PrintS("[EquipmentManager] SaveEquipmentToFile: core Equipment is not a Godot.Resource; skipping ResourceSaver. Implement serialization if needed.");
        return null;
    }

    // Return a set of items (Equipment or Weapon). By default selects base rarity==1 items
    // and sets their LevelRequirement based on playerLevel +/- levelOffset.
    public System.Collections.Generic.List<Item> GetEquipmentSet(int count, int playerLevel, int levelOffset = 0)
    {
        var all = LoadAllFromFolder();
        var candidates = new System.Collections.Generic.List<Item>();
        foreach (var it in all)
        {
            if (it is Equipment e && e.Rarity == 1)
                candidates.Add(it);
            else if (it is Weapon w && w.Rarity == 1)
                candidates.Add(it);
        }

        var result = new System.Collections.Generic.List<Item>();
        if (candidates.Count == 0 || count <= 0)
            return result;

        for (int i = 0; i < count; i++)
        {
            var idx = (int)Math.Floor(GD.Randf() * candidates.Count);
            if (idx < 0) idx = 0;
            if (idx >= candidates.Count) idx = candidates.Count - 1;

            var src = candidates[idx];
            if (src is Equipment se)
            {
                var eq = new Equipment();
                eq.EquipmentType = se.EquipmentType;
                eq.EquipmentAbilities = new Abilities();
                eq.EquipmentAbilities.Set(se.EquipmentAbilities?.Atk ?? 0, se.EquipmentAbilities?.Def ?? 0, se.EquipmentAbilities?.Spd ?? 0, se.EquipmentAbilities?.Vit ?? 0);
                eq.Rarity = se.Rarity;
                eq.Sprite = se.Sprite;
                    eq.Name = se.Name;
                    eq.SpritePath = se.SpritePath;
                eq.Source = se.Source;
                eq.Price = se.Price;

                int minLevel = Math.Max(1, playerLevel - levelOffset);
                int maxLevel = Math.Max(1, playerLevel + levelOffset);
                int chosen = minLevel;
                if (maxLevel > minLevel)
                    chosen = (int)Math.Floor(GD.Randf() * (maxLevel - minLevel + 1)) + minLevel;
                eq.LevelRequirement = chosen;
                // Scale abilities by factor = 1 + LevelRequirement * LevelScalingMultiplier
                float factor = 1f + eq.LevelRequirement * LevelScalingMultiplier;
                eq.EquipmentAbilities.Atk = (int)System.Math.Max(0, System.Math.Round(eq.EquipmentAbilities.Atk * factor));
                eq.EquipmentAbilities.Def = (int)System.Math.Max(0, System.Math.Round(eq.EquipmentAbilities.Def * factor));
                eq.EquipmentAbilities.Spd = (int)System.Math.Max(0, System.Math.Round(eq.EquipmentAbilities.Spd * factor));
                eq.EquipmentAbilities.Vit = (int)System.Math.Max(0, System.Math.Round(eq.EquipmentAbilities.Vit * factor));
                result.Add(eq);
            }
            else if (src is Weapon sw)
            {
                var w = new Weapon();
                w.WeaponType = sw.WeaponType;
                w.WeaponAbilities = new Abilities();
                w.WeaponAbilities.Set(sw.WeaponAbilities?.Atk ?? 0, sw.WeaponAbilities?.Def ?? 0, sw.WeaponAbilities?.Spd ?? 0, sw.WeaponAbilities?.Vit ?? 0);
                w.Rarity = sw.Rarity;
                w.Sprite = sw.Sprite;
                w.Name = sw.Name;
                w.SpritePath = sw.SpritePath;
                w.Source = sw.Source;
                w.Price = sw.Price;

                int minLevel = Math.Max(1, playerLevel - levelOffset);
                int maxLevel = Math.Max(1, playerLevel + levelOffset);
                int chosen = minLevel;
                if (maxLevel > minLevel)
                    chosen = (int)Math.Floor(GD.Randf() * (maxLevel - minLevel + 1)) + minLevel;
                w.LevelRequirement = chosen;
                // Scale abilities by factor = 1 + LevelRequirement * LevelScalingMultiplier
                float wfactor = 1f + w.LevelRequirement * LevelScalingMultiplier;
                w.WeaponAbilities.Atk = (int)System.Math.Max(0, System.Math.Round(w.WeaponAbilities.Atk * wfactor));
                w.WeaponAbilities.Def = (int)System.Math.Max(0, System.Math.Round(w.WeaponAbilities.Def * wfactor));
                w.WeaponAbilities.Spd = (int)System.Math.Max(0, System.Math.Round(w.WeaponAbilities.Spd * wfactor));
                w.WeaponAbilities.Vit = (int)System.Math.Max(0, System.Math.Round(w.WeaponAbilities.Vit * wfactor));
                result.Add(w);
            }
        }

        return result;
    }

    // Save an Equipment resource with a specified file name (without extension) under res://Items/Generated/
    // If overwrite is false and the file exists, the method returns null.
    public string SaveEquipmentWithName(Equipment eq, string fileNameWithoutExt, bool overwrite = false)
    {
        if (eq == null || string.IsNullOrEmpty(fileNameWithoutExt))
            return null;

        // sanitize filename
        // sanitize filename (keep letters, numbers, underscore, hyphen)
        var sanitized = System.Text.RegularExpressions.Regex.Replace(fileNameWithoutExt, "[^a-zA-Z0-9_-]", "_");
        var folder = "res://Items/Generated";
        var path = folder + "/" + sanitized + ".tres";

        var dir = new Godot.Directory();
        if (!dir.DirExists(folder))
        {
            var err = dir.MakeDirRecursive(folder);
            if (err != Error.Ok)
                return null;
        }

        if (dir.FileExists(path) && !overwrite)
        {
            return null;
        }

        GD.PrintS("[EquipmentManager] SaveEquipmentWithName: core Equipment is not a Godot.Resource; skipping ResourceSaver. Implement serialization if needed.");
        return null;
    }
}