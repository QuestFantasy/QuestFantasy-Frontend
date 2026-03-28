namespace QuestFantasy.Core.Data.Attributes
{
    public enum StatusType { Normal, Poison, Burn, Freeze, Sleep, Paralysis, Stun }

    /// <summary>
    /// Represents a temporary status effect applied to a character.
    /// Status effects modify combat statistics and should be tracked with duration.
    /// </summary>
    public class Status
    {
        public StatusType StatusType { get; set; }
        
        /// <summary>
        /// Duration in seconds. Value of 0 or less means permanent until removed.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Get attack rate modifier for this status. Lower = weaker attacks.
        /// </summary>
        public float AtkRate()
        {
            switch (StatusType)
            {
                case StatusType.Burn:
                    return GameConstants.STATUS_BURN_ATK_RATE;
                case StatusType.Freeze:
                case StatusType.Sleep:
                case StatusType.Stun:
                    return GameConstants.STATUS_CC_STAT_MULTIPLIER;
                case StatusType.Paralysis:
                    return 1f;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Get defense rate modifier for this status. Lower = weaker defense.
        /// </summary>
        public float DefRate()
        {
            switch (StatusType)
            {
                case StatusType.Poison:
                    return GameConstants.STATUS_POISON_DEF_RATE;
                case StatusType.Freeze:
                case StatusType.Stun:
                    return GameConstants.STATUS_CC_STAT_MULTIPLIER;
                case StatusType.Sleep:
                case StatusType.Paralysis:
                    return GameConstants.STATUS_POISON_DEF_RATE;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Get speed rate modifier for this status. Lower = slower movement/cooldowns.
        /// </summary>
        public float SpdRate()
        {
            switch (StatusType)
            {
                case StatusType.Freeze:
                case StatusType.Sleep:
                case StatusType.Stun:
                    return GameConstants.STATUS_CC_STAT_MULTIPLIER;
                case StatusType.Paralysis:
                    return GameConstants.STATUS_PARALYSIS_SPD_RATE;
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// Update status duration. Returns true if status is still active.
        /// </summary>
        public bool Update(float deltaTime)
        {
            if (Duration <= 0)
                return true; // Permanent status

            Duration -= deltaTime;
            return Duration > 0;
        }
    }
}