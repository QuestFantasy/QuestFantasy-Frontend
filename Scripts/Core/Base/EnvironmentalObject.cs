using Godot;

namespace QuestFantasy.Core.Base
{
    /// <summary>
    /// Base class for non-animated environmental objects in the game world.
    /// Unlike Creature, EnvironmentalObject is designed for static or passive objects
    /// that don't have their own movement logic.
    /// </summary>
    public class EnvironmentalObject : Sprite
    {
        public string ObjectName { get; protected set; }
        public string Description { get; protected set; }
        public bool IsDestroyable { get; protected set; } = false;
        public bool IsInteractable { get; protected set; } = true;

        /// <summary>
        /// Called when a character interacts with this environmental object.
        /// Override in derived classes to implement specific interaction behavior.
        /// </summary>
        public virtual void OnInteract(Creature interactor)
        {
            // Base implementation does nothing; override in subclasses
        }
    }
}