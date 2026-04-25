using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    public class PlayerInputHandler
    {
        private bool _lastMouseButtonState = false;

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

        public bool IsInteractPressed()
        {
            return Input.IsActionJustPressed("interact");
        }

        /// <summary>
        /// Check if skill activation input is triggered (left mouse button - single click detection)
        /// </summary>
        public bool IsSkillActivationPressed()
        {
            bool currentMouseState = Input.IsMouseButtonPressed(1);
            bool justPressed = currentMouseState && !_lastMouseButtonState;
            _lastMouseButtonState = currentMouseState;
            return justPressed;
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