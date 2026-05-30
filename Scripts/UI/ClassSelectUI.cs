using System;

using Godot;

using QuestFantasy.Core.Data;

namespace QuestFantasy.UI
{
    /// <summary>
    /// Fullscreen overlay panel presented when the player interacts with the "Previous Hero" NPC.
    /// Extends CanvasLayer (same as DifficultySelectionUI) so it is always fixed to screen-space,
    /// independent of the game camera position.
    /// Fires <see cref="ClassSelected"/> when the player confirms a choice.
    /// </summary>
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

        // ── State ─────────────────────────────────────────────────────────
        private PlayerClass _currentClass = PlayerClass.Adventurer;
        private PlayerClass _selectedClass = PlayerClass.Adventurer;

        // Per-card panel references kept for highlight refresh (no node-path lookup needed)
        private Panel[] _cardPanels;

        // Root container — shown/hidden as a whole
        private Control _root;

        public event Action<PlayerClass> ClassSelected;

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
            // Keep receiving input even when game tree is paused
            PauseMode = PauseModeEnum.Process;
            BuildLayout();
            _root.Visible = false;
        }

        /// <summary>Opens the panel, pre-selecting the player's current class.</summary>
        public void Show(PlayerClass currentClass)
        {
            _currentClass = currentClass;
            _selectedClass = currentClass;
            RefreshCardHighlights();
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
            // Full-screen Control root — anchored to entire viewport via preset
            _root = new Control();
            _root.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _root.MouseFilter = Control.MouseFilterEnum.Stop;
            AddChild(_root);

            // Dim overlay
            var overlay = new ColorRect
            {
                Color = BgOverlay,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            overlay.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            _root.AddChild(overlay);

            // Centred panel container
            var panel = new Panel();
            panel.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Center);
            // Offset by half the panel size so it is perfectly centred
            panel.MarginLeft = -PanelWidth / 2f;
            panel.MarginTop = -PanelHeight / 2f;
            panel.MarginRight = PanelWidth / 2f;
            panel.MarginBottom = PanelHeight / 2f;
            panel.AddStyleboxOverride("panel", MakePanelStyle());
            _root.AddChild(panel);

            // ── Header ──────────────────────────────────────────────────
            var header = new VBoxContainer();
            header.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            header.MarginLeft = 24f;
            header.MarginRight = -24f;
            header.MarginTop = 16f;
            header.MarginBottom = HeaderHeight;
            panel.AddChild(header);

            var title = new Label
            {
                Text = "⚡  Choose Your Class",
                Align = Label.AlignEnum.Center,
                RectMinSize = new Vector2(0f, 34f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            title.AddColorOverride("font_color", HeaderColor);
            header.AddChild(title);

            var subtitle = new Label
            {
                Text = "Different classes unlock different skills.  You can change class again any time.",
                Align = Label.AlignEnum.Center,
                Autowrap = true,
                RectMinSize = new Vector2(0f, 22f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            subtitle.AddColorOverride("font_color", SubHeaderColor);
            header.AddChild(subtitle);

            // ── Cards row ───────────────────────────────────────────────
            float totalCardsW = AllClasses.Length * CardWidth + (AllClasses.Length - 1) * CardSpacing;
            float cardsLeft = (PanelWidth - totalCardsW) / 2f;
            float cardsTop = HeaderHeight + 8f;

            _cardPanels = new Panel[AllClasses.Length];

            for (int i = 0; i < AllClasses.Length; i++)
            {
                PlayerClass cls = AllClasses[i];
                float cardX = cardsLeft + i * (CardWidth + CardSpacing);
                var card = BuildClassCard(cls, cardX, cardsTop, i);
                panel.AddChild(card);
            }

            // ── Footer buttons ───────────────────────────────────────────
            float footerY = PanelHeight - FooterHeight + 4f;

            var confirmBtn = CreateStyledButton("✔  Confirm Class", BtnConfirmBg, BtnConfirmHover);
            confirmBtn.RectPosition = new Vector2(PanelWidth / 2f - 152f, footerY);
            confirmBtn.RectMinSize = new Vector2(142f, 42f);
            confirmBtn.Connect("pressed", this, nameof(OnConfirmPressed));
            panel.AddChild(confirmBtn);

            var closeBtn = CreateStyledButton("✖  Cancel", BtnCloseBg, BtnCloseHover);
            closeBtn.RectPosition = new Vector2(PanelWidth / 2f + 14f, footerY);
            closeBtn.RectMinSize = new Vector2(112f, 42f);
            closeBtn.Connect("pressed", this, nameof(OnClosePressed));
            panel.AddChild(closeBtn);
        }

        // ── Card builder ──────────────────────────────────────────────────

        private Panel BuildClassCard(PlayerClass cls, float x, float y, int index)
        {
            Color accent = GetClassAccent(cls);

            var card = new Panel();
            card.RectPosition = new Vector2(x, y);
            card.RectMinSize = new Vector2(CardWidth, CardHeight);
            card.AddStyleboxOverride("panel", MakeCardStyle(CardBgNormal, CardBorderNormal));

            // Accent stripe at the very top
            var stripe = new ColorRect { Color = accent, MouseFilter = Control.MouseFilterEnum.Ignore };
            stripe.SetAnchorsAndMarginsPreset(Control.LayoutPreset.TopWide);
            stripe.MarginBottom = -(CardHeight - 6f);
            card.AddChild(stripe);

            // Content VBox
            var vbox = new VBoxContainer();
            vbox.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
            vbox.MarginLeft = 8f;
            vbox.MarginRight = -8f;
            vbox.MarginTop = 14f;
            vbox.MarginBottom = -8f;
            card.AddChild(vbox);

            vbox.AddChild(MakeLabel(GetClassEmoji(cls), 32f, CardTitleColor, center: true));
            vbox.AddChild(MakeLabel(PlayerClassData.GetDisplayName(cls), 22f, CardTitleColor, center: true));
            vbox.AddChild(new HSeparator { RectMinSize = new Vector2(0f, 4f) });
            vbox.AddChild(MakeLabel(PlayerClassData.GetDescription(cls), 72f, CardDescColor, center: true, wrap: true));
            vbox.AddChild(MakeLabel("Skills:", 16f, SubHeaderColor, center: true));
            vbox.AddChild(MakeLabel(PlayerClassData.GetSkillListText(cls), 30f, SkillLabelColor, center: true, wrap: true));

            // Transparent click-receiver over the whole card
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

        private void OnCardPressed(int index)
        {
            if (index >= 0 && index < AllClasses.Length)
            {
                _selectedClass = AllClasses[index];
                RefreshCardHighlights();
            }
        }

        private void OnConfirmPressed()
        {
            Hide();
            ClassSelected?.Invoke(_selectedClass);
        }

        private void OnClosePressed()
        {
            Hide();
        }

        // ── Highlight refresh ─────────────────────────────────────────────

        private void RefreshCardHighlights()
        {
            if (_cardPanels == null) return;

            for (int i = 0; i < AllClasses.Length; i++)
            {
                if (_cardPanels[i] == null) continue;

                PlayerClass cls = AllClasses[i];
                bool isSelected = cls == _selectedClass;
                bool isCurrent = cls == _currentClass;
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
}