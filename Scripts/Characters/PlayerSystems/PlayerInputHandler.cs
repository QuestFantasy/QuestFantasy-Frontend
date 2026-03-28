using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    public class PlayerInputHandler
    {
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

        public bool IsRespawnPressed()
        {
            return Input.IsActionJustPressed("ui_cancel");
        }

        public bool IsInteractPressed()
        {
            return Input.IsActionJustPressed("interact");
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
    }
}
