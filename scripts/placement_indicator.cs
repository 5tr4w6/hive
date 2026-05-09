using Godot;

public partial class placement_indicator : StaticBody3D
{
	private Game_Manager _gm;
	private Game_Manager.Coordinate _coord;
	private bool _isMovement = false;
	private Game_Manager.Coordinate _fromCoord;

	public void Init(Game_Manager gm, Game_Manager.Coordinate coord)
	{
		_gm = gm;
		_coord = coord;
		_isMovement = false;
	}

	public void InitMovement(Game_Manager gm, Game_Manager.Coordinate from, Game_Manager.Coordinate to)
	{
		_gm = gm;
		_coord = to;
		_fromCoord = from;
		_isMovement = true;
	}

	public override void _InputEvent(Camera3D camera, InputEvent inputEvent, Vector3 position, Vector3 normal, int shapeIdx)
	{
		if (inputEvent is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
		{
			if (_isMovement)
				_gm.MovePiece(_fromCoord, _coord);
			else
				_gm.PlaceTile(_coord);
		}
	}
}