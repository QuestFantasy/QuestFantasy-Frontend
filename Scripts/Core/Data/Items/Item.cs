using QuestFantasy.Core.Base;
using QuestFantasy.Characters;

namespace QuestFantasy.Core.Data.Items
{
    public enum ItemType { None, Potion, Equipment, Weapon, Misc }

    public class Item : NameAndDescription
    {
        public int Price { get; private set; }
        public ItemType ItemType { get; private set; }
        public void Use(Player player)
        {
        }
    }
}
