using System.Collections.Generic;

//cia surasyta logika visoms detalems
public abstract class Piece
{
	//unikali operacija (uznuodyti savo detale):
	public bool IsPoisoned { get; set; } = false;
	public bool IsStunned { get; set; } = false;

	public string Name { get; protected set; }
	public Player Owner { get; protected set; }
	public bool IsPlaced { get; set; } = false;

	public Piece(Player owner) { Owner = owner; } //detales konstruktorius :)
	~Piece() { }                                  //detales destruktorius :0 (C# jo nelabai reikia del garbage collectoriaus)

	public abstract List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board);

	//ar ejimas yra imanomas
	protected bool IsSlideValid(
			Game_Manager.Coordinate from,
			Game_Manager.Coordinate to,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		if (board.ContainsKey(to)) return false;

		var fromNeighbors = new HashSet<Game_Manager.Coordinate>(from.Neighbors());
		var toNeighbors = new HashSet<Game_Manager.Coordinate>(to.Neighbors());
		fromNeighbors.IntersectWith(toNeighbors);

		int blockers = 0;
		bool hasContact = false;

		foreach (var shared in fromNeighbors)
		{
			if (board.ContainsKey(shared))
			{
				blockers++;
				hasContact = true;
			}
		}

		if (!hasContact)
		{
			foreach (var n in to.Neighbors())
				if (board.ContainsKey(n) && !n.Equals(from))
				{ hasContact = true; break; }
		}

		return hasContact && blockers < 2;
	}

	public static bool operator ==(Piece a, Piece b)
	{
		if (a is null && b is null) return true;
		if (a is null || b is null) return false;
		return a.Owner == b.Owner && a.Name == b.Name;
	}
	public static bool operator !=(Piece a, Piece b) => !(a == b);
	public override bool Equals(object obj) => obj is Piece p && this == p;
	public override int GetHashCode() => (Name, Owner).GetHashCode();
}

public class Queen : Piece
{
	public Queen(Player owner) : base(owner) { Name = "Queen"; }

	public override List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		var moves = new List<Game_Manager.Coordinate>();
		foreach (var neighbor in current.Neighbors())
			if (!board.ContainsKey(neighbor) && IsSlideValid(current, neighbor, board))
				moves.Add(neighbor);
		return moves;
	}
}

public class Beetle : Piece
{
	public int Height { get; set; } = 0;

	public Beetle(Player owner) : base(owner) { Name = "Beetle"; }

	public override List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		var moves = new List<Game_Manager.Coordinate>();
		foreach (var neighbor in current.Neighbors())
		{
			if (board.ContainsKey(neighbor))
				moves.Add(neighbor);
			else if (IsSlideValid(current, neighbor, board))
				moves.Add(neighbor);
		}
		return moves;
	}
}

public class Spider : Piece
{
	public Spider(Player owner) : base(owner) { Name = "Spider"; }

	public override List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		var moves = new HashSet<Game_Manager.Coordinate>();
		var boardCopy = new Dictionary<Game_Manager.Coordinate, Piece>(board);
		boardCopy.Remove(current);

		SpiderDFS(current, 0, new HashSet<Game_Manager.Coordinate> { current }, moves, boardCopy);
		moves.Remove(current);
		return new List<Game_Manager.Coordinate>(moves);
	}

	private void SpiderDFS(
			Game_Manager.Coordinate pos,
			int depth,
			HashSet<Game_Manager.Coordinate> visited,
			HashSet<Game_Manager.Coordinate> results,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		if (depth == 3)
		{
			results.Add(pos);
			return;
		}

		foreach (var neighbor in pos.Neighbors())
		{
			if (!board.ContainsKey(neighbor) && !visited.Contains(neighbor) && IsSlideValid(pos, neighbor, board))
			{
				visited.Add(neighbor);
				SpiderDFS(neighbor, depth + 1, visited, results, board);
				visited.Remove(neighbor);
			}
		}
	}
}

public class Ant : Piece
{
	public Ant(Player owner) : base(owner) { Name = "Ant"; }

	public override List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		var moves = new HashSet<Game_Manager.Coordinate>();
		var boardCopy = new Dictionary<Game_Manager.Coordinate, Piece>(board);
		boardCopy.Remove(current);

		var queue = new Queue<Game_Manager.Coordinate>();
		var visited = new HashSet<Game_Manager.Coordinate> { current };
		queue.Enqueue(current);

		while (queue.Count > 0)
		{
			var pos = queue.Dequeue();
			foreach (var neighbor in pos.Neighbors())
			{
				if (!boardCopy.ContainsKey(neighbor) && !visited.Contains(neighbor) && IsSlideValid(pos, neighbor, boardCopy))
				{
					visited.Add(neighbor);
					moves.Add(neighbor);
					queue.Enqueue(neighbor);
				}
			}
		}
		return new List<Game_Manager.Coordinate>(moves);
	}
}

public class Grasshopper : Piece
{
	public Grasshopper(Player owner) : base(owner) { Name = "Grasshopper"; }

	public override List<Game_Manager.Coordinate> GetValidMoves(
			Game_Manager.Coordinate current,
			Dictionary<Game_Manager.Coordinate, Piece> board)
	{
		var moves = new List<Game_Manager.Coordinate>();

		foreach (var direction in Game_Manager.Coordinate.Directions)
		{
			var next = current + direction;
			if (!board.ContainsKey(next)) continue;

			while (board.ContainsKey(next))
				next = next + direction;

			moves.Add(next);
		}
		return moves;
	}
}