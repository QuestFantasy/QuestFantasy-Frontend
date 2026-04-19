using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Skills;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Manages player combat-related functionality:
    /// - Skill execution and learning
    /// - Attack targeting and range checking
    /// - Skill cooldown updates
    /// </summary>
    public class PlayerCombatSystem
    {
        public IReadOnlyList<Skills> CurrentSkills => _currentSkills.AsReadOnly();
        private readonly List<Skills> _currentSkills = new List<Skills>();

        public event Action<Skills> OnSkillLearned;
        public event Action<string> OnAttackPerformed;

        public PlayerCombatSystem()
        {
            _currentSkills = new List<Skills>();
        }

        /// <summary>
        /// Initialize combat system with basic skills
        /// </summary>
        public void Initialize()
        {
            // Add basic attack skill by default
            var basicAttack = new BasicAttackSkill();
            basicAttack.EffectRenderer = new Core.Data.Assets.BasicAttackEffectRenderer();
            _currentSkills.Add(basicAttack);
        }

        /// <summary>
        /// Replace all currently equipped/learned skills.
        /// </summary>
        public void SetSkills(IEnumerable<Skills> skills)
        {
            _currentSkills.Clear();

            if (skills != null)
            {
                _currentSkills.AddRange(skills.Where(skill => skill != null));
            }

            if (_currentSkills.Count == 0)
            {
                var basicAttack = new BasicAttackSkill();
                basicAttack.EffectRenderer = new Core.Data.Assets.BasicAttackEffectRenderer();
                _currentSkills.Add(basicAttack);
            }
        }

        /// <summary>
        /// Learn a new skill for the player
        /// </summary>
        public void LearnSkill(Skills skill)
        {
            if (skill == null)
            {
                GD.PrintErr("[PlayerCombatSystem] Cannot learn a null skill");
                return;
            }

            if (_currentSkills.Contains(skill))
            {
                GD.Print($"[PlayerCombatSystem] Already knows skill: {skill.Name}");
                return;
            }

            _currentSkills.Add(skill);
            GD.Print($"[PlayerCombatSystem] Learned skill: {skill.Name}");
            OnSkillLearned?.Invoke(skill);
        }

        /// <summary>
        /// Execute a specific skill by index
        /// </summary>
        public bool UseSkill(int skillIndex, Player player, Character target)
        {
            if (skillIndex < 0 || skillIndex >= _currentSkills.Count)
            {
                GD.PrintErr($"[PlayerCombatSystem] Invalid skill index: {skillIndex}");
                return false;
            }

            if (player == null)
            {
                GD.PrintErr("[PlayerCombatSystem] Player is null");
                return false;
            }

            // Target can be null for empty swing attacks
            bool success = _currentSkills[skillIndex].TryExecute(player, target);
            if (success)
            {
                OnAttackPerformed?.Invoke(_currentSkills[skillIndex].Name);
            }
            return success;
        }

        /// <summary>
        /// Find the nearest enemy within skill range
        /// </summary>
        public Character FindNearestEnemyInRange(Vector2 playerGlobalPosition, Skills skill, Node mapRoot)
        {
            if (skill == null || mapRoot == null)
                return null;

            float skillRange = skill.MaxRange;
            Character nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            var characters = GetAllNodesOfType<Character>(mapRoot.GetTree().Root);
            foreach (var character in characters)
            {
                if (character == null || character is Player || character.Attributes?.HP?.IsAlive != true)
                    continue;

                float distance = playerGlobalPosition.DistanceTo(character.GlobalPosition);
                if (distance <= skillRange && distance < nearestDistance)
                {
                    nearestEnemy = character;
                    nearestDistance = distance;
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// Update cooldowns for all skills
        /// </summary>
        public void UpdateSkillCooldowns(float deltaTime)
        {
            foreach (var skill in _currentSkills)
            {
                skill.CoolDown.Update(deltaTime);
            }
        }

        /// <summary>
        /// Check if the first skill is ready
        /// </summary>
        public bool IsBasicAttackReady()
        {
            return _currentSkills.Count > 0 && _currentSkills[0].CoolDown.IsReady;
        }

        /// <summary>
        /// Helper to recursively find all nodes of a specific type
        /// </summary>
        private List<T> GetAllNodesOfType<T>(Node root) where T : Node
        {
            var result = new List<T>();
            if (root is T tNode)
            {
                result.Add(tNode);
            }

            foreach (Node child in root.GetChildren())
            {
                result.AddRange(GetAllNodesOfType<T>(child));
            }

            return result;
        }
    }
}