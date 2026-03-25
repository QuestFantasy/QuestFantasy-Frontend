using Godot;

public class Creature : Sprite
{
    public string EntityName { get; private set; }
    public virtual void Move()
    {
    }
    public virtual void Interact()
    {
    }
}