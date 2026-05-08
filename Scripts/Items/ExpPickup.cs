using Godot;

using QuestFantasy.Characters;

public class ExpPickup : Node2D
{
    // Default 10 EXP per green dot
    public int ExpAmount = 10;

    private float _bobTime = 0f;
    private readonly float _bobSpeed = 5f;
    private readonly float _bobHeight = 5f;
    private Vector2 _basePosition;
    private Player _player;

    private bool _isMagnetic = false;
    private float _magnetSpeed = 200f; // px/sec

    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public override void _Ready()
    {
        _basePosition = Position;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 4f, new Color(0.2f, 0.9f, 0.2f));
        DrawCircle(Vector2.Zero, 6f, new Color(0.2f, 0.9f, 0.2f, 0.4f));
    }

    public override void _Process(float delta)
    {
        if (_player != null && IsInstanceValid(_player))
        {
            float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);

            // Radius for magnet ~ 80 pixels (about 1x character size)
            if (dist < 80f)
            {
                _isMagnetic = true;
            }

            if (_isMagnetic)
            {
                // Move towards player
                var dir = GlobalPosition.DirectionTo(_player.GlobalPosition);
                GlobalPosition += dir * _magnetSpeed * delta;
                _magnetSpeed += 500f * delta; // Accelerate over time

                if (dist < 15f) // Actual consume radius
                {
                    _player.GainExperience(ExpAmount);
                    QueueFree();
                    return;
                }
            }
            else
            {
                // Normal bob
                _bobTime += delta * _bobSpeed;
                Position = _basePosition + new Vector2(0, Mathf.Sin(_bobTime) * _bobHeight);
            }
        }
        else
        {
            // Normal bob if no player
            _bobTime += delta * _bobSpeed;
            Position = _basePosition + new Vector2(0, Mathf.Sin(_bobTime) * _bobHeight);
        }

        Update();
    }

    public override void _EnterTree()
    {
        _basePosition = Position;
    }
}