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
		// Main container
		var panelContainer = new PanelContainer();
		panelContainer.SetAnchorsAndMarginsPreset(Control.LayoutPreset.Center);
		panelContainer.RectMinSize = new Vector2(300, 250);
		AddChild(panelContainer);

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

		// Button container
		_buttonContainer = new VBoxContainer();
		_buttonContainer.AddConstantOverride("separation", 10);
		mainVBox.AddChild(_buttonContainer);

		// Easy button
		AddDifficultyButton("Easy", DifficultyLevel.Easy);

		// Spacer
		var spacer2 = new Control();
		spacer2.RectMinSize = new Vector2(0, 10);
		mainVBox.AddChild(spacer2);

		// Close button
		var closeButton = new Button();
		closeButton.Text = "Close";
		closeButton.Connect("pressed", this, nameof(OnClosePressed));
		mainVBox.AddChild(closeButton);

		_uiContainer = panelContainer;
	}

	private void AddDifficultyButton(string label, DifficultyLevel difficulty)
	{
		var button = new Button();
		button.Text = label;
		button.RectMinSize = new Vector2(200, 40);
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
