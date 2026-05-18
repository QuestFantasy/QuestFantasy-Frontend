using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Manages all active status effects on a single character.
    /// Handles application (with refresh-on-reapply), per-frame ticking,
    /// expiry, removal, and computing the current visual color overlay.
    ///
    /// Each <see cref="Character"/> owns one instance created in InitializeCharacter().
    /// </summary>
    public class StatusEffectManager
    {
        private readonly List<StatusEffect> _activeEffects = new List<StatusEffect>();

        /// <summary>Read-only view of all currently active effects.</summary>
        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects.AsReadOnly();

        /// <summary>True when at least one effect is active.</summary>
        public bool HasAnyEffect => _activeEffects.Count > 0;

        // ==================== Apply ====================
        /// <summary>
        /// Apply an effect to a character.
        /// If the same effect type is already active and <see cref="StatusEffect.CanStack"/> is false,
        /// the existing instance is refreshed instead of replaced.
        /// </summary>
        public void Apply(StatusEffect effect, Character target)
        {
            if (effect == null || target == null) return;

            // Check for an existing effect of the same type
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectType != effect.EffectType) continue;

                if (!effect.CanStack)
                {
                    _activeEffects[i].Refresh();
                    GD.Print($"[StatusEffectManager] {effect.EffectType} refreshed on {target.EntityName}");
                    return;
                }
                break; // CanStack: fall through to add
            }

            _activeEffects.Add(effect);
            effect.OnApply(target);
        }

        // ==================== Update ====================
        /// <summary>
        /// Advance all active effects by <paramref name="delta"/> seconds.
        /// Expired effects have their <see cref="StatusEffect.OnExpire"/> called and are removed.
        /// Call this every physics frame from the owning character's _PhysicsProcess.
        /// </summary>
        public void Update(Character target, float delta)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = _activeEffects[i];
                effect.Tick(target, delta);

                if (!effect.IsExpired) continue;

                effect.OnExpire(target);
                _activeEffects.RemoveAt(i);
            }
        }

        // ==================== Queries ====================
        /// <summary>Returns true if the specified effect type is currently active.</summary>
        public bool HasEffect(StatusEffectType type)
        {
            foreach (StatusEffect effect in _activeEffects)
            {
                if (effect.EffectType == type) return true;
            }
            return false;
        }

        /// <summary>Returns true if the specified generic effect type is currently active.</summary>
        public bool HasEffect<T>() where T : StatusEffect
        {
            foreach (StatusEffect effect in _activeEffects)
            {
                if (effect is T) return true;
            }
            return false;
        }

        // ==================== Removal ====================
        /// <summary>Remove all active effects of a given type.</summary>
        public void RemoveEffect(StatusEffectType type, Character target)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].EffectType != type) continue;

                _activeEffects[i].OnExpire(target);
                _activeEffects.RemoveAt(i);
            }
        }

        /// <summary>Remove all active effects of the specified generic type.</summary>
        public void RemoveEffect<T>(Character target) where T : StatusEffect
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (!(_activeEffects[i] is T)) continue;

                _activeEffects[i].OnExpire(target);
                _activeEffects.RemoveAt(i);
            }
        }

        /// <summary>Remove all active effects immediately.</summary>
        public void Clear(Character target)
        {
            foreach (StatusEffect effect in _activeEffects)
            {
                effect.OnExpire(target);
            }
            _activeEffects.Clear();
        }

        // ==================== Visual ====================
        /// <summary>
        /// Returns the Modulate color that should be applied to the character based on
        /// currently active effects. Priority (highest first):
        /// Stun → Invincible → Freeze → Burn → Bleed → Poison
        /// Returns white (no tint) when no effects are active.
        /// </summary>
        public Color GetModulateColor()
        {
            if (_activeEffects.Count == 0)
                return new Color(1f, 1f, 1f, 1f);

            StatusEffect stun = null;
            StatusEffect invincible = null;
            StatusEffect freeze = null;
            StatusEffect burn = null;
            StatusEffect bleed = null;
            StatusEffect poison = null;

            foreach (StatusEffect effect in _activeEffects)
            {
                switch (effect.EffectType)
                {
                    case StatusEffectType.Stun: stun = effect; break;
                    case StatusEffectType.Invincible: invincible = effect; break;
                    case StatusEffectType.Freeze: freeze = effect; break;
                    case StatusEffectType.Burn: burn = effect; break;
                    case StatusEffectType.Bleed: bleed = effect; break;
                    case StatusEffectType.Poison: poison = effect; break;
                }
            }

            if (stun != null) return stun.OverlayColor;
            if (invincible != null) return invincible.OverlayColor;
            if (freeze != null) return freeze.OverlayColor;
            if (burn != null) return burn.OverlayColor;
            if (bleed != null) return bleed.OverlayColor;
            if (poison != null) return poison.OverlayColor;

            return new Color(1f, 1f, 1f, 1f);
        }
    }
}