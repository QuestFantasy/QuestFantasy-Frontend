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
        return (Head?.EquipmentAbilities?.Atk ?? 0) + (Body?.EquipmentAbilities?.Atk ?? 0) + (Arms?.EquipmentAbilities?.Atk ?? 0) + (Legs?.EquipmentAbilities?.Atk ?? 0) + (Shoes?.EquipmentAbilities?.Atk ?? 0);
    }
    public int TotalDef()
    {
        return (Head?.EquipmentAbilities?.Def ?? 0) + (Body?.EquipmentAbilities?.Def ?? 0) + (Arms?.EquipmentAbilities?.Def ?? 0) + (Legs?.EquipmentAbilities?.Def ?? 0) + (Shoes?.EquipmentAbilities?.Def ?? 0);
    }
    public int TotalSpd()
    {
        return (Head?.EquipmentAbilities?.Spd ?? 0) + (Body?.EquipmentAbilities?.Spd ?? 0) + (Arms?.EquipmentAbilities?.Spd ?? 0) + (Legs?.EquipmentAbilities?.Spd ?? 0) + (Shoes?.EquipmentAbilities?.Spd ?? 0);
    }
    public int TotalVit()
    {
        return (Head?.EquipmentAbilities?.Vit ?? 0) + (Body?.EquipmentAbilities?.Vit ?? 0) + (Arms?.EquipmentAbilities?.Vit ?? 0) + (Legs?.EquipmentAbilities?.Vit ?? 0) + (Shoes?.EquipmentAbilities?.Vit ?? 0);
    }

}


public class Equipment : Item
{
    public EquipmentType EquipmentType { get; set; }
    public Abilities EquipmentAbilities { get; set; }
}