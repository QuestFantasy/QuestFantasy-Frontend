using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Burn effect: deals periodic fire damage every second.
    /// Applied with an orange color overlay.
    /// </summary>
    public class BurnEffect : StatusEffect
    {
        private readonly float _damagePerSecond;
        private float _tickAccumulator;

        public override StatusEffectType EffectType => StatusEffectType.Burn;

        /// <summary>Vivid orange — clearly signals fire damage.</summary>
        public override Color OverlayColor => new Color(1f, 0.45f, 0.05f, 1f);

        /// <param name="duration">Total burn duration in seconds.</param>
        /// <param name="damagePerSecond">Fire damage dealt per second.</param>
        public BurnEffect(float duration, float damagePerSecond) : base(duration)
        {
            _damagePerSecond = damagePerSecond;
        }

        public override void OnApply(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName} is burning! ({_damagePerSecond} dps for {Duration}s)");
        }

        public override void OnTick(Character target, float delta)
        {
            _tickAccumulator += delta;
            if (_tickAccumulator < 1f) return;

            _tickAccumulator -= 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_damagePerSecond));
            target.TakeDamage(damage);
            GD.Print($"[StatusEffect] Burn tick — {target.EntityName} takes {damage} fire damage. HP={target.Attributes?.HP?.CurrentHP}");
        }

        public override void OnExpire(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName}'s burn faded.");
        }
    }
}