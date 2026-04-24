using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;

/// <summary>
/// Minimal shop UI used by lobby NPCs.
/// It opens a window for buy/sell flow and currently shows empty placeholder lists.
/// </summary>
public class NpcShopUI : CanvasLayer
{
    public event Action Closed;

    private Control _root;
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _npcLabel;
    private Label _statusLabel;
    private VBoxContainer _buyListContainer;
    private VBoxContainer _sellListContainer;
    private Button _closeButton;

    private NPC _activeNpc;
    private Player _activePlayer;
    private bool _isVisible;

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
        BuildUi();
        HideShop();
    }

    public void ShowShop(NPC npc, Player player)
    {
        _activeNpc = npc;
        _activePlayer = player;

        if (_titleLabel != null)
        {
            _titleLabel.Text = npc != null ? $"{npc.EntityName} Shop" : "Shop";
        }

        if (_npcLabel != null)
        {
            _npcLabel.Text = npc != null
                ? $"Merchant: {npc.EntityName}"
                : "Merchant: Unknown";
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = npc != null && npc.GetShopItems().Count > 0
                ? "Items are available for purchase and selling."
                : "No items are configured yet. The trade UI is ready for future items.";
        }

        RebuildLists();

        _panel.Visible = true;
        _isVisible = true;
        GetTree().Paused = true;
    }

    public void HideShop()
    {
        if (_panel != null)
        {
            _panel.Visible = false;
        }

        _activeNpc = null;
        _activePlayer = null;
        _isVisible = false;
        GetTree().Paused = false;
    }

    public override void _Process(float delta)
    {
        if (_isVisible && Input.IsActionJustPressed("ui_cancel"))
        {
            HideShop();
            Closed?.Invoke();
        }
    }

    private void BuildUi()
    {
        _root = new Control
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_root);

        _panel = new PanelContainer
        {
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            RectMinSize = new Vector2(560f, 360f),
            RectPosition = new Vector2(-280f, -180f)
        };
        _root.AddChild(_panel);

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.13f, 0.97f),
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            BorderColor = new Color(0.35f, 0.42f, 0.56f, 0.85f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        _panel.AddStyleboxOverride("panel", panelStyle);

        var mainVBox = new VBoxContainer();
        mainVBox.AddConstantOverride("separation", 10);
        _panel.AddChild(mainVBox);

        _titleLabel = new Label
        {
            Text = "Shop",
            Align = Label.AlignEnum.Center
        };
        mainVBox.AddChild(_titleLabel);

        _npcLabel = new Label
        {
            Text = "Merchant: -",
            Align = Label.AlignEnum.Center
        };
        mainVBox.AddChild(_npcLabel);

        _statusLabel = new Label
        {
            Text = "",
            Autowrap = true
        };
        mainVBox.AddChild(_statusLabel);

        var columns = new HBoxContainer();
        columns.AddConstantOverride("separation", 16);
        mainVBox.AddChild(columns);

        columns.AddChild(BuildListColumn("Buy", out _buyListContainer));
        columns.AddChild(BuildListColumn("Sell", out _sellListContainer));

        _closeButton = new Button
        {
            Text = "Close",
            RectMinSize = new Vector2(120f, 36f)
        };
        _closeButton.Connect("pressed", this, nameof(OnClosePressed));
        mainVBox.AddChild(_closeButton);
    }

    private Control BuildListColumn(string title, out VBoxContainer listContainer)
    {
        var column = new VBoxContainer();
        column.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        column.AddConstantOverride("separation", 8);

        var titleLabel = new Label
        {
            Text = title,
            Align = Label.AlignEnum.Center
        };
        column.AddChild(titleLabel);

        listContainer = new VBoxContainer();
        listContainer.AddConstantOverride("separation", 6);
        column.AddChild(listContainer);

        return column;
    }

    private void RebuildLists()
    {
        RebuildList(_buyListContainer, GetBuyItemsPlaceholder());
        RebuildList(_sellListContainer, GetSellItemsPlaceholder());
    }

    private IReadOnlyList<string> GetBuyItemsPlaceholder()
    {
        return _activeNpc == null || _activeNpc.GetShopItems().Count == 0
            ? new List<string> { "No items yet" }
            : new List<string> { "Item list will be added here." };
    }

    private IReadOnlyList<string> GetSellItemsPlaceholder()
    {
        return _activePlayer == null
            ? new List<string> { "No player connected" }
            : new List<string> { "Player inventory will appear here later." };
    }

    private void RebuildList(VBoxContainer container, IReadOnlyList<string> entries)
    {
        if (container == null)
        {
            return;
        }

        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }

        for (int i = 0; i < entries.Count; i++)
        {
            container.AddChild(new Label
            {
                Text = "- " + entries[i],
                Autowrap = true
            });
        }
    }

    private void OnClosePressed()
    {
        HideShop();
        Closed?.Invoke();
    }
}