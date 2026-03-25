using System.Collections.Generic;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Environment
{
    /// <summary>
    /// Represents a lootable container in the game world.
    /// Players can open boxes to collect items and gold.
    /// </summary>
    public class Box : EnvironmentalObject
    {
        public bool IsOpen { get; private set; } = false;
        public int GoldAmount { get; private set; }
        public List<Item> ContainedItems { get; private set; } = new List<Item>();

        public override void OnInteract(Creature interactor)
        {
            if (!IsOpen)
            {
                Open();
            }
        }

        private void Open()
        {
            IsOpen = true;
            // TODO: Implement loot distribution logic
        }
    }
}
