using System;
using Godot;
using QuestFantasy.Characters;

namespace QuestFantasy.Items
{
    public class CoinDrop : Node2D
    {
        private int _value;
        private Player _player;
        private float _bobTime = 0f;
        private readonly float _bobSpeed = 5f;
        private readonly float _bobHeight = 5f;
        private Vector2 _basePosition;
        private bool _isMagnetic = false;
        private float _magnetSpeed = 200f; // px/sec

        private Sprite _sprite;
        private Texture[] _frames;
        private float _animationTimer = 0f;
        private const float FrameDuration = 0.05f;
        private int _currentFrame = 0;

        public void Initialize(int playerLevel, DifficultyLevel difficulty, float entityMultiplier, Player player)
        {
            _player = player;
            CalculateValue(playerLevel, difficulty, entityMultiplier);
        }

        private void CalculateValue(int playerLevel, DifficultyLevel difficulty, float entityMultiplier)
        {
            // Base value at level 1 is 5~10
            int baseMin = 5 + (playerLevel - 1) * 2;
            int baseMax = 10 + (playerLevel - 1) * 4;

            float difficultyMultiplier = 1f;
            switch (difficulty)
            {
                case DifficultyLevel.Easy: difficultyMultiplier = 0.5f; break;
                case DifficultyLevel.Normal: difficultyMultiplier = 1.0f; break;
                case DifficultyLevel.Hard: difficultyMultiplier = 5.0f; break;
                case DifficultyLevel.Nightmare: difficultyMultiplier = 20.0f; break;
            }

            var rng = new RandomNumberGenerator();
            rng.Randomize();
            int baseValue = rng.RandiRange(baseMin, baseMax);

            _value = Mathf.RoundToInt(baseValue * entityMultiplier * difficultyMultiplier);
            if (_value < 1) _value = 1;
            
            GD.Print($"[CoinDrop] Generated coin value: {_value} (Lv: {playerLevel}, Diff: {difficultyMultiplier}, Mult: {entityMultiplier})");
        }

        public override void _Ready()
        {
            _basePosition = Position;

            _sprite = new Sprite();
            AddChild(_sprite);

            // Load frames: money.png, money-f1.png, ..., money-f9.png
            _frames = new Texture[10];
            _frames[0] = GD.Load<Texture>("res://Assets/money/money.png");
            for (int i = 1; i <= 9; i++)
            {
                _frames[i] = GD.Load<Texture>($"res://Assets/money/money-f{i}.png");
            }

            _sprite.Texture = _frames[0];
            _sprite.Scale = new Vector2(0.05f, 0.05f); // Scaled down to 1/20 of 0.5f
        }

        public override void _Process(float delta)
        {
            // Update animation
            _animationTimer += delta;
            if (_animationTimer >= FrameDuration)
            {
                _animationTimer -= FrameDuration;
                _currentFrame = (_currentFrame + 1) % _frames.Length;
                _sprite.Texture = _frames[_currentFrame];
            }

            if (_player != null && IsInstanceValid(_player))
            {
                float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);

                // Magnet radius
                if (dist < 40f)
                {
                    _isMagnetic = true;
                }

                if (_isMagnetic)
                {
                    // Move towards player
                    var dir = GlobalPosition.DirectionTo(_player.GlobalPosition);
                    GlobalPosition += dir * _magnetSpeed * delta;
                    _magnetSpeed += 500f * delta; // Accelerate over time

                    if (dist < 8f) // Consume radius
                    {
                        // Needs AddGold method on player or inventory system
                        _player.AddGold(_value);
                        QueueFree();
                        return;
                    }
                }
                else
                {
                    // Normal bob
                    _bobTime += delta * _bobSpeed;
                    Position = _basePosition + new Vector2(0, Mathf.Sin(_bobTime) * _bobHeight);
                }
            }
            else
            {
                // Normal bob if no player
                _bobTime += delta * _bobSpeed;
                Position = _basePosition + new Vector2(0, Mathf.Sin(_bobTime) * _bobHeight);
            }
        }
    }
}
