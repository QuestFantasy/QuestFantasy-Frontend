using Godot;

namespace QuestFantasy.Core.Base
{
    /// <summary>
    /// Base class for all animated creatures (characters, monsters, etc.).
    /// Provides sprite rendering and basic interaction interface.
    /// </summary>
    public class Creature : Sprite
    {
        /// <summary>
        /// Unique name/identifier for this creature
        /// </summary>
        public string EntityName { get; protected set; }

        /// <summary>
        /// Called when another creature tries to move into this creature's space.
        /// Override to implement custom movement blocking logic.
        /// </summary>
        public virtual bool CanBeOccupied()
        {
            return false; // By default, creatures block movement
        }

        /// <summary>
        /// Called when another creature interacts with this creature.
        /// Override in derived classes for specific interaction behavior.
        /// </summary>
        public virtual void OnInteract(Creature interactor)
        {
            // Base implementation does nothing
            if (interactor != null)
                GD.Print($"[Creature] {EntityName}: Interacted by {interactor.EntityName}");
        }
    }
}