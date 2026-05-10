using Godot;

public partial class DragCamera : Camera3D
{
	[Export] public MouseButton DragButton = MouseButton.Middle;
	[Export] public float DragSpeed = 1.0f;
	[Export] public float ZoomSpeed = 1.0f;
	[Export] public float ZoomMin = 5.0f;
	[Export] public float ZoomMax = 40.0f;

	private bool _dragging = false;
	private Vector2 _dragStartMouse;
	private Vector3 _dragStartPosition;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			switch (mb.ButtonIndex)
			{
				case MouseButton.WheelUp:
					Size = Mathf.Clamp(Size - ZoomSpeed, ZoomMin, ZoomMax);
					break;
				case MouseButton.WheelDown:
					Size = Mathf.Clamp(Size + ZoomSpeed, ZoomMin, ZoomMax);
					break;
				case var btn when btn == DragButton:
					if (mb.Pressed)
					{
						_dragging = true;
						_dragStartMouse = mb.Position;
						_dragStartPosition = Position;
					}
					else
					{
						_dragging = false;
					}
					break;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!_dragging) return;

		Vector2 mouseDelta = GetViewport().GetMousePosition() - _dragStartMouse;

		Position = _dragStartPosition + new Vector3(
				-mouseDelta.X * DragSpeed * 0.01f,
				0,
				-mouseDelta.Y * DragSpeed * 0.01f
		);
	}
}