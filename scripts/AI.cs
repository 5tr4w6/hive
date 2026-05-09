using System.Collections.Generic;
using Godot;

public static class AI
{
	public static void TakeTurn(Game_Manager gm)
	{
		// decide whether to place or move
		bool canPlace = HasPiecesToPlace(gm.Player2);
		bool canMove = gm.Player2.HasPlacedQueen;

		// must place queen by 4th turn
		if (gm.Player2.TurnsPlayed == 3 && !gm.Player2.HasPlacedQueen)
		{
			TakePlacementTurn(gm, forcedPiece: Game_Manager.PieceType.Queen);
			return;
		}

		if (!canPlace && !canMove) return;

		// score both options and pick best
		var (bestPlacePiece, bestPlaceCoord, placeScore) = canPlace
				? ScoreBestPlacement(gm)
				: (Game_Manager.PieceType.None, default, int.MinValue);

		var (bestMoveFrom, bestMoveTo, moveScore) = canMove
				? ScoreBestMove(gm)
				: (default, default, int.MinValue);

		if (placeScore >= moveScore && canPlace)
			ExecutePlacement(gm, bestPlacePiece, bestPlaceCoord);
		else if (canMove && bestMoveFrom.Q != bestMoveTo.Q || bestMoveFrom.R != bestMoveTo.R)
			gm.MovePiece(bestMoveFrom, bestMoveTo);
		else if (canPlace)
			ExecutePlacement(gm, bestPlacePiece, bestPlaceCoord);
	}

	private static bool HasPiecesToPlace(Player player)
	{
		return player.QueenCount > 0 || player.BeetleCount > 0 ||
					 player.SpiderCount > 0 || player.AntCount > 0 ||
					 player.GrasshopperCount > 0;
	}

	// ---placement scoring:
	private static (Game_Manager.PieceType piece, Game_Manager.Coordinate coord, int score)
			ScoreBestPlacement(Game_Manager gm)
	{
		var validCoords = Rules.GetValidPlacements(gm._board, gm.Player2, gm._turns_taken);
		var piecesToTry = GetAvailablePieces(gm.Player2);

		var bestPiece = Game_Manager.PieceType.None;
		var bestCoord = default(Game_Manager.Coordinate);
		int bestScore = int.MinValue;

		foreach (var piece in piecesToTry)
		{
			foreach (var coord in validCoords)
			{
				int score = ScorePlacement(gm, piece, coord);
				if (score > bestScore)
				{
					bestScore = score;
					bestPiece = piece;
					bestCoord = coord;
				}
			}
		}

		return (bestPiece, bestCoord, bestScore);
	}

	private static int ScorePlacement(Game_Manager gm, Game_Manager.PieceType piece,
			Game_Manager.Coordinate coord)
	{
		int score = 0;

		// bonus for placing next to opponent queen
		if (gm._p1QueenCoord.HasValue)
		{
			foreach (var neighbor in coord.Neighbors())
			{
				if (neighbor.Q == gm._p1QueenCoord.Value.Q &&
						neighbor.R == gm._p1QueenCoord.Value.R)
				{
					score += 10;
					break;
				}
			}

			// count how many neighbors opponent queen already has
			int queenNeighbors = CountOccupiedNeighbors(gm._board, gm._p1QueenCoord.Value);
			score += queenNeighbors * 3;
		}

		// prefer placing ants and grasshoppers for mobility
		if (piece == Game_Manager.PieceType.Ant) score += 3;
		if (piece == Game_Manager.PieceType.Grasshopper) score += 2;

		// avoid placing next to own queen (leave it room to move)
		if (gm._p2QueenCoord.HasValue)
		{
			foreach (var neighbor in coord.Neighbors())
			{
				if (neighbor.Q == gm._p2QueenCoord.Value.Q &&
						neighbor.R == gm._p2QueenCoord.Value.R)
				{
					score -= 3;
					break;
				}
			}
		}

		return score;
	}

	// ---movement scoring:
	private static (Game_Manager.Coordinate from, Game_Manager.Coordinate to, int score)
			ScoreBestMove(Game_Manager gm)
	{
		var bestFrom = default(Game_Manager.Coordinate);
		var bestTo = default(Game_Manager.Coordinate);
		int bestScore = int.MinValue;

		foreach (var kvp in gm._board)
		{
			if (kvp.Value.Owner != gm.Player2) continue;

			// skip if moving this piece breaks hive
			if (!Rules.IsHiveConnected(gm._board, kvp.Key)) continue;

			var moves = kvp.Value.GetValidMoves(kvp.Key, gm._board);

			// filter occupied for non-beetles
			if (kvp.Value is not Beetle)
				moves.RemoveAll(c => gm._board.ContainsKey(c));

			foreach (var move in moves)
			{
				int score = ScoreMove(gm, kvp.Key, move, kvp.Value);
				if (score > bestScore)
				{
					bestScore = score;
					bestFrom = kvp.Key;
					bestTo = move;
				}
			}
		}

		return (bestFrom, bestTo, bestScore);
	}

	private static int ScoreMove(Game_Manager gm, Game_Manager.Coordinate from,
			Game_Manager.Coordinate to, Piece piece)
	{
		int score = 0;

		// highest priority: surround opponent queen
		if (gm._p1QueenCoord.HasValue)
		{
			int neighborsBefore = CountOccupiedNeighbors(gm._board, gm._p1QueenCoord.Value);

			// simulate move
			var tempBoard = new Dictionary<Game_Manager.Coordinate, Piece>(gm._board);
			tempBoard.Remove(from);
			tempBoard[to] = piece;

			int neighborsAfter = CountOccupiedNeighbors(tempBoard, gm._p1QueenCoord.Value);
			score += (neighborsAfter - neighborsBefore) * 10;

			// bonus for being adjacent to opponent queen
			foreach (var neighbor in to.Neighbors())
			{
				if (neighbor.Q == gm._p1QueenCoord.Value.Q &&
						neighbor.R == gm._p1QueenCoord.Value.R)
				{
					score += 5;
					break;
				}
			}
		}

		// protect own queen if in danger
		if (gm._p2QueenCoord.HasValue)
		{
			int queenDanger = CountOccupiedNeighbors(gm._board, gm._p2QueenCoord.Value);
			if (queenDanger >= 3)
			{
				// move pieces away from own queen to give it room
				foreach (var neighbor in from.Neighbors())
				{
					if (neighbor.Q == gm._p2QueenCoord.Value.Q &&
							neighbor.R == gm._p2QueenCoord.Value.R)
					{
						score += 8;
						break;
					}
				}
			}
		}

		return score;
	}

	private static int CountOccupiedNeighbors(
			Dictionary<Game_Manager.Coordinate, Piece> board,
			Game_Manager.Coordinate coord)
	{
		int count = 0;
		foreach (var neighbor in coord.Neighbors())
			if (board.ContainsKey(neighbor)) count++;
		return count;
	}

	private static List<Game_Manager.PieceType> GetAvailablePieces(Player player)
	{
		var pieces = new List<Game_Manager.PieceType>();
		if (player.QueenCount > 0) pieces.Add(Game_Manager.PieceType.Queen);
		if (player.BeetleCount > 0) pieces.Add(Game_Manager.PieceType.Beetle);
		if (player.SpiderCount > 0) pieces.Add(Game_Manager.PieceType.Spider);
		if (player.AntCount > 0) pieces.Add(Game_Manager.PieceType.Ant);
		if (player.GrasshopperCount > 0) pieces.Add(Game_Manager.PieceType.Grasshopper);
		return pieces;
	}

	private static void TakePlacementTurn(Game_Manager gm,
			Game_Manager.PieceType forcedPiece = Game_Manager.PieceType.None)
	{
		var pieces = forcedPiece != Game_Manager.PieceType.None
				? new List<Game_Manager.PieceType> { forcedPiece }
				: GetAvailablePieces(gm.Player2);

		var validCoords = Rules.GetValidPlacements(gm._board, gm.Player2, gm._turns_taken);
		if (validCoords.Count == 0) return;

		var bestPiece = pieces[0];
		var bestCoord = validCoords[0];
		int bestScore = int.MinValue;

		foreach (var piece in pieces)
		{
			foreach (var coord in validCoords)
			{
				int score = ScorePlacement(gm, piece, coord);
				if (score > bestScore)
				{
					bestScore = score;
					bestPiece = piece;
					bestCoord = coord;
				}
			}
		}

		ExecutePlacement(gm, bestPiece, bestCoord);
	}

	private static void ExecutePlacement(Game_Manager gm,
			Game_Manager.PieceType piece, Game_Manager.Coordinate coord)
	{
		if (piece == Game_Manager.PieceType.None) return;
		gm.SelectedPiece = piece;
		gm.PlaceTile(coord);
	}
}