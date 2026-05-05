using Godot;

namespace QuestFantasy.UI
{
    /// <summary>
    /// A floating interaction button that appears near interactable objects
    /// (treasure chests, NPCs, teleporters) for mobile-friendly touch input.
    /// The button follows the object's screen position so the player can tap it
    /// without needing a keyboard key.
    /// </summary>
    public class InteractionButtonUI : CanvasLayer
    {
        public static InteractionButtonUI Instance { get; private set; }

        /// <summary>
        /// True during the frame in which the button was pressed.
        /// Consumed by <see cref="ConsumePress"/>.
        /// </summary>
        public static bool WasJustPressed { get; private set; }

        /// <summary>
        /// True when the interaction button is currently visible on screen.
        /// Used by PlayerInputHandler to suppress attack activation.
        /// </summary>
        public static bool IsButtonVisible { get; private set; }

        private const float ButtonWidth = 110f;
        private const float ButtonHeight = 48f;
        private const float OffsetY = -40f; // Above the object
        private const float ScreenPadding = 8f;

        private Button _button;
        private bool _pressed;
        private Vector2 _worldTarget = Vector2.Zero;
        private bool _tracking = false;
        private int _framesSinceLastShow = 0;

        public override void _Ready()
        {
            Instance = this;
            Layer = 999; // High layer to render on top

            _button = new Button
            {
                Text = "Interact",
                RectMinSize = new Vector2(ButtonWidth, ButtonHeight),
                RectSize = new Vector2(ButtonWidth, ButtonHeight),
                MouseFilter = Control.MouseFilterEnum.Stop,
                FocusMode = Control.FocusModeEnum.None,
                Visible = false,
            };

            // Style the button with a vibrant look
            var normalStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.16f, 0.55f, 0.94f, 0.92f),
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 14,
                CornerRadiusBottomLeft = 14,
                CornerRadiusBottomRight = 14,
                BorderColor = new Color(0.85f, 0.92f, 1f, 0.95f),
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            };
            _button.AddStyleboxOverride("normal", normalStyle);

            var hoverStyle = normalStyle.Duplicate() as StyleBoxFlat;
            if (hoverStyle != null)
            {
                hoverStyle.BgColor = new Color(0.22f, 0.62f, 1f, 0.96f);
                _button.AddStyleboxOverride("hover", hoverStyle);
            }

            var pressedStyle = normalStyle.Duplicate() as StyleBoxFlat;
            if (pressedStyle != null)
            {
                pressedStyle.BgColor = new Color(0.10f, 0.42f, 0.82f, 0.98f);
                _button.AddStyleboxOverride("pressed", pressedStyle);
            }

            _button.AddStyleboxOverride("focus", new StyleBoxEmpty());

            _button.AddColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            _button.AddColorOverride("font_color_hover", new Color(1f, 1f, 1f, 1f));
            _button.AddColorOverride("font_color_pressed", new Color(0.9f, 0.95f, 1f, 1f));

            _button.Connect("pressed", this, nameof(OnButtonPressed));

            AddChild(_button);

            IsButtonVisible = false;
            PauseMode = PauseModeEnum.Process;
        }

        public override void _Process(float delta)
        {
            // Clear press flag at the end of each frame cycle
            if (WasJustPressed && !_pressed)
            {
                WasJustPressed = false;
            }
            _pressed = false;

            // Auto-hide if Show() hasn't been called recently
            if (_tracking)
            {
                _framesSinceLastShow++;
                if (_framesSinceLastShow > 2)
                {
                    HideButton();
                }
            }

            if (!_tracking || !_button.Visible)
            {
                return;
            }

            UpdateScreenPosition();
        }

        /// <summary>
        /// Show the interaction button near a world-space position.
        /// Should be called every frame while the object is interactable.
        /// </summary>
        public void Show(string label, Vector2 worldPosition)
        {
            _button.Text = label;
            _worldTarget = worldPosition;
            _tracking = true;
            _framesSinceLastShow = 0;

            if (!_button.Visible)
            {
                _button.Visible = true;
                IsButtonVisible = true;
            }

            UpdateScreenPosition();
        }

        /// <summary>
        /// Hide the interaction button.
        /// </summary>
        public void HideButton()
        {
            _button.Visible = false;
            _tracking = false;
            IsButtonVisible = false;
        }

        /// <summary>
        /// Check if the button was pressed this frame. Does not consume the press,
        /// allowing multiple scripts to react (matching keyboard 'F' behavior).
        /// </summary>
        public static bool IsPressed()
        {
            return WasJustPressed;
        }

        private void OnButtonPressed()
        {
            WasJustPressed = true;
            _pressed = true;
            GD.Print("[InteractionButtonUI] Button pressed");
        }

        private void UpdateScreenPosition()
        {
            // Convert world position to screen position using the current camera
            var viewport = GetViewport();
            if (viewport == null)
            {
                return;
            }

            var canvas = viewport.CanvasTransform;
            Vector2 screenPos = canvas * _worldTarget;

            // Place button above the object
            float x = screenPos.x - ButtonWidth * 0.5f;
            float y = screenPos.y + OffsetY - ButtonHeight;

            // Clamp to screen bounds
            Vector2 viewSize = viewport.GetVisibleRect().Size;
            x = Mathf.Clamp(x, ScreenPadding, viewSize.x - ButtonWidth - ScreenPadding);
            y = Mathf.Clamp(y, ScreenPadding, viewSize.y - ButtonHeight - ScreenPadding);

            _button.RectPosition = new Vector2(x, y);
        }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                Instance = null;
                IsButtonVisible = false;
                WasJustPressed = false;
            }
        }
    }
}