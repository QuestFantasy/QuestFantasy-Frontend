using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Available status effect types. Add new entries here when creating new effects.
    /// </summary>
    public enum StatusEffectType
    {
        Burn,
        Bleed,
        Stun,
        Invincible,
        Poison,   // Reserved
        Freeze,   // Reserved
    }

    /// <summary>
    /// Abstract base class for all status effects applied to characters.
    /// Subclasses implement OnApply / OnTick / OnExpire lifecycle hooks.
    /// Effects are managed by <see cref="StatusEffectManager"/>.
    /// </summary>
    public abstract class StatusEffect
    {
        // ==================== Identity ====================
        /// <summary>Unique type identifier for this effect.</summary>
        public abstract StatusEffectType EffectType { get; }

        /// <summary>
        /// Color tint applied to the character while this effect is active.
        /// Priority is handled by <see cref="StatusEffectManager.GetModulateColor"/>.
        /// </summary>
        public abstract Color OverlayColor { get; }

        // ==================== Duration ====================
        /// <summary>Total duration in seconds when the effect was applied.</summary>
        public float Duration { get; protected set; }

        /// <summary>Remaining active time in seconds.</summary>
        public float RemainingTime { get; private set; }

        /// <summary>True when the effect has run out of time.</summary>
        public bool IsExpired => RemainingTime <= 0f;

        /// <summary>
        /// If false (default), reapplying the same effect type refreshes duration.
        /// If true, multiple independent instances can coexist.
        /// </summary>
        public virtual bool CanStack => false;

        // ==================== Constructor ====================
        protected StatusEffect(float duration)
        {
            Duration = duration;
            RemainingTime = duration;
        }

        // ==================== Lifecycle Hooks ====================
        /// <summary>Called once when the effect is first applied to a character.</summary>
        public virtual void OnApply(Character target) { }

        /// <summary>Called every physics frame while the effect is active.</summary>
        public virtual void OnTick(Character target, float delta) { }

        /// <summary>Called when the effect expires naturally or is manually removed.</summary>
        public virtual void OnExpire(Character target) { }

        // ==================== Internal API ====================
        /// <summary>
        /// Advances the effect timer and calls <see cref="OnTick"/>.
        /// Called exclusively by <see cref="StatusEffectManager.Update"/>.
        /// </summary>
        internal void Tick(Character target, float delta)
        {
            if (IsExpired) return;

            OnTick(target, delta);

            RemainingTime -= delta;
            if (RemainingTime < 0f)
                RemainingTime = 0f;
        }

        /// <summary>Resets remaining time to full duration (used on re-application).</summary>
        public void Refresh()
        {
            RemainingTime = Duration;
        }
    }
}