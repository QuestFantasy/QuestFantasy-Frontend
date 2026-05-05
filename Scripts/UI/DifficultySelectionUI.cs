using System;

using Godot;

/// <summary>
/// UI for difficulty selection when entering a game level from the lobby.
/// Displays difficulty options and handles player selection.
/// </summary>
public class DifficultySelectionUI : CanvasLayer
{
	public event Action<DifficultyLevel> DifficultySelected;

	private Control _uiContainer;
	private VBoxContainer _buttonContainer;
	private bool _isVisible;

	public override void _Ready()
	{
		// Ensure this UI processes even when the game is paused (Godot 3.6)
		PauseMode = PauseModeEnum.Process;

		BuildUI();
		HideDifficultyMenu();
	}

	private void BuildUI()
	{
		// Wrap everything in a MarginContainer anchored to the screen
		var screenMargin = new MarginContainer();
		screenMargin.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Wide);
		screenMargin.AddConstantOverride("margin_right", 20);
		screenMargin.AddConstantOverride("margin_top", 20);
		screenMargin.AddConstantOverride("margin_left", 20);
		screenMargin.AddConstantOverride("margin_bottom", 20);
		AddChild(screenMargin);

		// Main container
		var panelContainer = new PanelContainer();
		panelContainer.SizeFlagsHorizontal = (int)Control.SizeFlags.ShrinkCenter;
		panelContainer.SizeFlagsVertical = (int)Control.SizeFlags.ShrinkCenter;
		screenMargin.AddChild(panelContainer);

		// Create a VBox for the entire panel content
		var mainVBox = new VBoxContainer();
		panelContainer.AddChild(mainVBox);

		// Title
		var titleLabel = new Label();
		titleLabel.Text = "Select Difficulty";
		titleLabel.Align = Label.AlignEnum.Center;
		mainVBox.AddChild(titleLabel);

		// Spacer
		var spacer = new Control();
		spacer.RectMinSize = new Vector2(0, 10);
		mainVBox.AddChild(spacer);

		// Scroll container for buttons in case height is constrained
		var scrollContainer = new ScrollContainer();
		scrollContainer.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
		scrollContainer.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
		scrollContainer.RectMinSize = new Vector2(240, 200); // Minimum view area
		mainVBox.AddChild(scrollContainer);

		// Button container
		_buttonContainer = new VBoxContainer();
		_buttonContainer.AddConstantOverride("separation", 10);
		_buttonContainer.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
		scrollContainer.AddChild(_buttonContainer);

		// Easy button
		AddDifficultyButton("Easy", DifficultyLevel.Easy);

		// Normal button
		AddDifficultyButton("Normal", DifficultyLevel.Normal);

		// Hard button
		AddDifficultyButton("Hard", DifficultyLevel.Hard);

		// Nightmare button
		AddDifficultyButton("Nightmare", DifficultyLevel.Nightmare);

		// Spacer
		var spacer2 = new Control();
		spacer2.RectMinSize = new Vector2(0, 10);
		mainVBox.AddChild(spacer2);

		// Close button
		var closeButton = new Button();
		closeButton.Text = "Close";
		closeButton.RectMinSize = new Vector2(200, 52);
		closeButton.Connect("pressed", this, nameof(OnClosePressed));
		mainVBox.AddChild(closeButton);

		_uiContainer = screenMargin;
	}

	private void AddDifficultyButton(string label, DifficultyLevel difficulty)
	{
		var button = new Button();
		button.Text = label;
		button.RectMinSize = new Vector2(200, 52);
		var binds = new Godot.Collections.Array();
		binds.Add(difficulty);
		button.Connect("pressed", this, nameof(OnDifficultyButtonPressed), binds);
		_buttonContainer.AddChild(button);
	}

	private void OnDifficultyButtonPressed(DifficultyLevel difficulty)
	{
		DifficultySelected?.Invoke(difficulty);
		HideDifficultyMenu();  // Close menu after selection
	}

	private void OnClosePressed()
	{
		HideDifficultyMenu();
	}

	public void ShowDifficultyMenu()
	{
		_uiContainer.Visible = true;
		_isVisible = true;
		GetTree().Paused = true;
	}

	public void HideDifficultyMenu()
	{
		_uiContainer.Visible = false;
		_isVisible = false;
		GetTree().Paused = false;
	}

	public bool IsMenuVisible => _isVisible;
}
