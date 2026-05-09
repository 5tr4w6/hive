using Godot;
using System;

public class Player
{
	public string Name { get; set; }

	///info apie detales (kiekis, material, ar pastate karaliene, ar panaudojo spec. ability)
	public int QueenCount { get; set; } = 1;
	public int BeetleCount { get; set; } = 2;
	public int SpiderCount { get; set; } = 2;
	public int AntCount { get; set; } = 3;
	public int GrasshopperCount { get; set; } = 3;

	public StandardMaterial3D QueenMaterial { get; set; }
	public StandardMaterial3D BeetleMaterial { get; set; }
	public StandardMaterial3D SpiderMaterial { get; set; }
	public StandardMaterial3D AntMaterial { get; set; }
	public StandardMaterial3D GrasshopperMaterial { get; set; }

	public bool HasPlacedQueen { get; set; } = false;

	public int TurnsPlayed { get; set; } = 0;

	public bool PoisonUsed { get; set; } = false;

	public Player(string name)
	{
		Name = name;
	}

	public StandardMaterial3D GetMaterial(Game_Manager.PieceType piece)
	{
		return piece switch
		{
			Game_Manager.PieceType.Queen => QueenMaterial,
			Game_Manager.PieceType.Beetle => BeetleMaterial,
			Game_Manager.PieceType.Spider => SpiderMaterial,
			Game_Manager.PieceType.Ant => AntMaterial,
			Game_Manager.PieceType.Grasshopper => GrasshopperMaterial,
			_ => null
		};
	}
}
