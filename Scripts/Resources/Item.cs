public enum ItemType { None, Potion, Equipment, Weapon, Misc }

public class Item : NameAndDescription
{
    public int Price { get; private set; }
    public ItemType ItemType { get; private set; }
    public void Use(Player player)
    {
    }
}
