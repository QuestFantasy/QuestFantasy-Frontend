using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;

// Equipment slot definition
public struct EquipSlotDef
{
    public string Label;
    public EquipmentType SlotType;
    public bool IsWeapon;
    public EquipSlotDef(string label, EquipmentType slotType, bool isWeapon = false)
    {
        Label = label;
        SlotType = slotType;
        IsWeapon = isWeapon;
    }
}

public class BackpackUI : CanvasLayer
{
    private const int SlotsPerPage = 12;
    private const int GridColumns = 4;
    private const int GridRows = 3;
    private const float SlotSize = 86f;

    [Export] public string BackpackButtonTexturePath = "res://Assets/backpack/backpackUIbutton.png";
    [Export] public string BackpackPanelTexturePath = "res://Assets/backpack/backpackUI.png";
    [Export] public string MoneyIconTexturePath = "res://Assets/money/money-f1.png";

    public event Action<Item> DropRequested;
    /// <summary>
    /// Fired when the backpack panel is opened. Subscribe in Main to trigger a backend sync
    /// so that all items receive their instance_id before the player can interact with them.
    /// </summary>
    public event Action SyncRequested;

    private Player _player;
    private Control _root;
    private TextureButton _toggleButton;
    private Control _panelRoot;
    private TextureRect _panelBackground;
    private Label _moneyValueLabel;
    private GridContainer _grid;
    private CenterContainer _gridCenter;
    private Label _pageLabel;
    private Button _prevButton;
    private Button _nextButton;
    private Button _dropButton;
    private Button _equipButton;

    // Equipment panel
    private VBoxContainer _equipSlotsContainer;
    private readonly EquipSlotDef[] _equipSlotDefs = new EquipSlotDef[]
    {
        new EquipSlotDef("頭部", EquipmentType.Head),
        new EquipSlotDef("胸甲", EquipmentType.Body),
        new EquipSlotDef("手套", EquipmentType.Arms),
        new EquipSlotDef("腿部", EquipmentType.Legs),
        new EquipSlotDef("鞋子", EquipmentType.Shoes),
        new EquipSlotDef("武器", EquipmentType.None, true),
    };

    private readonly List<Item> _cachedItems = new List<Item>();
    private int _currentPage = 0;
    private int _selectedGlobalIndex = -1;
    private bool _viewDirty = true;
    private int _lastItemCount = -1;
    private int _lastGold = -1;

    public override void _Ready()
    {
        EnsureToggleInputAction();
        BuildUi();
        SetGameplayVisible(false);
    }

    public override void _Process(float delta)
    {
        if (_root == null || !_root.Visible)
        {
            return;
        }

        if (Input.IsActionJustPressed("toggle_inventory"))
        {
            TogglePanelVisibility();
        }

        if (_panelRoot == null || !_panelRoot.Visible)
        {
            return;
        }

        int itemCount = _player?.InventoryItems?.Count ?? 0;
        int gold = _player?.Gold ?? 0;
        if (_viewDirty || itemCount != _lastItemCount || gold != _lastGold)
        {
            RefreshView();
        }

        if (!IsMouseHoveringAnyBackpackSlot())
        {
            EquipmentPreview.Instance?.HidePreview();
        }
    }

    public void Initialize(Player player)
    {
        _player = player;
        _cachedItems.Clear();
        _currentPage = 0;
        _selectedGlobalIndex = -1;
        _viewDirty = true;

        if (_player != null)
        {
            _player.OnGoldChanged += HandleGoldChanged;
        }

        RefreshView();
    }

    public override void _ExitTree()
    {
        if (_player != null)
        {
            _player.OnGoldChanged -= HandleGoldChanged;
        }
    }

    public void SetGameplayVisible(bool visible)
    {
        if (_root == null)
        {
            return;
        }

        _root.Visible = visible;
        if (!visible)
        {
            SetPanelVisible(false);
        }
    }

    private void BuildUi()
    {
        _root = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Visible = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_root);

        _toggleButton = new TextureButton
        {
            AnchorLeft = 0f,
            AnchorRight = 0f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            MarginLeft = 5f,
            MarginTop = -105f,
            MarginRight = 105f,
            MarginBottom = -5f,
            Expand = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Stop,
            HintTooltip = "背包 (E)",
        };
        _toggleButton.TextureNormal = GD.Load<Texture>(BackpackButtonTexturePath);
        _toggleButton.Connect("pressed", this, nameof(OnToggleButtonPressed));
        _root.AddChild(_toggleButton);

        _panelRoot = new Control
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            MarginLeft = -540f,
            MarginTop = -280f,
            MarginRight = 390f,
            MarginBottom = 280f,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.AddChild(_panelRoot);

        // Equipment panel background (left side)
        var equipBg = new Panel
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 0f,
            AnchorBottom = 1f,
            MarginLeft = 0f,
            MarginTop = 0f,
            MarginRight = 148f,
            MarginBottom = 0f,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        var equipBgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.14f, 0.92f),
            BorderColor = new Color(0.35f, 0.30f, 0.50f, 0.8f),
            BorderWidthTop = 2, BorderWidthBottom = 2,
            BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
        };
        equipBg.AddStyleboxOverride("panel", equipBgStyle);
        _panelRoot.AddChild(equipBg);

        // Equipment title
        var equipTitle = new Label
        {
            Text = "⚔ 裝備",
            Align = Label.AlignEnum.Center,
            RectMinSize = new Vector2(128f, 28f),
        };
        equipTitle.AddColorOverride("font_color", new Color(0.85f, 0.78f, 1f));

        _equipSlotsContainer = new VBoxContainer
        {
            AnchorLeft = 0f, AnchorRight = 1f,
            AnchorTop = 0f, AnchorBottom = 1f,
            MarginLeft = 8f, MarginTop = 10f,
            MarginRight = -8f, MarginBottom = -10f,
        };
        _equipSlotsContainer.AddConstantOverride("separation", 4);
        _equipSlotsContainer.AddChild(equipTitle);
        equipBg.AddChild(_equipSlotsContainer);

        // Backpack background (right side, offset to make room for equip panel)
        _panelBackground = new TextureRect
        {
            AnchorLeft = 0f, AnchorRight = 1f,
            AnchorTop = 0f, AnchorBottom = 1f,
            MarginLeft = 150f,
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Texture = GD.Load<Texture>(BackpackPanelTexturePath),
        };
        _panelRoot.AddChild(_panelBackground);

        var content = new VBoxContainer
        {
            AnchorLeft = 0f,
            AnchorRight = 1f,
            AnchorTop = 0f,
            AnchorBottom = 1f,
            MarginLeft = 210f,
            MarginTop = 66f,
            MarginRight = -50f,
            MarginBottom = -62f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddConstantOverride("separation", 10);
        _panelRoot.AddChild(content);

        var moneyRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignMode.Begin,
            SizeFlagsHorizontal = (int)Control.SizeFlags.Fill,
        };
        moneyRow.AddConstantOverride("separation", 8);
        content.AddChild(new Control { RectMinSize = new Vector2(1f, 12f) });
        moneyRow.AddChild(new Control { RectMinSize = new Vector2(48f, 1f) });
        var moneyIcon = new TextureRect
        {
            RectMinSize = new Vector2(30f, 30f),
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GD.Load<Texture>(MoneyIconTexturePath),
        };
        moneyRow.AddChild(moneyIcon);
        _moneyValueLabel = new Label { Text = "0" };
        _moneyValueLabel.AddColorOverride("font_color", new Color(1f, 0.92f, 0.4f));
        _moneyValueLabel.AddColorOverride("font_color_shadow", new Color(0f, 0f, 0f, 0.85f));
        _moneyValueLabel.RectMinSize = new Vector2(80f, 24f);
        moneyRow.AddChild(_moneyValueLabel);
        content.AddChild(moneyRow);

        _gridCenter = new CenterContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
        };
        content.AddChild(_gridCenter);

        _grid = new GridContainer
        {
            Columns = GridColumns,
            RectMinSize = new Vector2(
                GridColumns * SlotSize + (GridColumns - 1) * 10f,
                GridRows * SlotSize + (GridRows - 1) * 10f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _grid.AddConstantOverride("hseparation", 10);
        _grid.AddConstantOverride("vseparation", 10);
        _gridCenter.AddChild(_grid);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignMode.Center,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
        };
        footer.AddConstantOverride("separation", 10);
        _prevButton = new Button
        {
            Text = "< PREV",
            RectMinSize = new Vector2(112f, 42f),
        };
        _prevButton.Connect("pressed", this, nameof(OnPrevPagePressed));
        footer.AddChild(_prevButton);

        _pageLabel = new Label
        {
            Text = "1 / 1",
            RectMinSize = new Vector2(76f, 32f),
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center,
        };
        _pageLabel.AddColorOverride("font_color", new Color(0.95f, 0.95f, 0.95f));
        footer.AddChild(_pageLabel);

        _nextButton = new Button
        {
            Text = "NEXT >",
            RectMinSize = new Vector2(112f, 42f),
        };
        _nextButton.Connect("pressed", this, nameof(OnNextPagePressed));
        footer.AddChild(_nextButton);

        _equipButton = new Button
        {
            Text = "裝備 EQUIP",
            RectMinSize = new Vector2(120f, 42f),
        };
        _equipButton.AddColorOverride("font_color", new Color(0.6f, 1f, 0.7f));
        _equipButton.Connect("pressed", this, nameof(OnEquipButtonPressed));
        footer.AddChild(_equipButton);

        _dropButton = new Button
        {
            Text = "丟棄 DROP",
            RectMinSize = new Vector2(120f, 42f),
        };
        _dropButton.AddColorOverride("font_color", new Color(1f, 0.86f, 0.75f));
        _dropButton.Connect("pressed", this, nameof(OnDropButtonPressed));
        footer.AddChild(_dropButton);

        content.AddChild(footer);
    }

    private void EnsureToggleInputAction()
    {
        if (!InputMap.HasAction("toggle_inventory"))
        {
            InputMap.AddAction("toggle_inventory");
        }

        if (InputMap.GetActionList("toggle_inventory").Count > 0)
        {
            return;
        }

        var key = new InputEventKey { Scancode = (uint)KeyList.E };
        InputMap.ActionAddEvent("toggle_inventory", key);
    }

    private void OnToggleButtonPressed()
    {
        TogglePanelVisibility();
    }

    private void TogglePanelVisibility()
    {
        if (_panelRoot == null)
        {
            return;
        }

        SetPanelVisible(!_panelRoot.Visible);
    }

    private void SetPanelVisible(bool visible)
    {
        if (_panelRoot == null)
        {
            return;
        }

        _panelRoot.Visible = visible;
        if (visible)
        {
            _viewDirty = true;
            SyncRequested?.Invoke();   // Ask Main to sync so items get their instance_id.
            RefreshView();
        }
        else
        {
            EquipmentPreview.Instance?.HidePreview();
        }
    }

    private void RefreshView()
    {
        if (_player == null)
        {
            _moneyValueLabel.Text = "0";
            RebuildSlots(new List<Item>());
            RebuildEquipSlots();
            return;
        }

        _moneyValueLabel.Text = _player.Gold.ToString();
        var items = _player.InventoryItems;
        _cachedItems.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            _cachedItems.Add(items[i]);
        }

        int totalPages = Math.Max(1, (_cachedItems.Count + SlotsPerPage - 1) / SlotsPerPage);
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
        _pageLabel.Text = $"{_currentPage + 1} / {totalPages}";
        _prevButton.Disabled = _currentPage <= 0;
        _nextButton.Disabled = _currentPage >= totalPages - 1;
        _lastItemCount = _cachedItems.Count;
        _lastGold = _player.Gold;
        _viewDirty = false;

        RebuildSlots(_cachedItems);
        RebuildEquipSlots();
    }

    private void RebuildEquipSlots()
    {
        if (_equipSlotsContainer == null)
        {
            return;
        }

        // Remove all children except the title (first child)
        var children = _equipSlotsContainer.GetChildren();
        for (int i = children.Count - 1; i >= 1; i--)
        {
            ((Node)children[i]).QueueFree();
        }

        for (int i = 0; i < _equipSlotDefs.Length; i++)
        {
            var def = _equipSlotDefs[i];
            Control slot = BuildEquipSlot(def, i);
            _equipSlotsContainer.AddChild(slot);
        }
    }

    private Control BuildEquipSlot(EquipSlotDef def, int slotIndex)
    {
        const float EqSlotH = 72f;

        Item equipped = null;
        if (def.IsWeapon)
        {
            equipped = _player?.EquippedWeapon;
        }
        else
        {
            equipped = _player?.GetEquippedArmor(def.SlotType);
        }

        bool hasItem = equipped != null;

        var frame = new PanelContainer
        {
            RectMinSize = new Vector2(128f, EqSlotH),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        Color borderCol = hasItem
            ? new Color(0.55f, 0.75f, 1f, 0.9f)
            : new Color(0.25f, 0.25f, 0.35f, 0.7f);

        var style = new StyleBoxFlat
        {
            BgColor = hasItem
                ? new Color(0.12f, 0.15f, 0.25f, 0.9f)
                : new Color(0.06f, 0.07f, 0.12f, 0.7f),
            BorderColor = borderCol,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginTop = 4, ContentMarginBottom = 4,
            ContentMarginLeft = 6, ContentMarginRight = 6,
        };
        frame.AddStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddConstantOverride("separation", 6);
        hbox.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        hbox.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        hbox.Alignment = BoxContainer.AlignMode.Center;
        frame.AddChild(hbox);

        // Icon
        var icon = new TextureRect
        {
            RectMinSize = new Vector2(40f, 40f),
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        if (equipped is Equipment eq)
        {
            icon.Texture = eq.Sprite ?? LoadSpriteFromPath(eq.SpritePath);
        }
        else if (equipped is Weapon w)
        {
            icon.Texture = w.Sprite ?? LoadSpriteFromPath(w.SpritePath);
        }

        hbox.AddChild(icon);

        // Slot label only (no item name — hover shows details)
        var slotLabel = new Label
        {
            Text = def.Label,
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center,
            SizeFlagsVertical = (int)Control.SizeFlags.ShrinkCenter,
        };
        slotLabel.AddColorOverride("font_color", hasItem
            ? new Color(0.75f, 0.8f, 0.9f)
            : new Color(0.4f, 0.4f, 0.5f));
        hbox.AddChild(slotLabel);

        // Click to unequip
        frame.Connect("gui_input", this, nameof(OnEquipSlotGuiInput), new Godot.Collections.Array { slotIndex });
        frame.Connect("mouse_entered", this, nameof(OnEquipSlotMouseEntered), new Godot.Collections.Array { slotIndex });
        frame.Connect("mouse_exited", this, nameof(OnSlotMouseExited));

        return frame;
    }

    private void OnEquipSlotGuiInput(InputEvent @event, int slotIndex)
    {
        if (!(@event is InputEventMouseButton mb) || !mb.Pressed || mb.ButtonIndex != (int)ButtonList.Left)
        {
            return;
        }

        if (_player == null || slotIndex < 0 || slotIndex >= _equipSlotDefs.Length)
        {
            return;
        }

        var def = _equipSlotDefs[slotIndex];

        if (def.IsWeapon)
        {
            if (_player.EquippedWeapon != null)
            {
                Weapon w = _player.EquippedWeapon;
                _player.UnequipWeapon();
                _player.AddItem(w);
                GD.Print($"[BackpackUI] Unequipped weapon: {w.Name}");
            }
        }
        else
        {
            Equipment current = _player.GetEquippedArmor(def.SlotType);
            if (current != null)
            {
                _player.UnequipArmor(def.SlotType);
                _player.AddItem(current);
                GD.Print($"[BackpackUI] Unequipped armor: {current.Name} from {def.SlotType}");
            }
        }

        _viewDirty = true;
        RefreshView();
    }

    private void OnEquipSlotMouseEntered(int slotIndex)
    {
        if (_player == null || slotIndex < 0 || slotIndex >= _equipSlotDefs.Length)
        {
            return;
        }

        var def = _equipSlotDefs[slotIndex];
        Item item = null;

        if (def.IsWeapon)
        {
            item = _player.EquippedWeapon;
        }
        else
        {
            item = _player.GetEquippedArmor(def.SlotType);
        }

        if (item != null)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            EquipmentPreview.Instance?.ShowPreview(item, mousePos);
        }
    }

    private void OnEquipButtonPressed()
    {
        if (_player == null || _selectedGlobalIndex < 0 || _selectedGlobalIndex >= _cachedItems.Count)
        {
            return;
        }

        Item item = _cachedItems[_selectedGlobalIndex];
        if (item == null)
        {
            return;
        }

        if (item is Weapon weapon)
        {
            // If weapon already equipped, swap back to inventory
            Weapon oldWeapon = _player.EquippedWeapon;
            _player.RemoveItem(item);
            _player.EquipWeapon(weapon);
            if (oldWeapon != null)
            {
                _player.AddItem(oldWeapon);
            }
            GD.Print($"[BackpackUI] Equipped weapon: {weapon.Name}");
        }
        else if (item is Equipment eq)
        {
            if (eq.EquipmentType == EquipmentType.None || eq.EquipmentType == EquipmentType.Other)
            {
                GD.Print("[BackpackUI] Cannot equip item with no valid slot.");
                return;
            }

            _player.RemoveItem(item);
            Equipment oldArmor = _player.EquipArmor(eq);
            if (oldArmor != null)
            {
                _player.AddItem(oldArmor);
            }
            GD.Print($"[BackpackUI] Equipped armor: {eq.Name} to {eq.EquipmentType}");
        }
        else
        {
            GD.Print("[BackpackUI] Selected item is not equippable.");
            return;
        }

        _selectedGlobalIndex = -1;
        _viewDirty = true;
        RefreshView();
    }

    private void RebuildSlots(List<Item> allItems)
    {
        if (_grid == null)
        {
            return;
        }

        foreach (Node child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        int start = _currentPage * SlotsPerPage;
        int endExclusive = Math.Min(allItems.Count, start + SlotsPerPage);

        for (int i = start; i < start + SlotsPerPage; i++)
        {
            Control slot = BuildSlot(i, i < endExclusive ? allItems[i] : null);
            _grid.AddChild(slot);
        }
    }

    private Control BuildSlot(int globalIndex, Item item)
    {
        var frame = new PanelContainer
        {
            RectMinSize = new Vector2(SlotSize, SlotSize),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.16f, 0.87f),
            BorderColor = globalIndex == _selectedGlobalIndex
                ? new Color(1f, 0.88f, 0.35f, 1f)
                : new Color(0.3f, 0.36f, 0.45f, 0.95f),
            BorderWidthTop = globalIndex == _selectedGlobalIndex ? 2 : 1,
            BorderWidthRight = globalIndex == _selectedGlobalIndex ? 2 : 1,
            BorderWidthBottom = globalIndex == _selectedGlobalIndex ? 2 : 1,
            BorderWidthLeft = globalIndex == _selectedGlobalIndex ? 2 : 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
        };
        frame.AddStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
        vbox.Alignment = BoxContainer.AlignMode.Center;
        frame.AddChild(vbox);

        var icon = new TextureRect
        {
            RectMinSize = new Vector2(48f, 48f),
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        if (item is Equipment eq)
        {
            icon.Texture = eq.Sprite ?? LoadSpriteFromPath(eq.SpritePath);
        }
        else if (item is Weapon w)
        {
            icon.Texture = w.Sprite ?? LoadSpriteFromPath(w.SpritePath);
        }

        vbox.AddChild(icon);

        if (item != null)
        {
            var idLabel = new Label
            {
                Text = string.IsNullOrEmpty(item.InstanceId) ? "ID: (pending sync)" : $"ID: {item.InstanceId.Substring(0, 8)}…",
                Align = Label.AlignEnum.Center,
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            };
            idLabel.AddColorOverride("font_color", new Color(0.55f, 0.65f, 0.8f, 0.85f));
            idLabel.RectMinSize = new Vector2(0f, 14f);
            vbox.AddChild(idLabel);

            // Full instance_id visible on hover via tooltip.
            frame.HintTooltip = string.IsNullOrEmpty(item.InstanceId)
                ? $"{item.Name}\nID: (尚未同步 — 請先儲存背包以取得 ID)"
                : $"{item.Name}\nInstance ID: {item.InstanceId}";
        }

        frame.Connect("gui_input", this, nameof(OnSlotGuiInput), new Godot.Collections.Array { globalIndex });
        frame.Connect("mouse_entered", this, nameof(OnSlotMouseEntered), new Godot.Collections.Array { globalIndex });
        frame.Connect("mouse_exited", this, nameof(OnSlotMouseExited));

        return frame;
    }

    private Texture LoadSpriteFromPath(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath))
        {
            return null;
        }

        string path = spritePath.Trim().Replace('\\', '/');
        string normalized = path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? "res://" + path
            : path;

        if (!normalized.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            if (normalized.StartsWith("/"))
            {
                normalized = "res://" + normalized.TrimStart('/');
            }
            else if (!normalized.Contains("/"))
            {
                normalized = "res://Assets/Equipments/" + normalized;
            }
            else
            {
                normalized = "res://" + normalized;
            }
        }

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

        tex = GD.Load<Texture>("res://Assets/Equipments/" + fileName);
        if (tex != null)
        {
            return tex;
        }

        return GD.Load<Texture>("res://Assets/" + fileName);
    }

    private void OnSlotGuiInput(InputEvent @event, int globalIndex)
    {
        if (!(@event is InputEventMouseButton mb) || !mb.Pressed || mb.ButtonIndex != (int)ButtonList.Left)
        {
            return;
        }

        if (globalIndex < 0 || globalIndex >= _cachedItems.Count)
        {
            _selectedGlobalIndex = -1;
            _viewDirty = true;
            RefreshView();
            return;
        }

        _selectedGlobalIndex = globalIndex;
        _viewDirty = true;
        RefreshView();
    }

    private void OnSlotMouseEntered(int globalIndex)
    {
        if (globalIndex < 0 || globalIndex >= _cachedItems.Count)
        {
            return;
        }

        Item item = _cachedItems[globalIndex];
        if (item == null)
        {
            return;
        }

        Vector2 mousePos = GetViewport().GetMousePosition();
        EquipmentPreview.Instance?.ShowPreview(item, mousePos);
    }

    private void OnSlotMouseExited()
    {
        EquipmentPreview.Instance?.HidePreview();
    }

    private void OnPrevPagePressed()
    {
        _currentPage = Math.Max(0, _currentPage - 1);
        _viewDirty = true;
        RefreshView();
    }

    private void OnNextPagePressed()
    {
        int totalPages = Math.Max(1, (_cachedItems.Count + SlotsPerPage - 1) / SlotsPerPage);
        _currentPage = Math.Min(totalPages - 1, _currentPage + 1);
        _viewDirty = true;
        RefreshView();
    }

    private void OnDropButtonPressed()
    {
        if (_selectedGlobalIndex < 0 || _selectedGlobalIndex >= _cachedItems.Count)
        {
            return;
        }

        Item item = _cachedItems[_selectedGlobalIndex];
        if (item == null)
        {
            return;
        }

        DropRequested?.Invoke(item);
        _selectedGlobalIndex = -1;
        _viewDirty = true;
        RefreshView();
    }

    private void HandleGoldChanged(int gold)
    {
        if (_moneyValueLabel != null)
        {
            _moneyValueLabel.Text = Math.Max(0, gold).ToString();
        }

        _viewDirty = true;
    }

    private bool IsMouseHoveringAnyBackpackSlot()
    {
        if (_grid == null)
        {
            return false;
        }

        Vector2 mouse = GetViewport().GetMousePosition();
        foreach (Node child in _grid.GetChildren())
        {
            if (!(child is Control c) || !c.Visible)
            {
                continue;
            }

            if (c.GetGlobalRect().HasPoint(mouse))
            {
                return true;
            }
        }

        // Also check equipment slots
        if (_equipSlotsContainer != null)
        {
            foreach (Node child in _equipSlotsContainer.GetChildren())
            {
                if (!(child is Control c) || !c.Visible)
                {
                    continue;
                }

                if (c.GetGlobalRect().HasPoint(mouse))
                {
                    return true;
                }
            }
        }

        return false;
    }
}