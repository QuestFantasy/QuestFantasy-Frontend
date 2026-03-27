using Godot;

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
        if (Input.IsKeyPressed((int)KeyList.D))
        {
            input.x += 1f;
        }
        if (Input.IsKeyPressed((int)KeyList.A))
        {
            input.x -= 1f;
        }
        if (Input.IsKeyPressed((int)KeyList.S))
        {
            input.y += 1f;
        }
        if (Input.IsKeyPressed((int)KeyList.W))
        {
            input.y -= 1f;
        }

        return input;
    }
}