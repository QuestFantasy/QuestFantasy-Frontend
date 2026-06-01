using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Knight explosion skill for the Warrior.
    /// Creates an expanding explosion at the player's location that continuously
    /// damages enemies within its expanding radius.
    /// </summary>
    public class KnightExploseSkill : Attributes.Skills
    {
        public KnightExploseSkill()
        {
            Name = "Knight Explosion";
            Description = "Create an expanding explosion at your location that damages enemies inside it repeatedly.";
        }

        public override float MaxRange => 0f; // Centered on player

        public override float GetCooldownDuration() => 30.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            // Spawn knight explosion at player location
            SkillProjectileSpawner.SpawnKnightExplosion(player);
        }
    }
}