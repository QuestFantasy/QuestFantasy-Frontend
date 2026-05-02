using System;

using Godot;

namespace QuestFantasy.UI
{
    /// <summary>
    /// Mobile virtual input UI controller using CanvasLayer
    /// Provides on-screen D-pad (4-direction buttons) for mobile gameplay
    /// </summary>
    public class MobileInputUI : CanvasLayer
    {
        [Export] public float DPadSize = 80f;  // Size of each D-pad button
        [Export] public float DPadMargin = 15f; // Distance from screen edge
        [Export] public Color DPadColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        [Export] public Color DPadPressedColor = new Color(0.4f, 0.6f, 1f, 0.9f);

        // D-pad button panels
        private Panel _upButton;
        private Panel _downButton;
        private Panel _leftButton;
        private Panel _rightButton;
        private Panel _mapButton;

        // Touch tracking
        private int _activeTouchId = -1;
        private bool _isMouseDpadActive = false;
        private string _currentPressedAction = null;

        // Container control to properly handle touch events
        private Control _inputContainer;
        private bool _allowAutoShow = true;

        public static bool IsAnyDPadPressActive { get; private set; } = false;

        private const string UP_ACTION = "ui_up";
        private const string DOWN_ACTION = "ui_down";
        private const string LEFT_ACTION = "ui_left";
        private const string RIGHT_ACTION = "ui_right";
        private const string MAP_ACTION = "toggle_minimap";

        public override void _Ready()
        {
            // Set layer to render on top - use highest layer
            Layer = 1000;  // Very high to ensure it's above everything

            // Ensure visibility
            Visible = true;

            GD.Print("[MobileInputUI] _Ready() called, Layer=" + Layer + ", Visible=" + Visible);

            // Create a container for visual representation
            _inputContainer = new Control
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,  // Don't consume input - we handle it in _Input()
                Visible = true,
            };
            AddChild(_inputContainer);

            // Create D-pad buttons inside container
            CreateDPadButtons();

            // Enable input processing
            SetProcessInput(true);

            GD.Print("[MobileInputUI] Virtual D-pad initialized successfully");
        }

        public override void _EnterTree()
        {
            GD.Print("[MobileInputUI] _EnterTree() called");
        }

        public override void _ExitTree()
        {
            ReleaseAllDirections();
            IsAnyDPadPressActive = false;
            GD.Print("[MobileInputUI] _ExitTree() called");
        }

        public override void _Process(float delta)
        {
            // Only restore visibility when the UI is meant to be shown.
            if (_allowAutoShow && Visible == false)
            {
                GD.PrintErr("[MobileInputUI] Was hidden! Re-enabling...");
                Visible = true;
            }
        }

        public void ShowDPad()
        {
            _allowAutoShow = true;
            SetProcessInput(true);
            Visible = true;
        }

        public void HideDPad()
        {
            _allowAutoShow = false;
            ReleaseAllDirections();
            SetProcessInput(false);
            Visible = false;
        }

        public void ShowMapButton(bool visible)
        {
            if (_mapButton != null)
            {
                _mapButton.Visible = visible;
            }
        }

        /// <summary>
        /// Create D-pad button UI nodes
        /// </summary>
        private void CreateDPadButtons()
        {
            var buttonSize = (int)DPadSize;
            var margin = (int)DPadMargin;
            var viewport = GetViewport();
            float viewportWidth = viewport.GetVisibleRect().Size.x;
            float viewportHeight = viewport.GetVisibleRect().Size.y;

            GD.Print($"[MobileInputUI] Viewport size: {viewportWidth}x{viewportHeight}");

            // Set container size to match viewport
            _inputContainer.RectSize = new Vector2(viewportWidth, viewportHeight);
            _inputContainer.RectPosition = Vector2.Zero;

            int startX = margin;
            int startY = (int)viewportHeight - margin - buttonSize * 3;

            // Create button positions
            int centerX = startX + buttonSize;
            int centerY = startY + buttonSize;

            // UP button
            _upButton = CreateButton(UP_ACTION, centerX, startY, buttonSize, "▲");

            // DOWN button
            _downButton = CreateButton(DOWN_ACTION, centerX, startY + buttonSize * 2, buttonSize, "▼");

            // LEFT button
            _leftButton = CreateButton(LEFT_ACTION, startX, centerY, buttonSize, "◄");

            // RIGHT button
            _rightButton = CreateButton(RIGHT_ACTION, startX + buttonSize * 2, centerY, buttonSize, "►");

            // MAP button (Bottom-right corner, 0.7x size)
            int mapBtnSize = (int)(buttonSize * 1.0f);
            int offset = 0;
            _mapButton = CreateIconButton(MAP_ACTION, startX + buttonSize * 2 + offset, startY + buttonSize * 2 + offset, mapBtnSize, "res://Assets/map/map_icon.png");

            GD.Print($"[MobileInputUI] D-pad created at: UP=({centerX},{startY}), DOWN=({centerX},{startY + buttonSize * 2}), LEFT=({startX},{centerY}), RIGHT=({startX + buttonSize * 2},{centerY}), MAP=({startX + buttonSize * 2 + offset},{startY + buttonSize * 2 + offset})");
            GD.Print($"[MobileInputUI] Container size set to: {_inputContainer.RectSize}");
        }

        private Panel CreateIconButton(string action, int x, int y, int size, string iconPath)
        {
            var panel = new Panel
            {
                RectPosition = new Vector2(x, y),
                RectSize = new Vector2(size, size),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = true,
            };

            var panelStyle = new StyleBoxEmpty();

            var theme = new Theme();
            theme.SetStylebox("panel", "Panel", panelStyle);
            panel.Theme = theme;

            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture>(iconPath),
                AnchorRight = 1f,
                AnchorBottom = 1f,
                MarginLeft = 0f,
                MarginTop = 0f,
                MarginRight = 0f,
                MarginBottom = 0f,
                Expand = true,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            icon.SelfModulate = new Color(0.9f, 0.9f, 0.9f, 1f);
            panel.AddChild(icon);

            _inputContainer.AddChild(panel);
            return panel;
        }

        /// <summary>
        /// Create a single button with label
        /// </summary>
        private Panel CreateButton(string action, int x, int y, int size, string label)
        {
            var panel = new Panel
            {
                RectPosition = new Vector2(x, y),
                RectSize = new Vector2(size, size),
                MouseFilter = Control.MouseFilterEnum.Ignore,  // Ignore so we handle input in _Input()
                Visible = true,
            };

            // Set panel styling
            var panelStyle = new StyleBoxFlat
            {
                BgColor = DPadColor,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
            };

            var theme = new Theme();
            theme.SetStylebox("panel", "Panel", panelStyle);
            panel.Theme = theme;

            // Add label
            var labelControl = new Label
            {
                Text = label,
                RectPosition = Vector2.Zero,
                RectSize = new Vector2(size, size),
                Align = Label.AlignEnum.Center,
                Valign = Label.VAlign.Center,
                SelfModulate = new Color(1, 1, 1, 1),
                MouseFilter = Control.MouseFilterEnum.Ignore,  // Pass through for label
                Visible = true,
            };
            panel.AddChild(labelControl);

            // Add to container
            _inputContainer.AddChild(panel);

            GD.Print($"[MobileInputUI] Created button {label} at ({x},{y}) size {size}x{size}, Visible={panel.Visible}, Parent={_inputContainer.Name}");

            return panel;
        }

        /// <summary>
        /// Handle touch input events from container
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventScreenTouch touch)
            {
                if (touch.Pressed)
                {
                    if (TryGetActionAtPoint(touch.Position, out string action))
                    {
                        _activeTouchId = touch.Index;
                        PressDirection(action);
                        GetTree().SetInputAsHandled();
                    }
                }
                else if (touch.Index == _activeTouchId)
                {
                    _activeTouchId = -1;
                    ReleaseAllDirections();
                    GetTree().SetInputAsHandled();
                }
                return;
            }

            if (@event is InputEventScreenDrag drag)
            {
                if (drag.Index == _activeTouchId)
                {
                    if (TryGetActionAtPoint(drag.Position, out string action))
                    {
                        PressDirection(action);
                    }
                    else
                    {
                        ReleaseAllDirections();
                    }
                    GetTree().SetInputAsHandled();
                }
                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == (int)ButtonList.Left)
            {
                if (mouseButton.Pressed)
                {
                    if (TryGetActionAtPoint(mouseButton.Position, out string action))
                    {
                        _isMouseDpadActive = true;
                        PressDirection(action);
                        GetTree().SetInputAsHandled();
                    }
                }
                else if (_isMouseDpadActive)
                {
                    _isMouseDpadActive = false;
                    ReleaseAllDirections();
                    GetTree().SetInputAsHandled();
                }
                return;
            }

            if (@event is InputEventMouseMotion mouseMotion)
            {
                if (_isMouseDpadActive)
                {
                    if (TryGetActionAtPoint(mouseMotion.Position, out string action))
                    {
                        PressDirection(action);
                    }
                    else
                    {
                        ReleaseAllDirections();
                    }
                    GetTree().SetInputAsHandled();
                }
            }
        }

        private void PressDirection(string action)
        {
            if (_currentPressedAction == action)
            {
                return;
            }

            ReleaseAllDirections();

            _currentPressedAction = action;
            IsAnyDPadPressActive = true;
            Input.ActionPress(action);
            UpdateButtonVisuals(action);
            GD.Print($"[MobileInputUI] Direction pressed: {action}");
        }

        private void ReleaseAllDirections()
        {
            if (_currentPressedAction == null && !IsAnyDPadPressActive)
            {
                return;
            }

            Input.ActionRelease(UP_ACTION);
            Input.ActionRelease(DOWN_ACTION);
            Input.ActionRelease(LEFT_ACTION);
            Input.ActionRelease(RIGHT_ACTION);
            Input.ActionRelease(MAP_ACTION);

            _currentPressedAction = null;
            IsAnyDPadPressActive = false;
            UpdateButtonVisuals(null);
        }

        /// <summary>
        /// Check if touch position is inside button
        /// </summary>
        private bool IsPointInButton(Panel button, Vector2 point)
        {
            if (button == null)
                return false;
            // Button position is in screen coordinates (CanvasLayer)
            var rect = new Rect2(button.RectPosition, button.RectSize);
            bool result = rect.HasPoint(point);
            if (result)
            {
                GD.Print($"[MobileInputUI] Touch {point} hit button at {rect}");
            }
            return result;
        }

        private void UpdateButtonVisuals(string activeAction)
        {
            _upButton.SelfModulate = activeAction == UP_ACTION ? DPadPressedColor : new Color(1, 1, 1, 1);
            _downButton.SelfModulate = activeAction == DOWN_ACTION ? DPadPressedColor : new Color(1, 1, 1, 1);
            _leftButton.SelfModulate = activeAction == LEFT_ACTION ? DPadPressedColor : new Color(1, 1, 1, 1);
            _rightButton.SelfModulate = activeAction == RIGHT_ACTION ? DPadPressedColor : new Color(1, 1, 1, 1);
            _mapButton.SelfModulate = activeAction == MAP_ACTION ? DPadPressedColor : new Color(1, 1, 1, 1);
        }

        private bool TryGetActionAtPoint(Vector2 point, out string action)
        {
            if (IsPointInButton(_upButton, point))
            {
                action = UP_ACTION;
                return true;
            }

            if (IsPointInButton(_downButton, point))
            {
                action = DOWN_ACTION;
                return true;
            }

            if (IsPointInButton(_leftButton, point))
            {
                action = LEFT_ACTION;
                return true;
            }

            if (IsPointInButton(_rightButton, point))
            {
                action = RIGHT_ACTION;
                return true;
            }

            if (IsPointInButton(_mapButton, point))
            {
                action = MAP_ACTION;
                return true;
            }

            action = null;
            return false;
        }
    }
}