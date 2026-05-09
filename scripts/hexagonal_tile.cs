using Godot;

public partial class hexagonal_tile : StaticBody3D
{
	private MeshInstance3D _mesh;
	private MeshInstance3D _outlineMesh;
	public Piece Piece { get; set; }
	public Game_Manager.Coordinate Coord { get; set; }
	private Game_Manager _gm;
	private StandardMaterial3D _originalMaterial;

	public void Init(Game_Manager gm, Game_Manager.Coordinate coord)
	{
		_gm = gm;
		Coord = coord;
	}

	public override void _Ready()
	{
		_mesh = GetNode<MeshInstance3D>("Hexagonal_Tile");
	}

	public void SetMaterial(StandardMaterial3D material)
	{
		if (_mesh != null)
		{
			_mesh.MaterialOverride = material;
			_originalMaterial = material;
		}
	}

	public void SetSelected(bool selected)
	{
		if (_outlineMesh != null)
			_outlineMesh.Visible = selected;
	}

	public void SetPoisonEffect(bool isPoisoned)
	{
		if (_mesh == null) return;
		if (isPoisoned)
		{
			//gal reiktu pakeist i normalesni material, nes dabar negraziai atrodo
			var poisonMaterial = new StandardMaterial3D();
			poisonMaterial.AlbedoColor = new Color(0.0f, 0.8f, 0.0f);
			_mesh.MaterialOverride = poisonMaterial;
			_originalMaterial = poisonMaterial;
		}
	}

	public override void _InputEvent(Camera3D camera, InputEvent inputEvent, Vector3 position,
			Vector3 normal, int shapeIdx)
	{
		if (inputEvent is InputEventMouseButton mouse && mouse.Pressed &&
				mouse.ButtonIndex == MouseButton.Left)
		{
			_gm?.OnTileClicked(Coord);
		}
	}
}