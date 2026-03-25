namespace QuestFantasy.Core.Data.Attributes
{
    public enum StatusType { Normal, Poison, Burn, Freeze, Sleep, Paralysis, Stun }

    public class Status
    {
        public StatusType StatusType { get; set; }
        public float AtkRate()
        {
            switch (StatusType)
            {
                case StatusType.Burn:
                    return 0.5f;
                case StatusType.Freeze:
                    return 0f;
                case StatusType.Sleep:
                    return 0f;
                case StatusType.Paralysis:
                    return 1f;
                case StatusType.Stun:
                    return 0f;
                default:
                    return 1f;
            }
        }
        public float DefRate()
        {
            switch (StatusType)
            {
                case StatusType.Poison:
                    return 0.5f;
                case StatusType.Freeze:
                    return 0f;
                case StatusType.Sleep:
                    return 0.5f;
                case StatusType.Paralysis:
                    return 0.5f;
                case StatusType.Stun:
                    return 0f;
                default:
                    return 1f;
            }

        }
        public float SpdRate()
        {
            switch (StatusType)
            {
                case StatusType.Freeze:
                    return 0f;
                case StatusType.Sleep:
                    return 0f;
                case StatusType.Paralysis:
                    return 0.5f;
                case StatusType.Stun:
                    return 0f;
                default:
                    return 1f;
            }
        }
    }
}
