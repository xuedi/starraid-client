using Godot;

// Ship is one rendered object (own ship or a neighbour). It holds a server-given
// target position and each frame eases toward it (smooth interpolation, no
// prediction — see ../docs/client.md) while rotating its sprite toward the
// direction of travel. The single 50×50 top-down sprite is rotated in-engine (the
// "Godot way"); sprites face up (−Y), so heading 0 points along −Y.
//
// Build pattern matches the codebase (Login.cs builds its UI in code): set
// InitialTexture before AddChild, then call SnapTo for the first position.
public partial class Ship : Node2D
{
    // Framerate-independent smoothing rates (higher = snappier).
    private const float MoveSmoothing = 9f;
    private const float TurnSmoothing = 10f;

    public Texture2D? InitialTexture;
    public string TypeKey = "";

    private Sprite2D _sprite = null!;
    private Sprite2D? _reticle;
    private Vector2 _target;

    public override void _Ready()
    {
        _sprite = new Sprite2D { Texture = InitialTexture };
        AddChild(_sprite);
    }

    // SnapTo places the ship immediately (first sighting / self-assign) with no
    // glide from the origin.
    public void SnapTo(Vector2 pos)
    {
        Position = pos;
        _target = pos;
    }

    // MoveTo sets the authoritative target; _Process eases toward it.
    public void MoveTo(Vector2 pos) => _target = pos;

    // SetSelected toggles a local-only selection reticle (no targeting protocol).
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            _reticle ??= MakeReticle();
            _reticle.Visible = true;
        }
        else if (_reticle != null)
        {
            _reticle.Visible = false;
        }
    }

    private Sprite2D MakeReticle()
    {
        var r = new Sprite2D { Texture = Art.SelectReticle(TypeKey) };
        AddChild(r); // child of the (unrotated) Ship node, so it never spins
        return r;
    }

    public override void _Process(double delta)
    {
        var toTarget = _target - Position;
        if (toTarget.Length() > 1f)
        {
            // Rotate only the sprite so children (the reticle) stay upright.
            float heading = Mathf.Atan2(toTarget.Y, toTarget.X) + Mathf.Pi / 2f;
            float t = 1f - Mathf.Exp(-(float)delta * TurnSmoothing);
            _sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, heading, t);
        }
        float f = 1f - Mathf.Exp(-(float)delta * MoveSmoothing);
        Position = Position.Lerp(_target, f);
    }
}
