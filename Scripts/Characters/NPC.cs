using System;
using Godot;
using QuestFantasy.Systems.Inventory;

namespace QuestFantasy.Characters
{
    /// <summary>
    /// Non-playable character class. Handles NPC interactions like trading, dialogue, and quests.
    /// </summary>
    public class NPC : Character
    {
        public string Dialogue { get; set; } = "Hello, traveler!";
        public Bag ShopInventory { get; private set; } = new Bag();
        public bool IsShopkeeper { get; set; }
        
        public event Action<NPC> OnInteractionTriggered;
        
        public override void _Ready()
        {
            // Initialize base character attributes
            InitializeCharacter();
        }

        /// <summary>
        /// Handle NPC interaction (dialogue, trading, etc.)
        /// </summary>
        public void Interact(Player player)
        {
            if (player == null)
            {
                GD.PrintErr($"[NPC] {EntityName}: Cannot interact with null player");
                return;
            }
            
            GD.Print($"[NPC] {EntityName} says: {Dialogue}");
            OnInteractionTriggered?.Invoke(this);
        }

        /// <summary>
        /// Trade items with the player.
        /// </summary>
        public bool Trade(Player player, Item playerItem, Item shopItem, int tradePrice)
        {
            if (player == null)
            {
                GD.PrintErr($"[NPC] {EntityName}: Cannot trade with null player");
                return false;
            }

            if (!IsShopkeeper)
            {
                GD.Print($"[NPC] {EntityName}: I'm not a shopkeeper");
                return false;
            }

            if (player.Gold < tradePrice)
            {
                GD.Print($"[NPC] {EntityName}: You don't have enough gold! Need {tradePrice}, have {player.Gold}");
                return false;
            }

            // Process trade
            player.AddGold(-tradePrice);
            player.AddItem(shopItem);
            GD.Print($"[NPC] {EntityName}: Thank you for your business!");
            
            return true;
        }

        /// <summary>
        /// Update NPC attributes. NPCs typically don't level like monsters.
        /// </summary>
        public override void UpdateAttributes()
        {
            // NPCs can have attributes for combat, but typically don't update them
            if (Attributes != null && Abilities != null)
            {
                Attributes.TotalAtk = Abilities.Atk;
                Attributes.TotalDef = Abilities.Def;
                Attributes.TotalSpd = Abilities.Spd;
                Attributes.TotalVit = Abilities.Vit;
            }
        }
    }
}