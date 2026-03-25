namespace QuestFantasy.Core.Data.Attributes
{
    public class Abilities
    {
        public int Atk { get; private set; } // Attack: determines damage dealt to monsters and other players
        public int Def { get; private set; } // Defense: reduces incoming damage
        public int Spd { get; private set; } // Speed: determines walk speed and Skills cooldown
        public int Vit { get; private set; } // Vitality: determines max HP 
        private void Set(int atk, int def, int spd, int vit)
        {
            Atk = atk;
            Def = def;
            Spd = spd;
            Vit = vit;
        }
        private void Increase(int atk, int def, int spd, int vit)
        {
            Atk += atk;
            Def += def;
            Spd += spd;
            Vit += vit;
        }
        private void Decrease(int atk, int def, int spd, int vit)
        {
            Atk -= atk;
            Def -= def;
            Spd -= spd;
            Vit -= vit;
        }
    }
}
