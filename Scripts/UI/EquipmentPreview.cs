using System;
using System.Text;

using Godot;

public class EquipmentPreview : CanvasLayer
{
    public static EquipmentPreview Instance;

    private Panel _panel;
    private VBoxContainer _box;
    private ColorRect _bg;
    [Export]
    public int FixedPreviewHeight = 150;

    public override void _Ready()
    {
        Instance = this;
        EnsureInit();
    }

    private Camera2D FindActiveCamera2D()
    {
        var root = GetTree().Root;
        return FindCamera2DRecursive(root);
    }

    private Camera2D FindCamera2DRecursive(Node node)
    {
        if (node is Camera2D cam && cam.Current)
            return cam;

        foreach (Node child in node.GetChildren())
        {
            var found = FindCamera2DRecursive(child);
            if (found != null)
                return found;
        }

        return null;
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    private void EnsureInit()
    {
        if (_panel != null) return;

        _panel = new Panel();
        _panel.Visible = false;
        AddChild(_panel);

        _box = new VBoxContainer();
        _box.RectMinSize = new Vector2(200, 10);

        // Add a ColorRect behind content to ensure a visible semi-transparent background
        _bg = new ColorRect();
        _bg.Color = new Color(0.06f, 0.06f, 0.06f, 0.6f);
        _bg.RectMinSize = new Vector2(200, FixedPreviewHeight);
        _bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        _panel.AddChild(_bg);

        // Inner panel to provide padding and border for attributes
        var innerPanel = new Panel();
        var innerStyle = new StyleBoxFlat();
        innerStyle.BorderColor = new Color(0.2f, 0.2f, 0.2f);
        innerStyle.BorderWidthLeft = 1;
        innerStyle.BorderWidthRight = 1;
        innerStyle.BorderWidthTop = 1;
        innerStyle.BorderWidthBottom = 1;
        innerStyle.ContentMarginLeft = 8;
        innerStyle.ContentMarginTop = 6;
        innerStyle.ContentMarginRight = 8;
        innerStyle.ContentMarginBottom = 6;
        innerPanel.AddStyleboxOverride("panel", innerStyle);
        innerPanel.AddChild(_box);

        _panel.AddChild(innerPanel);

        // Ensure panel has an initial size
        _panel.RectSize = _bg.RectMinSize;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0, 0, 0, 0.75f);
        _panel.AddStyleboxOverride("panel", style);
    }

    public void ShowPreview(object item, Vector2 globalPos)
    {
        EnsureInit();

        // Remove any existing children from the box before populating
        foreach (Node child in _box.GetChildren())
        {
            child.QueueFree();
        }
        if (item == null)
            return;

        // Header: item type + rarity badge
        var header = new HBoxContainer();

        if (item is QuestFantasy.Core.Data.Items.Equipment eq)
        {
            var title = new Label(); title.Text = eq.Name ?? eq.EquipmentType.ToString(); title.AddColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f)); _box.AddChild(title);
            var meta = new Label(); meta.Text = $"{eq.EquipmentType} • Rarity {eq.Rarity}"; meta.AddColorOverride("font_color", new Color(0.8f, 0.8f, 0.7f)); _box.AddChild(meta);

            var atk = new Label(); atk.Text = $"Attack: +{eq.EquipmentAbilities?.Atk ?? 0}"; _box.AddChild(atk);
            var def = new Label(); def.Text = $"Defense: +{eq.EquipmentAbilities?.Def ?? 0}"; _box.AddChild(def);
            var agi = new Label(); agi.Text = $"Agility: +{eq.EquipmentAbilities?.Spd ?? 0}"; _box.AddChild(agi);
            var sta = new Label(); sta.Text = $"Stamina: +{eq.EquipmentAbilities?.Vit ?? 0}"; _box.AddChild(sta);
            var lvl = new Label(); lvl.Text = $"Level Req: {eq.LevelRequirement}"; _box.AddChild(lvl);
            var price = new Label(); price.Text = $"Price: {eq.Price}"; price.AddColorOverride("font_color", new Color(0.9f, 0.9f, 0.6f)); _box.AddChild(price);
        }
        else if (item is QuestFantasy.Core.Data.Items.Weapon w)
        {
            var title = new Label(); title.Text = w.Name ?? w.WeaponType.ToString(); title.AddColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f)); _box.AddChild(title);
            var meta = new Label(); meta.Text = $"{w.WeaponType} • Rarity {w.Rarity}"; meta.AddColorOverride("font_color", new Color(0.8f, 0.8f, 0.7f)); _box.AddChild(meta);

            var atk = new Label(); atk.Text = $"Attack: +{w.WeaponAbilities?.Atk ?? 0}"; _box.AddChild(atk);
            var def = new Label(); def.Text = $"Defense: +{w.WeaponAbilities?.Def ?? 0}"; _box.AddChild(def);
            var agi = new Label(); agi.Text = $"Agility: +{w.WeaponAbilities?.Spd ?? 0}"; _box.AddChild(agi);
            var sta = new Label(); sta.Text = $"Stamina: +{w.WeaponAbilities?.Vit ?? 0}"; _box.AddChild(sta);
            var lvl = new Label(); lvl.Text = $"Level Req: {w.LevelRequirement}"; _box.AddChild(lvl);
            var price = new Label(); price.Text = $"Price: {w.Price}"; price.AddColorOverride("font_color", new Color(0.9f, 0.9f, 0.6f)); _box.AddChild(price);
        }


        // Resize background and panel to cover the content.
        // Compute combined minimum size of the VBox and add the inner panel margins.
        Vector2 contentSize = _box.GetCombinedMinimumSize();
        float innerMarginH = 8 + 8;
        float innerMarginV = 6 + 6;
        // extra padding for breathing room
        float extraH = 8;
        float extraV = 8;
        float width = Math.Max(200f, contentSize.x + innerMarginH + extraH);
        float height = FixedPreviewHeight;
        if (_bg != null)
        {
            _bg.RectMinSize = new Vector2(width, height);
            _bg.RectSize = new Vector2(width, height);
        }
        _panel.RectSize = new Vector2(width, height);

        // Convert world position to screen coordinates using active Camera2D if available.
        Vector2 screenPos = globalPos;
        var vp = GetViewport();
        if (vp != null)
        {
            var cam = FindActiveCamera2D();
            if (cam != null)
            {
                screenPos = globalPos - cam.GlobalPosition + vp.Size * 0.5f;
            }
        }

        _panel.RectPosition = screenPos + new Vector2(16, 16);
        _panel.Visible = true;
    }

    public void Show(object item, Vector2 globalPos)
    {
        ShowPreview(item, globalPos);
    }

    public void HidePreview()
    {
        if (_panel == null) return;
        _panel.Visible = false;
    }

    public new void Hide()
    {
        HidePreview();
    }
}