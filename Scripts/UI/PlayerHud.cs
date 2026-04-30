using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;

public class PlayerHud : CanvasLayer
{
    private const float HudWidth = 460f;
    private const float HudHeight = 160f;
    private const float HudBottomOffset = 12f;
    private const float HpBarHeight = 15f;
    private const float SkillSlotSize = 52f;

    private readonly Dictionary<string, string> _skillIconPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["basic_attack"] = "res://Assets/SkillAnimation/slash-icon.png",
        ["bow_attack"] = "res://Assets/SkillAnimation/arrow.png",
        ["fireball"] = "res://Assets/SkillAnimation/fireball.png",
    };

    private readonly List<SkillSlotUi> _skillSlots = new List<SkillSlotUi>();

    private Player _player;
    private Map _map;

    private Label _levelValue;
    private ProgressBar _hpBar;
    private Label _hpValue;
    private GridContainer _skillsContainer;
    private Control _rootControl;

    public override void _Ready()
    {
        BuildUi();
    }

    public override void _Process(float delta)
    {
        if (_player == null)
        {
            return;
        }

        RefreshSkills();
    }

    public void Initialize(Player player, Map map)
    {
        _player = player;
        _map = map;
        RefreshAll();

        if (_player == null)
        {
            return;
        }

        _player.OnLevelChanged += HandleLevelChanged;
        _player.OnHpChanged += HandleHpChanged;
    }

    public override void _ExitTree()
    {
        if (_player != null)
        {
            _player.OnLevelChanged -= HandleLevelChanged;
            _player.OnHpChanged -= HandleHpChanged;
        }
    }

    public new void SetVisible(bool visible)
    {
        if (_rootControl != null)
        {
            _rootControl.Visible = visible;
        }
    }

    private void BuildUi()
    {
        _rootControl = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_rootControl);
        var root = _rootControl;

        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            MarginLeft = -HudWidth * 0.5f,
            MarginRight = HudWidth * 0.5f,
            MarginTop = -(HudHeight + HudBottomOffset),
            MarginBottom = -HudBottomOffset,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.1f, 0.14f, 0.70f),
            BorderColor = new Color(0.25f, 0.7f, 0.5f, 0.8f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 10,
            ContentMarginBottom = 8,
        };
        panel.AddStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 8);
        panel.AddChild(vbox);

        _levelValue = new Label
        {
            Text = "Level: -",
            Align = Label.AlignEnum.Center,
        };
        vbox.AddChild(_levelValue);

        var hpBarRoot = new Control
        {
            RectMinSize = new Vector2(0f, HpBarHeight),
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
        };
        vbox.AddChild(hpBarRoot);

        _hpBar = new ProgressBar
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            PercentVisible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hpBarRoot.AddChild(_hpBar);

        var hpFill = new StyleBoxFlat
        {
            BgColor = new Color(0.82f, 0.18f, 0.24f, 1f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        var hpBg = new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.14f, 0.16f, 0.95f),
            BorderColor = new Color(0.36f, 0.36f, 0.4f, 0.95f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
        _hpBar.AddStyleboxOverride("fg", hpFill);
        _hpBar.AddStyleboxOverride("bg", hpBg);

        _hpValue = new Label
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Text = "0/0",
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _hpValue.AddColorOverride("font_color", new Color(0.98f, 0.98f, 0.98f, 1f));
        hpBarRoot.AddChild(_hpValue);

        var skillsTitle = new Label { Text = "Skills" };
        vbox.AddChild(skillsTitle);

        _skillsContainer = new GridContainer
        {
            Columns = 7,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
        };
        _skillsContainer.AddConstantOverride("hseparation", 6);
        _skillsContainer.AddConstantOverride("vseparation", 4);
        vbox.AddChild(_skillsContainer);
    }

    private void RefreshAll()
    {
        if (_player == null)
        {
            _levelValue.Text = "Level: -";
            UpdateHp(0, 0);
            RefreshSkills();
            return;
        }

        _levelValue.Text = $"Level: {_player.Level}";
        UpdateHp(_player.Attributes?.HP?.CurrentHP ?? 0, _player.Attributes?.HP?.MaxHP ?? 0);
        RefreshSkills();
    }

    private void RefreshSkills()
    {
        if (_skillsContainer == null)
        {
            return;
        }

        if (_player == null)
        {
            ClearSkillSlots();
            return;
        }

        var skillSnapshots = _player.GetSkillSnapshots();
        int selectedSkillIndex = Mathf.Clamp(_player.GetSelectedSkillIndex(), 0, Math.Max(0, skillSnapshots.Count - 1));
        for (int i = 0; i < skillSnapshots.Count; i++)
        {
            if (i >= _skillSlots.Count)
            {
                SkillSlotUi slot = CreateSkillSlot();
                _skillSlots.Add(slot);
                _skillsContainer.AddChild(slot.Root);
            }

            bool isSelected = i == selectedSkillIndex;
            UpdateSkillSlot(_skillSlots[i], skillSnapshots[i], isSelected);
            _skillSlots[i].Root.Visible = true;
        }

        for (int i = skillSnapshots.Count; i < _skillSlots.Count; i++)
        {
            _skillSlots[i].Root.Visible = false;
        }
    }

    private SkillSlotUi CreateSkillSlot()
    {
        var slotRoot = new PanelContainer
        {
            RectMinSize = new Vector2(SkillSlotSize, SkillSlotSize),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var slotStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.14f, 0.2f, 0.92f),
            BorderColor = new Color(0.28f, 0.75f, 0.56f, 0.85f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
        slotRoot.AddStyleboxOverride("panel", slotStyle);

        var content = new Control
        {
            RectMinSize = new Vector2(SkillSlotSize, SkillSlotSize),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        slotRoot.AddChild(content);

        var icon = new TextureRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MarginLeft = 4f,
            MarginTop = 4f,
            MarginRight = -4f,
            MarginBottom = -4f,
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddChild(icon);

        var cooldownOverlay = new ColorRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(0f, 0f, 0f, 0.46f),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddChild(cooldownOverlay);

        var cooldownValue = new Label
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        cooldownValue.AddColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
        cooldownValue.AddColorOverride("font_color_shadow", new Color(0f, 0f, 0f, 0.9f));
        content.AddChild(cooldownValue);

        var slotButton = new Button
        {
            RectPosition = Vector2.Zero,
            RectMinSize = new Vector2(SkillSlotSize, SkillSlotSize),
            Flat = true,
            Text = string.Empty,
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        slotButton.Connect("pressed", this, nameof(HandleSkillSlotPressed), new Godot.Collections.Array { _skillSlots.Count });
        slotButton.AddStyleboxOverride("normal", BuildSlotButtonStyle(new Color(0f, 0f, 0f, 0f)));
        slotButton.AddStyleboxOverride("hover", BuildSlotButtonStyle(new Color(1f, 1f, 1f, 0.04f)));
        slotButton.AddStyleboxOverride("pressed", BuildSlotButtonStyle(new Color(1f, 1f, 1f, 0.10f)));
        slotButton.AddStyleboxOverride("disabled", BuildSlotButtonStyle(new Color(0f, 0f, 0f, 0f)));
        content.AddChild(slotButton);

        return new SkillSlotUi(slotRoot, slotStyle, icon, cooldownOverlay, cooldownValue, slotButton);
    }

    private void UpdateSkillSlot(SkillSlotUi slot, PlayerSkillSnapshot skill, bool isSelected)
    {
        if (slot == null)
        {
            return;
        }

        slot.Icon.Texture = LoadSkillIcon(skill?.SkillId);

        float remaining = Mathf.Max(0f, skill?.RemainingCooldownSeconds ?? 0f);
        bool onCooldown = remaining > 0.01f;
        slot.CooldownOverlay.Visible = onCooldown;
        slot.CooldownValue.Visible = onCooldown;
        slot.CooldownValue.Text = onCooldown ? FormatCooldown(remaining) : string.Empty;

        string tooltip = string.IsNullOrWhiteSpace(skill?.Name)
            ? "Unknown Skill"
            : skill.Name;
        slot.Root.HintTooltip = tooltip;
        slot.Button.Disabled = onCooldown || string.IsNullOrWhiteSpace(skill?.SkillId);

        slot.Style.BorderColor = isSelected
            ? new Color(1f, 0.88f, 0.42f, 1f)
            : new Color(0.28f, 0.75f, 0.56f, 0.85f);
        slot.Style.BorderWidthTop = isSelected ? 2 : 1;
        slot.Style.BorderWidthRight = isSelected ? 2 : 1;
        slot.Style.BorderWidthBottom = isSelected ? 2 : 1;
        slot.Style.BorderWidthLeft = isSelected ? 2 : 1;
        slot.Style.BgColor = isSelected
            ? new Color(0.16f, 0.18f, 0.24f, 0.96f)
            : new Color(0.1f, 0.14f, 0.2f, 0.92f);
    }

    private static string FormatCooldown(float seconds)
    {
        if (seconds >= 10f)
        {
            return Mathf.CeilToInt(seconds).ToString();
        }

        return $"{seconds:F1}";
    }

    private Texture LoadSkillIcon(string skillId)
    {
        string iconPath;
        if (!_skillIconPaths.TryGetValue(skillId ?? string.Empty, out iconPath))
        {
            iconPath = "res://Assets/SkillAnimation/arrow.png";
        }

        return GD.Load<Texture>(iconPath);
    }

    private void HandleLevelChanged(int level)
    {
        _levelValue.Text = $"Level: {level}";
    }

    private void HandleHpChanged(int current, int max)
    {
        UpdateHp(current, max);
    }

    private void UpdateHp(int current, int max)
    {
        if (_hpBar == null || _hpValue == null)
        {
            return;
        }

        int safeMax = Math.Max(1, max);
        int clampedCurrent = Mathf.Clamp(current, 0, safeMax);

        _hpBar.MaxValue = safeMax;
        _hpBar.Value = clampedCurrent;
        _hpValue.Text = $"{clampedCurrent}/{safeMax}";

        var fgStyle = _hpBar.GetStylebox("fg") as StyleBoxFlat;
        if (fgStyle != null)
        {
            float ratio = (float)clampedCurrent / safeMax;
            if (ratio > 0.5f) { fgStyle.BgColor = new Color(0.1f, 0.8f, 0.1f, 1f); } // Green
            else if (ratio > 0.25f) { fgStyle.BgColor = new Color(0.9f, 0.7f, 0.1f, 1f); } // Orange/Yellow
            else { fgStyle.BgColor = new Color(0.9f, 0.1f, 0.1f, 1f); } // Red
        }
    }

    private void ClearSkillSlots()
    {
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            _skillSlots[i].Root.QueueFree();
        }

        _skillSlots.Clear();
    }

    private StyleBoxFlat BuildSlotButtonStyle(Color bgColor)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            BorderWidthLeft = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
    }

    private void HandleSkillSlotPressed(int skillIndex)
    {
        if (_player == null || _map == null)
        {
            return;
        }

        // 立即消耗鼠標輸入，防止同一幀內再次觸發
        _player.ConsumeSkillActivationInput();
        _player.TriggerSkill(skillIndex, _map);
    }

    private sealed class SkillSlotUi
    {
        public SkillSlotUi(PanelContainer root, StyleBoxFlat style, TextureRect icon, ColorRect cooldownOverlay, Label cooldownValue, Button button)
        {
            Root = root;
            Style = style;
            Icon = icon;
            CooldownOverlay = cooldownOverlay;
            CooldownValue = cooldownValue;
            Button = button;
        }

        public PanelContainer Root { get; }

        public StyleBoxFlat Style { get; }

        public TextureRect Icon { get; }

        public ColorRect CooldownOverlay { get; }

        public Label CooldownValue { get; }

        public Button Button { get; }
    }
}