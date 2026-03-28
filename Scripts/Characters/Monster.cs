using Godot;

using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Monster character class. Represents NPCs that can be fought.
    /// Handles monster-specific attribute calculations and behavior.
    /// </summary>
    public class Monster : Character
    {
        public int ExperienceReward { get; set; }
        public int LootGoldReward { get; set; }

        public override void _Ready()
        {
            // Initialize base character attributes
            InitializeCharacter();

            // Set up monster-specific defaults
            if (ExperienceReward <= 0)
                ExperienceReward = 10;

            if (LootGoldReward <= 0)
                LootGoldReward = 5;
        }

        /// <summary>
        /// Update monster attributes based on level and base stats.
        /// Monsters don't have jobs or equipment, so this is simpler than Player.
        /// </summary>
        public override void UpdateAttributes()
        {
            if (Attributes == null || Abilities == null)
            {
                GD.PrintErr($"[Monster] {EntityName}: Attributes or Abilities not initialized");
                return;
            }

            // Simple level-based scaling: +1 per level
            int levelBonus = (int)(Level - 1);

            Attributes.TotalAtk = Abilities.Atk + levelBonus;
            Attributes.TotalDef = Abilities.Def + levelBonus;
            Attributes.TotalSpd = Abilities.Spd + levelBonus;
            Attributes.TotalVit = Abilities.Vit + levelBonus;

            // Update HP based on vitality
            if (Attributes.HP != null)
                Attributes.HP.UpdateMax(Attributes.TotalVit);
        }
    }
}