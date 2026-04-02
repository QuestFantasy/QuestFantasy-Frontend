using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Items;

namespace QuestFantasy.Systems.Gameplay
{
    public enum MapType { Lobby, SecretBase, Dungeon }

    public struct Exit
    {
        public Vector2 Position;
        public MapType DestinationMap;
    }

    /// <summary>
    /// Gameplay-level map container for game state management.
    /// Note: Renamed from "Map" to avoid namespace collision with Godot/Environment/Map/Map.cs.
    /// This class represents high-level dungeon data (enemies, items, exits) separate from tile rendering.
    /// </summary>
    public class GameplayMap : NameAndDescription
    {
        public MapType MapType { get; private set; }
        public Vector2 Size { get; private set; }

        public List<Monster> Enemies { get; private set; } = new List<Monster>();
        public List<Item> Items { get; private set; } = new List<Item>();
        public Vector2 Entrance { get; private set; }
        public List<Exit> Exits { get; private set; } = new List<Exit>();

        /// <summary>
        /// Generate dungeon contents (enemies, items, exits).
        /// </summary>
        public void DungeonGenerate(Vector2 size, int enemyCount, int itemCount, int exitCount, Vector2 entrance)
        {
            // TODO: Implement dungeon generation logic
        }

        /// <summary>
        /// Handle player exiting the map.
        /// </summary>
        public void Quit(Exit exit)
        {
            // TODO: Implement map exit logic
        }
    }
}