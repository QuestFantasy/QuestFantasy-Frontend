using Godot;

using QuestFantasy.Core.Systems.StatusEffects;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Monster variant that applies burn damage after hitting the player.
    /// </summary>
    public class FireMonster : Monster
    {
        private const float BurnDurationSeconds = 15f;
        private const float BurnDamageMultiplier = 0.5f;

        protected override float HpMultiplier
        {
            get { return 0.85f; }
        }

        protected override float AttackMultiplier
        {
            get { return 0.9f; }
        }

        protected override void LoadTextures()
        {
            _standTexture = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_stand.png");
            _walkTexture = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_walk.png");
            _attackTexture1 = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_attack.png");
            _attackTexture2 = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_attack1.png");
            _deadTexture = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_knockdown.png");
            _hitTexture = GD.Load<Texture>("res://Assets/FireMonster/fire_slime_hit.png");
            Texture = _standTexture;
        }

        public override void Attack()
        {
            int damage = Attributes?.EffectiveAtk ?? 1;
            bool canBurnPlayer = TargetPlayer != null
                && TargetPlayer.Attributes?.HP?.IsAlive == true
                && !TargetPlayer.IsInvincible;

            base.Attack();

            if (!canBurnPlayer || TargetPlayer.Attributes?.HP?.IsAlive != true)
            {
                return;
            }

            float burnDamagePerSecond = damage * BurnDamageMultiplier;
            TargetPlayer.EffectManager?.Apply(
                new BurnEffect(BurnDurationSeconds, burnDamagePerSecond),
                TargetPlayer);
        }
    }
}