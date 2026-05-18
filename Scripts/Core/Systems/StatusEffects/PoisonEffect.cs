using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Poison effect: deals periodic poison damage every second (reserved for future use).
    /// Applied with a sickly green-purple color overlay.
    /// </summary>
    public class PoisonEffect : StatusEffect
    {
        private readonly float _damagePerSecond;
        private float _tickAccumulator;

        public override StatusEffectType EffectType => StatusEffectType.Poison;

        /// <summary>Sickly green-purple — classic poison visual.</summary>
        public override Color OverlayColor => new Color(0.55f, 0.2f, 0.75f, 1f);

        /// <param name="duration">Total poison duration in seconds.</param>
        /// <param name="damagePerSecond">Poison damage per second.</param>
        public PoisonEffect(float duration, float damagePerSecond) : base(duration)
        {
            _damagePerSecond = damagePerSecond;
        }

        public override void OnApply(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName} is poisoned! ({_damagePerSecond} dps for {Duration}s)");
        }

        public override void OnTick(Character target, float delta)
        {
            _tickAccumulator += delta;
            if (_tickAccumulator < 1f) return;

            _tickAccumulator -= 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_damagePerSecond));
            target.TakeDamage(damage);
            GD.Print($"[StatusEffect] Poison tick — {target.EntityName} takes {damage} poison damage. HP={target.Attributes?.HP?.CurrentHP}");
        }

        public override void OnExpire(Character target)
        {
            GD.Print($"[StatusEffect] {target.EntityName}'s poison wore off.");
        }
    }
}