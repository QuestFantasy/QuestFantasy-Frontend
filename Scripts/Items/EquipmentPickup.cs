using System;

using Godot;

public class EquipmentPickup : Area2D
{
    public object ItemData;

    [Export]
    public float SpriteScale = 0.5f;

    private Sprite _sprite;
    private CollisionShape2D _shape;
    private ulong _pressStartMs = 0;
    private const int LONG_PRESS_MS = 500;

    public override void _Ready()
    {
        // Sprite
        _sprite = new Sprite();
        if (ItemData is QuestFantasy.Core.Data.Items.Equipment ed && ed.Sprite != null)
        {
            _sprite.Texture = ed.Sprite;
        }
        else if (ItemData is QuestFantasy.Core.Data.Items.Weapon wd && wd.Sprite != null)
        {
            _sprite.Texture = wd.Sprite;
        }
        _sprite.Centered = true;
        _sprite.Scale = new Vector2(SpriteScale, SpriteScale);
        AddChild(_sprite);

        // Collision shape based on sprite size
        _shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        if (_sprite.Texture != null)
        {
            rect.Extents = new Vector2(_sprite.Texture.GetWidth() * 0.5f * SpriteScale, _sprite.Texture.GetHeight() * 0.5f * SpriteScale);
        }
        else
        {
            rect.Extents = new Vector2(16, 16);
        }
        _shape.Shape = rect;
        AddChild(_shape);

        // Connect signals
        Connect("mouse_entered", this, nameof(OnMouseEntered));
        Connect("mouse_exited", this, nameof(OnMouseExited));
        Connect("input_event", this, nameof(OnInputEvent));
    }

    private void OnMouseEntered()
    {
        if (ItemData != null)
            EquipmentPreview.Instance?.ShowPreview(ItemData, GlobalPosition);
    }

    private void OnMouseExited()
    {
        EquipmentPreview.Instance?.HidePreview();
    }

    public void OnInputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.Pressed)
            {
                _pressStartMs = OS.GetTicksMsec();
            }
            else
            {
                if (_pressStartMs > 0)
                {
                    var held = (long)(OS.GetTicksMsec() - _pressStartMs);
                    if (held >= LONG_PRESS_MS)
                    {
                        EquipmentPreview.Instance?.ShowPreview(ItemData, GlobalPosition);
                    }
                }
                _pressStartMs = 0;
            }
        }

        if (@event is InputEventScreenTouch st)
        {
            if (st.Pressed)
            {
                _pressStartMs = OS.GetTicksMsec();
            }
            else
            {
                if (_pressStartMs > 0)
                {
                    var held = (long)(OS.GetTicksMsec() - _pressStartMs);
                    if (held >= LONG_PRESS_MS)
                    {
                        EquipmentPreview.Instance?.ShowPreview(ItemData, GlobalPosition);
                    }
                }
                _pressStartMs = 0;
            }
        }
    }
}