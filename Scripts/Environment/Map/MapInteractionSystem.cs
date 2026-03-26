using Godot;

public class MapInteractionSystem
{
	// If we must spawn on top of a portal tile, nudge slightly to avoid instant re-trigger.
	// 0.24f means 24% of one tile width; small enough to stay in the same tile visually.
	private const float PortalArrivalNudgeTiles = 0.24f;

	public bool IsAtRoomExit(MapTileData data, Vector2 worldPosition, Vector2 roomIndex, float exitTriggerRadius)
	{
		int roomX = Mathf.Clamp((int)roomIndex.x, 0, data.RoomsX - 1);
		int roomY = Mathf.Clamp((int)roomIndex.y, 0, data.RoomsY - 1);
		Vector2 tile = data.RoomExitTiles[roomX, roomY];
		Vector2 exitWorld = data.TileToWorldCenter((int)tile.x, (int)tile.y);
		return worldPosition.DistanceTo(exitWorld) <= exitTriggerRadius;
	}

	public bool TryGetPortalDestination(MapTileData data, Vector2 worldPosition, out Vector2 destinationWorld)
	{
		// 1) Convert player world position to tile coordinates.
		Vector2 tile = data.WorldToTile(worldPosition);

		// 2) Check whether current tile is a portal that has a linked destination tile.
		if (data.PortalLinks.TryGetValue(data.TileKey((int)tile.x, (int)tile.y), out Vector2 destinationTile))
		{
			// 3) Prefer placing the player on a nearby non-portal tile to avoid portal ping-pong.
			Vector2 arrivalTile = destinationTile;
			if (!TryFindNonPortalArrivalTile(data, (int)destinationTile.x, (int)destinationTile.y, out arrivalTile))
			{
				// 4) Fallback: if no valid adjacent tile exists, spawn on the portal tile itself.
				arrivalTile = destinationTile;
			}

			// 5) Convert chosen arrival tile back to world-space center.
			destinationWorld = data.TileToWorldCenter((int)arrivalTile.x, (int)arrivalTile.y);
			if (arrivalTile == destinationTile)
			{
				// 6) Nudge right by 0.24 tile to reduce immediate retrigger on next frame.
				// `f` in 0.24f marks a float literal in C#.
				destinationWorld += new Vector2(data.TileSize * PortalArrivalNudgeTiles, 0f);
			}

			return true;
		}

		destinationWorld = Vector2.Zero;
		return false;
	}

	public bool TryOpenNearbyBox(MapTileData data, Vector2 worldPosition, float maxDistanceTiles = 1.15f)
	{
		Vector2 center = data.WorldToTile(worldPosition);
		int cx = (int)center.x;
		int cy = (int)center.y;
		float maxDistanceSquared = maxDistanceTiles * maxDistanceTiles;
		float bestDistance = float.MaxValue;
		Vector2 bestTile = Vector2.Zero;
		bool found = false;

		for (int x = cx - 1; x <= cx + 1; x++)
		{
			for (int y = cy - 1; y <= cy + 1; y++)
			{
				if (x < 0 || y < 0 || x >= data.WorldTileWidth || y >= data.WorldTileHeight)
				{
					continue;
				}

				if (data.Tiles[x, y] != MapTileType.Box || data.OpenedBoxes[x, y])
				{
					continue;
				}

				float dx = x - cx;
				float dy = y - cy;
				float distanceSquared = dx * dx + dy * dy;
				if (distanceSquared > maxDistanceSquared)
				{
					continue;
				}

				if (distanceSquared < bestDistance)
				{
					bestDistance = distanceSquared;
					bestTile = new Vector2(x, y);
					found = true;
				}
			}
		}

		if (!found)
		{
			return false;
		}

		data.OpenedBoxes[(int)bestTile.x, (int)bestTile.y] = true;
		return true;
	}

	private bool TryFindNonPortalArrivalTile(MapTileData data, int portalX, int portalY, out Vector2 arrivalTile)
	{
		Vector2[] candidates = new[]
		{
			new Vector2(portalX + 1, portalY),
			new Vector2(portalX - 1, portalY),
			new Vector2(portalX, portalY + 1),
			new Vector2(portalX, portalY - 1),
			new Vector2(portalX + 1, portalY + 1),
			new Vector2(portalX - 1, portalY + 1),
			new Vector2(portalX + 1, portalY - 1),
			new Vector2(portalX - 1, portalY - 1)
		};

		for (int i = 0; i < candidates.Length; i++)
		{
			int x = (int)candidates[i].x;
			int y = (int)candidates[i].y;
			if (x < 0 || y < 0 || x >= data.WorldTileWidth || y >= data.WorldTileHeight)
			{
				continue;
			}

			MapTileType tileType = data.Tiles[x, y];
			if (tileType == MapTileType.Floor || tileType == MapTileType.Start || tileType == MapTileType.Exit)
			{
				arrivalTile = new Vector2(x, y);
				return true;
			}
		}

		arrivalTile = Vector2.Zero;
		return false;
	}
}
