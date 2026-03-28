namespace QuestFantasy.Core.Data.Attributes
{
    /// <summary>
    /// Represents character base abilities (core stats).
    /// These can be increased/decreased through items, jobs, buffs, etc.
    /// </summary>
    public class Abilities
    {
        public int Atk { get; set; } // Attack: determines damage dealt to monsters and other players
        public int Def { get; set; } // Defense: reduces incoming damage
        public int Spd { get; set; } // Speed: determines walk speed and Skills cooldown
        public int Vit { get; set; } // Vitality: determines max HP 

        /// <summary>
        /// Initialize abilities with specific values
        /// </summary>
        public void Set(int atk, int def, int spd, int vit)
        {
            Atk = atk;
            Def = def;
            Spd = spd;
            Vit = vit;
        }

        /// <summary>
        /// Increase abilities by given amounts
        /// </summary>
        public void Increase(int atk, int def, int spd, int vit)
        {
            Atk += atk;
            Def += def;
            Spd += spd;
            Vit += vit;
        }

        /// <summary>
        /// Decrease abilities by given amounts (clamped to 0)
        /// </summary>
        public void Decrease(int atk, int def, int spd, int vit)
        {
            Atk = System.Math.Max(0, Atk - atk);
            Def = System.Math.Max(0, Def - def);
            Spd = System.Math.Max(0, Spd - spd);
            Vit = System.Math.Max(0, Vit - vit);
        }
    }
}