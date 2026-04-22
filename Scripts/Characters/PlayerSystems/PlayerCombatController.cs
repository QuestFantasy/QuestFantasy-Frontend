using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Skills;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Handles all combat-related logic for the player:
    /// - Skill activation input handling
    /// - Target detection and validation
    /// - Skill execution
    /// - Attack state management
    /// </summary>
    public class PlayerCombatController
    {
        private readonly PlayerCombatSystem _combatSystem;
        private readonly PlayerInputHandler _inputHandler;
        private readonly PlayerAnimationController _animationController;
        private int _selectedSkillIndex;

        public int SelectedSkillIndex => _selectedSkillIndex;

        public PlayerCombatController(
            PlayerCombatSystem combatSystem,
            PlayerInputHandler inputHandler,
            PlayerAnimationController animationController)
        {
            _combatSystem = combatSystem;
            _inputHandler = inputHandler;
            _animationController = animationController;
        }

        /// <summary>
        /// Process skill input for this frame
        /// </summary>
        public void HandleSkillInput(Player player, Map map)
        {
            int requestedSkillIndex = _inputHandler.ConsumeSelectedSkillIndex();
            if (requestedSkillIndex >= 0)
            {
                _selectedSkillIndex = requestedSkillIndex;
                GD.Print($"[PlayerCombatController] Selected skill slot {_selectedSkillIndex + 1}");
            }

            if (_animationController.IsAttacking)
                return;  // Cannot start new skill while attacking

            if (!_inputHandler.IsSkillActivationPressed())
                return;

            Vector2 mousePos = player.GetGlobalMousePosition();
            float diffX = mousePos.x - player.GlobalPosition.x;
            _animationController.SetFacingDirection(diffX);

            ExecuteSkill(player, map);
        }

        /// <summary>
        /// Execute the basic attack skill
        /// </summary>
        private void ExecuteSkill(Player player, Map map)
        {
            var skills = _combatSystem.CurrentSkills;
            if (skills == null || skills.Count == 0)
            {
                GD.PrintErr("[PlayerCombatController] No skills available");
                return;
            }

            int skillIndex = Mathf.Clamp(_selectedSkillIndex, 0, skills.Count - 1);
            var selectedSkill = skills[skillIndex];

            // Check if skill is on cooldown
            if (!selectedSkill.CoolDown.IsReady)
            {
                GD.Print("[PlayerCombatController] Skill on cooldown");
                return;
            }

            // Find target (optional - can attack with no target)
            Character targetCharacter = _combatSystem.FindNearestEnemyInRange(
                player.GlobalPosition,
                selectedSkill,
                map);

            // Execute the attack with or without a target
            _animationController.PlayAttackAnimation(GetAttackAnimationStyle(selectedSkill));

            bool success = _combatSystem.UseSkill(skillIndex, player, targetCharacter);
            if (!success)
            {
                GD.PrintErr("[PlayerCombatController] Skill execution failed despite validation");
                _animationController.ResetAttackState();
                return;
            }

            if (targetCharacter != null)
            {
                GD.Print($"[PlayerCombatController] Successfully attacked {targetCharacter.EntityName}");
            }
            else
            {
                GD.Print("[PlayerCombatController] Performed attack - no targets in range (empty swing)");
            }
        }

        private static AttackAnimationStyle GetAttackAnimationStyle(Skills skill)
        {
            if (skill is BowAttackSkill)
            {
                return AttackAnimationStyle.Bow;
            }

            if (skill is FireballSkill)
            {
                return AttackAnimationStyle.Fireball;
            }

            return AttackAnimationStyle.Sword;
        }

        /// <summary>
        /// Update skill cooldowns
        /// </summary>
        public void UpdateSkillCooldowns(float delta)
        {
            _combatSystem.UpdateSkillCooldowns(delta);
        }

        /// <summary>
        /// Learn a new skill
        /// </summary>
        public void LearnSkill(Skills skill)
        {
            _combatSystem.LearnSkill(skill);
        }

        /// <summary>
        /// Get current skills (read-only)
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Skills> GetCurrentSkills()
        {
            return _combatSystem.CurrentSkills;
        }

        /// <summary>
        /// Subscribe to combat events
        /// </summary>
        public void SubscribeOnAttackPerformed(System.Action<string> callback)
        {
            _combatSystem.OnAttackPerformed += callback;
        }
    }
}