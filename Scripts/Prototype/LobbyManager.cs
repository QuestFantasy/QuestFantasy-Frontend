using System;
using System.Collections.Generic;

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
        public event Action<NPC> DialogueNpcInteractionRequested;
        public event Action<NPC> ShopNpcInteractionRequested;

        private Map _lobbyMap;
        private Player _player;
        private Teleporter _teleporter;
        private DifficultySelectionUI _difficultyUI;
        private NpcShopUI _shopUI;
        private readonly List<NPC> _lobbyNpcs = new List<NPC>();

        public override void _Ready()
        {
            SetupLobbyMap();
            SetupPlayer();
            SetupTeleporter();
            SetupNpcCharacters();
            SetupShopUI();
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

        private void SetupNpcCharacters()
        {
            SpawnNpc(
                "Map Guide",
                "I can point you to the teleporter, explain the lobby, and handle simple trading.",
                NpcRole.Guide,
                true,
                new Vector2(7, 11),
                new Color(0.85f, 0.95f, 1f));

            SpawnNpc(
                "Travel Merchant",
                "My shop list is empty for now, but the trade flow is ready.",
                NpcRole.Merchant,
                true,
                new Vector2(23, 11),
                new Color(1f, 0.92f, 0.75f));

            SpawnNpc(
                "Lobby Blacksmith",
                "I'll handle equipment sales once item data is in place.",
                NpcRole.Blacksmith,
                true,
                new Vector2(15, 23),
                new Color(1f, 0.82f, 0.82f));
        }

        private void SpawnNpc(string entityName, string dialogue, NpcRole role, bool isShopkeeper, Vector2 tilePosition, Color tint)
        {
            NPC npc = new NPC();
            npc.Initialize(entityName, dialogue, role, isShopkeeper);
            AddChild(npc);

            Vector2 spawnPosition = new Vector2(
                tilePosition.x * _lobbyMap.TileSize + _lobbyMap.TileSize / 2f,
                tilePosition.y * _lobbyMap.TileSize + _lobbyMap.TileSize / 2f);

            npc.Position = spawnPosition;
            npc.SetBaseTint(tint);
            npc.InteractionStarted += OnNpcInteractionStarted;
            npc.DialogueRequested += OnNpcDialogueRequested;
            npc.ShopRequested += OnNpcShopRequested;

            _lobbyNpcs.Add(npc);
            GD.Print($"[Lobby] Spawned NPC {entityName} at {spawnPosition}");
        }

        private void OnNpcInteractionStarted(NPC npc, Player player)
        {
            if (npc == null || player == null)
            {
                return;
            }

            GD.Print($"[Lobby] {npc.EntityName} interacted by {player.EntityName}");
        }

        private void OnNpcDialogueRequested(NPC npc, Player player)
        {
            if (npc == null)
            {
                return;
            }

            DialogueNpcInteractionRequested?.Invoke(npc);
            GD.Print($"[Lobby] Dialogue requested from {npc.EntityName}");
        }

        private void OnNpcShopRequested(NPC npc, Player player)
        {
            if (npc == null)
            {
                return;
            }

            ShopNpcInteractionRequested?.Invoke(npc);
            _shopUI?.ShowShop(npc, _player);
            GD.Print($"[Lobby] Shop requested from {npc.EntityName}. Inventory is currently empty.");
        }

        private void SetupShopUI()
        {
            _shopUI = new NpcShopUI();
            AddChild(_shopUI);
            _shopUI.Closed += OnShopClosed;
        }

        private void OnShopClosed()
        {
            GD.Print("[Lobby] Shop closed");
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