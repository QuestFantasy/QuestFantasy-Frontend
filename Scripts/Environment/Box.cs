using System.Collections.Generic;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Environment
{
    public class Box : Creature
    {
        public bool IsOpen { get; private set; }
        public int GoldAmount { get; private set; }
        public List<Item> ContainedItems { get; private set; } = new List<Item>();
    }
}
