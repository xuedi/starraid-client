using System.Collections.Generic;
using Godot;

// Radar is the top-right minimap. It renders only the interest-managed neighbour
// set the server already sends (honouring "render only what the server sends" —
// ../docs/client.md): own ship at the dial centre, each contact as a blip at its
// bearing relative to own ship, scaled by a radar range and clamped to the dial
// edge. Contacts are pushed in by Game each frame (no new data path).
//
// Blip colour is "unknown/neutral" for everyone: colour-by-standing needs faction
// data the wire does not carry yet (flagged in the plan, not faked).
public partial class Radar : Control
{
    private const float Dial = 200f;          // sfRadar.png is 200×200
    private const float BlipRange = 0.22f;     // screen-units → radar-pixels
    private float _radius;                     // usable blip radius inside the dial

    private Texture2D _dial = null!;
    private Texture2D _blip = null!;
    private readonly List<Vector2> _relative = new(); // contact offsets from own ship

    public override void _Ready()
    {
        _dial = Art.Load("radar/radar.png");
        _blip = Art.Load("radar/spot_unknown_small.png");
        _radius = Dial / 2f - 8f;

        // Pin a Dial×Dial box `margin` px inside the top-right corner via the
        // right-edge anchors + offsets (Control.Position is absolute parent-space,
        // not relative to the anchor, so it can't place a right-anchored box).
        const float margin = 12f;
        AnchorLeft = 1f;
        AnchorTop = 0f;
        AnchorRight = 1f;
        AnchorBottom = 0f;
        OffsetLeft = -(Dial + margin);
        OffsetTop = margin;
        OffsetRight = -margin;
        OffsetBottom = Dial + margin;
    }

    // SetContacts replaces the blip set with offsets (in the game's screen-space
    // units) of each contact from own ship.
    public void SetContacts(IEnumerable<Vector2> relativeOffsets)
    {
        _relative.Clear();
        _relative.AddRange(relativeOffsets);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawTexture(_dial, Vector2.Zero);
        var center = new Vector2(Dial / 2f, Dial / 2f);

        // Own ship blip at centre.
        DrawTextureBlip(center);

        foreach (Vector2 off in _relative)
        {
            Vector2 p = off * BlipRange;
            if (p.Length() > _radius)
                p = p.Normalized() * _radius; // clamp out-of-range contacts to the rim
            DrawTextureBlip(center + p);
        }
    }

    private void DrawTextureBlip(Vector2 at) =>
        DrawTexture(_blip, at - _blip.GetSize() / 2f);
}
