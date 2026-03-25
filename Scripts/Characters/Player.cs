using Godot;
using System.Collections.Generic;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Systems.Inventory;

namespace QuestFantasy.Characters
{
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

        public override void Move()
        {
            _velocity = Vector2.Zero;

            if (Input.IsActionPressed("ui_right"))
                _velocity.x += 1;
            if (Input.IsActionPressed("ui_left"))
                _velocity.x -= 1;
            if (Input.IsActionPressed("ui_down"))
                _velocity.y += 1;
            if (Input.IsActionPressed("ui_up"))
                _velocity.y -= 1;

            if (_velocity.Length() > 0)
            {
                var baseSpd = Attributes?.TotalSpd ?? 0;
                float actualSpeed = (baseSpd + 1) * SpeedMultiplier;
                _velocity = _velocity.Normalized() * actualSpeed;
            }
            Position += _velocity;
        }
        public override void _PhysicsProcess(float delta)
        {
            Move();
        }
    }
}
