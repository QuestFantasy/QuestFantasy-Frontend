using System;
using System.Collections.Generic;

using Godot;

public class SidebarMenu : CanvasLayer
{
    private class SidebarMenuItem
    {
        public string Id;
        public string Label;
        public Action Callback;
    }

    private const float CollapsedWidth = 90f;
    private const float ExpandedWidth = 320f;
    private const float TransitionDurationSeconds = 0.22f;

    private readonly List<SidebarMenuItem> _items = new List<SidebarMenuItem>();

    private PanelContainer _panel;
    private VBoxContainer _itemsContainer;
    private Button _toggleButton;
    private Tween _tween;
    private bool _isExpanded;

    public override void _Ready()
    {
        BuildUi();
        SetExpanded(false, true);
    }

    public void SetMenuVisible(bool visible)
    {
        Visible = visible;
        if (!visible)
        {
            SetExpanded(false, true);
        }
    }

    public void AddMenuItem(string id, string label, Action callback)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        _items.Add(new SidebarMenuItem
        {
            Id = id,
            Label = label,
            Callback = callback
        });

        RebuildMenuButtons();
    }

    public void ClearMenuItems()
    {
        _items.Clear();
        RebuildMenuButtons();
    }

    private void BuildUi()
    {
        var root = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(root);

        _panel = new PanelContainer
        {
            RectPosition = new Vector2(15f, 15f),
            RectSize = new Vector2(CollapsedWidth, 40f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        root.AddChild(_panel);

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.14f, 0.93f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            BorderColor = new Color(0.36f, 0.42f, 0.62f, 0.45f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        _panel.AddStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 8);
        _panel.AddChild(vbox);

        _toggleButton = CreateMenuButton("☰ Menu");
        _toggleButton.Connect("pressed", this, nameof(OnTogglePressed));
        vbox.AddChild(_toggleButton);

        _itemsContainer = new VBoxContainer();
        _itemsContainer.AddConstantOverride("separation", 6);
        _itemsContainer.Visible = false;
        _itemsContainer.Modulate = new Color(1f, 1f, 1f, 0f);
        vbox.AddChild(_itemsContainer);

        _tween = new Tween();
        AddChild(_tween);
        _tween.Connect("tween_all_completed", this, nameof(OnTweenCompleted));
    }

    private void RebuildMenuButtons()
    {
        if (_itemsContainer == null)
        {
            return;
        }

        foreach (Node child in _itemsContainer.GetChildren())
        {
            child.QueueFree();
        }

        for (int i = 0; i < _items.Count; i++)
        {
            SidebarMenuItem item = _items[i];
            var button = CreateMenuButton(item.Label);
            button.Connect("pressed", this, nameof(OnMenuItemPressed), new Godot.Collections.Array { item.Id });
            _itemsContainer.AddChild(button);
        }

        UpdatePanelHeight();
    }

    private Button CreateMenuButton(string text)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            Flat = true,
            Align = Button.TextAlign.Left,
            ClipText = true
        };

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.24f, 0.35f, 0.5f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };

        var hover = normal.Duplicate() as StyleBoxFlat;
        if (hover != null)
        {
            hover.BgColor = new Color(0.29f, 0.35f, 0.5f, 0.78f);
            button.AddStyleboxOverride("hover", hover);
        }

        button.AddStyleboxOverride("normal", normal);
        return button;
    }

    private void OnTogglePressed()
    {
        SetExpanded(!_isExpanded, false);
    }

    private void SetExpanded(bool expanded, bool instant)
    {
        _isExpanded = expanded;

        float targetWidth = expanded ? ExpandedWidth : CollapsedWidth;
        float targetAlpha = expanded ? 1f : 0f;
        _toggleButton.Text = expanded ? "✕ Close" : "☰ Menu";

        if (expanded)
        {
            _itemsContainer.Visible = true;
        }

        _tween.StopAll();

        if (instant)
        {
            _panel.RectSize = new Vector2(targetWidth, _panel.RectSize.y);
            _itemsContainer.Modulate = new Color(1f, 1f, 1f, targetAlpha);
            _itemsContainer.Visible = expanded;
            UpdatePanelHeight();
            return;
        }

        _tween.InterpolateProperty(
            _panel,
            "rect_size:x",
            _panel.RectSize.x,
            targetWidth,
            TransitionDurationSeconds,
            Tween.TransitionType.Cubic,
            Tween.EaseType.Out);

        _tween.InterpolateProperty(
            _itemsContainer,
            "modulate:a",
            _itemsContainer.Modulate.a,
            targetAlpha,
            TransitionDurationSeconds * 0.9f,
            Tween.TransitionType.Cubic,
            Tween.EaseType.Out);

        _tween.Start();
        UpdatePanelHeight();
    }

    private void OnTweenCompleted()
    {
        if (!_isExpanded)
        {
            _itemsContainer.Visible = false;
        }

        UpdatePanelHeight();
    }

    private void OnMenuItemPressed(string itemId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            SidebarMenuItem item = _items[i];
            if (!string.Equals(item.Id, itemId, StringComparison.Ordinal))
            {
                continue;
            }

            item.Callback?.Invoke();
            return;
        }
    }

    private void UpdatePanelHeight()
    {
        if (_panel == null)
        {
            return;
        }

        float height = 50f;
        if (_isExpanded)
        {
            height += Mathf.Max(0f, _items.Count) * 40f + (_items.Count > 0 ? 12f : 0f);
        }

        _panel.RectSize = new Vector2(_panel.RectSize.x, height);
    }
}