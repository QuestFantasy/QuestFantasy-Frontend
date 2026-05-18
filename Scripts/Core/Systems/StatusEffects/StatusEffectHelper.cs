using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Static utility for applying status effects with a probability check.
    /// Skills and projectiles should use this as the single entry point for
    /// triggering effects on hit, keeping effect-application logic centralised.
    ///
    /// Usage example:
    /// <code>
    ///   StatusEffectHelper.TryApplyWithChance(
    ///       target,
    ///       () => new StunEffect(GameConstants.BASIC_ATTACK_STUN_DURATION),
    ///       GameConstants.BASIC_ATTACK_STUN_CHANCE);
    /// </code>
    /// Accepting a factory delegate ensures each target gets a fresh effect instance
    /// (important for stateful effects like BleedEffect that track tick accumulators).
    /// </summary>
    public static class StatusEffectHelper
    {
        /// <summary>
        /// Roll against <paramref name="chance"/> and, on success, create a new effect
        /// via <paramref name="effectFactory"/> and apply it to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The character to receive the effect.</param>
        /// <param name="effectFactory">
        ///     A factory delegate that produces a fresh <see cref="StatusEffect"/> instance.
        ///     Using a factory prevents sharing state between multiple targets.
        /// </param>
        /// <param name="chance">Probability in [0, 1] that the effect is applied.</param>
        /// <returns>True if the effect was applied.</returns>
        public static bool TryApplyWithChance(
            Character target,
            System.Func<StatusEffect> effectFactory,
            float chance)
        {
            if (target == null || effectFactory == null) return false;
            if (target.EffectManager == null) return false;
            if (chance <= 0f) return false;

            if (GD.Randf() >= chance) return false;

            StatusEffect effect = effectFactory();
            if (effect == null) return false;

            target.EffectManager.Apply(effect, target);
            return true;
        }
    }
}