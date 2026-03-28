using Godot;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Assets
{
    /// <summary>
    /// Example implementation of ISkillEffectRenderer.
    /// Demonstrates how artists can create visual effects for skills.
    /// Can be extended to load sprite animations, particles, sounds, etc.
    /// </summary>
    public class BasicAttackEffectRenderer : ISkillEffectRenderer
    {
        /// <summary>
        /// Path to the attack effect sprite/animation. Can be set by designers.
        /// </summary>
        public string EffectSpritePath { get; set; } = "res://Assets/Effects/BasicAttack.png";

        /// <summary>
        /// Duration of the effect animation in seconds.
        /// </summary>
        public float EffectDuration { get; set; } = 0.5f;

        /// <summary>
        /// Renders the effect. Can be overridden to load custom assets.
        /// </summary>
        public void RenderEffect(Vector2 originPosition, Vector2 targetPosition)
        {
            // TODO: Example implementation for artists/designers
            // Can load and display:
            // - Sprite animations from EffectSpritePath
            // - Particle systems
            // - Screen shake effects
            // - Sound effects
            // - Slash animation, hit flash, etc.

            GD.Print($"[EFFECT] Basic Attack effect shown from {originPosition} to {targetPosition}");
        }
    }

    /// <summary>
    /// Factory for creating skill effects with configurable visual assets.
    /// Allows designers to easily customize skill visuals without coding.
    /// </summary>
    public class SkillEffectFactory
    {
        public static ISkillEffectRenderer CreateBasicAttackEffect(string customSpritePath = null)
        {
            var renderer = new BasicAttackEffectRenderer();
            if (!string.IsNullOrEmpty(customSpritePath))
            {
                renderer.EffectSpritePath = customSpritePath;
            }
            return renderer;
        }
    }
}
