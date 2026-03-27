# Playable Map Prototype (Godot 3.6)

This prototype adds a playable procedural map loop with these rules:

- Each room is `100 x 100` tiles.
- A world is generated as `RoomsX x RoomsY` rooms.
- Each room is assigned a random scenario (`Grassland`, `Mountain`, `Lava`, `Sea`).
- Each room has one `Start` and one `Exit` marker.
- A randomized guaranteed path is carved from `Start` to `Exit` in every room.
- Camera is locked to the current room bounds, so other rooms are hidden until entered.
- `Lava` only appears in `Lava` scenario rooms.
- `Mountain` scenario never generates `Lava` or `Water`.
- `Water`/`Lava` are generated as grouped patches (`6-20` tiles per group), not single isolated tiles.
- Tile textures are generated at runtime from colors (no external texture assets required).

## Scene Entry

The prototype is wired into `main.tscn` via `Scripts/Main.cs`.

## Controls

- Move: arrow keys (`ui_up`, `ui_down`, `ui_left`, `ui_right`).
- Revive to current room start (prototype key): `Esc` (`ui_cancel`).

## Core Scripts

- `Scripts/Prototype/PrototypeMap.cs`
  - `Generate()`: builds a scenario-based multi-room world.
  - `CanMoveTo(Rect2 worldRect)`: collision test function for movement.
  - `IsWalkableTile(int tileX, int tileY)`: tile-level collision query.
  - `GetRoomBoundsPixels(...)`: room camera-lock bounds.
  - `GetRoomStartWorldPosition(...)` / `GetRoomExitWorldPosition(...)`: room portals.
  - `TryGetNextRoomIndex(...)`: next room progression.
  - `IsAtRoomExit(...)`: exit trigger test.
  - `TryGetPortalDestination(...)`: portal teleport destination query.

- `Scripts/Prototype/PrototypePlayer.cs`
  - `SetMap(PrototypeMap map)`: links movement to map collision queries.
  - `ConfigureCameraBounds(Rect2 worldBounds)`: applies camera limits.
  - Locks camera to the active room only.
  - Uses portal tiles to teleport between paired locations.
  - Teleport arrival prefers non-portal adjacent tile (avoids same-point portal chaining).
  - Handles room transition: reaching exit teleports player to next room start.

## Visual and Marker Legend

- Start marker: yellow tile.
- Exit marker: purple tile.
- Portal: magenta tile (teleports between linked pair).
- Wall: dark gray tile (blocked).
- Box: brown tile (blocked).
- Lava: orange tile (blocked in this prototype).
- Water/sea: blue tile (blocked in this prototype).
- Floor: scenario-colored walkable tile.

## Prototype Tuning

Adjust these exported values in `PrototypeMap`:

- `RoomTileSize` (default `100`)
- `RoomsX`, `RoomsY`
- `TileSize`
- `StartExitInsetTiles`
- `ObstacleFillRate`
- `PortalPairChance`
- `HazardClusterMinTiles`
- `HazardClusterMaxTiles`
