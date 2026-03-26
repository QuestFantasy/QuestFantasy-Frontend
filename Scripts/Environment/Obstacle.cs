using QuestFantasy.Core.Base;

namespace QuestFantasy.Environment
{
    /// <summary>
    /// Represents an obstacle or destructible object in the game world.
    /// Obstacles can block movement and may be destroyable.
    /// </summary>
    public class Obstacle : EnvironmentalObject
    {
        public int Health { get; private set; } = 1;
        public int MaxHealth { get; private set; } = 1;

        public override void OnInteract(Creature interactor)
        {
            // TODO: Implement obstacle destruction or damage logic
        }

        public void TakeDamage(int damageAmount)
        {
            if (IsDestroyable)
            {
                Health -= damageAmount;
                if (Health <= 0)
                {
                    Destroy();
                }
            }
        }

        private void Destroy()
        {
            // TODO: Implement destruction effects (particles, sound, removal, etc.)
            QueueFree();
        }
    }
}