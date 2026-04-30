using Godot;

public class ProgressSyncIndicator : CanvasLayer
{
    public enum SyncState
    {
        Hidden,
        Loading,
        Saving,
    }


    private PanelContainer _panel;
    private TextureRect _icon;
    private Label _label;
    private SyncState _state = SyncState.Hidden;

    public override void _Ready()
    {
        BuildUi();
        SetState(SyncState.Hidden);
    }

    public override void _Process(float delta)
    {
        if (_state == SyncState.Hidden || _icon == null)
        {
            return;
        }

        _icon.RectRotation += delta * 220f;
    }

    public void SetState(SyncState state)
    {
        _state = state;
        if (_panel == null || _label == null)
        {
            return;
        }

        switch (state)
        {
            case SyncState.Loading:
                _panel.Visible = true;
                _label.Text = "Loading progress...";
                break;
            case SyncState.Saving:
                _panel.Visible = true;
                _label.Text = "Saving progress...";
                break;
            default:
                _panel.Visible = false;
                break;
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

        _panel = new PanelContainer
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            RectMinSize = new Vector2(220f, 44f),
            RectPosition = new Vector2(-232f, 14f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        root.AddChild(_panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.12f, 0.1f, 0.88f),
            BorderColor = new Color(0.2f, 0.88f, 0.58f, 0.95f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        _panel.AddStyleboxOverride("panel", style);

        var row = new HBoxContainer();
        row.AddConstantOverride("separation", 8);
        _panel.AddChild(row);

        _icon = new TextureRect
        {
            RectMinSize = new Vector2(20f, 20f),
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GD.Load<Texture>("res://icon.png"),
        };
        row.AddChild(_icon);

        _label = new Label
        {
            Text = "Syncing...",
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            ClipText = true,
        };
        row.AddChild(_label);
    }

}