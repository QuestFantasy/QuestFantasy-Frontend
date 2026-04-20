using Godot;

public class PlayerHUD : CanvasLayer
{
    private ProgressBar _hpBar;
    private Label _hpLabel;
    private QuestFantasy.Characters.Player _player;

    public void Initialize(QuestFantasy.Characters.Player player)
    {
        _player = player;
    }

    public override void _Ready()
    {
        SetProcess(true);
        var marginContainer = new MarginContainer
        {
            AnchorTop = 1f,
            AnchorBottom = 1f,
            AnchorRight = 0.5f,
            AnchorLeft = 0.5f,
            MarginTop = -80f,
            MarginBottom = -20f,
            MarginLeft = -200f,
            MarginRight = 200f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(marginContainer);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignMode.End;
        marginContainer.AddChild(vbox);

        _hpLabel = new Label
        {
            Text = "HP: 20 / 20",
            Align = Label.AlignEnum.Center
        };
        vbox.AddChild(_hpLabel);

        _hpBar = new ProgressBar
        {
            RectMinSize = new Vector2(400, 20),
            PercentVisible = false // hide default percentage
        };

        var bgStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 };
        var fgStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.8f, 0.2f, 1f), CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 };

        _hpBar.AddStyleboxOverride("bg", bgStyle);
        _hpBar.AddStyleboxOverride("fg", fgStyle);

        vbox.AddChild(_hpBar);
    }

    public override void _Process(float delta)
    {
        if (_player != null && Godot.Object.IsInstanceValid(_player) && _player.Attributes?.HP != null)
        {
            int currentHp = _player.Attributes.HP.CurrentHP;
            int maxHp = _player.Attributes.HP.MaxHP;
            _hpBar.MaxValue = maxHp;
            _hpBar.Value = currentHp;
            _hpLabel.Text = $"HP: {currentHp} / {maxHp}";

            var fgStyle = _hpBar.GetStylebox("fg") as StyleBoxFlat;
            if (fgStyle != null)
            {
                float ratio = (float)currentHp / maxHp;
                if (ratio > 0.5f) { fgStyle.BgColor = new Color(0.1f, 0.8f, 0.1f, 1f); } // Green
                else if (ratio > 0.25f) { fgStyle.BgColor = new Color(0.9f, 0.7f, 0.1f, 1f); } // Orange/Yellow
                else { fgStyle.BgColor = new Color(0.9f, 0.1f, 0.1f, 1f); } // Red
            }
        }
    }
}