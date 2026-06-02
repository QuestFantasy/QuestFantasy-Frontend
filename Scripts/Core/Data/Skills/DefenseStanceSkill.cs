using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    public class DefenseStanceSkill : Attributes.Skills
    {
        public DefenseStanceSkill()
        {
            Name = "Defense Stance";
            Description = "Become invulnerable for 3 seconds. Any attack received triggers a counter-attack.";
        }

        public override float MaxRange => 0f;

        public override float GetCooldownDuration() => 8.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null) return;

            // Activate defense stance for 3 seconds
            player.ActivateDefenseStance(3.0f);
        }
    }
}