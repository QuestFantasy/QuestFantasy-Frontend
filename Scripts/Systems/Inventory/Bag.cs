using System.Collections.Generic;

using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Systems.Inventory
{
    public class Bag
    {
        public List<Item> Items { get; set; } = new List<Item>();
    }
}