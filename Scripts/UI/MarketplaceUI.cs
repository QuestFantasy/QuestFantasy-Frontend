using System;
using System.Collections.Generic;

using Godot;

using QuestFantasy.Characters;
using QuestFantasy.Core.Data.Items;

/// <summary>
/// Player-to-player marketplace UI.
/// Opened when the player interacts with the Trader NPC.
/// Tabs: Browse (buy others' listings) | My Listings (cancel) | List Item (sell from inventory)
/// </summary>
public class MarketplaceUI : CanvasLayer
{
    public event Action Closed;
    public event Action<string> StatusChanged;   // for Main to show feedback

    // Injected by LobbyManager
    private Player _player;
    private AuthApiClient _apiClient;
    private string _authToken;

    // ── UI roots ──────────────────────────────────────────────────────────
    private Control _root;
    private PanelContainer _panel;
    private Label _titleLabel;
    private Label _goldLabel;
    private Label _statusLabel;
    private TabContainer _tabs;
    private bool _isVisible;

    // Browse tab
    private GridContainer _browseGrid;
    private Button _refreshBtn;

    // My Listings tab
    private GridContainer _myGrid;
    private Button _myRefreshBtn;

    private Timer _listingFetchRetryTimer;
    private int _listingFetchRetryAttempts;
    private const float ListingFetchRetryDelaySeconds = 0.25f;
    private const int ListingFetchRetryLimit = 20;

    // List Item tab
    private GridContainer _inventoryGrid;
    private SpinBox _priceInput;
    private Button _listBtn;
    private Label _listHintLabel;
    private Item _selectedInventoryItem;

    // Live data
    private readonly List<ListingEntry> _allListings = new List<ListingEntry>();

    private sealed class ListingEntry
    {
        public int Id;
        public long SellerUserId;
        public string SellerUsername;
        public Item Item;
        public int Price;
    }

    // ─────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        PauseMode = PauseModeEnum.Process;
        BuildUi();
        Hide();
    }

    public void Initialize(Player player, AuthApiClient apiClient, string token)
    {
        _player = player;
        _apiClient = apiClient;
        _authToken = token;
    }

    public void UpdateToken(string token) => _authToken = token;

    public new void Show()
    {
        _isVisible = true;
        _root.Visible = true;
        _panel.Visible = true;
        GetTree().Paused = true;
        RefreshGold();
        SetStatus("Loading marketplace listings…");
        RebuildInventoryTab();
        _listingFetchRetryAttempts = 0;
        FetchListings();
    }

    public new void Hide()
    {
        _isVisible = false;
        if (_listingFetchRetryTimer != null)
        {
            _listingFetchRetryTimer.Stop();
        }
        if (_root != null) _root.Visible = false;
        if (_panel != null) _panel.Visible = false;
        GetTree().Paused = false;
    }

    public override void _Process(float delta)
    {
        if (_isVisible && Input.IsActionJustPressed("ui_cancel"))
        {
            Hide();
            Closed?.Invoke();
        }
    }

    // ─── API calls ────────────────────────────────────────────────────────

    private void FetchListings()
    {
        if (!_isVisible)
        {
            return;
        }

        if (_apiClient == null || string.IsNullOrEmpty(_authToken))
        {
            QueueListingFetchRetry("Waiting for auth session…");
            return;
        }

        if (_apiClient.IsBusy)
        {
            QueueListingFetchRetry("Another request is in progress — retrying shortly.");
            return;
        }

        if (_listingFetchRetryTimer != null)
        {
            _listingFetchRetryTimer.Stop();
        }

        _listingFetchRetryAttempts = 0;
        _apiClient.FetchMarketplaceListings(_authToken, OnListingsFetched);
    }

    private void OnListingsFetched(AuthApiResult result)
    {
        if (!result.NetworkOk || !result.IsSuccessStatus(200))
        {
            SetStatus("Failed to load marketplace: " + result.GetApiErrorMessage("Network error"));
            RebuildInventoryTab();
            return;
        }

        _allListings.Clear();
        var arr = result.ArrayData;
        if (arr != null)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (!(arr[i] is Godot.Collections.Dictionary d)) continue;
                var entry = ParseListing(d);
                if (entry != null) _allListings.Add(entry);
            }
        }

        RebuildBrowseTab();
        RebuildMyListingsTab();
        RebuildInventoryTab();
        SetStatus($"Marketplace refreshed — {_allListings.Count} listing(s) available.");
        RefreshGold();
    }

    private void QueueListingFetchRetry(string statusMessage)
    {
        if (!_isVisible)
        {
            return;
        }

        SetStatus(statusMessage);

        if (_listingFetchRetryTimer == null)
        {
            return;
        }

        if (_listingFetchRetryAttempts >= ListingFetchRetryLimit)
        {
            SetStatus("Marketplace load timed out. Press Refresh to try again.");
            return;
        }

        _listingFetchRetryAttempts += 1;
        _listingFetchRetryTimer.WaitTime = ListingFetchRetryDelaySeconds;
        _listingFetchRetryTimer.Start();
    }

    private void OnBuyPressed(ListingEntry entry)
    {
        if (entry == null || _apiClient == null || _apiClient.IsBusy) return;
        SetStatus($"Purchasing {entry.Item?.Name ?? "item"}…");
        _apiClient.BuyMarketplaceListing(_authToken, entry.Id, result =>
        {
            if (!result.NetworkOk || !result.IsSuccessStatus(200))
            {
                SetStatus("Purchase failed: " + result.GetApiErrorMessage("Unknown error"));
                return;
            }

            // Apply gold update returned from backend
            int gold = ReadInt(result.Data, "gold_remaining", _player?.Gold ?? 0);
            // Update local item too (item_data was transferred to this player)
            if (result.Data != null && result.Data.Contains("item_data") &&
                result.Data["item_data"] is Godot.Collections.Dictionary itemDict)
            {
                var item = PlayerItemSnapshotCodec.Decode(itemDict);
                _player?.AddItem(item);
            }

            if (_player != null)
            {
                // Force gold via SpendGold delta trick (avoid exposing SetGold)
                int delta2 = (_player.Gold) - gold;
                if (delta2 > 0) _player.SpendGold(delta2);
            }

            SetStatus($"Purchase successful!  Gold remaining: {gold}");
            FetchListings();
        });
    }

    private void OnCancelPressed(ListingEntry entry)
    {
        if (entry == null || _apiClient == null || _apiClient.IsBusy) return;
        SetStatus("Cancelling listing…");
        _apiClient.CancelMarketplaceListing(_authToken, entry.Id, result =>
        {
            if (!result.NetworkOk || !result.IsSuccessStatus(200))
            {
                SetStatus("Cancel failed: " + result.GetApiErrorMessage("Unknown error"));
                return;
            }
            SetStatus("Listing cancelled — item returned to your inventory.");
            // Return item to local inventory from listing
            var item = entry.Item;
            if (item != null) _player?.AddItem(item);
            FetchListings();
        });
    }

    private void OnListItemPressed()
    {
        if (_selectedInventoryItem == null) { SetStatus("Select an item from your inventory first."); return; }
        if (string.IsNullOrEmpty(_selectedInventoryItem.InstanceId))
        {
            SetStatus("This item has no server ID yet — close this window, open your backpack once to sync, then try again.");
            return;
        }

        int price = (int)_priceInput.Value;
        if (price < 1) { SetStatus("Price must be at least 1 gold."); return; }
        if (_apiClient == null || _apiClient.IsBusy) return;

        var itemDict = PlayerItemSnapshotCodec.Encode(_selectedInventoryItem);
        var payload = new Godot.Collections.Dictionary
        {
            ["item_data"] = itemDict,
            ["price"] = price,
        };

        SetStatus($"Listing {_selectedInventoryItem.Name} for {price} gold…");
        _apiClient.CreateMarketplaceListing(_authToken, payload, result =>
        {
            if (!result.NetworkOk || !result.IsSuccessStatus(201))
            {
                SetStatus("Failed to create listing: " + result.GetApiErrorMessage("Unknown error"));
                return;
            }

            if (!string.IsNullOrEmpty(_selectedInventoryItem.InstanceId))
            {
                _player?.RemoveItemByInstanceId(_selectedInventoryItem.InstanceId);
            }
            else
            {
                _player?.RemoveItem(_selectedInventoryItem);
            }

            _selectedInventoryItem = null;
            SetStatus("Item listed successfully!");
            RebuildInventoryTab();
            FetchListings();
        });
    }

    // ─── UI builders ──────────────────────────────────────────────────────

    private void BuildUi()
    {
        _root = new Control
        {
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Visible = false   // hidden until Show() is called
        };
        AddChild(_root);

        // Dark overlay
        var overlay = new ColorRect
        {
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 1,
            AnchorBottom = 1,
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(overlay);

        _panel = new PanelContainer
        {
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            RectMinSize = new Vector2(900f, 560f),
            RectPosition = new Vector2(-450f, -280f)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.08f, 0.12f, 0.98f),
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            BorderColor = new Color(0.4f, 0.55f, 0.8f, 0.9f),
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        _panel.AddStyleboxOverride("panel", style);
        _root.AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddConstantOverride("separation", 10);
        _panel.AddChild(vbox);

        // Header row
        var header = new HBoxContainer();
        header.AddConstantOverride("separation", 12);
        _titleLabel = new Label { Text = "Marketplace", SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill };
        _titleLabel.AddColorOverride("font_color", new Color(0.95f, 0.88f, 0.5f));
        header.AddChild(_titleLabel);

        _goldLabel = new Label { Text = "Gold: —", Align = Label.AlignEnum.Right };
        _goldLabel.AddColorOverride("font_color", new Color(0.98f, 0.85f, 0.3f));
        header.AddChild(_goldLabel);

        var closeBtn = new Button { Text = "✕", RectMinSize = new Vector2(36f, 36f) };
        closeBtn.Connect("pressed", this, nameof(OnClosePressed));
        header.AddChild(closeBtn);
        vbox.AddChild(header);

        // Status bar
        _statusLabel = new Label { Text = "", Autowrap = true };
        _statusLabel.AddColorOverride("font_color", new Color(0.75f, 0.88f, 1f));
        vbox.AddChild(_statusLabel);

        // Tabs
        _tabs = new TabContainer { SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill, SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill };
        vbox.AddChild(_tabs);

        _listingFetchRetryTimer = new Timer
        {
            OneShot = true,
            WaitTime = ListingFetchRetryDelaySeconds,
            PauseMode = PauseModeEnum.Process
        };
        _listingFetchRetryTimer.Connect("timeout", this, nameof(FetchListings));
        AddChild(_listingFetchRetryTimer);

        BuildBrowseTab();
        BuildMyListingsTab();
        BuildListItemTab();
    }

    private void BuildBrowseTab()
    {
        var tab = new VBoxContainer { Name = "Browse" };
        tab.AddConstantOverride("separation", 8);

        var hintBrowse = new Label { Text = "Browse items listed by other players. Double-check your gold before purchasing.", Autowrap = true };
        hintBrowse.AddColorOverride("font_color", new Color(0.72f, 0.82f, 0.95f));
        tab.AddChild(hintBrowse);

        var toolbar = new HBoxContainer();
        _refreshBtn = new Button { Text = "↺  Refresh" };
        _refreshBtn.Connect("pressed", this, nameof(FetchListings));
        toolbar.AddChild(_refreshBtn);
        tab.AddChild(toolbar);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            RectMinSize = new Vector2(0, 360)
        };
        _browseGrid = new GridContainer { Columns = 4, SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill };
        _browseGrid.AddConstantOverride("hseparation", 10);
        _browseGrid.AddConstantOverride("vseparation", 10);
        scroll.AddChild(_browseGrid);
        tab.AddChild(scroll);

        _tabs.AddChild(tab);
    }

    private void BuildMyListingsTab()
    {
        var tab = new VBoxContainer { Name = "My Listings" };
        tab.AddConstantOverride("separation", 8);

        var hintMine = new Label { Text = "Items you are currently selling. Click Cancel Listing to retrieve an item back into your inventory.", Autowrap = true };
        hintMine.AddColorOverride("font_color", new Color(0.72f, 0.82f, 0.95f));
        tab.AddChild(hintMine);

        var toolbar = new HBoxContainer();
        _myRefreshBtn = new Button { Text = "↺  Refresh" };
        _myRefreshBtn.Connect("pressed", this, nameof(FetchListings));
        toolbar.AddChild(_myRefreshBtn);
        tab.AddChild(toolbar);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            RectMinSize = new Vector2(0, 380)
        };
        _myGrid = new GridContainer { Columns = 4, SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill };
        _myGrid.AddConstantOverride("hseparation", 10);
        _myGrid.AddConstantOverride("vseparation", 10);
        scroll.AddChild(_myGrid);
        tab.AddChild(scroll);

        _tabs.AddChild(tab);
    }

    private void BuildListItemTab()
    {
        var tab = new VBoxContainer { Name = "List Item" };
        tab.AddConstantOverride("separation", 10);

        _listHintLabel = new Label { Text = "Select an item from your inventory below, set a price, then click List Item.\nNote: items need a server ID (shown in blue) before they can be listed — open your Backpack once to sync.", Autowrap = true };
        _listHintLabel.AddColorOverride("font_color", new Color(0.8f, 0.85f, 0.95f));
        tab.AddChild(_listHintLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill,
            RectMinSize = new Vector2(0, 280)
        };
        _inventoryGrid = new GridContainer { Columns = 5, SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill };
        _inventoryGrid.AddConstantOverride("hseparation", 8);
        _inventoryGrid.AddConstantOverride("vseparation", 8);
        scroll.AddChild(_inventoryGrid);
        tab.AddChild(scroll);

        var priceRow = new HBoxContainer();
        priceRow.AddConstantOverride("separation", 10);
        priceRow.AddChild(new Label { Text = "Price (gold):" });
        _priceInput = new SpinBox { MinValue = 1, MaxValue = 9999999, Value = 100, Step = 1, RectMinSize = new Vector2(120, 0) };
        priceRow.AddChild(_priceInput);
        tab.AddChild(priceRow);

        _listBtn = new Button { Text = "List Selected Item", RectMinSize = new Vector2(180, 40) };
        _listBtn.Connect("pressed", this, nameof(OnListItemPressed));
        tab.AddChild(_listBtn);

        _tabs.AddChild(tab);
    }

    // ─── Rebuild helpers ──────────────────────────────────────────────────

    private void RebuildBrowseTab()
    {
        ClearContainer(_browseGrid);

        if (_allListings.Count == 0)
        {
            _browseGrid.AddChild(MakeInfoLabel("No marketplace listings yet. Check back later!"));
            return;
        }

        foreach (var entry in _allListings)
            _browseGrid.AddChild(MakeBrowseCard(entry));
    }

    private void RebuildMyListingsTab()
    {
        ClearContainer(_myGrid);

        var mine = new List<ListingEntry>();
        foreach (var e in _allListings)
            if (IsMyListing(e)) mine.Add(e);

        if (mine.Count == 0)
        {
            _myGrid.AddChild(MakeInfoLabel("You have no active listings. Go to the \"List Item\" tab to sell something!"));
            return;
        }

        foreach (var entry in mine)
            _myGrid.AddChild(MakeMyListingCard(entry));
    }

    private void RebuildInventoryTab()
    {
        ClearContainer(_inventoryGrid);
        _inventoryItemMap.Clear();
        _selectedInventoryItem = null;

        if (_player == null) { _inventoryGrid.AddChild(MakeInfoLabel("No player data available.")); return; }

        var items = _player.InventoryItems;
        if (items == null || items.Count == 0)
        {
            _inventoryGrid.AddChild(MakeInfoLabel("Your inventory is empty — go defeat some monsters!"));
            return;
        }

        foreach (var item in items)
        {
            if (item == null) continue;
            _inventoryGrid.AddChild(MakeInventoryCard(item));
        }
    }

    // ─── Card builders ────────────────────────────────────────────────────

    private Control MakeBrowseCard(ListingEntry entry)
    {
        var card = MakeBaseCard(entry.Item, out var vbox);

        var seller = new Label
        {
            Text = $"Seller: {entry.SellerUsername}",
            Align = Label.AlignEnum.Center
        };
        seller.AddColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
        vbox.AddChild(seller);

        var price = new Label { Text = $"💰 {entry.Price} gold", Align = Label.AlignEnum.Center };
        price.AddColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        vbox.AddChild(price);

        var btn = new Button { Text = "Buy", RectMinSize = new Vector2(0, 32) };
        btn.AddColorOverride("font_color", new Color(0.2f, 1f, 0.5f));
        var captured = entry;
        btn.Connect("pressed", this, nameof(OnBuyPressedProxy), new Godot.Collections.Array { captured.Id });
        vbox.AddChild(btn);

        return card;
    }

    private Control MakeMyListingCard(ListingEntry entry)
    {
        var card = MakeBaseCard(entry.Item, out var vbox);

        var price = new Label { Text = $"💰 {entry.Price} gold", Align = Label.AlignEnum.Center };
        price.AddColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
        vbox.AddChild(price);

        var btn = new Button { Text = "Cancel Listing", RectMinSize = new Vector2(0, 32) };
        btn.AddColorOverride("font_color", new Color(1f, 0.5f, 0.4f));
        btn.Connect("pressed", this, nameof(OnCancelPressedProxy), new Godot.Collections.Array { entry.Id });
        vbox.AddChild(btn);

        return card;
    }

    private Control MakeInventoryCard(Item item)
    {
        var panel = new PanelContainer { RectMinSize = new Vector2(110, 130), MouseFilter = Control.MouseFilterEnum.Stop };
        var cardStyle = MakeCardStyle(false);
        panel.AddStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        vbox.AddConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var icon = new TextureRect { RectMinSize = new Vector2(64, 64), Expand = true, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
        icon.Texture = ResolveTexture(item);
        vbox.AddChild(icon);

        var name = new Label { Text = item.Name ?? "?", Align = Label.AlignEnum.Center, Autowrap = true };
        vbox.AddChild(name);

        bool hasId = !string.IsNullOrEmpty(item.InstanceId);
        var idLabel = new Label
        {
            Text = hasId ? $"ID: {item.InstanceId.Substring(0, 6)}…" : "No ID — sync needed",
            Align = Label.AlignEnum.Center
        };
        idLabel.HintTooltip = hasId
            ? $"Instance ID: {item.InstanceId}"
            : "Open your Backpack once to assign a server ID to this item.";
        idLabel.AddColorOverride("font_color", hasId ? new Color(0.55f, 0.75f, 1f) : new Color(1f, 0.5f, 0.3f));
        vbox.AddChild(idLabel);

        // Use index-based selection: store item ref keyed by vbox instance id.
        panel.SetMeta("item_instance_id", item.InstanceId ?? "");

        var sel = new Button { Text = "Select", RectMinSize = new Vector2(0, 28), MouseFilter = Control.MouseFilterEnum.Stop };
        var capturedItem = item;
        sel.Connect("pressed", this, nameof(OnInventoryItemSelected), new Godot.Collections.Array { vbox.GetInstanceId() });
        sel.SetMeta("item_ref_id", vbox.GetInstanceId());
        _inventoryItemMap[vbox.GetInstanceId()] = item;
        vbox.AddChild(sel);

        return panel;
    }

    // Map vbox instance id → Item so we can retrieve on button press
    private readonly Dictionary<ulong, Item> _inventoryItemMap = new Dictionary<ulong, Item>();

    private void OnInventoryItemSelected(ulong vboxId)
    {
        if (_inventoryItemMap.TryGetValue(vboxId, out Item item))
        {
            _selectedInventoryItem = item;
            string idText = string.IsNullOrEmpty(item.InstanceId) ? "(no ID — open Backpack to sync)" : item.InstanceId;
            SetStatus($"Selected: {item.Name}  |  Instance ID: {idText}");
        }
    }

    // Proxy methods for Godot signal binding (can't pass objects directly)
    private void OnBuyPressedProxy(int listingId)
    {
        foreach (var e in _allListings) { if (e.Id == listingId) { OnBuyPressed(e); return; } }
    }

    private void OnCancelPressedProxy(int listingId)
    {
        foreach (var e in _allListings) { if (e.Id == listingId) { OnCancelPressed(e); return; } }
    }

    // ─── Shared card helper ───────────────────────────────────────────────

    private PanelContainer MakeBaseCard(Item item, out VBoxContainer vbox)
    {
        var panel = new PanelContainer { RectMinSize = new Vector2(150, 160), MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddStyleboxOverride("panel", MakeCardStyle(false));

        vbox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        vbox.AddConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var icon = new TextureRect { RectMinSize = new Vector2(72, 72), Expand = true, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
        icon.Texture = ResolveTexture(item);
        vbox.AddChild(icon);

        var name = new Label { Text = item?.Name ?? "Unknown Item", Align = Label.AlignEnum.Center, Autowrap = true };
        vbox.AddChild(name);

        return panel;
    }

    private StyleBoxFlat MakeCardStyle(bool selected)
    {
        return new StyleBoxFlat
        {
            BgColor = selected ? new Color(0.18f, 0.17f, 0.1f) : new Color(0.1f, 0.12f, 0.18f, 0.95f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            BorderColor = selected ? new Color(0.96f, 0.84f, 0.34f) : new Color(0.3f, 0.38f, 0.52f),
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            BorderWidthBottom = selected ? 2 : 1,
            BorderWidthLeft = selected ? 2 : 1,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
    }

    // ─── Parsing helpers ──────────────────────────────────────────────────

    private ListingEntry ParseListing(Godot.Collections.Dictionary d)
    {
        if (d == null) return null;

        int id = ReadInt(d, "id", -1);
        if (id < 0) return null;

        long sellerUserId = ReadLong(d, "seller_user_id", -1);
        if (sellerUserId < 0) sellerUserId = ReadLong(d, "seller_id", -1);
        if (sellerUserId < 0) sellerUserId = ReadLong(d, "user_id", -1);

        string seller = ReadString(d, "seller_username");
        if (string.IsNullOrWhiteSpace(seller)) seller = ReadString(d, "seller_name");
        if (string.IsNullOrWhiteSpace(seller)) seller = ReadString(d, "username");
        if (string.IsNullOrWhiteSpace(seller)) seller = ReadString(d, "seller");
        int price = ReadInt(d, "price", 0);

        Item item = null;
        if (d.Contains("item_data") && d["item_data"] is Godot.Collections.Dictionary itemDict)
            item = PlayerItemSnapshotCodec.Decode(itemDict);

        item = item ?? new Item { Name = "Unknown Item" };

        return new ListingEntry { Id = id, SellerUserId = sellerUserId, SellerUsername = seller, Item = item, Price = price };
    }

    // ─── Utility ──────────────────────────────────────────────────────────

    private void RefreshGold()
    {
        if (_goldLabel != null)
            _goldLabel.Text = $"Gold: {_player?.Gold ?? 0}";
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null) _statusLabel.Text = msg;
        StatusChanged?.Invoke(msg);
        GD.Print("[Marketplace] " + msg);
    }

    private string GetMyUsername()
    {
        // AuthStorage holds the current username
        if (AuthStorage.TryLoadSession(out AuthSession session))
            return session.Username ?? "";
        return "";
    }

    private long GetMyUserId()
    {
        if (AuthStorage.TryLoadSession(out AuthSession session))
            return session.UserId;
        return 0;
    }

    private bool IsMyListing(ListingEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        long myUserId = GetMyUserId();
        if (myUserId > 0 && entry.SellerUserId > 0 && entry.SellerUserId == myUserId)
        {
            return true;
        }

        string myUsername = NormalizeIdentity(GetMyUsername());
        string sellerUsername = NormalizeIdentity(entry.SellerUsername);
        return !string.IsNullOrEmpty(myUsername) && myUsername == sellerUsername;
    }

    private static string ReadString(Godot.Collections.Dictionary d, string key)
    {
        if (d == null || !d.Contains(key) || d[key] == null)
        {
            return string.Empty;
        }

        return d[key].ToString();
    }

    private static long ReadLong(Godot.Collections.Dictionary d, string key, long fallback)
    {
        if (d == null || !d.Contains(key) || d[key] == null)
        {
            return fallback;
        }

        var raw = d[key];
        if (raw is long l)
        {
            return l;
        }

        if (raw is int i)
        {
            return i;
        }

        if (long.TryParse(raw.ToString(), out long parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string NormalizeIdentity(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private Texture ResolveTexture(Item item)
    {
        if (item is Equipment eq)
        {
            if (eq.Sprite != null) return eq.Sprite;
            if (!string.IsNullOrEmpty(eq.SpritePath)) return GD.Load<Texture>(eq.SpritePath);
        }
        else if (item is Weapon w)
        {
            if (w.Sprite != null) return w.Sprite;
            if (!string.IsNullOrEmpty(w.SpritePath)) return GD.Load<Texture>(w.SpritePath);
        }
        return null;
    }

    private static int ReadInt(Godot.Collections.Dictionary d, string key, int fallback)
    {
        if (d == null || !d.Contains(key) || d[key] == null) return fallback;
        var raw = d[key];
        if (raw is int i) return i;
        if (raw is long l) return (int)l;
        if (int.TryParse(raw.ToString(), out int p)) return p;
        return fallback;
    }

    private static void ClearContainer(Container c)
    {
        if (c == null) return;
        foreach (Node child in c.GetChildren()) child.QueueFree();
    }

    private static Label MakeInfoLabel(string text)
    {
        var l = new Label { Text = text, Autowrap = true };
        l.AddColorOverride("font_color", new Color(0.65f, 0.7f, 0.8f));
        return l;
    }

    private void OnClosePressed()
    {
        Hide();
        Closed?.Invoke();
    }
}