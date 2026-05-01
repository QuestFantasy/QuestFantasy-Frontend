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

            // Check for UI-triggered skill activation (tap on HUD skill slot)
            int uiSkillIndex = _inputHandler.ConsumeUiSkillActivation();
            if (uiSkillIndex >= 0)
            {
                _selectedSkillIndex = uiSkillIndex;
                _inputHandler.ConsumeSkillActivationInput();
                if (!_animationController.IsAttacking)
                {
                    ExecuteSkill(player, map, uiSkillIndex);
                }
                return;
            }

            if (_animationController.IsAttacking)
                return;  // Cannot start new skill while attacking

            if (!_inputHandler.IsSkillActivationPressed())
                return;

            ExecuteSkill(player, map, _selectedSkillIndex);
        }


        /// <summary>
        /// Execute the basic attack skill
        /// </summary>
        private bool ExecuteSkill(Player player, Map map, int skillIndex)
        {
            var skills = _combatSystem.CurrentSkills;
            if (skills == null || skills.Count == 0)
            {
                GD.PrintErr("[PlayerCombatController] No skills available");
                return false;
            }

            int selectedIndex = Mathf.Clamp(skillIndex, 0, skills.Count - 1);
            _selectedSkillIndex = selectedIndex;

            Vector2 mousePos = player.GetGlobalMousePosition();
            float diffX = mousePos.x - player.GlobalPosition.x;
            _animationController.SetFacingDirection(diffX);

            var selectedSkill = skills[selectedIndex];

            // Check if skill is on cooldown
            if (!selectedSkill.CoolDown.IsReady)
            {
                GD.Print("[PlayerCombatController] Skill on cooldown");
                return false;
            }

            // Find target (optional - can attack with no target)
            Character targetCharacter = _combatSystem.FindNearestEnemyInRange(
                player.GlobalPosition,
                selectedSkill,
                map);

            // Execute the attack with or without a target
            _animationController.PlayAttackAnimation(GetAttackAnimationStyle(selectedSkill));

            bool success = _combatSystem.UseSkill(selectedIndex, player, targetCharacter);
            if (!success)
            {
                GD.PrintErr("[PlayerCombatController] Skill execution failed despite validation");
                _animationController.ResetAttackState();
                return false;
            }

            if (targetCharacter != null)
            {
                GD.Print($"[PlayerCombatController] Successfully attacked {targetCharacter.EntityName}");
            }
            else
            {
                GD.Print("[PlayerCombatController] Performed attack - no targets in range (empty swing)");
            }

            return true;
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