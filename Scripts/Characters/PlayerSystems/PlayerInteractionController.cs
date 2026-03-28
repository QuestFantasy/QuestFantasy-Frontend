using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Handles player interaction with the environment:
    /// - Box opening
    /// - Respawning
    /// - Portal interference
    /// </summary>
    public class PlayerInteractionController
    {
        private readonly PlayerInputHandler _inputHandler;
        private readonly PlayerPhysicsController _physicsController;

        public PlayerInteractionController(
            PlayerInputHandler inputHandler,
            PlayerPhysicsController physicsController)
        {
            _inputHandler = inputHandler;
            _physicsController = physicsController;
        }

        /// <summary>
        /// Handle respawn input if player presses the respawn key
        /// </summary>
        public void HandleRespawnInput(Player player, Map map)
        {
            if (!_inputHandler.IsRespawnPressed())
                return;

            _physicsController.RespawnAtCurrentRoomStart(player, map);
        }

        /// <summary>
        /// Handle interaction input (opening boxes, etc.)
        /// </summary>
        public void HandleInteractionInput(Map map, Vector2 playerPosition)
        {
            if (!_inputHandler.IsInteractPressed())
                return;

            if (map.TryOpenNearbyBox(playerPosition))
            {
                // Trigger portal interference when interacting with objects
                _physicsController.InterferPortalWithInteraction();
            }
        }
    }
}
