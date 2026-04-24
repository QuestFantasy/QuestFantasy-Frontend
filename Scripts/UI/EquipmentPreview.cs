using System;
using System.Text;

using Godot;

public class EquipmentPreview : CanvasLayer
{
    public static EquipmentPreview Instance;

    private Panel _panel;
    private Panel _shadowPanel;
    private VBoxContainer _box;
    private readonly ColorRect _bg;
    [Export]
    public int FixedPreviewHeight = 150;
    [Export]
    public int InnerTopPadding = 14;
    [Export]
    public int HeaderTopPadding = 8;

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

        // Shadow panel (slightly offset to simulate drop shadow)
        _shadowPanel = new Panel();
        var shadowStyle = new StyleBoxFlat();
        shadowStyle.BgColor = new Color(0, 0, 0, 0.25f);
        shadowStyle.CornerRadiusTopLeft = 8;
        shadowStyle.CornerRadiusTopRight = 8;
        shadowStyle.CornerRadiusBottomLeft = 8;
        shadowStyle.CornerRadiusBottomRight = 8;
        _shadowPanel.AddStyleboxOverride("panel", shadowStyle);
        _shadowPanel.Visible = false;
        AddChild(_shadowPanel);

        _panel = new Panel();
        _panel.Visible = false;
        AddChild(_panel);

        _box = new VBoxContainer();
        _box.RectMinSize = new Vector2(200, 10);

        // Panel background will be drawn by the Panel's StyleBoxFlat (rounded corners)

        // Inner panel to provide padding and border for attributes
        var innerPanel = new Panel();
        var innerStyle = new StyleBoxFlat();
        innerStyle.BorderColor = new Color(0.2f, 0.2f, 0.2f);
        innerStyle.BorderWidthLeft = 1;
        innerStyle.BorderWidthRight = 1;
        innerStyle.BorderWidthTop = 1;
        innerStyle.BorderWidthBottom = 1;
        innerStyle.ContentMarginLeft = 8;
        innerStyle.ContentMarginTop = 14;
        innerStyle.ContentMarginRight = 8;
        innerStyle.ContentMarginBottom = 6;
        // Rounded corners for a softer look
        innerStyle.CornerRadiusTopLeft = 8;
        innerStyle.CornerRadiusTopRight = 8;
        innerStyle.CornerRadiusBottomLeft = 8;
        innerStyle.CornerRadiusBottomRight = 8;
        innerPanel.AddStyleboxOverride("panel", innerStyle);
        innerPanel.AddChild(_box);

        _panel.AddChild(innerPanel);

        // Ensure panel has an initial size
        _panel.RectSize = new Vector2(200, FixedPreviewHeight);

        var style = new StyleBoxFlat();
        // Stronger background for contrast so rounded corners are visible
        style.BgColor = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
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
        // Add explicit top spacer so there's visible space above the first line
        var topSpacer = new Control();
        topSpacer.RectMinSize = new Vector2(0, InnerTopPadding);
        _box.AddChild(topSpacer);
        // Header: icon + title + rarity
        var header = new HBoxContainer();
        var headerStyle = new StyleBoxFlat();
        headerStyle.BgColor = new Color(0, 0, 0, 0.0f);
        headerStyle.ContentMarginLeft = 4;
        headerStyle.ContentMarginTop = 8;
        headerStyle.ContentMarginRight = 4;
        headerStyle.ContentMarginBottom = 4;
        headerStyle.CornerRadiusTopLeft = 8;
        headerStyle.CornerRadiusTopRight = 8;
        headerStyle.CornerRadiusBottomLeft = 8;
        headerStyle.CornerRadiusBottomRight = 8;
        header.AddStyleboxOverride("panel", headerStyle);

        Texture iconTex = null;
        string titleText = "Item";
        string metaText = "";
        Color rarityColor = new Color(0.8f, 0.8f, 0.8f);

        if (item is QuestFantasy.Core.Data.Items.Equipment eq)
        {
            titleText = eq.Name ?? eq.EquipmentType.ToString();
            metaText = $"{eq.EquipmentType} • Rarity {eq.Rarity}";
            rarityColor = GetRarityColor(eq.Rarity);
            if (!string.IsNullOrEmpty(eq.SpritePath))
            {
                iconTex = (Texture)GD.Load(eq.SpritePath);
            }

            AddAttributeGrid(eq.EquipmentAbilities?.Atk ?? 0, eq.EquipmentAbilities?.Def ?? 0, eq.EquipmentAbilities?.Spd ?? 0, eq.EquipmentAbilities?.Vit ?? 0, eq.LevelRequirement, eq.Price);
        }
        else if (item is QuestFantasy.Core.Data.Items.Weapon w)
        {
            titleText = w.Name ?? w.WeaponType.ToString();
            metaText = $"{w.WeaponType} • Rarity {w.Rarity}";
            rarityColor = GetRarityColor(w.Rarity);
            if (!string.IsNullOrEmpty(w.SpritePath))
            {
                iconTex = (Texture)GD.Load(w.SpritePath);
            }

            AddAttributeGrid(w.WeaponAbilities?.Atk ?? 0, w.WeaponAbilities?.Def ?? 0, w.WeaponAbilities?.Spd ?? 0, w.WeaponAbilities?.Vit ?? 0, w.LevelRequirement, w.Price);
        }

        // Icon
        var iconRect = new TextureRect();
        iconRect.RectMinSize = new Vector2(48, 48);
        iconRect.Expand = true;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        if (iconTex != null) iconRect.Texture = iconTex; else iconRect.Texture = null;
        header.AddChild(iconRect);

        // Title + meta
        var titleBox = new VBoxContainer();
        var title = new Label();
        title.Text = titleText;
        title.AddColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        title.AddColorOverride("font_color_shadow", new Color(0, 0, 0, 0.5f));
        title.RectMinSize = new Vector2(200, 0);
        titleBox.AddChild(title);

        var meta = new Label();
        meta.Text = metaText;
        meta.AddColorOverride("font_color", rarityColor);
        titleBox.AddChild(meta);

        header.AddChild(titleBox);
        _box.AddChild(header);


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

        // clamp within viewport
        var pos = screenPos + new Vector2(16, 16);
        var maxX = vp.Size.x - _panel.RectSize.x - 8;
        var maxY = vp.Size.y - _panel.RectSize.y - 8;
        pos.x = Math.Min(Math.Max(8, pos.x), Math.Max(8, maxX));
        pos.y = Math.Min(Math.Max(8, pos.y), Math.Max(8, maxY));
        // position shadow slightly offset and sized the same
        if (_shadowPanel != null)
        {
            _shadowPanel.RectSize = _panel.RectSize;
            _shadowPanel.RectPosition = pos + new Vector2(4, 4);
            _shadowPanel.Visible = true;
        }
        _panel.RectPosition = pos;
        _panel.Visible = true;
    }

    private void AddAttributeGrid(int atk, int def, int spd, int vit, int levelReq, int price)
    {
        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddChild(new Label() { Text = $"Attack: +{atk}" });
        grid.AddChild(new Label() { Text = $"Defense: +{def}" });
        grid.AddChild(new Label() { Text = $"Agility: +{spd}" });
        grid.AddChild(new Label() { Text = $"Stamina: +{vit}" });
        grid.AddChild(new Label() { Text = $"Level Req:" });
        grid.AddChild(new Label() { Text = $"{levelReq}" });
        var priceLabel = new Label() { Text = $"Price: {price}" };
        priceLabel.AddColorOverride("font_color", new Color(0.95f, 0.9f, 0.6f));
        grid.AddChild(priceLabel);
        _box.AddChild(grid);
    }

    private Color GetRarityColor(int rarity)
    {
        switch (rarity)
        {
            case 0: return new Color(0.8f, 0.8f, 0.8f); // common
            case 1: return new Color(0.3f, 0.8f, 0.3f); // uncommon
            case 2: return new Color(0.2f, 0.6f, 1.0f); // rare
            case 3: return new Color(0.7f, 0.3f, 1.0f); // epic
            case 4: return new Color(1.0f, 0.75f, 0.2f); // legendary
            default: return new Color(0.9f, 0.9f, 0.9f);
        }
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