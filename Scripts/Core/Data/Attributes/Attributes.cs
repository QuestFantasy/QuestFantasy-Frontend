using System;

namespace QuestFantasy.Core.Data.Attributes
{
    public enum ElementsTypes { Normal, Earth, Water, Fire, Wind }

    public class Element
    {
        public ElementsTypes ElementType { get; set; }
        public float AgainstGet(Element targetElement)
        {
            // Calculate effectiveness against another element (input is the Defender's element)
            if (targetElement == null) return 1f;

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
        private const int VIT_TO_HP_RATE = 10;

        public event Action<int, int> OnChanged;

        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }

        /// <summary>
        /// Initialize HP with default value
        /// </summary>
        public HP()
        {
            MaxHP = 10 * VIT_TO_HP_RATE; // 100 HP by default
            CurrentHP = MaxHP;
            EmitChanged();
        }

        /// <summary>
        /// Explicitly set max and current HP directly
        /// </summary>
        public void SetMaxHPAndCurrentHP(int value)
        {
            SetMaxHPAndCurrentHP(value, value);
        }

        public void SetMaxHPAndCurrentHP(int maxValue, int currentValue)
        {
            MaxHP = maxValue < 1 ? 1 : maxValue;
            CurrentHP = currentValue < 0 ? 0 : currentValue;
            if (CurrentHP > MaxHP)
            {
                CurrentHP = MaxHP;
            }

            EmitChanged();
        }

        /// <summary>
        /// Update max HP based on Vitality stat
        /// </summary>
        public void UpdateMax(int vit)
        {
            int newMaxHP = vit * VIT_TO_HP_RATE;
            MaxHP = newMaxHP;

            // Adjust current HP if it exceeds new max
            if (CurrentHP > MaxHP)
            {
                CurrentHP = MaxHP;
            }

            EmitChanged();
        }

        /// <summary>
        /// Apply damage to the character
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage < 0)
                damage = 0;

            CurrentHP -= damage;
            if (CurrentHP < 0)
            {
                CurrentHP = 0;
            }

            EmitChanged();
        }

        /// <summary>
        /// Restore HP
        /// </summary>
        public void Heal(int amount)
        {
            if (amount < 0)
                amount = 0;

            CurrentHP += amount;
            if (CurrentHP > MaxHP)
            {
                CurrentHP = MaxHP;
            }

            EmitChanged();
        }

        /// <summary>
        /// Check if character is alive
        /// </summary>
        public bool IsAlive => CurrentHP > 0;

        private void EmitChanged()
        {
            OnChanged?.Invoke(CurrentHP, MaxHP);
        }
    }

    public class Attributes
    {
        public int TotalAtk { get; set; } // Attack: determines damage dealt to monsters and other players
        public int TotalDef { get; set; } // Defense: reduces incoming damage
        public int TotalSpd { get; set; } // Speed: determines walk speed and Skills cooldown
        public int TotalVit { get; set; } // Vitality: determines max HP 
        public Element Element { get; set; } = new Element { ElementType = ElementsTypes.Normal };
        public HP HP { get; private set; } = new HP();

        /// <summary>
        /// Update total attributes based on job, equipment, and buffs
        /// </summary>
        public void Update(int newAtk, int newDef, int newSpd, int newVit)
        {
            TotalAtk = newAtk;
            TotalDef = newDef;
            TotalSpd = newSpd;
            TotalVit = newVit;

            // Update HP max when vitality changes
            HP.UpdateMax(newVit);
        }
    }
}