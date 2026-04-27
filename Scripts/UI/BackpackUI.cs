using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;

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
            MarginLeft = 18f,
            MarginTop = -100f,
            MarginRight = 96f,
            MarginBottom = -18f,
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
            MarginLeft = -390f,
            MarginTop = -280f,
            MarginRight = 390f,
            MarginBottom = 280f,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.AddChild(_panelRoot);

        _panelBackground = new TextureRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
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
            MarginLeft = 132f,
            MarginTop = 66f,
            MarginRight = -108f,
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
        moneyRow.AddChild(new Control { RectMinSize = new Vector2(12f, 1f) });
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

        _dropButton = new Button
        {
            Text = "DROP 丟棄選取",
            RectMinSize = new Vector2(148f, 42f),
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

        var nameLabel = new Label
        {
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center,
            Text = item?.Name ?? "(空)",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipText = true,
            RectMinSize = new Vector2(60f, 0f),
        };
        nameLabel.AddColorOverride("font_color", item == null ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.9f, 0.95f, 1f));
        vbox.AddChild(nameLabel);

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

        return false;
    }
}