using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;

namespace QuestFantasy.Environment
{
    /// <summary>
    /// Represents a teleporter/portal device that transports players to different levels.
    /// Triggers difficulty selection when player presses interact button while in range.
    /// </summary>
    public class Teleporter : EnvironmentalObject
    {
        public event Action<Player> TeleporterInteracted;

        private const float INTERACTION_RANGE_PIXELS = 80f; // Interact within 80 pixels
        private Player _nearbyPlayer;
        private bool _playerInRange = false;
        private bool _fKeyPressed = false;  // Debounce flag for F key

        public override void _Ready()
        {
            SetProcess(true);
        }

        public override void _Process(float delta)
        {
            try
            {
                // Find player in the parent scene
                if (_nearbyPlayer == null)
                {
                    var parent = GetParent();
                    if (parent == null)
                        return;

                    var player = parent.GetNodeOrNull<Player>("Player");
                    if (player != null)
                    {
                        _nearbyPlayer = player;
                    }
                    else
                    {
                        return;
                    }
                }

                // Check distance to player
                if (_nearbyPlayer != null)
                {
                    float distance = Position.DistanceTo(_nearbyPlayer.Position);
                    bool inRange = distance <= INTERACTION_RANGE_PIXELS;

                    if (inRange && !_playerInRange)
                    {
                        _playerInRange = true;
                        Modulate = Colors.Yellow;
                        GD.Print("[Teleporter] *** Player ENTERED range ***");
                    }
                    else if (!inRange && _playerInRange)
                    {
                        _playerInRange = false;
                        Modulate = Colors.White;
                        GD.Print("[Teleporter] Player LEFT range");
                    }

                    // Check if player pressed F key to interact
                    bool fKeyPressed = Input.IsActionPressed("interact");
                    if (fKeyPressed && !_fKeyPressed)  // Debounce: only trigger on key press, not hold
                    {
                        GD.Print("[Teleporter] F key pressed!");
                        if (_playerInRange)
                        {
                            GD.Print("[Teleporter] Player in range - triggering interaction");
                            OnInteract(_nearbyPlayer);
                        }
                        else
                        {
                            GD.Print("[Teleporter] F pressed but player NOT in range");
                        }
                    }
                    _fKeyPressed = fKeyPressed;  // Update for next frame
                }
            }
            catch (System.Exception ex)
            {
                GD.PrintErr("[Teleporter] Exception in _Process: " + ex.Message);
                GD.PrintErr(ex.StackTrace);
            }
        }

        public override void OnInteract(Creature interactor)
        {
            if (interactor is Player player)
            {
                TeleporterInteracted?.Invoke(player);
            }
        }

        public void Initialize(string name, string description)
        {
            ObjectName = name;
            Description = description;
        }
    }
}