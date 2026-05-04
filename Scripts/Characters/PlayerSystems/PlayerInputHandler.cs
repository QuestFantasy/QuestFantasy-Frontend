using Godot;

using QuestFantasy.UI;

namespace QuestFantasy.Characters.PlayerSystems
{
    public class PlayerInputHandler
    {
        private bool _lastMouseButtonState = false;
        private int _uiSkillActivationIndex = -1;

        // Mobile touch pad state
        private bool _isTouchPadActive = false;
        private Vector2 _touchPadCenter = Vector2.Zero;
        private float _touchPadRadius = 0f;

        public void EnsureInteractInputAction()
        {
            if (!InputMap.HasAction("interact"))
            {
                InputMap.AddAction("interact");
            }

            if (InputMap.GetActionList("interact").Count > 0)
            {
                return;
            }

            var interactKey = new InputEventKey();
            interactKey.Scancode = (uint)KeyList.F;
            InputMap.ActionAddEvent("interact", interactKey);
        }

        public void EnsureSkillInputActions()
        {
            EnsureSingleKeyAction("skill_slot_1", KeyList.Key1);
            EnsureSingleKeyAction("skill_slot_2", KeyList.Key2);
            EnsureSingleKeyAction("skill_slot_3", KeyList.Key3);
        }

        public bool IsRespawnPressed()
        {
            return Input.IsActionJustPressed("ui_cancel");
        }

        private int _lastInteractFrame = -1;

        /// <summary>
        /// Check and consume the interaction input (F key).
        /// Returns true only for the first caller in a frame.
        /// </summary>
        public bool IsInteractPressed()
        {
            if (Input.IsActionJustPressed("interact"))
            {
                int currentFrame = (int)Engine.GetIdleFrames();
                if (_lastInteractFrame != currentFrame)
                {
                    _lastInteractFrame = currentFrame;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if skill activation input is triggered (left mouse button - single click detection).
        /// Suppressed when D-pad is active, HUD is hovered, the interaction button is visible,
        /// or an equipment pickup was just touched.
        /// </summary>
        public bool IsSkillActivationPressed()
        {
            // Suppress attacks when the interaction button is showing
            // (player is near an interactable object)
            if (InteractionButtonUI.IsButtonVisible)
            {
                _lastMouseButtonState = Input.IsMouseButtonPressed(1);
                return false;
            }

            // Suppress attacks when a pickup was just touched
            if (EquipmentPickup.WasPickupTouched)
            {
                _lastMouseButtonState = Input.IsMouseButtonPressed(1);
                return false;
            }

            if (MobileInputUI.IsAnyDPadPressActive)
            {
                _lastMouseButtonState = Input.IsMouseButtonPressed(1);
                return false;
            }

            if (PlayerHud.IsMouseOverHud)
            {
                _lastMouseButtonState = Input.IsMouseButtonPressed(1);
                return false;
            }

            bool currentMouseState = Input.IsMouseButtonPressed(1);
            bool justPressed = currentMouseState && !_lastMouseButtonState;
            _lastMouseButtonState = currentMouseState;
            return justPressed;
        }

        /// <summary>
        /// Queue a skill activation request from the UI (e.g. tapping a skill slot).
        /// This will both select and activate the skill on the next combat frame.
        /// </summary>
        public void RequestSkillActivation(int skillIndex)
        {
            _uiSkillActivationIndex = skillIndex;
        }

        /// <summary>
        /// Consume and return any pending UI skill activation request.
        /// Returns the skill index to activate, or -1 if none.
        /// </summary>
        public int ConsumeUiSkillActivation()
        {
            int index = _uiSkillActivationIndex;
            _uiSkillActivationIndex = -1;
            return index;
        }

        /// <summary>
        /// Consume the current mouse button state to prevent future detection in this frame
        /// </summary>
        public void ConsumeSkillActivationInput()
        {
            _lastMouseButtonState = Input.IsMouseButtonPressed(1);
        }

        public Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.Zero;
            if (Input.IsActionPressed("ui_right"))
            {
                input.x += 1f;
            }
            if (Input.IsActionPressed("ui_left"))
            {
                input.x -= 1f;
            }
            if (Input.IsActionPressed("ui_down"))
            {
                input.y += 1f;
            }
            if (Input.IsActionPressed("ui_up"))
            {
                input.y -= 1f;
            }

            return input;
        }

        /// <summary>
        /// Update virtual touch pad state (called from MobileInputUI)
        /// </summary>
        public void SetTouchPadActive(bool active, Vector2 center, float radius)
        {
            _isTouchPadActive = active;
            _touchPadCenter = center;
            _touchPadRadius = radius;
        }

        /// <summary>
        /// Get movement input from virtual touch pad
        /// </summary>
        public Vector2 GetTouchPadInput()
        {
            if (!_isTouchPadActive)
            {
                return Vector2.Zero;
            }

            Vector2 input = Vector2.Zero;
            if (Input.IsActionPressed("ui_right"))
            {
                input.x += 1f;
            }
            if (Input.IsActionPressed("ui_left"))
            {
                input.x -= 1f;
            }
            if (Input.IsActionPressed("ui_down"))
            {
                input.y += 1f;
            }
            if (Input.IsActionPressed("ui_up"))
            {
                input.y -= 1f;
            }

            return input;
        }

        public int ConsumeSelectedSkillIndex()
        {
            if (Input.IsActionJustPressed("skill_slot_1"))
            {
                return 0;
            }

            if (Input.IsActionJustPressed("skill_slot_2"))
            {
                return 1;
            }

            if (Input.IsActionJustPressed("skill_slot_3"))
            {
                return 2;
            }

            return -1;
        }

        private static void EnsureSingleKeyAction(string actionName, KeyList key)
        {
            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName);
            }

            if (InputMap.GetActionList(actionName).Count > 0)
            {
                return;
            }

            var inputEvent = new InputEventKey
            {
                Scancode = (uint)key,
            };
            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }
}