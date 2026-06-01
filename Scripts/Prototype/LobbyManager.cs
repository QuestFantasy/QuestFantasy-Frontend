using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Environment;
using QuestFantasy.UI;

namespace QuestFantasy.Prototype
{
    /// <summary>
    /// Manages the lobby scene lifecycle and difficulty selection.
    /// Handles player spawning in the lobby, teleporter interactions, and transitions to game levels.
    /// </summary>
    public class LobbyManager : Node2D
    {
        [Export] public string TeleporterTexturePath = "res://Assets/Lobby/lobby-teleporter.png";

        public event Action<DifficultyLevel> DifficultySelected;
        public event Action<NPC> DialogueNpcInteractionRequested;
        public event Action<NPC> ShopNpcInteractionRequested;
        public event Action<PlayerClass> ClassChangeRequested;
        public event Action ShopClosed;
        public event Action SyncRequested;

        private Map _lobbyMap;
        private Player _player;
        private Teleporter _teleporter;
        private DifficultySelectionUI _difficultyUI;
        private NpcShopUI _shopUI;
        private MarketplaceUI _marketplaceUI;
        private ClassSelectUI _classSelectUI;
        private AuthApiClient _apiClient;
        private string _authToken;
        private readonly EquipmentManager _equipmentFactory = new EquipmentManager();
        private readonly List<NPC> _lobbyNpcs = new List<NPC>();
        private Player _sharedPlayer;

        public void Initialize(Player sharedPlayer, AuthApiClient apiClient = null, string authToken = null)
        {
            _sharedPlayer = sharedPlayer;
            _apiClient = apiClient;
            _authToken = authToken;
        }

        public override void _Ready()
        {
            SetupLobbyMap();
            SetupTeleporter();
            SetupNpcCharacters();
            SetupPlayer();
            SetupShopUI();
            SetupMarketplaceUI();
            SetupClassSelectUI();
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

            if (_sharedPlayer != null)
            {
                _player = _sharedPlayer;
                _player.Name = "Player";
                Node previousParent = _player.GetParent();
                if (previousParent != null)
                {
                    previousParent.RemoveChild(_player);
                }

                AddChild(_player);
            }
            else
            {
                _player = new Player();
                _player.Name = "Player";  // Set explicit name for Teleporter to find
                AddChild(_player);
            }

            _player.Visible = true;
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
            // Place teleporter exactly at the center of tile (15, 15) so it aligns perfectly with the 3x3 grid
            Vector2 lobbyCenter = new Vector2(372f, 372f);

            _teleporter = new Teleporter
            {
                Texture = ResourceLoader.Load<Texture>(TeleporterTexturePath),
                Scale = new Vector2(0.28f, 0.28f)  // Scale 256x256 asset to approx 3x3 tiles (72x72 pixels)
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
                "Previous Hero",
                $"I once walked these lands, and I know the power that lies within each path. (Requires Level {GameConstants.CLASS_CHANGE_MIN_LEVEL} to change class)",
                NpcRole.ClassSelector,
                false,
                new Vector2(7, 11),
                new Color(0.85f, 0.95f, 1f));

            SpawnNpc(
                "Trader",
                "I speak in verses, but I still know the roads and the winds.",
                NpcRole.Merchant,
                true,
                new Vector2(23, 11),
                new Color(1f, 0.92f, 0.75f));

            SpawnNpc(
                "Blacksmith",
                "I stock basic gear for new adventurers.",
                NpcRole.Blacksmith,
                true,
                new Vector2(15, 23),
                new Color(1f, 0.82f, 0.82f));

            SpawnNpc(
                "Poet(online shop)",
                "I can help you browse the player marketplace.",
                NpcRole.Merchant,
                true,
                new Vector2(15, 11),
                new Color(0.8f, 1f, 0.8f),
                true);
        }

        private void SpawnNpc(string entityName, string dialogue, NpcRole role, bool isShopkeeper, Vector2 tilePosition, Color tint, bool isMarketplaceNpc = false)
        {
            NPC npc = new NPC();
            npc.Initialize(entityName, dialogue, role, isShopkeeper);
            AddChild(npc);

            Vector2 spawnPosition = new Vector2(
                tilePosition.x * _lobbyMap.TileSize + _lobbyMap.TileSize / 2f,
                tilePosition.y * _lobbyMap.TileSize + _lobbyMap.TileSize / 2f);

            npc.Position = spawnPosition;
            npc.SetBaseTint(tint);
            if (isShopkeeper)
            {
                if (role == NpcRole.Blacksmith)
                {
                    npc.SetShopInventory(CreateBlacksmithStock());
                }
                else if (string.Equals(entityName, "Poet", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(entityName, "Trader", StringComparison.OrdinalIgnoreCase))
                {
                    npc.SetShopInventory(CreatePoetStock());
                }
            }
            npc.InteractionStarted += OnNpcInteractionStarted;
            npc.DialogueRequested += OnNpcDialogueRequested;
            npc.ShopRequested += isMarketplaceNpc ? (Action<NPC, Player>)OnTraderShopRequested : OnNpcShopRequested;

            if (role == NpcRole.ClassSelector)
            {
                npc.ClassChangeRequested += OnNpcClassChangeRequested;
            }

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
            SyncRequested?.Invoke();
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
            _shopUI?.ShowShop(npc, player ?? _player);
            GD.Print($"[Lobby] Shop requested from {npc.EntityName}. Stock count: {npc.GetShopItems().Count}");
        }

        private IEnumerable<Item> CreateBlacksmithStock()
        {
            var stock = new List<Item>();

            AddIfNotNull(stock, _equipmentFactory.CreateFromAssetWithCategory("Assets/Equipments/sword/basic-sword.png", "sword", 1));
            AddIfNotNull(stock, _equipmentFactory.CreateFromAssetWithCategory("Assets/Equipments/chestplate/basic-chestplate.png", "chestplate", 1));
            AddIfNotNull(stock, _equipmentFactory.CreateFromAssetWithCategory("Assets/Equipments/gloves/basic-gloves.png", "gloves", 1));
            AddIfNotNull(stock, _equipmentFactory.CreateFromAssetWithCategory("Assets/Equipments/helmet/basic-helmet.png", "helmet", 1));
            AddIfNotNull(stock, _equipmentFactory.CreateFromAssetWithCategory("Assets/Equipments/shoes/basic-shoes.png", "shoes", 1));

            return stock;
        }

        private IEnumerable<Item> CreatePoetStock()
        {
            var stock = new List<Item>();

            // Add a variety of potions
            var small = ItemCatalog.CreatePotion(ItemCatalog.HpPotionS);
            var medium = ItemCatalog.CreatePotion(ItemCatalog.HpPotionM);
            var large = ItemCatalog.CreatePotion(ItemCatalog.HpPotionL);
            var burn = ItemCatalog.CreatePotion(ItemCatalog.BurnPotion);

            AddIfNotNull(stock, small);
            AddIfNotNull(stock, medium);
            AddIfNotNull(stock, large);
            AddIfNotNull(stock, burn);

            // Add tickets for different difficulties and set prices
            var normalTicket = ItemCatalog.CreateTicket(DifficultyLevel.Normal);
            var hardTicket = ItemCatalog.CreateTicket(DifficultyLevel.Hard);
            var nightmareTicket = ItemCatalog.CreateTicket(DifficultyLevel.Nightmare);

            if (normalTicket != null) normalTicket.Price = 100;
            if (hardTicket != null) hardTicket.Price = 250;
            if (nightmareTicket != null) nightmareTicket.Price = 600;

            AddIfNotNull(stock, normalTicket);
            AddIfNotNull(stock, hardTicket);
            AddIfNotNull(stock, nightmareTicket);

            return stock;
        }

        private void AddIfNotNull(List<Item> stock, Item item)
        {
            if (stock == null || item == null)
            {
                return;
            }

            stock.Add(item);
        }

        private void SetupShopUI()
        {
            _shopUI = new NpcShopUI();
            AddChild(_shopUI);
            _shopUI.Closed += OnShopClosed;
        }

        private void SetupMarketplaceUI()
        {
            _marketplaceUI = new MarketplaceUI();
            AddChild(_marketplaceUI);
            _marketplaceUI.Closed += OnShopClosed;
        }

        private void SetupClassSelectUI()
        {
            _classSelectUI = new ClassSelectUI();
            AddChild(_classSelectUI);
            _classSelectUI.ClassSelected += OnClassSelected;
        }

        private void OnNpcClassChangeRequested(NPC npc, Player player)
        {
            if (npc == null || _classSelectUI == null)
            {
                return;
            }

            Player target = player ?? _player;
            PlayerClass current = target?.PlayerClass ?? PlayerClass.Adventurer;
            int level = (int)(target?.Level ?? 1);

            _classSelectUI.Show(current, level);
            GD.Print($"[Lobby] Class selector opened by {npc.EntityName}. Current class: {current}, Player Level: {level}");
        }

        private void OnClassSelected(PlayerClass newClass)
        {
            Player target = _player;
            if (target == null)
            {
                return;
            }

            target.SetClass(newClass);
            GD.Print($"[Lobby] Player class set to {newClass}");
            ClassChangeRequested?.Invoke(newClass);
        }

        private void OnShopClosed()
        {
            GD.Print("[Lobby] Shop closed");
            ShopClosed?.Invoke();
        }

        private void OnTraderShopRequested(NPC npc, Player player)
        {
            if (npc == null || _marketplaceUI == null)
            {
                return;
            }

            _marketplaceUI.Initialize(player ?? _player, _apiClient, _authToken);
            _marketplaceUI.Show();
            GD.Print($"[Lobby] Marketplace requested from {npc.EntityName}");
        }

        private void SetupDifficultyUI()
        {
            _difficultyUI = new DifficultySelectionUI();
            AddChild(_difficultyUI);
            _difficultyUI.Initialize(_player);
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