using Godot;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Base class for all playable and non-playable characters.
    /// Manages core character attributes, status effects, and combat abilities.
    /// </summary>
    public abstract class Character : Creature
    {
        public long Level { get; protected set; }
        
        public Status CurrentStatus { get; set; }
        
        public Abilities Abilities { get; protected set; }
        
        public Element Element { get; protected set; }
        
        public Attributes Attributes { get; protected set; }

        /// <summary>
        /// Initialize character with default attributes.
        /// Should be called in derived class _Ready() method.
        /// </summary>
        protected virtual void InitializeCharacter()
        {
            if (Abilities == null)
                Abilities = new Abilities();
            
            if (Attributes == null)
                Attributes = new Attributes();
            
            if (Element == null)
                Element = new Element { ElementType = ElementsTypes.Normal };
            
            if (CurrentStatus == null)
                CurrentStatus = new Status { StatusType = StatusType.Normal };
            
            Level = 1;
        }

        /// <summary>
        /// Execute attack action. Override in derived classes to implement specific attack behavior.
        /// </summary>
        public virtual void Attack()
        {
            // Override in derived classes
            GD.PrintErr($"[Character] {EntityName}: Attack not implemented");
        }

        /// <summary>
        /// Update total attributes based on job, equipment, and status effects.
        /// Must be implemented by derived classes.
        /// </summary>
        public virtual void UpdateAttributes()
        {
            if (Attributes == null)
            {
                GD.PrintErr($"[Character] {EntityName}: Attributes not initialized");
                return;
            }
        }
        
        /// <summary>
        /// Apply damage to this character.
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (Attributes?.HP == null)
            {
                GD.PrintErr($"[Character] {EntityName}: Cannot take damage, HP not initialized");
                return;
            }
            
            if (damage < 0)
            {
                GD.PrintErr($"[Character] {EntityName}: Damage cannot be negative");
                return;
            }
            
            Attributes.HP.TakeDamage(damage);
        }
    }
}