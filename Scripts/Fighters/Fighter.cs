public class Fighter : Creature
{
	public long Level { get; private set; }
	public Status CurrentStatus { get; set; }
	public Abilities Abilities;
	public Element Element { get; private set; }
	public HP HP;
	public Attributes Attributes;
	public virtual void UpdateAttributes()
	{}
}
