public enum ElementsTypes { Normal, Earth, Water, Fire, Wind }

public class Element
{
    public ElementsTypes ElementType { get; set; }
    public float AgainstGet(Element targetElement)
    {
        // Calculate effectiveness against another element (input is the Defender's element)

        if (ElementType == ElementsTypes.Normal) return 1f;
        if (ElementType == ElementsTypes.Earth)
        {
            if (targetElement.ElementType == ElementsTypes.Wind) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Water) return 2f;
        }
        if (ElementType == ElementsTypes.Water)
        {
            if (targetElement.ElementType == ElementsTypes.Earth) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Fire) return 2f;
        }
        if (ElementType == ElementsTypes.Fire)
        {
            if (targetElement.ElementType == ElementsTypes.Water) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Wind) return 2f;
        }
        if (ElementType == ElementsTypes.Wind)
        {
            if (targetElement.ElementType == ElementsTypes.Fire) return 0.5f;
            if (targetElement.ElementType == ElementsTypes.Earth) return 2f;
        }
        return 1f;
    }
}

public class HP
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public void UpdateMax(int Vit)
    {
        const int VIT_TO_HP_RATE = 10;
        bool CurrentHPExceedsMax = CurrentHP >= MaxHP;
        MaxHP = Vit * VIT_TO_HP_RATE;
        if (CurrentHP > MaxHP || CurrentHPExceedsMax)
        {
            CurrentHP = MaxHP; // Adjust current HP if it exceeds new max
        }
    }
    public void TakeDamage(float rate, int attackerAtk, int Def, Status attackerStatus, Status defenderStatus)
    {
        const float DAMAGE_RATE = 1f;
        CurrentHP -= (int)((attackerAtk * attackerStatus.AtkRate() - defenderStatus.DefRate() * Def) * DAMAGE_RATE * rate);
        if (CurrentHP < 0)
        {
            CurrentHP = 0;
        }
    }
}

public class Attributes
{
    public int TotalAtk { get; internal set; } // Attack: determines damage dealt to monsters and other players
    public int TotalDef { get; internal set; } // Defense: reduces incoming damage
    public int TotalSpd { get; internal set; } // Speed: determines walk speed and Skills cooldown
    public int TotalVit { get; internal set; } // Vitality: determines max HP 
    public Element Element { get; set; } = new Element { ElementType = ElementsTypes.Normal };
    public HP HP { get; private set; } = new HP();
    public void Update(int newAtk, int newDef, int newSpd, int newVit)
    {
        TotalAtk = newAtk;
        TotalDef = newDef;
        TotalSpd = newSpd;
        TotalVit = newVit;
    }
}