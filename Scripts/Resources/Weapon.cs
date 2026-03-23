public enum WeaponType { Sword, Bow, Staff }

public class Weapon : Item
{
    public WeaponType WeaponType { get; private set; }
    public int Atk { get; private set; }
}