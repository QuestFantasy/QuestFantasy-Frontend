using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Base;

namespace QuestFantasy.Core.Data.Attributes
{
    /// <summary>
    /// Represents a character job/class. Defines base abilities and available skills.
    /// Each job provides different stat bonuses and skill progression.
    /// </summary>
    public class Jobs : NameAndDescription
    {
        public Abilities BaseAbilities { get; private set; }
        public List<Skills> SkillSet { get; private set; } = new List<Skills>();

        public Jobs()
        {
            BaseAbilities = new Abilities();
        }

        /// <summary>
        /// Initialize job with specific base abilities
        /// </summary>
        public void Initialize(string name, string description, int atk, int def, int spd, int vit)
        {
            Name = name;
            Description = description;
            BaseAbilities.Set(atk, def, spd, vit);
        }

        /// <summary>
        /// Add a skill to this job's skill set
        /// </summary>
        public void AddSkill(Skills skill)
        {
            if (skill == null)
            {
                GD.PrintErr($"[Jobs] {Name}: Cannot add null skill");
                return;
            }

            if (!SkillSet.Contains(skill))
            {
                SkillSet.Add(skill);
                GD.Print($"[Jobs] {Name}: Added skill {skill.Name}");
            }
        }

        /// <summary>
        /// Get a copy of this job's base abilities for a new character
        /// </summary>
        public Abilities GetBaseAbilitiesCopy()
        {
            var copy = new Abilities();
            copy.Set(BaseAbilities.Atk, BaseAbilities.Def, BaseAbilities.Spd, BaseAbilities.Vit);
            return copy;
        }
    }
}