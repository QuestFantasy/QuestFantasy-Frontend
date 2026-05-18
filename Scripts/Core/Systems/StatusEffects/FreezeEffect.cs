using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Freeze effect: stuns the target AND reduces its stats (reserved for future use).
    /// Combines the behavior of Stun (complete freeze) with a cold visual.
    /// Applied with an icy blue color overlay.
    /// </summary>
    public class FreezeEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Freeze;

        /// <summary>Icy blue — classic freeze visual.</summary>
        public override Color OverlayColor => new Color(0.4f, 0.75f, 1f, 1f);

        /// <param name="duration">Freeze duration in seconds.</param>
        public FreezeEffect(float duration) : base(duration) { }

        public override void OnApply(Character target)
        {
            target.IsStunned = true;
            GD.Print($"[StatusEffect] {target.EntityName} is frozen for {Duration}s!");
        }

        public override void OnExpire(Character target)
        {
            target.IsStunned = false;
            GD.Print($"[StatusEffect] {target.EntityName} thawed out.");
        }
    }
}