using Godot;

using QuestFantasy.Core.Data.Attributes;

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
            if (_animationController.IsAttacking)
                return;  // Cannot start new skill while attacking

            if (!_inputHandler.IsSkillActivationPressed())
                return;

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

            var basicSkill = skills[0];

            // Check if skill is on cooldown
            if (!basicSkill.CoolDown.IsReady)
            {
                GD.Print("[PlayerCombatController] Skill on cooldown");
                return;
            }

            // Find target (optional - can attack with no target)
            Character targetCharacter = _combatSystem.FindNearestEnemyInRange(
                player.Position,
                basicSkill,
                map);

            // Execute the attack with or without a target
            _animationController.PlayAttackAnimation();

            bool success = _combatSystem.UseSkill(0, player, targetCharacter);
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