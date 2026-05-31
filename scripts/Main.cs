using Godot;

// Entry node for the StarRaid client (see ../docs/client.md). It hands straight
// off to the Login scene, which runs the handshake/login flow and then switches
// to the Game scene. Native desktop, server-authoritative: render only what the
// server sends, send intent, over Protobuf/TCP.
public partial class Main : Node2D
{
    public override void _Ready()
    {
        GD.Print("StarRaid client starting");
        CallDeferred(nameof(GoToLogin));
    }

    private void GoToLogin()
    {
        GetTree().ChangeSceneToFile("res://scenes/Login.tscn");
    }
}
