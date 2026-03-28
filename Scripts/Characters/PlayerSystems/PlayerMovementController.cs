using Godot;

namespace QuestFantasy.Characters.PlayerSystems
{
    /// <summary>
    /// Handles player character movement logic, including collision detection and axis-aligned movement.
    /// 
    /// Design Notes:
    /// - Uses axis-aligned movement: attempts X axis first, then Y axis
    /// - Each axis calculates independently from the original position, preventing one axis from affecting the other
    /// - If one axis is blocked, movement on the other axis can still proceed (sliding effect)
    /// - Collision detection only checks the lower half of the character body, allowing head to pass through terrain
    /// </summary>
    public class PlayerMovementController
    {
        public void TryMove(Node2D actor, Map map, Vector2 deltaMove, Vector2 bodySize)
        {
            // Save original position to allow independent calculation for both axes
            Vector2 originalPos = actor.Position;
            Vector2 newPos = originalPos;

            // Attempt movement along X axis
            Vector2 nextX = new Vector2(originalPos.x + deltaMove.x, originalPos.y);
            if (map.CanMoveTo(GetBodyRect(nextX, bodySize)))
            {
                newPos.x = nextX.x;
            }

            // Attempt movement along Y axis (based on original Y, unaffected by X axis)
            Vector2 nextY = new Vector2(originalPos.x, originalPos.y + deltaMove.y);
            if (map.CanMoveTo(GetBodyRect(nextY, bodySize)))
            {
                newPos.y = nextY.y;
            }

            // Apply new position atomically
            actor.Position = newPos;
        }

        private Rect2 GetBodyRect(Vector2 centerPosition, Vector2 bodySize)
        {
            // Only check the lower half of the body for collision detection
            // This allows the character's head to pass through terrain
            float lowerBodyHeight = bodySize.y / 2f;
            Vector2 lowerBodyStart = centerPosition - bodySize / 2f;
            lowerBodyStart.y += bodySize.y / 2f; // Move to start of lower half

            return new Rect2(lowerBodyStart, new Vector2(bodySize.x, lowerBodyHeight));
        }
    }
}