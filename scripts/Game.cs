using System.Collections.Generic;
using Godot;
using Starraid.Net;
using Starraid.Protocol.V1;

// Game scene: renders server truth and sends navigation intent (../docs/client.md).
// It drains the Connection's inbox each frame and reacts to the Self* (own ship)
// and Object* (neighbour beacon) messages. Click empty space to move; the ship
// only moves when the server's SelfUpdate says so (server-authoritative).
public partial class Game : Node2D
{
    // Server coordinates are galaxy-scale (the starting ships span ±5000 units);
    // scale them down so the spread fits comfortably on screen around the camera.
    private const float WorldScale = 0.05f;

    private Connection? _conn;
    private Camera2D _camera = null!;
    private Label _hud = null!;

    private ulong _selfId;
    private bool _hasSelf;
    private Node2D? _selfNode;
    private readonly Dictionary<ulong, Node2D> _neighbours = new();

    public override void _Ready()
    {
        _conn = Session.Conn;

        _camera = new Camera2D();
        AddChild(_camera);
        _camera.MakeCurrent();

        var layer = new CanvasLayer();
        AddChild(layer);
        _hud = new Label
        {
            Text = $"{Session.Email} — left-click to move",
            Position = new Vector2(12, 8),
        };
        layer.AddChild(_hud);

        if (_conn == null)
            _hud.Text = "No connection — return to login.";
    }

    public override void _Process(double delta)
    {
        if (_conn == null) return;

        while (_conn.TryDequeueError(out string e))
            _hud.Text = e;

        while (_conn.TryDequeue(out ServerMessage msg))
            Handle(msg);
    }

    private void Handle(ServerMessage msg)
    {
        switch (msg.MsgCase)
        {
            case ServerMessage.MsgOneofCase.SelfAssign:
                _selfId = msg.SelfAssign.ObjectId;
                _hasSelf = true;
                _selfNode ??= SpawnShip(new Color(0.4f, 0.9f, 1.0f)); // own ship: cyan
                _selfNode.Position = ToScreen(msg.SelfAssign.Position);
                // Follow the player with the camera.
                if (_camera.GetParent() != _selfNode)
                {
                    _camera.GetParent().RemoveChild(_camera);
                    _selfNode.AddChild(_camera);
                    _camera.Position = Vector2.Zero;
                    _camera.MakeCurrent();
                }
                break;

            case ServerMessage.MsgOneofCase.SelfUpdate:
                if (_selfNode != null)
                    _selfNode.Position = ToScreen(msg.SelfUpdate.Position);
                break;

            case ServerMessage.MsgOneofCase.ObjectEnter:
                if (msg.ObjectEnter.ObjectId == _selfId && _hasSelf)
                    break; // the server's naive set may include us; skip our own
                {
                    Node2D n = GetOrSpawnNeighbour(msg.ObjectEnter.ObjectId);
                    n.Position = ToScreen(msg.ObjectEnter.Position);
                }
                break;

            case ServerMessage.MsgOneofCase.ObjectUpdate:
                if (msg.ObjectUpdate.ObjectId == _selfId)
                    break;
                {
                    Node2D n = GetOrSpawnNeighbour(msg.ObjectUpdate.ObjectId);
                    n.Position = ToScreen(msg.ObjectUpdate.Position);
                }
                break;

            case ServerMessage.MsgOneofCase.ObjectLeave:
                if (_neighbours.Remove(msg.ObjectLeave.ObjectId, out Node2D? gone))
                    gone.QueueFree();
                break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_conn == null) return;
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Vector2 world = GetGlobalMousePosition();
            _conn.SendMove(
                (long)Mathf.Round(world.X / WorldScale),
                (long)Mathf.Round(world.Y / WorldScale));
        }
    }

    private Node2D GetOrSpawnNeighbour(ulong id)
    {
        if (_neighbours.TryGetValue(id, out Node2D? existing))
            return existing;
        Node2D n = SpawnShip(new Color(1.0f, 0.5f, 0.3f)); // neighbour: orange
        _neighbours[id] = n;
        return n;
    }

    // SpawnShip builds a simple triangular placeholder ship (no art dependency).
    private Node2D SpawnShip(Color color)
    {
        var node = new Node2D();
        var poly = new Polygon2D
        {
            Polygon = new[] { new Vector2(0, -12), new Vector2(9, 11), new Vector2(-9, 11) },
            Color = color,
        };
        node.AddChild(poly);
        AddChild(node);
        return node;
    }

    private static Vector2 ToScreen(Vec2 p) =>
        new((float)p.X * WorldScale, (float)p.Y * WorldScale);
}
