using Godot;
using System.Collections.Generic;

public class Player : Fighter
{
	[Export] public float SpeedMultiplier = 50f; // pixels per spd point
	private Vector2 _velocity = Vector2.Zero;

	public Jobs Job { get; private set; }
	public int Exp { get; private set; }
	public Jobs CurrentJob { get; private set; }
	public List<Skills> CurrentSkills { get; private set; } = new List<Skills>();
	private Weapon EquippedWeapon { get; set; }
	private EquippedItems Equipped { get; set; }
	public int Gold { get; private set; }

	private Bag Inventory;

	// TODO: set Job function
	public override void UpdateAttributes()
	{
		Attributes.TotalAtk = (Job?.BaseAbilities?.Atk ?? 0) + (EquippedWeapon?.WeaponAbilities?.Atk ?? 0) + (Equipped?.TotalAtk() ?? 0);
		Attributes.TotalDef = (Job?.BaseAbilities?.Def ?? 0) + (EquippedWeapon?.WeaponAbilities?.Def ?? 0) + (Equipped?.TotalDef() ?? 0);
		Attributes.TotalSpd = (Job?.BaseAbilities?.Spd ?? 0) + (EquippedWeapon?.WeaponAbilities?.Spd ?? 0) + (Equipped?.TotalSpd() ?? 0);
		Attributes.TotalVit = (Job?.BaseAbilities?.Vit ?? 0) + (EquippedWeapon?.WeaponAbilities?.Vit ?? 0) + (Equipped?.TotalVit() ?? 0);
	}
    // TODO: set Job function
    public override void UpdateAttributes()
    {
        Attributes.TotalAtk = (Job?.BaseStats?.Atk ?? 0) + (EquippedWeapon?.Atk ?? 0) + (Equipped?.TotalAtk() ?? 0);
        Attributes.TotalDef = (Job?.BaseStats?.Def ?? 0) + (EquippedWeapon?.Def ?? 0) + (Equipped?.TotalDef() ?? 0);
        Attributes.TotalSpd = (Job?.BaseStats?.Spd ?? 0) + (EquippedWeapon?.Spd ?? 0) + (Equipped?.TotalSpd() ?? 0);
        Attributes.TotalVit = (Job?.BaseStats?.Vit ?? 0) + (EquippedWeapon?.Vit ?? 0) + (Equipped?.TotalVit() ?? 0);
    }
}}
