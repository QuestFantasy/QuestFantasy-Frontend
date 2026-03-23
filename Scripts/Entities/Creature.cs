using Godot;

public class Creature : Sprite
{
    public string EntityName { get; private set; }
    public virtual void Move(Vector2 direction, float speed)
    {
    }
    public virtual void Interact()
    {
    }
}