using System.Collections.Generic;
using Godot;

// Art centralises loading the dummy sprite kit in client/assets/ and the
// object-class → ship-sprite mapping. The catalog slice put the class key on the
// beacon (ObjectEnter/SelfAssign.TypeKey); this turns that key into a sprite so
// each object renders distinctly. The textures are placeholder ("aC*" / "map*"
// art from the archive); swap the PNGs without touching code.
public static class Art
{
    // Godot caches loaded resources, so repeated Load calls are cheap.
    public static Texture2D Load(string rel) => GD.Load<Texture2D>($"res://assets/{rel}");

    // type_key → ship sprite. Only four dummy hull sprites exist, so stations and
    // asteroids reuse them for now (real asteroid/station art is a later slice).
    private static readonly Dictionary<string, string> ShipByType = new()
    {
        { "skiff", "ships/ship1.png" },
        { "corvette", "ships/ship2.png" },
        { "hauler", "ships/ship3.png" },
        { "cruiser", "ships/ship4.png" },
        { "station", "ships/ship4.png" },
        { "asteroid", "ships/ship3.png" },
    };

    // ShipTexture resolves a class key to its sprite, falling back when the key is
    // empty (dev/offline spawn) or unknown.
    public static Texture2D ShipTexture(string typeKey, string fallback = "ships/ship2.png")
    {
        if (!string.IsNullOrEmpty(typeKey) && ShipByType.TryGetValue(typeKey, out string? path))
            return Load(path);
        return Load(fallback);
    }

    // Selection reticle sized by a rough class footprint (bigger hulls / structures
    // get the 100px bracket).
    public static Texture2D SelectReticle(string typeKey)
    {
        return typeKey switch
        {
            "cruiser" or "station" or "asteroid" => Load("select/select100.png"),
            _ => Load("select/select50.png"),
        };
    }
}
