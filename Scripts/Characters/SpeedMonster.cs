using Godot;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Faster monster variant that warns the player before dashing in a fixed line.
    /// </summary>
    public class SpeedMonster : Monster
    {
        private enum SpeedMonsterState
        {
            Chasing,
            Charging,
            Dashing,
            Recovering
        }

        private const float AggroRange = 200f;
        private const float ChargeDuration = 2f;
        private const float DashSpeed = BaseMoveSpeed * 3f;
        private const float DashStopDistance = 8f;
        private const float DashCooldown = 1.5f;
        private const float BlinkInterval = 0.15f;

        private SpeedMonsterState _state = SpeedMonsterState.Chasing;
        private float _stateTimer;
        private float _blinkTimer;
        private Vector2 _dashDirection = Vector2.Zero;
        private Vector2 _dashTargetPosition = Vector2.Zero;
        private bool _hasHitPlayerThisDash;

        protected override void LoadTextures()
        {
            _standTexture = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_stand.png");
            _walkTexture = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_walk.png");
            _attackTexture1 = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_attack.png");
            _attackTexture2 = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_attack1.png");
            _deadTexture = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_knockdown.png");
            _hitTexture = GD.Load<Texture>("res://Assets/SpeedMonster/speed_slime_hit.png");
            Texture = _standTexture;
        }

        protected override bool TryHandleSpecialBehavior(float delta, float distanceToPlayer)
        {
            if (TargetPlayer == null || CurrentMap == null)
            {
                return false;
            }

            switch (_state)
            {
                case SpeedMonsterState.Charging:
                    UpdateCharge(delta);
                    return true;
                case SpeedMonsterState.Dashing:
                    UpdateDash(delta);
                    return true;
                case SpeedMonsterState.Recovering:
                    _stateTimer -= delta;
                    if (_stateTimer <= 0f)
                    {
                        _state = SpeedMonsterState.Chasing;
                    }
                    return false;
            }

            if (distanceToPlayer <= AggroRange)
            {
                StartCharge();
                return true;
            }

            return false;
        }

        private void StartCharge()
        {
            _state = SpeedMonsterState.Charging;
            _stateTimer = ChargeDuration;
            _blinkTimer = 0f;
            Velocity = Vector2.Zero;
            Texture = _attackTexture1 ?? _standTexture;
            GD.Print("[SpeedMonster] Charging dash attack.");
        }

        private void UpdateCharge(float delta)
        {
            Velocity = Vector2.Zero;
            _stateTimer -= delta;
            _blinkTimer += delta;

            if (TargetPlayer != null)
            {
                if (TargetPlayer.GlobalPosition.x < GlobalPosition.x) FlipH = true;
                else if (TargetPlayer.GlobalPosition.x > GlobalPosition.x) FlipH = false;
            }

            bool flash = Mathf.FloorToInt(_blinkTimer / BlinkInterval) % 2 == 0;
            Texture = flash ? (_attackTexture1 ?? _standTexture) : _standTexture;
            Modulate = flash ? new Color(0.2f, 0.9f, 1f, 1f) : GetEffectModulate();

            if (_stateTimer <= 0f)
            {
                StartDash();
            }
        }

        private void StartDash()
        {
            Vector2 targetPosition = TargetPlayer != null ? TargetPlayer.GlobalPosition : GlobalPosition;
            Vector2 dashVector = targetPosition - GlobalPosition;

            if (dashVector.LengthSquared() <= 0.01f)
            {
                dashVector = FlipH ? Vector2.Left : Vector2.Right;
                targetPosition = GlobalPosition + dashVector * AttackRange;
            }

            _state = SpeedMonsterState.Dashing;
            _dashTargetPosition = targetPosition;
            _dashDirection = dashVector.Normalized();
            _stateTimer = Mathf.Max(0.1f, dashVector.Length() / DashSpeed);
            _hasHitPlayerThisDash = false;
            Texture = _attackTexture2 ?? _walkTexture ?? _standTexture;
            Modulate = GetEffectModulate();
            GD.Print("[SpeedMonster] Dash started.");
        }

        private void UpdateDash(float delta)
        {
            _stateTimer -= delta;
            Velocity = _dashDirection * DashSpeed;

            Vector2 deltaMove = Velocity * delta;
            Vector2 nextPosition = GlobalPosition + deltaMove;

            if (!CurrentMap.CanMoveTo(GetBodyRect(nextPosition)))
            {
                EndDash();
                return;
            }

            GlobalPosition = nextPosition;
            Texture = _attackTexture2 ?? _walkTexture ?? _standTexture;
            Modulate = GetEffectModulate();

            TryHitPlayerDuringDash();

            bool reachedTarget = GlobalPosition.DistanceTo(_dashTargetPosition) <= Mathf.Max(DashStopDistance, deltaMove.Length() + 1f);
            if (reachedTarget || _stateTimer <= 0f)
            {
                EndDash();
            }
        }

        private void TryHitPlayerDuringDash()
        {
            if (_hasHitPlayerThisDash || TargetPlayer == null || TargetPlayer.Attributes?.HP?.IsAlive != true)
            {
                return;
            }

            if (GlobalPosition.DistanceTo(TargetPlayer.GlobalPosition) > AttackRange)
            {
                return;
            }

            Attack();
            _hasHitPlayerThisDash = true;
        }

        private void EndDash()
        {
            Velocity = Vector2.Zero;
            _state = SpeedMonsterState.Recovering;
            _stateTimer = DashCooldown;
            Texture = _standTexture;
            Modulate = GetEffectModulate();
        }
    }
}