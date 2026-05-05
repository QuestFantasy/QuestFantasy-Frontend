using Godot;

using QuestFantasy.UI;

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
            if (map == null || map.DisableRoomExits)
                return;

            if (!_inputHandler.IsRespawnPressed())
                return;

            _physicsController.RespawnAtCurrentRoomStart(player, map);
        }

        /// <summary>
        /// Handle interaction input (opening boxes, etc.)
        /// Accepts both keyboard (F key) and mobile interaction button tap.
        /// </summary>
        public void HandleInteractionInput(Map map, Vector2 playerPosition)
        {
            bool keyPressed = _inputHandler.IsInteractPressed();
            bool buttonPressed = InteractionButtonUI.IsPressed();

            if (!keyPressed && !buttonPressed)
                return;

            if (map.TryOpenNearbyBox(playerPosition))
            {
                // Trigger portal interference when interacting with objects
                _physicsController.InterferPortalWithInteraction();
            }
        }
    }
}