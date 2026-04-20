using System;
using Godot;

public class DeathScreenUI : CanvasLayer
{
    public event Action OnRespawnClicked;
    public event Action OnExitClicked;

    private Control _root;

    public override void _Ready()
    {
        BuildUi();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.Visible = visible;
        }
    }

    private void BuildUi()
    {
        _root = new Control
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_root);

        // Background overlay
        var bg = new ColorRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(0, 0, 0, 0.75f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _root.AddChild(bg);

        var centerContainer = new CenterContainer
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(centerContainer);

        var panel = new PanelContainer();
        centerContainer.AddChild(panel);

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        panel.AddStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 20);
        panel.AddChild(vbox);

        var titleLabel = new Label
        {
            Text = "You Died",
            Align = Label.AlignEnum.Center,
            Valign = Label.VAlign.Center
        };
        vbox.AddChild(titleLabel);

        var buttonVbox = new VBoxContainer();
        buttonVbox.AddConstantOverride("separation", 10);
        vbox.AddChild(buttonVbox);

        var respawnBtn = CreateButton("Respawn");
        respawnBtn.Connect("pressed", this, nameof(HandleRespawnPressed));
        buttonVbox.AddChild(respawnBtn);

        var exitBtn = CreateButton("Exit to Login");
        exitBtn.Connect("pressed", this, nameof(HandleExitPressed));
        buttonVbox.AddChild(exitBtn);
    }

    private Button CreateButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            RectMinSize = new Vector2(200, 40),
            FocusMode = Control.FocusModeEnum.None
        };
        
        var normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.24f, 0.35f, 1f),
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5
        };
        
        var hoverStyle = normalStyle.Duplicate() as StyleBoxFlat;
        if (hoverStyle != null) hoverStyle.BgColor = new Color(0.3f, 0.35f, 0.5f, 1f);
        
        var pressedStyle = normalStyle.Duplicate() as StyleBoxFlat;
        if (pressedStyle != null) pressedStyle.BgColor = new Color(0.15f, 0.18f, 0.26f, 1f);

        btn.AddStyleboxOverride("normal", normalStyle);
        btn.AddStyleboxOverride("hover", hoverStyle);
        btn.AddStyleboxOverride("pressed", pressedStyle);

        return btn;
    }

    private void HandleRespawnPressed()
    {
        OnRespawnClicked?.Invoke();
    }

    private void HandleExitPressed()
    {
        OnExitClicked?.Invoke();
    }
}
