using Godot;

// ShipStatus is the bottom-right widget: the own ship's sprite ringed by a
// directional shield indicator (top/bottom arcs), with small hull/power readouts.
//
// The values are STYLED PLACEHOLDERS — real shield/hull/power need the server to
// send them, but derived attributes are currently server-internal (the catalog
// slice). The ring is drawn procedurally here (a clean stand-in for the
// mapSchilde gradient arcs); wiring real per-facing strength is a future
// protocol + HUD slice.
public partial class ShipStatus : Control
{
    private const float Box = 150f;
    private static readonly Vector2 Center = new(Box / 2f, Box / 2f);
    private const float Ring = 52f;

    // Demo strengths (0..1) until the wire carries them.
    private const float ShieldTop = 0.85f;
    private const float ShieldBottom = 0.55f;
    private const float Hull = 1.0f;
    private const float Power = 0.9f;

    private Sprite2D _ship = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Box, Box);
        SetAnchorsPreset(LayoutPreset.BottomRight);
        Position = new Vector2(-Box - 12f, -Box - 12f);

        _ship = new Sprite2D { Position = Center, Scale = new Vector2(1.4f, 1.4f) };
        AddChild(_ship);

        var hull = new Label { Text = "HULL", Position = new Vector2(6, Box - 34) };
        var pwr = new Label { Text = "PWR", Position = new Vector2(Box - 44, Box - 34) };
        hull.AddThemeFontSizeOverride("font_size", 11);
        pwr.AddThemeFontSizeOverride("font_size", 11);
        AddChild(hull);
        AddChild(pwr);
    }

    // SetShip sets the portrait sprite to the own ship's class sprite.
    public void SetShip(Texture2D tex)
    {
        _ship.Texture = tex;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Faint full ring as the backdrop.
        DrawArc(Center, Ring, 0, Mathf.Tau, 64, new Color(1, 1, 1, 0.12f), 3f, true);

        // Top arc (−45°..−135° around the upper half) and bottom arc, coloured by
        // their demo strength on a red→green gradient.
        DrawArc(Center, Ring, Mathf.Pi + Mathf.Pi / 4f, Mathf.Tau - Mathf.Pi / 4f,
            32, StrengthColor(ShieldTop), 5f, true);
        DrawArc(Center, Ring, Mathf.Pi / 4f, Mathf.Pi - Mathf.Pi / 4f,
            32, StrengthColor(ShieldBottom), 5f, true);

        // Hull / power pips under the labels.
        DrawRect(new Rect2(6, Box - 20, 38 * Hull, 6), StrengthColor(Hull));
        DrawRect(new Rect2(Box - 44, Box - 20, 38 * Power, 6), StrengthColor(Power));
    }

    private static Color StrengthColor(float s) =>
        new Color(1f - s, 0.3f + 0.7f * s, 0.2f + 0.2f * s);
}
