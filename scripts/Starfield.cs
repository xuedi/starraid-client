using Godot;

// Starfield builds a parallax backdrop: three layers of scattered star dots at
// increasing scroll scale, so panning the camera gives a sense of depth. It is
// pure decoration — no server data — and follows the active Camera2D
// automatically (Parallax2D). The dots are the bg/star_* tiles.
public static class Starfield
{
    // Each layer: its star tile, scroll scale (0 = far/slow, 1 = near/fast), and
    // how many dots to scatter across one repeated tile block.
    private readonly record struct Layer(string Tile, float Scale, int Count);

    private static readonly Layer[] Layers =
    {
        new("bg/star_small.png", 0.2f, 70),
        new("bg/star_middle.png", 0.45f, 40),
        new("bg/star_big.png", 0.75f, 20),
    };

    // Block is the repeated tile size (world units); the field repeats every block
    // as the camera moves, so it never runs out.
    private const float Block = 1024f;

    public static Node2D Build()
    {
        var root = new Node2D();
        // Deterministic scatter so the field is stable across runs.
        var rng = new RandomNumberGenerator { Seed = 0xB1A5E };

        foreach (Layer layer in Layers)
        {
            Texture2D tex = Art.Load(layer.Tile);
            var px = new Parallax2D
            {
                ScrollScale = new Vector2(layer.Scale, layer.Scale),
                RepeatSize = new Vector2(Block, Block),
            };
            for (int i = 0; i < layer.Count; i++)
            {
                px.AddChild(new Sprite2D
                {
                    Texture = tex,
                    Position = new Vector2(rng.RandfRange(0, Block), rng.RandfRange(0, Block)),
                });
            }
            root.AddChild(px);
        }
        return root;
    }
}
