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
        _bobTime += delta * _bobSpeed;
        Position = _basePosition + new Vector2(0, Mathf.Sin(_bobTime) * _bobHeight);
        Update();

        if (_player != null && IsInstanceValid(_player))
        {
            if (GlobalPosition.DistanceTo(_player.GlobalPosition) < 24f) // 24px pickup radius
            {
                _player.GainExperience(ExpAmount);
                QueueFree();
            }
        }
    }

    public override void _EnterTree()
    {
        _basePosition = Position;
    }
}