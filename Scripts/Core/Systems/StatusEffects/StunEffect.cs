using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Stun effect: completely freezes a character (no movement, no attacks, no animation).
    /// Sets <see cref="Character.IsStunned"/> for the duration.
    /// Applied with a bright yellow color overlay.
    /// </summary>
    public class StunEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Stun;

        /// <summary>Bright yellow — classic stun visual cue.</summary>
        public override Color OverlayColor => new Color(1f, 0.92f, 0.15f, 1f);

        /// <param name="duration">Stun duration in seconds.</param>
        public StunEffect(float duration) : base(duration) { }

        public override void OnApply(Character target)
        {
            target.IsStunned = true;
            GD.Print($"[StatusEffect] {target.EntityName} is stunned for {Duration}s!");
        }

        public override void OnExpire(Character target)
        {
            target.IsStunned = false;
            GD.Print($"[StatusEffect] {target.EntityName}'s stun wore off.");
        }
    }
}