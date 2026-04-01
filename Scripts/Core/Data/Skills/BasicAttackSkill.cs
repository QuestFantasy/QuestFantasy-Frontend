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
        /// <summary>
        /// Damage formula parameters
        /// </summary>
        private const float DAMAGE_VARIANCE_MIN = 0.9f;  // -10%
        private const float DAMAGE_VARIANCE_MAX = 1.1f;  // +10%
        private const float DAMAGE_REDUCTION_FACTOR = 0.5f; // Defense reduces damage

        public BasicAttackSkill()
        {
            Name = "Basic Attack";
            Description = "A basic melee attack. Attacks nearby enemies within range.";
        }

        /// <summary>
        /// Attack range in pixels.
        /// </summary>
        public override float MaxRange => 60f;

        /// <summary>
        /// Cooldown duration in seconds.
        /// </summary>
        public override float GetCooldownDuration() => 0.3f;

        /// <summary>
        /// Calculate and apply damage to target.
        /// Damage formula: (Attacker ATK - Defender DEF × 0.5) × random variance
        /// If target is null, perform an empty swing (no damage dealt).
        /// </summary>
        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                GD.PrintErr("BasicAttackSkill: Cannot execute attack with null player");
                return;
            }

            // Allow empty swing if no target
            if (target == null)
            {
                GD.Print("[BasicAttackSkill] Empty swing - no targets in range");
                return;
            }

            // Get attacker and defender stats
            int attackerAtk = player.Attributes?.TotalAtk ?? 1;
            int defenderDef = target.Attributes?.TotalDef ?? 0;

            // Calculate base damage: Defense reduces damage by 50% of its value
            int baseDamage = Mathf.Max(1, attackerAtk - Mathf.FloorToInt(defenderDef * DAMAGE_REDUCTION_FACTOR));

            // Add damage variance (±10%)
            float variance = GD.Randf() * (DAMAGE_VARIANCE_MAX - DAMAGE_VARIANCE_MIN) + DAMAGE_VARIANCE_MIN;
            int finalDamage = Mathf.RoundToInt(baseDamage * variance);

            // Apply damage to target
            if (target.Attributes?.HP != null)
            {
                target.Attributes.HP.TakeDamage(finalDamage);
                GD.Print($"[COMBAT] {player.EntityName} attacks {target.EntityName} for {finalDamage} damage! Target HP: {target.Attributes.HP.CurrentHP}/{target.Attributes.HP.MaxHP}");

                // Check if target died
                if (!target.Attributes.HP.IsAlive)
                {
                    GD.Print($"[COMBAT] {target.EntityName} has been defeated!");
                }
            }
            else
            {
                GD.PrintErr($"BasicAttackSkill: Target {target.EntityName} has no HP system");
            }
        }
    }
}