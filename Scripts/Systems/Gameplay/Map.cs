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

    public class Map : NameAndDescription
    {
        public MapType MapType { get; private set; }
        public Vector2 Size { get; private set; }

        public List<Monster> Enemies { get; private set; } = new List<Monster>();
        public List<Item> Items { get; private set; } = new List<Item>();
        public Vector2 Entrance { get; private set; }
        public List<Exit> Exits { get; private set; } = new List<Exit>();

        public void DungeonGenerate(Vector2 size, int enemyCount, int itemCount, int exitCount, Vector2 entrance)
        {
        }

        public void Quit(Exit exit)
        {
        }
    }
}