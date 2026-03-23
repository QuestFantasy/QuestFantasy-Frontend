using System.Collections.Concurrent;
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