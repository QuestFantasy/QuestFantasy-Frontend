using Godot;
using QuestFantasy.Characters;
using QuestFantasy.Core.Base;

namespace QuestFantasy.Core.Data.Attributes
{
    public struct Cooldown
    {
        public float RemainingTime { get; private set; }
        public bool IsReady => RemainingTime <= 0;

        public void Start(float cooldownTime)
        {
            RemainingTime = cooldownTime;
        }

        public void Update(float deltaTime)
        {
            if (RemainingTime > 0)
            {
                RemainingTime -= deltaTime;
                if (RemainingTime < 0)
                    RemainingTime = 0;
            }
        }
    }

    /// <summary>
    /// Interface for skill effects that can be rendered with images/animations.
    /// Allows async asset loading and visual effect handling for skill execution.
    /// </summary>
    public interface ISkillEffectRenderer
    {
        /// <summary>
        /// Called when the skill is executed. Can load and display visual effects.
        /// </summary>
        void RenderEffect(Vector2 originPosition, Vector2 targetPosition);
    }

    public class Skills : NameAndDescription
    {
        public Cooldown CoolDown;
        
        /// <summary>
        /// Optional effect renderer for visual assets. Can be set by designers/artists.
        /// </summary>
        public ISkillEffectRenderer EffectRenderer { get; set; }

        /// <summary>
        /// Maximum range in pixels at which the skill can be used.
        /// </summary>
        public virtual float MaxRange => 100f;

        /// <summary>
        /// Checks if a target is within skill range.
        /// </summary>
        public bool IsTargetInRange(Vector2 casterPosition, Vector2 targetPosition)
        {
            float distance = casterPosition.DistanceTo(targetPosition);
            return distance <= MaxRange;
        }

        /// <summary>
        /// Main skill execution method. Call this to use the skill.
        /// Target can be null for empty swing attacks.
        /// </summary>
        public virtual bool TryExecute(Player player, Character target)
        {
            if (!CoolDown.IsReady)
                return false;

            // If target exists, check if it's in range
            if (target != null && !IsTargetInRange(player.Position, target.Position))
                return false;

            Effect(player, target);
            CoolDown.Start(GetCooldownDuration());

            // Render visual effect if renderer is available
            if (target != null && EffectRenderer != null)
            {
                EffectRenderer.RenderEffect(player.Position, target.Position);
            }
            else if (target == null && EffectRenderer != null)
            {
                // For empty swing, render effect at player position looking in facing direction
                Vector2 effectPosition = player.Position + Vector2.Right.Rotated(player.Rotation) * MaxRange;
                EffectRenderer.RenderEffect(player.Position, effectPosition);
            }

            return true;
        }

        /// <summary>
        /// Override this to implement skill-specific damage/effects.
        /// </summary>
        public virtual void Effect(Player player, Character target)
        {
            // TODO skill effect include damage rate, special effects, etc. to be implemented in subclasses
            // also will call Damage function in HP.cs to calculate damage dealt to target?
        }

        /// <summary>
        /// Override this to customize cooldown duration per skill.
        /// </summary>
        public virtual float GetCooldownDuration() => 1f;
    }
}