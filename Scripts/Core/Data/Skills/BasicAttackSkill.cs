using Godot;
using QuestFantasy.Characters;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Basic melee attack skill.
    /// Deals damage based on attacker's attack stat and defender's defense.
    /// Can be used as foundation for more complex skills.
    /// </summary>
    public class BasicAttackSkill : Attributes.Skills
    {
        public BasicAttackSkill()
        {
            Name = "Basic Attack";
            Description = "A basic melee attack. Attacks nearby enemies within range.";
        }

        /// <summary>
        /// Attack range in pixels. Default is 50 pixels.
        /// </summary>
        public override float MaxRange => 50f;

        /// <summary>
        /// Cooldown duration in seconds.
        /// </summary>
        public override float GetCooldownDuration() => 0.5f;

        /// <summary>
        /// Calculate damage and apply to target.
        /// Damage formula: (Attacker ATK - Defender DEF) * random variance
        /// </summary>
        public override void Effect(Player player, Character target)
        {
            if (player == null || target == null)
                return;

            // Calculate base damage
            int attackerAtk = player.Attributes?.TotalAtk ?? 1;
            int defenderDef = target.Attributes?.TotalDef ?? 0;
            
            int baseDamage = Mathf.Max(1, attackerAtk - defenderDef);
            
            // Add damage variance (±10%)
            float variance = GD.Randf() * 0.2f + 0.9f; // 0.9 to 1.1
            int finalDamage = Mathf.RoundToInt(baseDamage * variance);

            // TODO: Apply damage to target using HP system
            GD.Print($"{player.EntityName} attacks {target.EntityName} for {finalDamage} damage!");
        }
    }
}
