using Godot;

public partial class UI : Control
{
	[Export] public Game_Manager _gm;

	[Export] public Label TurnLabel { get; set; }
	[Export] public Button QueenButton { get; set; }
	[Export] public Button BeetleButton { get; set; }
	[Export] public Button SpiderButton { get; set; }
	[Export] public Button AntButton { get; set; }
	[Export] public Button GrasshopperButton { get; set; }
	[Export] public Button PoisonButton { get; set; }
	[Export] public Panel GameOverPanel { get; set; }
	[Export] public Label GameOverLabel { get; set; }

	public override void _Ready()
	{
		QueenButton.Pressed += () => _gm.OnPieceSelected(Game_Manager.PieceType.Queen);
		BeetleButton.Pressed += () => _gm.OnPieceSelected(Game_Manager.PieceType.Beetle);
		SpiderButton.Pressed += () => _gm.OnPieceSelected(Game_Manager.PieceType.Spider);
		AntButton.Pressed += () => _gm.OnPieceSelected(Game_Manager.PieceType.Ant);
		GrasshopperButton.Pressed += () => _gm.OnPieceSelected(Game_Manager.PieceType.Grasshopper);
		PoisonButton.Pressed += () => _gm.OnPoisonActivated();
	}

	public void UpdateUI(Game_Manager gm)
	{
		TurnLabel.Text = gm.CurrentPlayer == gm.Player1
			? $"{gm.CurrentPlayer.Name}'s turn"
			: "AI is thinking...";

		QueenButton.Text = $"Queen ({gm.CurrentPlayer.QueenCount})";
		BeetleButton.Text = $"Beetle ({gm.CurrentPlayer.BeetleCount})";
		SpiderButton.Text = $"Spider ({gm.CurrentPlayer.SpiderCount})";
		AntButton.Text = $"Ant ({gm.CurrentPlayer.AntCount})";
		GrasshopperButton.Text = $"Grasshopper ({gm.CurrentPlayer.GrasshopperCount})";

		QueenButton.Disabled = gm.CurrentPlayer.QueenCount <= 0;
		BeetleButton.Disabled = gm.CurrentPlayer.BeetleCount <= 0;
		SpiderButton.Disabled = gm.CurrentPlayer.SpiderCount <= 0;
		AntButton.Disabled = gm.CurrentPlayer.AntCount <= 0;
		GrasshopperButton.Disabled = gm.CurrentPlayer.GrasshopperCount <= 0;

		PoisonButton.Visible = !gm.CurrentPlayer.PoisonUsed;
		PoisonButton.Text = gm._poisonMode ? "Click a piece to poison!" : "Use Poison";
	}

	public void ShowGameOver(string message)
	{
		GameOverLabel.Text = message;
		GameOverPanel.Visible = true;

		QueenButton.Disabled = true;
		BeetleButton.Disabled = true;
		SpiderButton.Disabled = true;
		AntButton.Disabled = true;
		GrasshopperButton.Disabled = true;
		PoisonButton.Visible = false;
	}
}