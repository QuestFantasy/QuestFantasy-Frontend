using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Burn effect: deals periodic fire damage every second AND reduces the target's ATK.
    /// - Tick damage: configurable DPS (applied once per second)
    /// - ATK debuff: multiplied by <see cref="GameConstants.BURN_ATK_MODIFIER"/> for the full duration
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
            // Apply ATK reduction for the burn duration
            if (target.Attributes != null)
            {
                target.Attributes.AtkModifier = GameConstants.BURN_ATK_MODIFIER;
            }

            GD.Print($"[StatusEffect] {target.EntityName} is burning! " +
                     $"ATK reduced to {GameConstants.BURN_ATK_MODIFIER * 100f:F0}%, " +
                     $"{_damagePerSecond} dps for {Duration}s");
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
            // Restore ATK modifier to normal
            if (target.Attributes != null)
            {
                target.Attributes.AtkModifier = 1f;
            }

            GD.Print($"[StatusEffect] {target.EntityName}'s burn faded. ATK restored.");
        }
    }
}