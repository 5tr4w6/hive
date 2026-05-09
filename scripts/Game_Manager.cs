//cia pagr. zaidimo logika, kaip:
//
//zaideju vardai (kinda useless, nes zaidziama pries AI),
//materials, cam setup, asines koordinaciu sistemos setup
using Godot;
using System.Collections.Generic;

public partial class Game_Manager : Node
{
	[Export] public UI _ui { get; set; }

	public Player Player1 = new Player("Player 1");
	public Player Player2 = new Player("Player 2");

	[Export] public StandardMaterial3D _p1QueenMat { get; set; }
	[Export] public StandardMaterial3D _p1BeetleMat { get; set; }
	[Export] public StandardMaterial3D _p1SpiderMat { get; set; }
	[Export] public StandardMaterial3D _p1AntMat { get; set; }
	[Export] public StandardMaterial3D _p1GrasshopperMat { get; set; }

	[Export] public StandardMaterial3D _p2QueenMat { get; set; }
	[Export] public StandardMaterial3D _p2BeetleMat { get; set; }
	[Export] public StandardMaterial3D _p2SpiderMat { get; set; }
	[Export] public StandardMaterial3D _p2AntMat { get; set; }
	[Export] public StandardMaterial3D _p2GrasshopperMat { get; set; }

	public Player CurrentPlayer => _turns_taken % 2 == 0 ? Player1 : Player2;

	[Export] public PackedScene _placementIndicatorScene { get; set; }
	[Export] public PackedScene _queenScene { get; set; }
	[Export] public PackedScene _beetleScene { get; set; }
	[Export] public PackedScene _spiderScene { get; set; }
	[Export] public PackedScene _antScene { get; set; }
	[Export] public PackedScene _grasshopperScene { get; set; }

	[Export] public Camera3D _cam { get; set; }

	private const float HexRadius = 0.125f;
	public Dictionary<Coordinate, Piece> _board = new();
	public Dictionary<Coordinate, hexagonal_tile> _tileNodes = new();

	private List<placement_indicator> _validPlacementTiles = new();
	private Coordinate? _selectedTileCoord = null;

	public bool _isAITurn = false;
	public bool _poisonMode = false;

	public int _turns_taken = 0;

	public Coordinate? _p1QueenCoord = null;
	public Coordinate? _p2QueenCoord = null;

	//asines koordinaciu sistemos logika, remiantis https://www.redblobgames.com/grids/hexagons/
	public struct Coordinate
	{
		public int Q, R;
		public Coordinate(int q, int r) { Q = q; R = r; }

		public static readonly Coordinate[] Directions = {
			new(1, 0), new(1, -1), new(0, -1),
			new(-1, 0), new(-1, 1), new(0, 1)
		};

		public Coordinate[] Neighbors()
		{
			Coordinate[] result = new Coordinate[6];
			for (int i = 0; i < 6; i++)
			{
				Coordinate direction = Directions[i];
				result[i] = new Coordinate(Q + direction.Q, R + direction.R);
			}
			return result;
		}

		public static Coordinate operator +(Coordinate a, Coordinate b)
		{
			return new Coordinate(a.Q + b.Q, a.R + b.R);
		}
	}


	public enum PieceType { None, Queen, Beetle, Spider, Ant, Grasshopper }
	public PieceType SelectedPiece { get; set; } = PieceType.None;

	public int GetCount(PieceType piece)
	{
		//switchas, kad nereiketu rasyti milijono if'u
		return piece switch
		{
			PieceType.Queen => CurrentPlayer.QueenCount,
			PieceType.Beetle => CurrentPlayer.BeetleCount,
			PieceType.Spider => CurrentPlayer.SpiderCount,
			PieceType.Ant => CurrentPlayer.AntCount,
			PieceType.Grasshopper => CurrentPlayer.GrasshopperCount,
			_ => 0
		};
	}

	public Vector3 HexToWorld(Coordinate coord)
	{
		float x = HexRadius * (3.0f / 2.0f * coord.Q);
		float z = HexRadius * (Mathf.Sqrt(3.0f) * (coord.R + coord.Q / 2.0f));
		return new Vector3(x, 0, z);
	}

	public override void _Ready()
	{
		//priskiriami visi material'ai
		Player1.QueenMaterial = _p1QueenMat;
		Player1.BeetleMaterial = _p1BeetleMat;
		Player1.SpiderMaterial = _p1SpiderMat;
		Player1.AntMaterial = _p1AntMat;
		Player1.GrasshopperMaterial = _p1GrasshopperMat;

		Player2.QueenMaterial = _p2QueenMat;
		Player2.BeetleMaterial = _p2BeetleMat;
		Player2.SpiderMaterial = _p2SpiderMat;
		Player2.AntMaterial = _p2AntMat;
		Player2.GrasshopperMaterial = _p2GrasshopperMat;

		_ui.UpdateUI(this);
	}

	public override void _Process(double delta) { }

	private void SpawnValidPlacementTiles(List<Coordinate> validCoords)
	{
		ClearValidPlacementTiles();
		foreach (var coord in validCoords)
		{
			var tile = _placementIndicatorScene.Instantiate<placement_indicator>();
			AddChild(tile);
			tile.GlobalPosition = HexToWorld(coord);
			tile.Init(this, coord);
			_validPlacementTiles.Add(tile);
		}
	}

	//panaikinam indikatoriu kur galima statyti detales
	private void ClearValidPlacementTiles()
	{
		foreach (var tile in _validPlacementTiles)
			tile.QueueFree();
		_validPlacementTiles.Clear();
	}

	//padedam indikatorius, kur statyt detales
	private void SpawnMovementIndicators(List<Coordinate> coords, Coordinate from)
	{
		ClearValidPlacementTiles();
		foreach (var coord in coords)
		{
			var tile = _placementIndicatorScene.Instantiate<placement_indicator>();
			AddChild(tile);
			tile.GlobalPosition = HexToWorld(coord);
			tile.InitMovement(this, from, coord);
			_validPlacementTiles.Add(tile);
		}
	}

	//detaliu pastatymas
	public void PlaceTile(Coordinate targetCoord)
	{
		Piece piece = SelectedPiece switch
		{
			PieceType.Queen => new Queen(CurrentPlayer),
			PieceType.Beetle => new Beetle(CurrentPlayer),
			PieceType.Spider => new Spider(CurrentPlayer),
			PieceType.Ant => new Ant(CurrentPlayer),
			PieceType.Grasshopper => new Grasshopper(CurrentPlayer),
			_ => null
		};

		var scene = GetSceneForPiece(SelectedPiece);
		if (scene == null || piece == null) return;

		var tile = scene.Instantiate<hexagonal_tile>();
		AddChild(tile);
		tile.GlobalPosition = HexToWorld(targetCoord);
		tile.Piece = piece;
		tile.Init(this, targetCoord);
		tile.SetMaterial(CurrentPlayer.GetMaterial(SelectedPiece));
		_board[targetCoord] = piece;
		_tileNodes[targetCoord] = tile;

		if (SelectedPiece == PieceType.Queen)
		{
			if (CurrentPlayer == Player1) _p1QueenCoord = targetCoord;
			else _p2QueenCoord = targetCoord;
			CurrentPlayer.HasPlacedQueen = true;
		}

		switch (SelectedPiece)
		{
			case PieceType.Queen: CurrentPlayer.QueenCount--; break;
			case PieceType.Beetle: CurrentPlayer.BeetleCount--; break;
			case PieceType.Spider: CurrentPlayer.SpiderCount--; break;
			case PieceType.Ant: CurrentPlayer.AntCount--; break;
			case PieceType.Grasshopper: CurrentPlayer.GrasshopperCount--; break;
		}

		CurrentPlayer.TurnsPlayed++;
		SelectedPiece = PieceType.None;
		ClearValidPlacementTiles();
		_turns_taken++;
		_ui.UpdateUI(this);

		var result = Rules.CheckWinCondition(_board, _p1QueenCoord, _p2QueenCoord);
		if (result != Rules.GameResult.None)
		{
			HandleGameOver(result);
			return;
		}
		CheckAITurn();
	}

	public void MovePiece(Coordinate from, Coordinate to)
	{
		if (!_board.TryGetValue(from, out Piece piece)) return;

		bool isClimbing = _board.ContainsKey(to);
		_board.Remove(from);
		_board[to] = piece;

		if (_tileNodes.TryGetValue(from, out hexagonal_tile tile))
		{
			Vector3 worldPos = HexToWorld(to);
			if (piece is Beetle && isClimbing)
			{
				if (_tileNodes.TryGetValue(to, out hexagonal_tile tileBelow))
					worldPos.Y = tileBelow.GlobalPosition.Y + 0.2f;
			}
			tile.GlobalPosition = worldPos;
			tile.Init(this, to);
			_tileNodes.Remove(from);
			_tileNodes[to] = tile;
		}

		if (piece is Queen)
		{
			if (piece.Owner == Player1) _p1QueenCoord = to;
			else _p2QueenCoord = to;
		}

		piece.IsStunned = false;

		foreach (var neighbor in to.Neighbors())
		{
			if (_board.TryGetValue(neighbor, out Piece neighborPiece))
			{
				if (neighborPiece.IsPoisoned && neighborPiece.Owner != piece.Owner)
				{
					piece.IsStunned = true;
					GD.Print($"Piece stunned by poison!");
					break;
				}
			}
		}

		_selectedTileCoord = null;
		ClearValidPlacementTiles();
		_turns_taken++;
		_ui.UpdateUI(this);

		var result = Rules.CheckWinCondition(_board, _p1QueenCoord, _p2QueenCoord);
		if (result != Rules.GameResult.None)
		{
			HandleGameOver(result);
			return;
		}
		CheckAITurn();
	}

	public void OnPieceSelected(PieceType piece)
	{
		if (_isAITurn) return;

		if (_selectedTileNode != null)
		{
			_selectedTileNode.SetSelected(false);
			_selectedTileNode = null;
		}

		if (GetCount(piece) <= 0) return;

		if (CurrentPlayer.TurnsPlayed == 3 && !CurrentPlayer.HasPlacedQueen && piece != PieceType.Queen)
		{
			GD.Print("You must place your Queen by the 4th turn!");
			return;
		}

		SelectedPiece = piece;
		_ui.UpdateUI(this);
		SpawnValidPlacementTiles(Rules.GetValidPlacements(_board, CurrentPlayer, _turns_taken));
	}

	public void OnPoisonActivated()
	{
		if (_isAITurn) return;
		if (CurrentPlayer.PoisonUsed)
		{
			GD.Print("Poison already used!");
			return;
		}
		_poisonMode = true;
		ClearValidPlacementTiles();
		SelectedPiece = PieceType.None;
		_ui.UpdateUI(this);
	}
	private hexagonal_tile _selectedTileNode = null;

	public void OnTileClicked(Coordinate coord)
	{
		if (_isAITurn) return;
		if (!_board.TryGetValue(coord, out Piece piece)) return;

		if (_poisonMode)
		{
			if (piece.Owner != CurrentPlayer)
			{
				GD.Print("You can only poison your own pieces!");
				return;
			}
			piece.IsPoisoned = true;
			CurrentPlayer.PoisonUsed = true;
			_poisonMode = false;
			if (_tileNodes.TryGetValue(coord, out hexagonal_tile poisonedTile))
				poisonedTile.SetPoisonEffect(true);
			_ui.UpdateUI(this);
			return;
		}

		if (piece.Owner != CurrentPlayer) return;
		if (!CurrentPlayer.HasPlacedQueen) return;
		if (piece.IsStunned)
		{
			GD.Print("This piece is stunned and cannot move!");
			return;
		}
		if (_selectedTileNode != null)
			_selectedTileNode.SetSelected(false);

		SelectedPiece = PieceType.None;
		_selectedTileCoord = coord;
		ClearValidPlacementTiles();

		if (!Rules.IsHiveConnected(_board, coord))
		{
			GD.Print("Moving this piece would break the hive!");
			_selectedTileNode = null;
			_ui.UpdateUI(this);
			return;
		}

		if (_tileNodes.TryGetValue(coord, out hexagonal_tile selectedTile))
		{
			_selectedTileNode = selectedTile;
			selectedTile.SetSelected(true);
		}

		var moves = piece.GetValidMoves(coord, _board);

		if (piece is not Beetle)
			moves.RemoveAll(c => _board.ContainsKey(c));

		moves.RemoveAll(c =>
		{
			var tempBoard = new Dictionary<Coordinate, Piece>(_board);
			tempBoard.Remove(coord);
			tempBoard[c] = piece;
			return !Rules.IsHiveConnected(tempBoard);
		});

		SpawnMovementIndicators(moves, coord);
		_ui.UpdateUI(this);
	}

	private void CheckAITurn()
	{
		if (CurrentPlayer == Player2)
		{
			_isAITurn = true;
			_ui.UpdateUI(this);
			GetTree().CreateTimer(0.5f).Timeout += () =>
			{
				AI.TakeTurn(this);
				_isAITurn = false;
				_ui.UpdateUI(this);
			};
		}
	}

	private void HandleGameOver(Rules.GameResult result)
	{
		string message = result switch
		{
			Rules.GameResult.Player1Wins => "Player 1 Wins!",
			Rules.GameResult.Player2Wins => "Player 2 Wins!",
			Rules.GameResult.Draw => "It's a Draw!",
			_ => ""
		};
		GD.Print(message);
		_ui.ShowGameOver(message);
	}

	private PackedScene GetSceneForPiece(PieceType piece)
	{
		return piece switch
		{
			PieceType.Queen => _queenScene,
			PieceType.Beetle => _beetleScene,
			PieceType.Spider => _spiderScene,
			PieceType.Ant => _antScene,
			PieceType.Grasshopper => _grasshopperScene,
			_ => null
		};
	}
}