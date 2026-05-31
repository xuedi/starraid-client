using Godot;

// MenuBar is the left-border vertical bar: the logo on top, then a column of menu
// buttons standing in for the later panels (fitting, cargo, contracts, character,
// chat — ../docs/client.md "UI surfaces"). It is inert: buttons hover/press and
// log a click, opening nothing yet (the 9-slice window theme is a later slice).
public partial class MenuBar : Control
{
    private const float Width = 35f; // menu chrome is 35px wide

    // Placeholder panel buttons (label kept only for the click log).
    private static readonly string[] Items = { "Fitting", "Cargo", "Contracts", "Character", "Chat" };

    public override void _Ready()
    {
        // Full-height strip pinned to the left edge (sized via offsets so the
        // anchors, not an explicit Size, drive the rect).
        SetAnchorsPreset(LayoutPreset.LeftWide);
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = Width; // anchorRight is 0 → right edge sits at Width
        OffsetBottom = 0;    // anchorBottom is 1 → full height

        // Tiled background (menu_bg is 35×1).
        var bg = new TextureRect
        {
            Texture = Art.Load("ui/menu_bg.png"),
            StretchMode = TextureRect.StretchModeEnum.Tile,
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var col = new VBoxContainer();
        col.SetAnchorsPreset(LayoutPreset.TopWide);
        col.AddThemeConstantOverride("separation", 4);
        AddChild(col);

        col.AddChild(new TextureRect
        {
            Texture = Art.Load("ui/menu_logo.png"),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
        });

        Texture2D passive = Art.Load("ui/menu_passive.png");
        Texture2D active = Art.Load("ui/menu_active.png");
        foreach (string item in Items)
        {
            var b = new TextureButton
            {
                TextureNormal = passive,
                TextureHover = active,
                TexturePressed = active,
                TooltipText = item,
            };
            string captured = item;
            b.Pressed += () => GD.Print($"[menu] {captured} (not wired yet)");
            col.AddChild(b);
        }
    }
}
