using QuestFantasy.Core.Base;

namespace QuestFantasy.Core.Data.Attributes
{
    public struct Cooldown
    {
        public float RemainingTime { get; private set; }
        public bool IsReady => RemainingTime <= 0;

        public void Start(float cooldownTime)
        {
            RemainingTime = cooldownTime;
        }

        public void Update(float deltaTime)
        {
            if (RemainingTime > 0)
            {
                RemainingTime -= deltaTime;
                if (RemainingTime < 0)
                    RemainingTime = 0;
            }
        }
    }

    public class Skills : NameAndDescription
    {
        public Cooldown CoolDown;

        public virtual void Effect(Player player, Monster target)
        {
            // TODO skill effect include damage rate, special effects, etc. to be implemented in subclasses
            // also will call Damage function in HP.cs to calculate damage dealt to target?
        }
    }
}
