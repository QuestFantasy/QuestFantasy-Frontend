using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
    public class Character : Creature
    {
        public long Level { get; private set; }
        public Status CurrentStatus { get; set; }
        public Abilities Abilities;
        public Element Element { get; private set; }
        public Attributes Attributes;
        public virtual void UpdateAttributes()
        { }
    }
}