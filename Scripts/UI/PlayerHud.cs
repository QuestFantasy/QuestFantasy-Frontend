using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;

public class PlayerHud : CanvasLayer
{
    private readonly Dictionary<string, string> _skillIconPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["basic_attack"] = "res://Assets/SkillAnimation/arrow.png",
        ["bow_attack"] = "res://Assets/SkillAnimation/arrow.png",
        ["fireball"] = "res://Assets/SkillAnimation/fireball.png",
    };

    private Player _player;

    private Label _levelValue;
    private Label _hpValue;
    private VBoxContainer _skillsContainer;

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

    public void Initialize(Player player)
    {
        _player = player;
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

    private void BuildUi()
    {
        var root = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(root);

        var panel = new PanelContainer
        {
            RectPosition = new Vector2(16f, 16f),
            RectSize = new Vector2(280f, 180f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.1f, 0.14f, 0.84f),
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
            ContentMarginBottom = 10,
        };
        panel.AddStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 8);
        panel.AddChild(vbox);

        var title = new Label
        {
            Text = "PLAYER STATUS",
        };
        vbox.AddChild(title);

        _levelValue = new Label { Text = "Level: -" };
        _hpValue = new Label { Text = "HP: -" };
        vbox.AddChild(_levelValue);
        vbox.AddChild(_hpValue);

        var skillsTitle = new Label { Text = "Skills" };
        vbox.AddChild(skillsTitle);

        _skillsContainer = new VBoxContainer();
        _skillsContainer.AddConstantOverride("separation", 5);
        vbox.AddChild(_skillsContainer);
    }

    private void RefreshAll()
    {
        if (_player == null)
        {
            _levelValue.Text = "Level: -";
            _hpValue.Text = "HP: -";
            return;
        }

        _levelValue.Text = $"Level: {_player.Level}";
        _hpValue.Text = $"HP: {_player.Attributes?.HP?.CurrentHP ?? 0}/{_player.Attributes?.HP?.MaxHP ?? 0}";
        RefreshSkills();
    }

    private void RefreshSkills()
    {
        if (_skillsContainer == null)
        {
            return;
        }

        foreach (Node child in _skillsContainer.GetChildren())
        {
            child.QueueFree();
        }

        if (_player == null)
        {
            return;
        }

        var skillSnapshots = _player.GetSkillSnapshots();
        for (int i = 0; i < skillSnapshots.Count; i++)
        {
            var skill = skillSnapshots[i];
            _skillsContainer.AddChild(CreateSkillRow(skill));
        }
    }

    private Control CreateSkillRow(PlayerSkillSnapshot skill)
    {
        var row = new HBoxContainer();
        row.AddConstantOverride("separation", 6);

        var icon = new TextureRect
        {
            Expand = true,
            RectMinSize = new Vector2(16, 16),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        icon.Texture = LoadSkillIcon(skill.SkillId);
        row.AddChild(icon);

        string cooldownText = skill.RemainingCooldownSeconds <= 0.01f
            ? "Ready"
            : $"{skill.RemainingCooldownSeconds:F1}s";

        var text = new Label
        {
            Text = $"{skill.Name} [{cooldownText}]",
            ClipText = true,
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
        };
        row.AddChild(text);

        return row;
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
        _hpValue.Text = $"HP: {current}/{max}";
    }
}