using System;

using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Handles all physics-related logic for the player:
    /// - Movement and collision detection
    /// - Room tracking and transitions
    /// - Portal teleportation
    /// - Position updates
    /// </summary>
    public class PlayerPhysicsController
    {
        public event Action ExitReached;  // Event fired when player reaches a level exit

        private readonly PlayerMovementController _movementController;
        private readonly PlayerRoomTracker _roomTracker;
        private readonly PlayerCameraManager _cameraManager;

        public event Action<Vector2, string> OnRoomChanged;

        public PlayerPhysicsController(
            PlayerMovementController movementController,
            PlayerRoomTracker roomTracker,
            PlayerCameraManager cameraManager)
        {
            _movementController = movementController;
            _roomTracker = roomTracker;
            _cameraManager = cameraManager;
        }

        /// <summary>
        /// Update all physics calculations for a frame
        /// </summary>
        public void Update(
            Player player,
            Map map,
            Vector2 input,
            Vector2 bodySize,
            float moveSpeed,
            float delta)
        {
            if (map == null)
                return;

            // Update room tracking state
            _roomTracker.Tick(delta);

            // Apply movement if player is not attacking
            if (input.LengthSquared() > 0 && !player.IsAttacking)
            {
                Vector2 velocity = input.Normalized() * moveSpeed;
                _movementController.TryMove(player, map, velocity * delta, bodySize);
            }

            // Handle portal and room transitions
            HandlePortalTeleportation(player, map);
            HandleRoomTransitions(player, map);

            // Update skill cooldowns
            player.UpdateSkillCooldowns(delta);
        }

        /// <summary>
        /// Check and execute portal teleportation if conditions are met
        /// </summary>
        private void HandlePortalTeleportation(Player player, Map map)
        {
            if (!_roomTracker.IsPortalReady)
                return;

            if (_roomTracker.TryHandlePortal(map, player.Position, out Vector2 destinationPosition))
            {
                TransitionToLocation(player, map, destinationPosition);
                OnRoomChanged?.Invoke(_roomTracker.CurrentRoomIndex, "portal");
            }
        }

        /// <summary>
        /// Check and execute room exit or intra-room transitions
        /// </summary>
        private void HandleRoomTransitions(Player player, Map map)
        {
            // Skip all room exit handling if exits are disabled (e.g., in lobby)
            if (map.DisableRoomExits)
                return;

            // Prioritize room exit (level completion)
            if (_roomTracker.TryHandleExit(map, player.Position, out Vector2 exitPosition))
            {
                TransitionToLocation(player, map, exitPosition);
                OnRoomChanged?.Invoke(_roomTracker.CurrentRoomIndex, "generated_room_enter");
                // Fire event that player reached an exit
                ExitReached?.Invoke();
                return;  // Prevent multiple transitions in same frame
            }

            // Then handle normal room transitions
            if (_roomTracker.TryUpdateRoomByPosition(map, player.Position))
            {
                LockCameraToCurrentRoom(player, map);
                OnRoomChanged?.Invoke(_roomTracker.CurrentRoomIndex, "room_enter");
            }
        }

        /// <summary>
        /// Move player to a new location and update room tracking
        /// </summary>
        public void TransitionToLocation(Player player, Map map, Vector2 newWorldPosition)
        {
            player.Position = newWorldPosition;
            _roomTracker.InitializeFromPosition(map, player.Position);
            LockCameraToCurrentRoom(player, map);
        }

        /// <summary>
        /// Lock camera to the room the player is currently in
        /// </summary>
        private void LockCameraToCurrentRoom(Player player, Map map)
        {
            _cameraManager.LockToRoom(map, _roomTracker.CurrentRoomIndex);
        }

        /// <summary>
        /// Respawn player at the current room's start position
        /// </summary>
        public void RespawnAtCurrentRoomStart(Player player, Map map)
        {
            if (!_roomTracker.TryRespawnAtCurrentRoomStart(map, out Vector2 respawnPosition))
                return;

            TransitionToLocation(player, map, respawnPosition);
        }

        /// <summary>
        /// Trigger portal interference (used when player interacts with objects)
        /// </summary>
        public void InterferPortalWithInteraction()
        {
            _roomTracker.InterferPortalWithInteraction();
        }

        /// <summary>
        /// Get the current room index where the player is located
        /// </summary>
        public Vector2 GetCurrentRoomIndex()
        {
            return _roomTracker.CurrentRoomIndex;
        }
    }
}