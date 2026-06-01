using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Godot;

using QuestFantasy.Core.Data;

namespace QuestFantasy.UI
{
    public class ClassSelectUI : CanvasLayer
    {
        // ── Layout constants ──────────────────────────────────────────────
        private const float PanelWidth = 760f;
        private const float PanelHeight = 500f;
        private const float CardWidth = 162f;
        private const float CardHeight = 270f;
        private const float CardSpacing = 14f;
        private const float HeaderHeight = 76f;
        private const float FooterHeight = 60f;

        // ── Palette ───────────────────────────────────────────────────────
        private static readonly Color BgOverlay = new Color(0f, 0f, 0f, 0.78f);
        private static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.13f, 0.97f);
        private static readonly Color PanelBorder = new Color(0.30f, 0.55f, 1.00f, 0.70f);
        private static readonly Color HeaderColor = new Color(0.86f, 0.73f, 1.00f, 1f);
        private static readonly Color SubHeaderColor = new Color(0.62f, 0.72f, 0.90f, 1f);
        private static readonly Color CardBgNormal = new Color(0.12f, 0.14f, 0.20f, 1f);
        private static readonly Color CardBgSelected = new Color(0.16f, 0.24f, 0.42f, 1f);
        private static readonly Color CardBorderNormal = new Color(0.25f, 0.30f, 0.45f, 1f);
        private static readonly Color CardBorderSelect = new Color(0.40f, 0.70f, 1.00f, 1f);
        private static readonly Color CardTitleColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color CardDescColor = new Color(0.75f, 0.82f, 0.95f, 1f);
        private static readonly Color SkillLabelColor = new Color(0.55f, 0.88f, 0.60f, 1f);
        private static readonly Color BtnConfirmBg = new Color(0.20f, 0.55f, 0.95f, 1f);
        private static readonly Color BtnConfirmHover = new Color(0.30f, 0.65f, 1.00f, 1f);
        private static readonly Color BtnCloseBg = new Color(0.20f, 0.22f, 0.28f, 1f);
        private static readonly Color BtnCloseHover = new Color(0.30f, 0.33f, 0.42f, 1f);

        // Tab colors
        private static readonly Color TabNormal = new Color(0.15f, 0.18f, 0.25f, 1f);
        private static readonly Color TabSelected = new Color(0.25f, 0.45f, 0.85f, 1f);

        // ── State ─────────────────────────────────────────────────────────
        private PlayerClass _currentClass = PlayerClass.Adventurer;
        private PlayerClass _selectedClass = PlayerClass.Adventurer;
        private int _playerLevel = 1;

        // Skill state
        private readonly List<string> _equippedSkillIds = new List<string>();
        private ReadOnlyCollection<SkillDefinition> _availableSkills;

        // UI References
        private Control _root;
        private Label _subtitleLabel;
        private Control _classTabContent;
        private Control _skillTabContent;
        private Button _tabClassBtn;
        private Button _tabSkillsBtn;
        private Panel[] _cardPanels;

        // Skill UI References
        private Label[] _skillSlotLabels;
        private Panel[] _skillCardPanels;

        public event Action<PlayerClass> ClassSelected;
        public event Action<List<string>> SkillLoadoutChanged;

        private static readonly PlayerClass[] AllClasses =
        {
            PlayerClass.Adventurer,
            PlayerClass.Mage,
            PlayerClass.Archer,
            PlayerClass.Warrior
        };

        // ── Class accent colours ──────────────────────────────────────────
        private static Color GetClassAccent(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return new Color(0.75f, 0.40f, 1.00f, 1f);
                case PlayerClass.Archer: return new Color(0.40f, 0.90f, 0.50f, 1f);
                case PlayerClass.Warrior: return new Color(1.00f, 0.50f, 0.25f, 1f);
                default: return new Color(0.40f, 0.70f, 1.00f, 1f);
            }
        }

        private static string GetClassEmoji(PlayerClass cls)
        {
            switch (cls)
            {
                case PlayerClass.Mage: return "🔮";
                case PlayerClass.Archer: return "🏹";
                case PlayerClass.Warrior: return "⚔️";
                default: return "🗺️";
            }
        }

        // ─────────────────────────────────────────────────────────────────

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            BuildLayout();
            _root.Visible = false;
        }

        public void Show(PlayerClass currentClass, int playerLevel, IReadOnlyList<string> currentEquippedSkills = null)
        {
            _currentClass = currentClass;
            _selectedClass = currentClass;
            _playerLevel = playerLevel;

            _equippedSkillIds.Clear();
            if (currentEquippedSkills != null)
            {
                _equippedSkillIds.AddRange(currentEquippedSkills);
            }
            else
            {
                _equippedSkillIds.AddRange(PlayerClassData.GetDefaultSkillLoadout(_currentClass));
            }

            _availableSkills = PlayerClassData.GetAllSkillDefinitions(_currentClass);

            if (_subtitleLabel != null)
            {
                _subtitleLabel.Text = $"Different classes unlock different skills.  You can change class again any time. (Level Requirement: Lv.{playerLevel}/{GameConstants.CLASS_CHANGE_MIN_LEVEL})";
            }
            RefreshCardHighlights();
            SwitchTab(0);
            _root.Visible = true;
        }

        public new void Hide()
        {
            if (_root != null)
            {
                _root.Visible = false;
            }
        }

        // ── Layout ────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            _root = new Control();
            _root.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _root.MouseFilter = Control.MouseFilterEnum.Stop;
            AddChild(_root);

            var overlay = new ColorRect
            {
                Color = BgOverlay,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            overlay.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _root.AddChild(overlay);

            var panel = new Panel();
            panel.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Center);
            panel.MarginLeft = -PanelWidth / 2f;
            panel.MarginTop = -PanelHeight / 2f;
            panel.MarginRight = PanelWidth / 2f;
            panel.MarginBottom = PanelHeight / 2f;
            panel.AddStyleboxOverride("panel", MakePanelStyle());
            _root.AddChild(panel);

            // Tabs
            var tabsBox = new HBoxContainer();
            tabsBox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            tabsBox.MarginLeft = 24f;
            tabsBox.MarginRight = -24f;
            tabsBox.MarginTop = 16f;
            tabsBox.MarginBottom = 50f;
            panel.AddChild(tabsBox);

            _tabClassBtn = CreateStyledButton("⚡ Change Class", TabSelected, TabSelected);
            _tabClassBtn.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            _tabClassBtn.Connect("pressed", this, nameof(OnTabPressed), new Godot.Collections.Array { 0 });
            tabsBox.AddChild(_tabClassBtn);

            _tabSkillsBtn = CreateStyledButton("🗡 Skills", TabNormal, TabSelected);
            _tabSkillsBtn.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            _tabSkillsBtn.Connect("pressed", this, nameof(OnTabPressed), new Godot.Collections.Array { 1 });
            tabsBox.AddChild(_tabSkillsBtn);

            _classTabContent = new Control();
            _classTabContent.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _classTabContent.MarginTop = 50f;
            panel.AddChild(_classTabContent);

            _skillTabContent = new Control();
            _skillTabContent.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _skillTabContent.MarginTop = 50f;
            _skillTabContent.Visible = false;
            panel.AddChild(_skillTabContent);

            BuildClassTabContent();
            BuildSkillTabContent();
        }

        private void BuildClassTabContent()
        {
            var header = new VBoxContainer();
            header.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            header.MarginLeft = 24f;
            header.MarginRight = -24f;
            header.MarginTop = 16f;
            header.MarginBottom = HeaderHeight;
            _classTabContent.AddChild(header);

            _subtitleLabel = new Label
            {
                Text = $"Different classes unlock different skills.  You can change class again any time. (Level Requirement: Lv.1/{GameConstants.CLASS_CHANGE_MIN_LEVEL})",
                Align = Label.AlignEnum.Center,
                Autowrap = true,
                RectMinSize = new Vector2(0f, 22f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _subtitleLabel.AddColorOverride("font_color", SubHeaderColor);
            header.AddChild(_subtitleLabel);

            float totalCardsW = AllClasses.Length * CardWidth + (AllClasses.Length - 1) * CardSpacing;
            float cardsLeft = (PanelWidth - totalCardsW) / 2f;
            float cardsTop = HeaderHeight + 8f;

            _cardPanels = new Panel[AllClasses.Length];

            for (int i = 0; i < AllClasses.Length; i++)
            {
                PlayerClass cls = AllClasses[i];
                float cardX = cardsLeft + i * (CardWidth + CardSpacing);
                var card = BuildClassCard(cls, cardX, cardsTop, i);
                _classTabContent.AddChild(card);
            }

            float footerY = PanelHeight - FooterHeight - 50f;

            var confirmBtn = CreateStyledButton("✔ Confirm Class", BtnConfirmBg, BtnConfirmHover);
            confirmBtn.RectPosition = new Vector2(PanelWidth / 2f - 152f, footerY);
            confirmBtn.RectMinSize = new Vector2(142f, 42f);
            confirmBtn.Connect("pressed", this, nameof(OnConfirmPressed));
            _classTabContent.AddChild(confirmBtn);

            var closeBtn = CreateStyledButton("✖ Cancel", BtnCloseBg, BtnCloseHover);
            closeBtn.RectPosition = new Vector2(PanelWidth / 2f + 14f, footerY);
            closeBtn.RectMinSize = new Vector2(112f, 42f);
            closeBtn.Connect("pressed", this, nameof(OnClosePressed));
            _classTabContent.AddChild(closeBtn);
        }

        private void BuildSkillTabContent()
        {
            // Skill slots (Top)
            var slotsBox = new HBoxContainer();
            slotsBox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            slotsBox.MarginLeft = 50f;
            slotsBox.MarginRight = -50f;
            slotsBox.MarginTop = 16f;
            slotsBox.MarginBottom = 76f;
            slotsBox.Alignment = BoxContainer.AlignMode.Center;
            slotsBox.AddConstantOverride("separation", 20);
            _skillTabContent.AddChild(slotsBox);

            _skillSlotLabels = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                var slotPanel = new Panel();
                slotPanel.RectMinSize = new Vector2(200f, 60f);
                slotPanel.AddStyleboxOverride("panel", MakeCardStyle(new Color(0.12f, 0.15f, 0.22f, 1f), new Color(0.3f, 0.4f, 0.6f, 1f), 2));

                var lbl = new Label
                {
                    Text = $"Slot {i + 1}: Empty",
                    Align = Label.AlignEnum.Center,
                    Valign = Label.VAlign.Center,
                    RectMinSize = new Vector2(200f, 60f)
                };
                slotPanel.AddChild(lbl);
                _skillSlotLabels[i] = lbl;
                slotsBox.AddChild(slotPanel);
            }

            // Skill grid (Middle)
            // We use a container that will hold the skill cards based on current class
            var gridBox = new HBoxContainer();
            gridBox.Name = "SkillGrid";
            gridBox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            gridBox.MarginTop = 100f;
            gridBox.MarginBottom = 340f;
            gridBox.Alignment = BoxContainer.AlignMode.Center;
            gridBox.AddConstantOverride("separation", (int)CardSpacing);
            _skillTabContent.AddChild(gridBox);

            // Footer (Bottom)
            float footerY = PanelHeight - FooterHeight - 50f;
            var saveBtn = CreateStyledButton("✔ Save Loadout", BtnConfirmBg, BtnConfirmHover);
            saveBtn.RectPosition = new Vector2(PanelWidth / 2f - 71f, footerY);
            saveBtn.RectMinSize = new Vector2(142f, 42f);
            saveBtn.Connect("pressed", this, nameof(OnSaveLoadoutPressed));
            _skillTabContent.AddChild(saveBtn);
        }

        // ── Card builder ──────────────────────────────────────────────────

        private Panel BuildClassCard(PlayerClass cls, float x, float y, int index)
        {
            Color accent = GetClassAccent(cls);

            var card = new Panel();
            card.RectPosition = new Vector2(x, y);
            card.RectMinSize = new Vector2(CardWidth, CardHeight);
            card.AddStyleboxOverride("panel", MakeCardStyle(CardBgNormal, CardBorderNormal));
            card.MouseFilter = Control.MouseFilterEnum.Stop;

            var stripe = new ColorRect { Color = accent, MouseFilter = Control.MouseFilterEnum.Ignore };
            stripe.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            stripe.MarginBottom = -(CardHeight - 6f);
            card.AddChild(stripe);

            var vbox = new VBoxContainer();
            vbox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            vbox.MarginLeft = 8f;
            vbox.MarginRight = -8f;
            vbox.MarginTop = 14f;
            vbox.MarginBottom = -8f;
            vbox.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
            vbox.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
            card.AddChild(vbox);

            vbox.AddChild(MakeLabel(GetClassEmoji(cls), 32f, CardTitleColor, center: true));
            vbox.AddChild(MakeLabel(PlayerClassData.GetDisplayName(cls), 22f, CardTitleColor, center: true));
            vbox.AddChild(new HSeparator { RectMinSize = new Vector2(0f, 4f) });
            vbox.AddChild(MakeLabel(PlayerClassData.GetDescription(cls), 72f, CardDescColor, center: true, wrap: true));
            
            // Display all available skills for this class
            vbox.AddChild(MakeLabel("Available Skills:", 16f, SubHeaderColor, center: true));
            
            var skillDefs = PlayerClassData.GetAllSkillDefinitions(cls);
            foreach (var skillDef in skillDefs)
            {
                var skillLbl = MakeLabel($"{skillDef.Emoji} {skillDef.DisplayName}", 12f, SkillLabelColor, center: false, wrap: true);
                vbox.AddChild(skillLbl);
            }

            var btn = new Button { Text = string.Empty, Flat = true };
            btn.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            foreach (var s in new[] { "normal", "hover", "pressed", "focus" })
            {
                btn.AddStyleboxOverride(s, new StyleBoxEmpty());
            }
            btn.Connect("pressed", this, nameof(OnCardPressed), new Godot.Collections.Array { index });
            card.AddChild(btn);

            _cardPanels[index] = card;
            return card;
        }

        private Panel BuildSkillCard(SkillDefinition def)
        {
            var card = new Panel();
            card.RectMinSize = new Vector2(CardWidth, CardHeight);

            var vbox = new VBoxContainer();
            vbox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            vbox.MarginLeft = 8f;
            vbox.MarginRight = -8f;
            vbox.MarginTop = 14f;
            vbox.MarginBottom = -8f;
            card.AddChild(vbox);

            vbox.AddChild(MakeLabel(def.Emoji, 32f, CardTitleColor, center: true));
            vbox.AddChild(MakeLabel(def.DisplayName, 22f, CardTitleColor, center: true));
            vbox.AddChild(new HSeparator { RectMinSize = new Vector2(0f, 4f) });
            vbox.AddChild(MakeLabel(def.Description, 72f, CardDescColor, center: true, wrap: true));
            vbox.AddChild(MakeLabel($"CD: {def.CooldownSec}s", 16f, SubHeaderColor, center: true));

            var btn = new Button { Text = string.Empty, Flat = true };
            btn.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            foreach (var s in new[] { "normal", "hover", "pressed", "focus" })
            {
                btn.AddStyleboxOverride(s, new StyleBoxEmpty());
            }
            btn.Connect("pressed", this, nameof(OnSkillCardPressed), new Godot.Collections.Array { def.Id });
            card.AddChild(btn);

            return card;
        }

        // ── Style / Label helpers ─────────────────────────────────────────

        private static StyleBoxFlat MakePanelStyle()
        {
            return new StyleBoxFlat
            {
                BgColor = PanelBg,
                BorderColor = PanelBorder,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12,
                CornerRadiusBottomRight = 12,
            };
        }

        private static StyleBoxFlat MakeCardStyle(Color bg, Color border, int borderW = 2)
        {
            return new StyleBoxFlat
            {
                BgColor = bg,
                BorderColor = border,
                BorderWidthLeft = borderW,
                BorderWidthRight = borderW,
                BorderWidthTop = borderW,
                BorderWidthBottom = borderW,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
            };
        }

        private static Label MakeLabel(string text, float minHeight, Color color,
                                       bool center = false, bool wrap = false)
        {
            var lbl = new Label
            {
                Text = text,
                Autowrap = wrap,
                RectMinSize = new Vector2(0f, minHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            if (center) lbl.Align = Label.AlignEnum.Center;
            lbl.AddColorOverride("font_color", color);
            return lbl;
        }

        private Button CreateStyledButton(string text, Color normal, Color hover)
        {
            var btn = new Button { Text = text };
            btn.AddStyleboxOverride("normal", MakeButtonStyle(normal));
            btn.AddStyleboxOverride("hover", MakeButtonStyle(hover));
            btn.AddStyleboxOverride("pressed", MakeButtonStyle(hover));
            btn.AddStyleboxOverride("focus", MakeButtonStyle(normal));
            return btn;
        }

        private static StyleBoxFlat MakeButtonStyle(Color bg)
        {
            return new StyleBoxFlat
            {
                BgColor = bg,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6,
                ContentMarginLeft = 12f,
                ContentMarginRight = 12f,
                ContentMarginTop = 6f,
                ContentMarginBottom = 6f,
            };
        }

        // ── Signal handlers ───────────────────────────────────────────────

        private void SwitchTab(int tabIndex)
        {
            _classTabContent.Visible = (tabIndex == 0);
            _skillTabContent.Visible = (tabIndex == 1);

            _tabClassBtn.AddStyleboxOverride("normal", MakeButtonStyle(tabIndex == 0 ? TabSelected : TabNormal));
            _tabSkillsBtn.AddStyleboxOverride("normal", MakeButtonStyle(tabIndex == 1 ? TabSelected : TabNormal));

            if (tabIndex == 1)
            {
                RefreshSkillTab();
            }
        }

        private void OnTabPressed(int tabIndex)
        {
            SwitchTab(tabIndex);
        }

        private void OnCardPressed(int index)
        {
            if (index >= 0 && index < AllClasses.Length)
            {
                PlayerClass targetClass = AllClasses[index];
                if (targetClass != PlayerClass.Adventurer && _playerLevel < GameConstants.CLASS_CHANGE_MIN_LEVEL)
                {
                    return;
                }

                _selectedClass = targetClass;
                RefreshCardHighlights();
            }
        }

        private void OnConfirmPressed()
        {
            if (_selectedClass != _currentClass)
            {
                // Class changed -> Reset skills to new class defaults
                _currentClass = _selectedClass;
                _availableSkills = PlayerClassData.GetAllSkillDefinitions(_currentClass);
                _equippedSkillIds.Clear();
                _equippedSkillIds.AddRange(PlayerClassData.GetDefaultSkillLoadout(_currentClass));
                ClassSelected?.Invoke(_selectedClass);
                SwitchTab(1); // Auto jump to skills tab
            }
            else
            {
                Hide();
            }
        }

        private void OnSaveLoadoutPressed()
        {
            Hide();
            SkillLoadoutChanged?.Invoke(_equippedSkillIds);
        }

        private void OnClosePressed()
        {
            Hide();
        }

        private void OnSkillCardPressed(string skillId)
        {
            // If class only has 1 skill, it's always equipped
            if (_availableSkills.Count <= 1) return;

            if (_equippedSkillIds.Contains(skillId))
            {
                _equippedSkillIds.Remove(skillId);
            }
            else
            {
                if (_equippedSkillIds.Count >= 3)
                {
                    // Replace the last slot if full
                    _equippedSkillIds[2] = skillId;
                }
                else
                {
                    _equippedSkillIds.Add(skillId);
                }
            }
            RefreshSkillTab();
        }

        // ── Highlight refresh ─────────────────────────────────────────────

        private void RefreshCardHighlights()
        {
            if (_cardPanels == null) return;

            bool levelLocked = _playerLevel < GameConstants.CLASS_CHANGE_MIN_LEVEL;

            for (int i = 0; i < AllClasses.Length; i++)
            {
                if (_cardPanels[i] == null) continue;

                PlayerClass cls = AllClasses[i];
                bool isSelected = cls == _selectedClass;
                bool isCurrent = cls == _currentClass;

                bool isLocked = levelLocked && cls != PlayerClass.Adventurer;

                if (isLocked)
                {
                    _cardPanels[i].Modulate = new Color(0.4f, 0.4f, 0.4f, 0.7f);
                    _cardPanels[i].AddStyleboxOverride("panel", MakeCardStyle(CardBgNormal, CardBorderNormal));
                }
                else
                {
                    _cardPanels[i].Modulate = new Color(1f, 1f, 1f, 1f);

                    Color accent = GetClassAccent(cls);
                    Color border = isSelected ? CardBorderSelect
                                 : isCurrent ? accent.Blend(CardBorderNormal)
                                 : CardBorderNormal;

                    _cardPanels[i].AddStyleboxOverride("panel",
                        MakeCardStyle(
                            isSelected ? CardBgSelected : CardBgNormal,
                            border,
                            isSelected ? 3 : 2));
                }
            }
        }

        private void RefreshSkillTab()
        {
            // Update slots
            for (int i = 0; i < 3; i++)
            {
                if (i < _equippedSkillIds.Count)
                {
                    string skillId = _equippedSkillIds[i];
                    var def = GetSkillDef(skillId);
                    if (def != null)
                    {
                        _skillSlotLabels[i].Text = $"Slot {i + 1}: {def.Emoji} {def.DisplayName}";
                    }
                    else
                    {
                        _skillSlotLabels[i].Text = $"Slot {i + 1}: Empty";
                    }
                }
                else
                {
                    _skillSlotLabels[i].Text = $"Slot {i + 1}: Empty";
                }
            }

            // Rebuild grid
            var gridBox = _skillTabContent.GetNodeOrNull<HBoxContainer>("SkillGrid");
            if (gridBox != null)
            {
                foreach (Node child in gridBox.GetChildren())
                {
                    child.QueueFree();
                }

                _skillCardPanels = new Panel[_availableSkills.Count];

                for (int i = 0; i < _availableSkills.Count; i++)
                {
                    var def = _availableSkills[i];
                    var card = BuildSkillCard(def);

                    bool isEquipped = _equippedSkillIds.Contains(def.Id);

                    card.AddStyleboxOverride("panel", MakeCardStyle(
                        isEquipped ? CardBgSelected : CardBgNormal,
                        isEquipped ? SkillLabelColor : CardBorderNormal,
                        isEquipped ? 3 : 2
                    ));

                    gridBox.AddChild(card);
                    _skillCardPanels[i] = card;
                }
            }
        }

        private SkillDefinition GetSkillDef(string id)
        {
            if (_availableSkills == null) return null;
            foreach (var def in _availableSkills)
            {
                if (def.Id == id) return def;
            }
            return null;
        }
    }


}