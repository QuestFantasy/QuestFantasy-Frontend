using Godot;

using QuestFantasy.Characters;

namespace QuestFantasy.Core.Systems.StatusEffects
{
    /// <summary>
    /// Invincible effect: makes a character immune to all damage for the duration.
    /// Sets <see cref="Character.IsInvincible"/> for the duration.
    /// Applied with a golden color overlay.
    /// NOTE: Currently player respawn invincibility is handled separately via
    /// _respawnInvincibilityTimer. This class is provided for future skill use.
    /// </summary>
    public class InvincibleEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Invincible;

        /// <summary>Golden glow — signals divine protection.</summary>
        public override Color OverlayColor => new Color(1f, 0.9f, 0.35f, 0.9f);

        /// <param name="duration">Invincibility duration in seconds.</param>
        public InvincibleEffect(float duration) : base(duration) { }

        public override void OnApply(Character target)
        {
            target.IsInvincible = true;
            GD.Print($"[StatusEffect] {target.EntityName} is invincible for {Duration}s!");
        }

        public override void OnExpire(Character target)
        {
            target.IsInvincible = false;
            GD.Print($"[StatusEffect] {target.EntityName}'s invincibility ended.");
        }
    }
}