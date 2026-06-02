using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Roundhouse slash skill for the Warrior.
    /// A spinning attack that hits all nearby enemies.
    /// </summary>
    public class RoundhouseSlashSkill : Attributes.Skills
    {
        private const float DAMAGE_VARIANCE_MIN = 0.85f;
        private const float DAMAGE_VARIANCE_MAX = 1.15f;
        private const float DAMAGE_REDUCTION_FACTOR = 0.5f;

        public RoundhouseSlashSkill()
        {
            Name = "Roundhouse Slash";
            Description = "Spin and strike all nearby enemies with a powerful slash.";
        }

        public override float MaxRange => 80f;

        public override float GetCooldownDuration() => 2.0f;

        public override void Effect(Player player, Character target)
        {
            if (player == null)
            {
                return;
            }

            int attackerAtk = player.Attributes?.EffectiveAtk ?? 1;

            // Spawn spinning roundhouse slash visual effect
            SkillProjectileSpawner.SpawnTemporaryVisualEffect(
                player,
                "res://Assets/SkillAnimation/roundhouse-slash.png",
                player.GlobalPosition,
                0.3f,
                0.4f,
                0.5f,
                15f
            );

            // Get all Character objects in the scene tree and damage those in range
            var root = player.GetTree()?.Root;
            if (root != null)
            {
                var characters = GetAllNodesOfType<Character>(root);
                foreach (var enemy in characters)
                {
                    if (enemy == null || enemy == player || enemy.Attributes?.HP?.IsAlive != true)
                    {
                        continue;
                    }

                    float distance = player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                    if (distance <= MaxRange)
                    {
                        int defenderDef = enemy.Attributes?.EffectiveDef ?? 0;
                        int baseDamage = Mathf.Max(1, attackerAtk - Mathf.FloorToInt(defenderDef * DAMAGE_REDUCTION_FACTOR));
                        float variance = GD.Randf() * (DAMAGE_VARIANCE_MAX - DAMAGE_VARIANCE_MIN) + DAMAGE_VARIANCE_MIN;
                        int finalDamage = Mathf.RoundToInt(baseDamage * variance);

                        enemy.TakeDamage(finalDamage);
                        GD.Print($"[COMBAT] {player.EntityName} performs Roundhouse Slash on {enemy.EntityName} for {finalDamage} damage!");
                    }
                }
            }
        }

        private System.Collections.Generic.List<T> GetAllNodesOfType<T>(Node root) where T : Node
        {
            var result = new System.Collections.Generic.List<T>();
            if (root is T tNode)
            {
                result.Add(tNode);
            }

            foreach (Node child in root.GetChildren())
            {
                result.AddRange(GetAllNodesOfType<T>(child));
            }

            return result;
        }
    }
}