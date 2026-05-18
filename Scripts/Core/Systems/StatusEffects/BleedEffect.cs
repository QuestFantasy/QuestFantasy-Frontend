using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Bleed effect: deals periodic physical damage every second.
    /// Applied with a deep red color overlay.
    /// </summary>
    public class BleedEffect : StatusEffect
    {
        private readonly float _damagePerSecond;
        private float _tickAccumulator;

        public override StatusEffectType EffectType => StatusEffectType.Bleed;

        /// <summary>Deep red — signals bleeding wound.</summary>
        public override Color OverlayColor => new Color(0.85f, 0.08f, 0.08f, 1f);

        /// <param name="duration">Total bleed duration in seconds.</param>
        /// <param name="damagePerSecond">Physical damage dealt per second.</param>
        public BleedEffect(float duration, float damagePerSecond) : base(duration)
        {
            _damagePerSecond = damagePerSecond;
        }

        public override void OnApply(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName} is bleeding! ({_damagePerSecond} dps for {Duration}s)");
        }

        public override void OnTick(Character target, float delta)
        {
            _tickAccumulator += delta;
            if (_tickAccumulator < 1f) return;

            _tickAccumulator -= 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_damagePerSecond));
            target.TakeDamage(damage);
            GD.Print($"[StatusEffect] Bleed tick — {target.EntityName} takes {damage} physical damage. HP={target.Attributes?.HP?.CurrentHP}");
        }

        public override void OnExpire(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName}'s bleed stopped.");
        }
    }
}