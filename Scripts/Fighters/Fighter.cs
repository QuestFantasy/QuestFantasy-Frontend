public class Fighter : Creature
{
    public int Level { get; private set; }
    public Status CurrentStatus { get; set; }
    public Abilities Abilities;
    public Element Element { get; set; }
    public HP HP;
    public Attributes Attributes;
    public virtual void UpdateAttributes()
    {}
}