public class Attributes
{
    public int TotalAtk { get; internal set; } // Attack: determines damage dealt to monsters and other players
    public int TotalDef { get; internal set; } // Defense: reduces incoming damage
    public int TotalSpd { get; internal set; } // Speed: determines walk speed and Skills cooldown
    public int TotalVit { get; internal set; } // Vitality: determines max HP 
    public void Update(int newAtk, int newDef, int newSpd, int newVit)
    {
        TotalAtk = newAtk;
        TotalDef = newDef;
        TotalSpd = newSpd;
        TotalVit = newVit;
    }
}