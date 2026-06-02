using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Digit arrow skill for the Archer.
    /// Fires an arrow that passes through obstacles and enemies,
    /// traveling a set distance without being blocked.
    /// </summary>
    public class DigitArrowSkill : Attributes.Skills
    {
        public DigitArrowSkill()
        {
            Name = "Digit Arrow";
            Description = "Fire an arrow that passes through obstacles and enemies, traveling a set distance.";
        }

        public override float MaxRange => 400f;

        public override float GetCooldownDuration() => 1.2f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Spawn a digit arrow that passes through obstacles
            SkillProjectileSpawner.SpawnDigitArrow(player, target, MaxRange);
        }
    }
}