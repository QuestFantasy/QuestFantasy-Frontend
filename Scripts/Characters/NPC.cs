using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Core.Base;
using QuestFantasy.Core.Data.Attributes;
using QuestFantasy.Core.Data.Items;
using QuestFantasy.Systems.Inventory;

namespace QuestFantasy.Characters
{
    public enum NpcRole
    {
        Guide,
        Merchant,
        Blacksmith
    }

    /// <summary>
    /// Non-playable character class. Handles NPC interactions like trading, dialogue, and quests.
    /// </summary>
    public class NPC : Character
    {
        private const float DefaultInteractionRangePixels = 80f;
        private const float PortraitDrawScale = 0.20f;

        private Player _nearbyPlayer;
        private bool _playerInRange;
        private Label _namePlateLabel;
        private Label _interactionPromptLabel;
        private Texture _portraitTexture;

        public string Dialogue { get; private set; } = "Hello, traveler!";
        public NpcRole Role { get; private set; } = NpcRole.Guide;
        public bool IsShopkeeper { get; private set; }
        public float InteractionRangePixels { get; set; } = DefaultInteractionRangePixels;
        public Bag ShopInventory { get; private set; } = new Bag { MaxSlots = 0 };

        public event Action<NPC, Player> InteractionStarted;
        public event Action<NPC, Player> DialogueRequested;
        public event Action<NPC, Player> ShopRequested;

        public override void _Ready()
        {
            InitializeCharacter();
            SetProcess(true);
            BuildOverlayLabels();
            Update();
        }

        public void Initialize(string entityName, string dialogue, NpcRole role, bool isShopkeeper = false)
        {
            EntityName = string.IsNullOrWhiteSpace(entityName) ? "NPC" : entityName;
            Name = EntityName;
            Dialogue = string.IsNullOrWhiteSpace(dialogue) ? "Hello, traveler!" : dialogue;
            Role = role;
            IsShopkeeper = isShopkeeper;
            _portraitTexture = LoadPortraitTexture(role, EntityName);

            if (ShopInventory == null)
            {
                ShopInventory = new Bag { MaxSlots = 0 };
            }
        }

        public void SetBaseTint(Color tint)
        {
            _ = tint;
        }

        private void BuildOverlayLabels()
        {
            _namePlateLabel = new Label
            {
                Text = EntityName,
                Align = Label.AlignEnum.Center,
                RectPosition = new Vector2(-64f, -52f),
                RectMinSize = new Vector2(128f, 18f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_namePlateLabel);

            _interactionPromptLabel = new Label
            {
                Text = GetInteractionPromptText(),
                Align = Label.AlignEnum.Center,
                RectPosition = new Vector2(-80f, 24f),
                RectMinSize = new Vector2(160f, 18f),
                Visible = false,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_interactionPromptLabel);
        }

        private string GetInteractionPromptText()
        {
            return IsShopkeeper ? "Press F to talk / trade" : "Press F to talk";
        }

        public override void _Process(float delta)
        {
            try
            {
                _nearbyPlayer = ResolveNearbyPlayer();
                if (_nearbyPlayer == null)
                {
                    SetInRangeState(false);
                    return;
                }

                bool inRange = Position.DistanceTo(_nearbyPlayer.Position) <= InteractionRangePixels;
                SetInRangeState(inRange);

                if (inRange && Input.IsActionJustPressed("interact"))
                {
                    OnInteract(_nearbyPlayer);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("[NPC] Exception in _Process: " + ex.Message);
                GD.PrintErr(ex.StackTrace);
            }
        }

        public override void OnInteract(Creature interactor)
        {
            if (!(interactor is Player player))
            {
                return;
            }

            GD.Print($"[NPC] {EntityName} says: {Dialogue}");
            InteractionStarted?.Invoke(this, player);
            DialogueRequested?.Invoke(this, player);

            if (IsShopkeeper)
            {
                ShopRequested?.Invoke(this, player);
            }
        }

        public IReadOnlyList<Item> GetShopItems()
        {
            return ShopInventory.Items.AsReadOnly();
        }

        public void SetShopInventory(IEnumerable<Item> items)
        {
            if (ShopInventory == null)
            {
                ShopInventory = new Bag { MaxSlots = 0 };
            }

            ShopInventory.Clear();

            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item != null)
                {
                    ShopInventory.AddItem(item);
                }
            }
        }

        public bool AddShopItem(Item item)
        {
            if (ShopInventory == null)
            {
                ShopInventory = new Bag { MaxSlots = 0 };
            }

            return item != null && ShopInventory.AddItem(item);
        }

        public bool TrySellToPlayer(Player player, Item item, int price)
        {
            if (!ValidateTradeParticipant(player, item, price))
            {
                return false;
            }

            if (!player.SpendGold(price))
            {
                GD.Print($"[NPC] {EntityName}: Player does not have enough gold to buy {item.Name}");
                return false;
            }

            Item purchasedItem = CreateShopItemCopy(item);
            if (purchasedItem == null)
            {
                player.AddGold(price);
                GD.PrintErr($"[NPC] {EntityName}: Could not clone {item.Name} for purchase");
                return false;
            }

            if (!player.AddItem(purchasedItem))
            {
                player.AddGold(price);
                GD.Print($"[NPC] {EntityName}: Player inventory is full, trade cancelled");
                return false;
            }

            GD.Print($"[NPC] {EntityName}: Sold {item.Name} for {price} gold");
            return true;
        }

        public bool TryBuyFromPlayer(Player player, Item item, int price)
        {
            if (!ValidateTradeParticipant(player, item, price))
            {
                return false;
            }

            bool removed = !string.IsNullOrWhiteSpace(item.InstanceId)
                ? player.RemoveItemByInstanceId(item.InstanceId)
                : player.RemoveItem(item);

            if (!removed)
            {
                GD.Print($"[NPC] {EntityName}: Player does not have {item.Name}");
                return false;
            }

            if (!ShopInventory.AddItem(item))
            {
                player.AddItem(item);
                GD.Print($"[NPC] {EntityName}: Shop inventory is full, trade cancelled");
                return false;
            }

            player.AddGold(price);
            GD.Print($"[NPC] {EntityName}: Bought {item.Name} for {price} gold");
            return true;
        }

        private Item CreateShopItemCopy(Item item)
        {
            if (item == null)
            {
                return null;
            }

            if (item is Equipment equipment)
            {
                return new Equipment
                {
                    Name = equipment.Name,
                    Description = equipment.Description,
                    Price = equipment.Price,
                    Quantity = equipment.Quantity,
                    EquipmentType = equipment.EquipmentType,
                    EquipmentAbilities = equipment.EquipmentAbilities == null
                        ? null
                        : new Abilities
                        {
                            Atk = equipment.EquipmentAbilities.Atk,
                            Def = equipment.EquipmentAbilities.Def,
                            Spd = equipment.EquipmentAbilities.Spd,
                            Vit = equipment.EquipmentAbilities.Vit
                        },
                    Rarity = equipment.Rarity,
                    LevelRequirement = equipment.LevelRequirement,
                    SpritePath = equipment.SpritePath,
                    Source = equipment.Source,
                    Sprite = equipment.Sprite
                };
            }

            if (item is Weapon weapon)
            {
                return new Weapon
                {
                    Name = weapon.Name,
                    Description = weapon.Description,
                    Price = weapon.Price,
                    Quantity = weapon.Quantity,
                    WeaponType = weapon.WeaponType,
                    WeaponAbilities = weapon.WeaponAbilities == null
                        ? null
                        : new Abilities
                        {
                            Atk = weapon.WeaponAbilities.Atk,
                            Def = weapon.WeaponAbilities.Def,
                            Spd = weapon.WeaponAbilities.Spd,
                            Vit = weapon.WeaponAbilities.Vit
                        },
                    Rarity = weapon.Rarity,
                    LevelRequirement = weapon.LevelRequirement,
                    SpritePath = weapon.SpritePath,
                    Source = weapon.Source,
                    Sprite = weapon.Sprite
                };
            }

            return new Item
            {
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Quantity = item.Quantity
            };
        }

        /// <summary>
        /// Update NPC attributes. NPCs typically don't level like monsters.
        /// </summary>
        public override void UpdateAttributes()
        {
            if (Attributes != null && Abilities != null)
            {
                Attributes.TotalAtk = Abilities.Atk;
                Attributes.TotalDef = Abilities.Def;
                Attributes.TotalSpd = Abilities.Spd;
                Attributes.TotalVit = Abilities.Vit;
            }
        }

        public override void _Draw()
        {
            if (_portraitTexture != null)
            {
                Vector2 portraitSize = _portraitTexture.GetSize() * PortraitDrawScale;
                var portraitRect = new Rect2(new Vector2(-portraitSize.x / 2f, -12f - portraitSize.y / 2f), portraitSize);
                DrawTextureRect(_portraitTexture, portraitRect, false);
            }
            else
            {
                Vector2 bodyCenter = new Vector2(0f, -4f);
                Vector2 bodySize = new Vector2(22f, 28f);

                Color bodyColor = IsShopkeeper
                    ? new Color(0.55f, 0.72f, 1f)
                    : new Color(0.68f, 0.78f, 0.72f);

                DrawRect(new Rect2(bodyCenter.x - bodySize.x / 2f, bodyCenter.y - bodySize.y / 2f, bodySize.x, bodySize.y), bodyColor);
                DrawCircle(new Vector2(0f, -24f), 9f, new Color(0.93f, 0.82f, 0.72f));

                if (IsShopkeeper)
                {
                    DrawRect(new Rect2(-7f, -7f, 14f, 10f), new Color(0.18f, 0.2f, 0.24f));
                }
                else
                {
                    DrawRect(new Rect2(-6f, -8f, 12f, 6f), new Color(0.18f, 0.2f, 0.24f));
                }

                DrawCircle(new Vector2(-4f, -26f), 1.2f, Colors.Black);
                DrawCircle(new Vector2(4f, -26f), 1.2f, Colors.Black);
            }

            Update();
        }

        private Texture LoadPortraitTexture(NpcRole role, string entityName)
        {
            string texturePath = ResolvePortraitPath(role, entityName);
            if (string.IsNullOrEmpty(texturePath))
            {
                return null;
            }

            return GD.Load<Texture>(texturePath);
        }

        private string ResolvePortraitPath(NpcRole role, string entityName)
        {
            switch (role)
            {
                case NpcRole.Blacksmith:
                    return "res://Assets/NPC/NPC-blacksmith.png";
                case NpcRole.Merchant:
                    return "res://Assets/NPC/NPC-poet.png";
                case NpcRole.Guide:
                default:
                    return "res://Assets/NPC/NPC-previous-hero.png";
            }
        }

        private Player ResolveNearbyPlayer()
        {
            Node parent = GetParent();
            if (parent == null)
            {
                return null;
            }

            return parent.GetNodeOrNull<Player>("Player");
        }

        private void SetInRangeState(bool inRange)
        {
            if (inRange == _playerInRange)
            {
                return;
            }

            _playerInRange = inRange;
            if (_interactionPromptLabel != null)
            {
                _interactionPromptLabel.Visible = inRange;
            }
        }

        private bool ValidateTradeParticipant(Player player, Item item, int price)
        {
            if (player == null)
            {
                GD.PrintErr($"[NPC] {EntityName}: Cannot trade with null player");
                return false;
            }

            if (item == null)
            {
                GD.PrintErr($"[NPC] {EntityName}: Cannot trade null item");
                return false;
            }

            if (!IsShopkeeper)
            {
                GD.Print($"[NPC] {EntityName}: This NPC is not marked as a shopkeeper");
                return false;
            }

            if (price < 0)
            {
                GD.PrintErr($"[NPC] {EntityName}: Trade price cannot be negative");
                return false;
            }

            return true;
        }
    }
}