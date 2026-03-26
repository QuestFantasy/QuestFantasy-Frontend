using Godot;

public class PlayerMovementController
{
	public void TryMove(Node2D actor, Map map, Vector2 deltaMove, Vector2 bodySize)
	{
		Vector2 nextX = new Vector2(actor.Position.x + deltaMove.x, actor.Position.y);
		if (map.CanMoveTo(GetBodyRect(nextX, bodySize)))
		{
			actor.Position = nextX;
		}

		Vector2 nextY = new Vector2(actor.Position.x, actor.Position.y + deltaMove.y);
		if (map.CanMoveTo(GetBodyRect(nextY, bodySize)))
		{
			actor.Position = nextY;
		}
	}

	private Rect2 GetBodyRect(Vector2 centerPosition, Vector2 bodySize)
	{
		return new Rect2(centerPosition - bodySize / 2f, bodySize);
	}
}
