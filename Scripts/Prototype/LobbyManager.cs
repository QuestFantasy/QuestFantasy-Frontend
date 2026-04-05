using System;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data;
using QuestFantasy.Environment;

namespace QuestFantasy.Prototype
{
    /// <summary>
    /// Manages the lobby scene lifecycle and difficulty selection.
    /// Handles player spawning in the lobby, teleporter interactions, and transitions to game levels.
    /// </summary>
    public class LobbyManager : Node2D
    {
        [Export] public string TeleporterTexturePath = "res://Assets/Box/Box_Closed.png";

        public event Action<DifficultyLevel> DifficultySelected;

        private Map _lobbyMap;
        private Player _player;
        private Teleporter _teleporter;
        private DifficultySelectionUI _difficultyUI;

        public override void _Ready()
        {
            SetupLobbyMap();
            SetupPlayer();
            SetupTeleporter();
            SetupDifficultyUI();
        }

        private void SetupLobbyMap()
        {
            _lobbyMap = new LobbyMap();
            AddChild(_lobbyMap);
            _lobbyMap.Position = Vector2.Zero;

            GD.Print("[Lobby] Lobby map created: " + _lobbyMap.WorldPixelWidth + "x" + _lobbyMap.WorldPixelHeight + "px");
        }

        private void SetupPlayer()
        {
            Vector2 spawnPos = _lobbyMap.GetSpawnWorldPosition();
            GD.Print("[Lobby] Spawn position: " + spawnPos);

            _player = new Player();
            _player.Name = "Player";  // Set explicit name for Teleporter to find
            AddChild(_player);
            _player.Position = spawnPos;

            GD.Print("[Lobby] Player spawned at: " + _player.Position);

            _player.SetMap(_lobbyMap);

            // Set camera bounds to entire lobby with padding
            float lobbyWidth = _lobbyMap.WorldPixelWidth;
            float lobbyHeight = _lobbyMap.WorldPixelHeight;

            Rect2 lobbyBounds = new Rect2(
                -100,  // Add padding for viewport
                -100,
                lobbyWidth + 200,
                lobbyHeight + 200
            );
            _player.ConfigureCameraBounds(lobbyBounds);

            GD.Print("[Lobby] Camera bounds set to: " + lobbyBounds);
        }

        private void SetupTeleporter()
        {
            // Place teleporter at the center of the lobby
            Vector2 lobbyCenter = new Vector2(
                _lobbyMap.WorldPixelWidth / 2f,
                _lobbyMap.WorldPixelHeight / 2f
            );

            _teleporter = new Teleporter
            {
                Texture = ResourceLoader.Load<Texture>(TeleporterTexturePath),
                Scale = new Vector2(0.05f, 0.05f)  // Very small, approximately 1 block size
            };

            _teleporter.Initialize("Portal to Adventure", "Get ready for your quest!");

            AddChild(_teleporter);
            _teleporter.Position = lobbyCenter;
            _teleporter.TeleporterInteracted += OnTeleporterInteracted;

            GD.Print("[Lobby] Teleporter placed at center: " + lobbyCenter);
        }

        private void SetupDifficultyUI()
        {
            _difficultyUI = new DifficultySelectionUI();
            AddChild(_difficultyUI);
            _difficultyUI.DifficultySelected += OnDifficultySelected;
        }

        private void OnTeleporterInteracted(Player player)
        {
            _difficultyUI.ShowDifficultyMenu();
        }

        private void OnDifficultySelected(DifficultyLevel difficulty)
        {
            DifficultySelected?.Invoke(difficulty);
            _difficultyUI.HideDifficultyMenu();
        }

        public Map GetLobbyMap()
        {
            return _lobbyMap;
        }
    }
}