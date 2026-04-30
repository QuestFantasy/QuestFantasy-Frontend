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
        private int _pendingUISkillIndex = -1;

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
            // 優先執行 UI 觸發的待執行技能
            if (_pendingUISkillIndex >= 0)
            {
                int skillIndex = _pendingUISkillIndex;
                _pendingUISkillIndex = -1;
                _selectedSkillIndex = skillIndex;
                
                // 消耗鼠標輸入，避免同一幀內又觸發舊技能
                _inputHandler.ConsumeSkillActivationInput();

                if (!_animationController.IsAttacking)
                {
                    ExecuteSkill(player, map, skillIndex);
                }
                return; // 跳過鍵盤和鼠標檢測
            }

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

            TriggerSkill(_selectedSkillIndex, player, map);
        }

        /// <summary>
        /// Trigger a specific skill slot directly from UI input.
        /// </summary>
        public bool TriggerSkill(int skillIndex, Player player, Map map)
        {
            if (skillIndex < 0)
            {
                return false;
            }

            // 設置待執行技能，讓 HandleSkillInput 優先處理，避免與鼠標輸入衝突
            _pendingUISkillIndex = skillIndex;
            _selectedSkillIndex = skillIndex;
            GD.Print($"[PlayerCombatController] UI triggered skill slot {skillIndex + 1}");

            return true;
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