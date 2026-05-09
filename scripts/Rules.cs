//zaidimo taisykles
using System.Collections.Generic;

public static class Rules
{
	public static List<Game_Manager.Coordinate> GetValidPlacements(
			Dictionary<Game_Manager.Coordinate, Piece> board,
			Player currentPlayer,
			int turnsTaken)
	{
		var valid = new HashSet<Game_Manager.Coordinate>();

		if (board.Count == 0)
		{
			valid.Add(new Game_Manager.Coordinate(0, 0));
			return new List<Game_Manager.Coordinate>(valid);
		}

		if (turnsTaken <= 1)
		{
			foreach (var coord in board.Keys)
				foreach (var neighbor in coord.Neighbors())
					if (!board.ContainsKey(neighbor))
						valid.Add(neighbor);
			return new List<Game_Manager.Coordinate>(valid);
		}

		foreach (var kvp in board)
		{
			if (kvp.Value.Owner != currentPlayer) continue;

			foreach (var neighbor in kvp.Key.Neighbors())
			{
				if (board.ContainsKey(neighbor)) continue;

				bool touchesOpponent = false;
				foreach (var nn in neighbor.Neighbors())
				{
					if (board.TryGetValue(nn, out Piece p) && p.Owner != currentPlayer)
					{
						touchesOpponent = true;
						break;
					}
				}

				if (!touchesOpponent)
					valid.Add(neighbor);
			}
		}

		return new List<Game_Manager.Coordinate>(valid);
	}
	//hive'as negali buti susiskaides i daugiau nei viena dali
	public static bool IsHiveConnected(
		Dictionary<Game_Manager.Coordinate, Piece> board,
		Game_Manager.Coordinate? excludeCoord = null)
	{
		var cells = new HashSet<Game_Manager.Coordinate>(board.Keys);
		if (excludeCoord.HasValue)
			cells.Remove(excludeCoord.Value);

		if (cells.Count == 0) return true;

		var start = default(Game_Manager.Coordinate);
		foreach (var c in cells) { start = c; break; }

		var visited = new HashSet<Game_Manager.Coordinate>();
		var queue = new Queue<Game_Manager.Coordinate>();
		queue.Enqueue(start);
		visited.Add(start);

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			foreach (var neighbor in current.Neighbors())
			{
				if (cells.Contains(neighbor) && !visited.Contains(neighbor))
				{
					visited.Add(neighbor);
					queue.Enqueue(neighbor);
				}
			}
		}

		return visited.Count == cells.Count;
	}

	//zaidimo pabaigos logika
	public enum GameResult { None, Player1Wins, Player2Wins, Draw }

	public static GameResult CheckWinCondition(
			Dictionary<Game_Manager.Coordinate, Piece> board,
			Game_Manager.Coordinate? p1QueenCoord,
			Game_Manager.Coordinate? p2QueenCoord)
	{
		bool p1Surrounded = p1QueenCoord.HasValue && IsQueenSurrounded(board, p1QueenCoord.Value);
		bool p2Surrounded = p2QueenCoord.HasValue && IsQueenSurrounded(board, p2QueenCoord.Value);

		if (p1Surrounded && p2Surrounded) return GameResult.Draw;
		if (p1Surrounded) return GameResult.Player2Wins;
		if (p2Surrounded) return GameResult.Player1Wins;
		return GameResult.None;
	}

	private static bool IsQueenSurrounded(
			Dictionary<Game_Manager.Coordinate, Piece> board,
			Game_Manager.Coordinate queenCoord)
	{
		foreach (var neighbor in queenCoord.Neighbors())
		{
			if (!board.ContainsKey(neighbor))
				return false;
		}
		return true;
	}
}