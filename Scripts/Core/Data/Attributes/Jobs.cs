using System.Collections.Generic;

using QuestFantasy.Core.Base;

namespace QuestFantasy.Core.Data.Attributes
{
    public class Jobs : NameAndDescription
    {
        public Abilities BaseAbilities { get; private set; }
        public List<Skills> SkillSet { get; private set; }
        // TODO: job init function
    }
}