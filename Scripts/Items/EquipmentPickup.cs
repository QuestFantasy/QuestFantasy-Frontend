using System;

using Godot;

public class EquipmentPickup : Area2D
{
    [Signal]
    public delegate void PickupRequested(EquipmentPickup pickup);

    /// <summary>
    /// Static flag set to true when any EquipmentPickup receives a touch/click input
    /// this frame. Used by PlayerInputHandler to suppress attack activation.
    /// Reset each physics frame by the first pickup that processes.
    /// </summary>
    public static bool WasPickupTouched { get; set; }

    public object ItemData;

    [Export]
    public float SpriteScale = 0.5f;

    private Sprite _sprite;
    private CollisionShape2D _shape;
    private bool _isHovered = false;
    private ulong _pressStartMs = 0;
    private const int LONG_PRESS_MS = 500;
    private const int TAP_MAX_MS = 400; // Short tap threshold for pickup

    // Increase touch hit area for mobile - minimum 48x48
    private const float MinTouchExtent = 24f;

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

        // Collision shape based on sprite size with minimum touch target
        _shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        if (_sprite.Texture != null)
        {
            float halfW = _sprite.Texture.GetWidth() * 0.5f * SpriteScale;
            float halfH = _sprite.Texture.GetHeight() * 0.5f * SpriteScale;
            rect.Extents = new Vector2(
                Mathf.Max(halfW, MinTouchExtent),
                Mathf.Max(halfH, MinTouchExtent));
        }
        else
        {
            rect.Extents = new Vector2(MinTouchExtent, MinTouchExtent);
        }
        _shape.Shape = rect;
        AddChild(_shape);

        // Enable input picking for touch/mouse
        InputPickable = true;

        // Connect signals
        Connect("mouse_entered", this, nameof(OnMouseEntered));
        Connect("mouse_exited", this, nameof(OnMouseExited));
        Connect("input_event", this, nameof(OnInputEvent));

        SetProcessUnhandledInput(true);
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        if (ItemData != null)
            EquipmentPreview.Instance?.ShowPreview(ItemData, GlobalPosition);
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        EquipmentPreview.Instance?.HidePreview();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isHovered)
        {
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Scancode == (uint)KeyList.G)
        {
            RequestPickup();
        }
    }

    public void OnInputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        // Mark that a pickup was touched this frame to suppress attacks
        WasPickupTouched = true;

        if (@event is InputEventMouseButton mb)
        {
            // Right-click: immediate pickup (legacy)
            if (mb.ButtonIndex == (int)ButtonList.Right && mb.Pressed)
            {
                RequestPickup();
                return;
            }

            // Left-click: tap-to-pickup / long-press-to-preview
            if (mb.ButtonIndex == (int)ButtonList.Left)
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
                        else if (held < TAP_MAX_MS)
                        {
                            // Short tap = pick up
                            RequestPickup();
                        }
                    }
                    _pressStartMs = 0;
                }
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
                    else if (held < TAP_MAX_MS)
                    {
                        // Short tap = pick up
                        RequestPickup();
                    }
                }
                _pressStartMs = 0;
            }
        }
    }

    private void RequestPickup()
    {
        EmitSignal(nameof(PickupRequested), this);
    }
}