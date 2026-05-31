using Godot;

// ActionBar is the bottom-centre strip of activatable-module slots, built from the
// action/*.png icons. The slots represent a ship's fitted module actions; the
// combat slice wires them to real modules + cooldowns. Inert for now: they
// hover/press and log, bound to nothing.
public partial class ActionBar : Control
{
    private static readonly string[] Icons =
    {
        "ui/action/laserSingle.png", "ui/action/laserMulti.png",
        "ui/action/gunSingle.png", "ui/action/gunMulti.png",
        "ui/action/comScan.png", "ui/action/comEmp.png",
    };

    public override void _Ready()
    {
        // A full-width strip pinned to the bottom edge; a CenterContainer inside it
        // keeps the icon row horizontally centred without manual layout maths.
        const float height = 62f, margin = 12f;
        AnchorLeft = 0f;
        AnchorRight = 1f;
        AnchorTop = 1f;
        AnchorBottom = 1f;
        OffsetLeft = 0f;
        OffsetRight = 0f;
        OffsetTop = -(height + margin);
        OffsetBottom = -margin;

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        center.AddChild(row);

        for (int i = 0; i < Icons.Length; i++)
        {
            var b = new TextureButton
            {
                TextureNormal = Art.Load(Icons[i]),
                IgnoreTextureSize = true,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(50, 50),
            };
            int slot = i;
            b.Pressed += () => GD.Print($"[action] slot {slot} (not wired yet)");
            row.AddChild(b);
        }
    }
}
