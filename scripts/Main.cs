using Godot;

// Entry node for the StarRaid client (see ../docs/client.md).
//
// Native desktop, server-authoritative: render only what the server sends,
// send intent, connect via Protobuf over TCP. A main zoomable view plus a
// multi-zoom minimap, both bound to the interest-managed neighbour set.
public partial class Main : Node2D
{
	public override void _Ready()
	{
		GD.Print("StarRaid client starting");
		// TODO: connect to the server (Protobuf/TCP), version handshake, login,
		// then render the interest-managed neighbours + HUD.
	}
}
