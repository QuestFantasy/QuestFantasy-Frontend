public enum EquipmentType { None, Head, Body, Arms, Legs, Shoes }

public class EquippedItems
{
    // Added 'private set;' so the 'Equipped' method can actually assign items to these slots
    public Equipment Head { get; private set; }
    public Equipment Body { get; private set; }
    public Equipment Arms { get; private set; }
    public Equipment Legs { get; private set; }
    public Equipment Shoes { get; private set; }

    public Equipment Equipped(Equipment newEquipment)
    {
        Equipment old_equipment;

        switch (newEquipment.EquipmentType)
        {
            case EquipmentType.Head:
                old_equipment = this.Head;
                this.Head = newEquipment;
                break;

            case EquipmentType.Body:
                old_equipment = this.Body;
                this.Body = newEquipment;
                break;

            case EquipmentType.Arms:
                old_equipment = this.Arms;
                this.Arms = newEquipment;
                break;

            case EquipmentType.Legs:
                old_equipment = this.Legs;
                this.Legs = newEquipment;
                break;

            case EquipmentType.Shoes:
                old_equipment = this.Shoes;
                this.Shoes = newEquipment;
                break;

            default:
                old_equipment = new Equipment { EquipmentType = EquipmentType.None };
                break;
        }

        return old_equipment;
    }
    public int TotalAtk()
    {
        return (Head?.Stats.Atk ?? 0) + (Body?.Stats.Atk ?? 0) + (Arms?.Stats.Atk ?? 0) + (Legs?.Stats.Atk ?? 0) + (Shoes?.Stats.Atk ?? 0);
    }
    public int TotalDef()
    {
        return (Head?.Stats.Def ?? 0) + (Body?.Stats.Def ?? 0) + (Arms?.Stats.Def ?? 0) + (Legs?.Stats.Def ?? 0) + (Shoes?.Stats.Def ?? 0);
    }
    public int TotalSpd()
    {
        return (Head?.Stats.Spd ?? 0) + (Body?.Stats.Spd ?? 0) + (Arms?.Stats.Spd ?? 0) + (Legs?.Stats.Spd ?? 0) + (Shoes?.Stats.Spd ?? 0);
    }
    public int TotalVit()
    {
        return (Head?.Stats.Vit ?? 0) + (Body?.Stats.Vit ?? 0) + (Arms?.Stats.Vit ?? 0) + (Legs?.Stats.Vit ?? 0) + (Shoes?.Stats.Vit ?? 0);
    }

}


public class Equipment : Item
{
    public EquipmentType EquipmentType { get; set; }
    public Abilities Stats { get; set; }
}