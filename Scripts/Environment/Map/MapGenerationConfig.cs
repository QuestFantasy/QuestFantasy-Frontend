using Godot;

public class MapGenerationConfig : Resource
{
    // C# note: decimal literals with `f` are float (single precision), e.g. 0.14f.

    // Pixel size of one tile in world space.
    [Export] public int TileSize = 24;
    // Number of tiles on one side of each room.
    [Export] public int RoomTileSize = 100;
    // Number of rooms along X.
    [Export] public int RoomsX = 2;
    // Number of rooms along Y.
    [Export] public int RoomsY = 2;
    // 0 or negative means generate a random seed at runtime.
    [Export] public int Seed = 0;
    // Inset from room border for start/exit placement to avoid corners and walls.
    [Export] public int StartExitInsetTiles = 6;
    // Border wall thickness as a fraction of RoomTileSize (0.08f = 8%).
    [Export] public float BorderWallThicknessRatio = 0.08f;
    // Chance to place a solid obstacle on eligible floor tiles (0.09f = 9%).
    [Export] public float ObstacleFillRate = 0.09f;
    // Probability that a room gets a portal pair (0.55f = 55%).
    [Export] public float PortalPairChance = 0.55f;
    // Distance in pixels from exit center that triggers room completion.
    [Export] public float ExitTriggerRadius = 24f;
    // Minimum tiles in one hazard cluster.
    [Export] public int HazardClusterMinTiles = 15;
    // Maximum tiles in one hazard cluster.
    [Export] public int HazardClusterMaxTiles = 120;
    // Minimum number of boxes to place per room.
    [Export] public int BoxCountMin = 5;
    // Maximum number of boxes to place per room.
    [Export] public int BoxCountMax = 20;
    // Texture used for unopened boxes.
    [Export] public string BoxClosedTexturePath = "res://Assets/Box/Box_Closed.png";
    // Texture used for opened boxes.
    [Export] public string BoxOpenTexturePath = "res://Assets/Box/Box_Open.png";

    public bool Validate(out string error)
    {
        if (TileSize <= 0)
        {
            error = "TileSize must be > 0.";
            return false;
        }
        if (RoomTileSize <= 0)
        {
            error = "RoomTileSize must be > 0.";
            return false;
        }
        if (RoomsX <= 0 || RoomsY <= 0)
        {
            error = "RoomsX and RoomsY must be > 0.";
            return false;
        }
        if (BorderWallThicknessRatio <= 0f || BorderWallThicknessRatio >= 0.5f)
        {
            error = "BorderWallThicknessRatio must be between 0 and 0.5.";
            return false;
        }
        if (ObstacleFillRate < 0f || ObstacleFillRate > 0.6f)
        {
            error = "ObstacleFillRate must be between 0 and 0.6.";
            return false;
        }
        if (PortalPairChance < 0f || PortalPairChance > 1f)
        {
            error = "PortalPairChance must be between 0 and 1.";
            return false;
        }
        if (ExitTriggerRadius <= 0f)
        {
            error = "ExitTriggerRadius must be > 0.";
            return false;
        }
        if (HazardClusterMinTiles <= 0 || HazardClusterMaxTiles < HazardClusterMinTiles)
        {
            error = "Hazard cluster bounds are invalid.";
            return false;
        }
        if (BoxCountMin <= 0 || BoxCountMax < BoxCountMin)
        {
            error = "Box count bounds are invalid.";
            return false;
        }

        error = null;
        return true;
    }
}