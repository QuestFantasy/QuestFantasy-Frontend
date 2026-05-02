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

    private sealed class ShopItemSlot : PanelContainer
    {
        public Item ItemData { get; private set; }
        public event Action<ShopItemSlot> HoverEntered;
        public event Action<ShopItemSlot> HoverExited;
        public event Action<ShopItemSlot> Selected;
        public event Action<ShopItemSlot> ActivateRequested;

        private StyleBoxFlat _style;
        private bool _isHovered;
        private bool _isSelected;

        public void Initialize(Item item, Texture itemTexture, string priceText, Color rarityColor)
        {
            ItemData = item;

            _style = new StyleBoxFlat
            {
                BgColor = new Color(0.11f, 0.13f, 0.19f, 0.95f),
                CornerRadiusTopLeft = 10,
                CornerRadiusTopRight = 10,
                CornerRadiusBottomLeft = 10,
                CornerRadiusBottomRight = 10,
                BorderColor = rarityColor,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                BorderWidthLeft = 1,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 8,
                ContentMarginBottom = 8
            };
            AddStyleboxOverride("panel", _style);

            var row = new VBoxContainer();
            row.AddConstantOverride("separation", 4);
            AddChild(row);

            var icon = new TextureRect
            {
                RectMinSize = new Vector2(72f, 72f),
                Expand = true,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = itemTexture
            };
            row.AddChild(icon);

            var priceBadge = new Label
            {
                Text = priceText,
                Align = Label.AlignEnum.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                RectMinSize = new Vector2(0f, 18f)
            };
            priceBadge.AddColorOverride("font_color", new Color(0.98f, 0.92f, 0.68f));
            row.AddChild(priceBadge);

            Connect("mouse_entered", this, nameof(OnMouseEntered));
            Connect("mouse_exited", this, nameof(OnMouseExited));
            Connect("gui_input", this, nameof(OnGuiInput));

            UpdateVisualState();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }

        private void OnMouseEntered()
        {
            _isHovered = true;
            UpdateVisualState();
            HoverEntered?.Invoke(this);
        }

        private void OnMouseExited()
        {
            _isHovered = false;
            UpdateVisualState();
            HoverExited?.Invoke(this);
        }

        private void OnGuiInput(InputEvent eventData)
        {
            if (!(eventData is InputEventMouseButton mouseButton))
            {
                return;
            }

            if (!mouseButton.Pressed || mouseButton.ButtonIndex != (int)ButtonList.Left)
            {
                return;
            }

            Selected?.Invoke(this);

            if (mouseButton.Doubleclick)
            {
                ActivateRequested?.Invoke(this);
            }
        }

        private void UpdateVisualState()
        {
            if (_style == null)
            {
                return;
            }

            if (_isSelected)
            {
                _style.BorderColor = new Color(0.96f, 0.84f, 0.34f);
                _style.BorderWidthTop = 2;
                _style.BorderWidthRight = 2;
                _style.BorderWidthBottom = 2;
                _style.BorderWidthLeft = 2;
                _style.BgColor = new Color(0.18f, 0.17f, 0.11f, 0.98f);
                return;
            }

            if (_isHovered)
            {
                _style.BorderColor = new Color(0.88f, 0.92f, 1.0f);
                _style.BorderWidthTop = 2;
                _style.BorderWidthRight = 2;
                _style.BorderWidthBottom = 2;
                _style.BorderWidthLeft = 2;
                _style.BgColor = new Color(0.16f, 0.18f, 0.24f, 0.98f);
                return;
            }

            _style.BorderWidthTop = 1;
            _style.BorderWidthRight = 1;
            _style.BorderWidthBottom = 1;
            _style.BorderWidthLeft = 1;
            _style.BgColor = new Color(0.11f, 0.13f, 0.19f, 0.95f);
        }
    }

    private Control _root;
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _npcLabel;
    private Label _statusLabel;
    private ScrollContainer _buyScrollContainer;
    private ScrollContainer _sellScrollContainer;
    private GridContainer _buyGridContainer;
    private PanelContainer _previewPanel;
    private ScrollContainer _previewScrollContainer;
    private VBoxContainer _previewContentContainer;
    private TextureRect _previewIcon;
    private Label _previewTitleLabel;
    private Label _previewMetaLabel;
    private Label _previewPriceLabel;
    private Label _previewDescriptionLabel;
    private VBoxContainer _sellListContainer;
    private ShopItemSlot _selectedBuySlot;
    private ShopItemSlot _selectedSellSlot;
    private Item _previewedItem;
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
        ClearBuySelection();
        _previewedItem = null;

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
            _statusLabel.Text = GetShopStatusText();
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

        ClearBuySelection();
        ClearSellSelection();
        ResetPreviewPanel();
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
            RectMinSize = new Vector2(860f, 440f),
            RectPosition = new Vector2(-430f, -220f)
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

        columns.AddChild(BuildBuyColumn(out _buyScrollContainer, out _buyGridContainer));
        columns.AddChild(BuildPreviewColumn());
        columns.AddChild(BuildListColumn("Sell", out _sellScrollContainer, out _sellListContainer));

        _closeButton = new Button
        {
            Text = "Close",
            RectMinSize = new Vector2(120f, 36f)
        };
        _closeButton.Connect("pressed", this, nameof(OnClosePressed));
        mainVBox.AddChild(_closeButton);
    }

    private Control BuildBuyColumn(out ScrollContainer scrollContainer, out GridContainer gridContainer)
    {
        var column = new VBoxContainer();
        column.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        column.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        column.AddConstantOverride("separation", 8);

        var titleLabel = new Label
        {
            Text = "Buy",
            Align = Label.AlignEnum.Center
        };
        column.AddChild(titleLabel);

        scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            RectMinSize = new Vector2(0f, 220f)
        };

        gridContainer = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill
        };
        gridContainer.AddConstantOverride("hseparation", 8);
        gridContainer.AddConstantOverride("vseparation", 8);

        scrollContainer.AddChild(gridContainer);
        column.AddChild(scrollContainer);

        return column;
    }

    private Control BuildPreviewColumn()
    {
        var column = new VBoxContainer();
        column.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        column.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        column.RectMinSize = new Vector2(260f, 0f);
        column.AddConstantOverride("separation", 8);

        var titleLabel = new Label
        {
            Text = "Preview",
            Align = Label.AlignEnum.Center
        };
        column.AddChild(titleLabel);

        _previewPanel = new PanelContainer
        {
            RectMinSize = new Vector2(0f, 320f),
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill
        };

        var previewStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.11f, 0.16f, 0.98f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            BorderColor = new Color(0.32f, 0.38f, 0.5f, 0.95f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        _previewPanel.AddStyleboxOverride("panel", previewStyle);

        _previewScrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        _previewPanel.AddChild(_previewScrollContainer);

        _previewContentContainer = new VBoxContainer();
        _previewContentContainer.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        _previewContentContainer.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        _previewContentContainer.AddConstantOverride("separation", 8);
        _previewScrollContainer.AddChild(_previewContentContainer);

        _previewIcon = new TextureRect
        {
            RectMinSize = new Vector2(160f, 160f),
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        _previewContentContainer.AddChild(_previewIcon);

        _previewTitleLabel = new Label
        {
            Text = "Hover an item",
            Align = Label.AlignEnum.Center
        };
        _previewContentContainer.AddChild(_previewTitleLabel);

        _previewMetaLabel = new Label
        {
            Text = "",
            Align = Label.AlignEnum.Center,
            Autowrap = true
        };
        _previewContentContainer.AddChild(_previewMetaLabel);

        _previewPriceLabel = new Label
        {
            Text = "",
            Align = Label.AlignEnum.Center
        };
        _previewContentContainer.AddChild(_previewPriceLabel);

        _previewDescriptionLabel = new Label
        {
            Text = "Move the cursor over a slot to inspect it.",
            Autowrap = true
        };
        _previewContentContainer.AddChild(_previewDescriptionLabel);

        column.AddChild(_previewPanel);
        ResetPreviewPanel();
        return column;
    }

    private Control BuildListColumn(string title, out ScrollContainer scrollContainer, out VBoxContainer listContainer)
    {
        var column = new VBoxContainer();
        column.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        column.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        column.AddConstantOverride("separation", 8);

        var titleLabel = new Label
        {
            Text = title,
            Align = Label.AlignEnum.Center
        };
        column.AddChild(titleLabel);

        scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            RectMinSize = new Vector2(0f, 220f)
        };

        listContainer = new VBoxContainer();
        listContainer.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        listContainer.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        listContainer.AddConstantOverride("separation", 6);
        scrollContainer.AddChild(listContainer);
        column.AddChild(scrollContainer);

        return column;
    }

    private void RebuildLists()
    {
        RebuildBuyList();
        RebuildSellList();
    }

    private string GetShopStatusText()
    {
        if (_activeNpc == null)
        {
            return "No merchant selected.";
        }

        int stockCount = _activeNpc.GetShopItems().Count;
        string goldText = _activePlayer != null ? $"You have {_activePlayer.Gold} gold." : "No player connected.";

        if (stockCount == 0)
        {
            return $"{_activeNpc.EntityName} has no items in stock yet. {goldText}";
        }

        return $"Buy basic equipment from {_activeNpc.EntityName}. {goldText}";
    }

    private void RebuildBuyList()
    {
        if (_buyGridContainer == null)
        {
            return;
        }

        ClearBuySelection();
        ClearContainer(_buyGridContainer);

        if (_activeNpc == null)
        {
            _buyGridContainer.AddChild(CreateInfoLabel("No merchant selected"));
            return;
        }

        var items = _activeNpc.GetShopItems();
        if (items.Count == 0)
        {
            _buyGridContainer.AddChild(CreateInfoLabel("No items in stock"));
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
            {
                continue;
            }

            _buyGridContainer.AddChild(CreateBuySlot(item));
        }
    }

    private void RebuildSellList()
    {
        if (_sellListContainer == null)
        {
            return;
        }

        ClearSellSelection();
        ClearContainer(_sellListContainer);

        if (_activePlayer == null)
        {
            _sellListContainer.AddChild(CreateInfoLabel("No player connected"));
            return;
        }

        var items = _activePlayer.InventoryItems;
        if (items == null || items.Count == 0)
        {
            _sellListContainer.AddChild(CreateInfoLabel("No items to sell"));
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
            {
                continue;
            }

            _sellListContainer.AddChild(CreateSellSlot(item));
        }
    }

    private Control CreateBuySlot(Item item)
    {
        var slot = new ShopItemSlot();
        slot.RectMinSize = new Vector2(104f, 104f);
        slot.MouseFilter = Control.MouseFilterEnum.Stop;

        var iconTexture = ResolveItemTexture(item);
        var rarityColor = GetRarityColor(item);
        slot.Initialize(item, iconTexture, $"{item.Price}g", rarityColor);
        slot.HoverEntered += OnBuySlotMouseEntered;
        slot.HoverExited += OnBuySlotMouseExited;
        slot.Selected += OnBuySlotSelected;
        slot.ActivateRequested += OnBuySlotActivated;

        return slot;
    }

    private Control CreateSellSlot(Item item)
    {
        var slot = new ShopItemSlot();
        slot.RectMinSize = new Vector2(104f, 104f);
        slot.MouseFilter = Control.MouseFilterEnum.Stop;

        var iconTexture = ResolveItemTexture(item);
        var rarityColor = GetRarityColor(item);
        slot.Initialize(item, iconTexture, $"Sell {GetSellPrice(item)}g", rarityColor);
        slot.HoverEntered += OnSellSlotMouseEntered;
        slot.HoverExited += OnSellSlotMouseExited;
        slot.Selected += OnSellSlotSelected;
        slot.ActivateRequested += OnSellSlotActivated;

        return slot;
    }

    private Color GetRarityColor(Item item)
    {
        int rarity = GetItemRarity(item);
        switch (rarity)
        {
            case 0:
                return new Color(0.72f, 0.72f, 0.72f);
            case 1:
                return new Color(0.34f, 0.80f, 0.34f);
            case 2:
                return new Color(0.23f, 0.60f, 1.0f);
            case 3:
                return new Color(0.75f, 0.38f, 1.0f);
            case 4:
                return new Color(1.0f, 0.78f, 0.24f);
            default:
                return new Color(0.95f, 0.95f, 0.95f);
        }
    }

    private int GetItemRarity(Item item)
    {
        if (item is Equipment equipment)
        {
            return equipment.Rarity;
        }

        if (item is Weapon weapon)
        {
            return weapon.Rarity;
        }

        return 0;
    }

    private Texture ResolveItemTexture(Item item)
    {
        if (item is Equipment equipment)
        {
            if (equipment.Sprite != null)
            {
                return equipment.Sprite;
            }

            if (!string.IsNullOrEmpty(equipment.SpritePath))
            {
                return GD.Load<Texture>(equipment.SpritePath);
            }
        }
        else if (item is Weapon weapon)
        {
            if (weapon.Sprite != null)
            {
                return weapon.Sprite;
            }

            if (!string.IsNullOrEmpty(weapon.SpritePath))
            {
                return GD.Load<Texture>(weapon.SpritePath);
            }
        }

        return null;
    }

    private void OnBuySlotMouseEntered(ShopItemSlot slot)
    {
        if (slot == null || slot.ItemData == null)
        {
            return;
        }

        _previewedItem = slot.ItemData;
        ShowPreviewPanel(slot.ItemData);
    }

    private void OnBuySlotMouseExited(ShopItemSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (_selectedBuySlot != null)
        {
            ShowPreviewPanel(_selectedBuySlot.ItemData);
            return;
        }

        if (_previewedItem == slot.ItemData)
        {
            ResetPreviewPanel();
            _previewedItem = null;
        }
    }

    private void OnBuySlotSelected(ShopItemSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (_selectedBuySlot != null && _selectedBuySlot != slot)
        {
            _selectedBuySlot.SetSelected(false);
        }

        _selectedBuySlot = slot;
        _selectedBuySlot.SetSelected(true);
        _previewedItem = slot.ItemData;
        ShowPreviewPanel(slot.ItemData);
    }

    private void OnBuySlotActivated(ShopItemSlot slot)
    {
        if (slot == null || slot.ItemData == null || _activeNpc == null || _activePlayer == null)
        {
            return;
        }

        if (_activeNpc.TrySellToPlayer(_activePlayer, slot.ItemData, slot.ItemData.Price))
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Bought {slot.ItemData.Name} for {slot.ItemData.Price} gold.";
            }

            ClearBuySelection();
            ResetPreviewPanel();
            RebuildLists();
            return;
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Could not buy {slot.ItemData.Name}.";
        }
    }

    private void OnSellSlotMouseEntered(ShopItemSlot slot)
    {
        if (slot == null || slot.ItemData == null)
        {
            return;
        }

        _previewedItem = slot.ItemData;
        ShowPreviewPanel(slot.ItemData);
    }

    private void OnSellSlotMouseExited(ShopItemSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (_selectedSellSlot != null)
        {
            ShowPreviewPanel(_selectedSellSlot.ItemData);
            return;
        }

        if (_previewedItem == slot.ItemData)
        {
            ResetPreviewPanel();
            _previewedItem = null;
        }
    }

    private void OnSellSlotSelected(ShopItemSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (_selectedSellSlot != null && _selectedSellSlot != slot)
        {
            _selectedSellSlot.SetSelected(false);
        }

        _selectedSellSlot = slot;
        _selectedSellSlot.SetSelected(true);
        _previewedItem = slot.ItemData;
        ShowPreviewPanel(slot.ItemData);
    }

    private void OnSellSlotActivated(ShopItemSlot slot)
    {
        if (slot == null || slot.ItemData == null || _activeNpc == null || _activePlayer == null)
        {
            return;
        }

        int sellPrice = GetSellPrice(slot.ItemData);
        if (_activeNpc.TryBuyFromPlayer(_activePlayer, slot.ItemData, sellPrice))
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Sold {slot.ItemData.Name} for {sellPrice} gold.";
            }

            ClearSellSelection();
            ResetPreviewPanel();
            RebuildLists();
            return;
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Could not sell {slot.ItemData.Name}.";
        }
    }

    private void ShowPreviewPanel(Item item)
    {
        if (_previewPanel == null || item == null)
        {
            return;
        }

        _previewPanel.Visible = true;

        var rarityColor = GetRarityColor(item);
        if (_previewPanel.GetStylebox("panel") is StyleBoxFlat previewStyle)
        {
            previewStyle.BorderColor = rarityColor;
        }

        _previewIcon.Texture = ResolveItemTexture(item);
        _previewTitleLabel.Text = item.Name ?? "Item";
        _previewTitleLabel.AddColorOverride("font_color", rarityColor);
        _previewPriceLabel.Text = $"Price: {item.Price} gold";
        _previewMetaLabel.Text = BuildPreviewMeta(item);
        _previewDescriptionLabel.Text = BuildPreviewStats(item);
        _previewDescriptionLabel.RectMinSize = new Vector2(0f, 0f);
    }

    private string BuildPreviewMeta(Item item)
    {
        int rarity = GetItemRarity(item);

        if (item is Equipment equipment)
        {
            return $"{equipment.EquipmentType}  •  Rarity {rarity}";
        }

        if (item is Weapon weapon)
        {
            return $"{weapon.WeaponType}  •  Rarity {rarity}";
        }

        return $"Rarity {rarity}";
    }

    private string BuildPreviewStats(Item item)
    {
        if (item is Equipment equipment)
        {
            return $"ATK +{equipment.EquipmentAbilities?.Atk ?? 0}\nDEF +{equipment.EquipmentAbilities?.Def ?? 0}\nSPD +{equipment.EquipmentAbilities?.Spd ?? 0}\nVIT +{equipment.EquipmentAbilities?.Vit ?? 0}\nLevel Req {equipment.LevelRequirement}";
        }

        if (item is Weapon weapon)
        {
            return $"ATK +{weapon.WeaponAbilities?.Atk ?? 0}\nDEF +{weapon.WeaponAbilities?.Def ?? 0}\nSPD +{weapon.WeaponAbilities?.Spd ?? 0}\nVIT +{weapon.WeaponAbilities?.Vit ?? 0}\nLevel Req {weapon.LevelRequirement}";
        }

        return "No extra stats.";
    }

    private void ResetPreviewPanel()
    {
        if (_previewPanel == null)
        {
            return;
        }

        _previewPanel.Visible = true;
        _previewIcon.Texture = null;
        _previewTitleLabel.Text = "Hover an item";
        _previewTitleLabel.AddColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        _previewMetaLabel.Text = "Select a slot to keep it pinned.";
        _previewPriceLabel.Text = "";
        _previewDescriptionLabel.Text = "The shop preview stays fixed here.\n\nAny extra text will scroll inside this panel instead of resizing it.";
    }

    private void ClearBuySelection()
    {
        if (_selectedBuySlot != null)
        {
            _selectedBuySlot.SetSelected(false);
            _selectedBuySlot = null;
        }

        _previewedItem = null;
    }

    private void ClearSellSelection()
    {
        if (_selectedSellSlot != null)
        {
            _selectedSellSlot.SetSelected(false);
            _selectedSellSlot = null;
        }

        _previewedItem = null;
    }

    private int GetSellPrice(Item item)
    {
        if (item == null || item.Price <= 0)
        {
            return 0;
        }

        return Math.Max(1, item.Price / 2);
    }

    private Label CreateInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            Autowrap = true
        };
    }

    private void ClearContainer(VBoxContainer container)
    {
        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void ClearContainer(GridContainer container)
    {
        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnClosePressed()
    {
        HideShop();
        Closed?.Invoke();
    }
}