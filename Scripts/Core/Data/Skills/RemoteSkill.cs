using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Attributes;

namespace QuestFantasy.Core.Data.Skills
{
    /// <summary>
    /// Generic skill reconstructed from backend profile payload.
    /// </summary>
    public class RemoteSkill : Attributes.Skills
    {
        private readonly float _cooldownSeconds;

        public string SkillId { get; }

        public RemoteSkill(string skillId, string name, float cooldownSeconds)
        {
            SkillId = string.IsNullOrWhiteSpace(skillId) ? "remote_skill" : skillId;
            Name = string.IsNullOrWhiteSpace(name) ? "Remote Skill" : name;
            _cooldownSeconds = cooldownSeconds > 0f ? cooldownSeconds : 1f;
        }

        public override float GetCooldownDuration()
        {
            return _cooldownSeconds;
        }

        public override void Effect(Player player, Character target)
        {
            // Placeholder behavior until a concrete skill effect is implemented.
        }
    }
}
