using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Basic ranged arrow attack for the adventurer.
    /// </summary>
    public class BowAttackSkill : Attributes.Skills
    {
        public BowAttackSkill()
        {
            Name = "Bow Attack";
            Description = "Shoot an arrow in the aimed direction.";
        }

        public override float MaxRange => 260f;

        public override float GetCooldownDuration() => 0.6f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnArrow(player, target, MaxRange);
        }
    }
}