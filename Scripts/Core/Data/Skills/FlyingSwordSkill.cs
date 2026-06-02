using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// A returning projectile skill for the Warrior.
    /// Throws a sword that pierces through enemies, reaches max range, 
    /// and then flies back to the player, damaging enemies again.
    /// </summary>
    public class FlyingSwordSkill : Attributes.Skills
    {
        public FlyingSwordSkill()
        {
            Name = "Flying Sword";
            Description = "Throw a sword that pierces enemies and returns to you.";
        }

        public override float MaxRange => 250f;

        public override float GetCooldownDuration() => 1.5f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            SkillProjectileSpawner.SpawnFlyingSword(
                player,
                target,
                MaxRange);
        }
    }
}