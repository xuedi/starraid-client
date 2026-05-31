using System.Collections.Generic;
using Godot;
using Starraid.Net;
using Starraid.Protocol.V1;

// Game scene: renders server truth and sends navigation intent (../docs/client.md).
// It drains the Connection's inbox each frame and reacts to the Self* (own ship)
// and Object* (neighbour beacon) messages — each carrying a type_key the client
// turns into a sprite. Ships glide between server updates (interpolation, no
// prediction) and rotate toward travel. The HUD shell (menu bar, radar, action
// bar, ship+shield widget) frames the view; it is inert. Click empty space to
// move; click a ship to ring it (local-only — no targeting protocol yet).
public partial class Game : Node2D
{
    // Server coordinates are galaxy-scale (the seeded scene spans ±9000 units);
    // scale them down so the spread fits comfortably on screen around the camera.
    private const float WorldScale = 0.05f;
    private const float SelectRadius = 30f; // world-space click tolerance to ring a ship

    private Connection? _conn;
    private Camera2D _camera = null!;
    private Label _hud = null!;
    private Radar _radar = null!;
    private ShipStatus _status = null!;

    private ulong _selfId;
    private bool _hasSelf;
    private Ship? _selfNode;
    private readonly Dictionary<ulong, Ship> _neighbours = new();
    private Ship? _selected;

    public override void _Ready()
    {
        _conn = Session.Conn;

        // Parallax starfield behind everything (follows the active camera).
        AddChild(Starfield.Build());

        _camera = new Camera2D();
        AddChild(_camera);
        _camera.MakeCurrent();

        // Custom cursor for the in-world view.
        Input.SetCustomMouseCursor(Art.Load("cursor/cursor.png"));

        // UI overlay: anchored HUD shell + a minimal status line.
        var ui = new CanvasLayer();
        AddChild(ui);
        ui.AddChild(new MenuBar());
        ui.AddChild(new ActionBar());
        _radar = new Radar();
        ui.AddChild(_radar);
        _status = new ShipStatus();
        ui.AddChild(_status);
        _hud = new Label
        {
            Text = $"{Session.Email} — left-click to move, click a ship to select",
            Position = new Vector2(48, 8),
        };
        ui.AddChild(_hud);

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

        UpdateRadar();
    }

    private void Handle(ServerMessage msg)
    {
        switch (msg.MsgCase)
        {
            case ServerMessage.MsgOneofCase.SelfAssign:
                _selfId = msg.SelfAssign.ObjectId;
                _hasSelf = true;
                if (_selfNode == null)
                {
                    _selfNode = SpawnShip(msg.SelfAssign.TypeKey, ownFallback: true);
                    _selfNode.SnapTo(ToScreen(msg.SelfAssign.Position));
                    FollowWithCamera(_selfNode);
                    _status.SetShip(Art.ShipTexture(msg.SelfAssign.TypeKey, "ships/ship1.png"));
                }
                else
                {
                    _selfNode.MoveTo(ToScreen(msg.SelfAssign.Position));
                }
                break;

            case ServerMessage.MsgOneofCase.SelfUpdate:
                _selfNode?.MoveTo(ToScreen(msg.SelfUpdate.Position));
                break;

            case ServerMessage.MsgOneofCase.ObjectEnter:
                if (msg.ObjectEnter.ObjectId == _selfId && _hasSelf)
                    break; // the server's naive set may include us; skip our own
                GetOrSpawnNeighbour(msg.ObjectEnter.ObjectId, msg.ObjectEnter.TypeKey)
                    .SnapTo(ToScreen(msg.ObjectEnter.Position));
                break;

            case ServerMessage.MsgOneofCase.ObjectUpdate:
                if (msg.ObjectUpdate.ObjectId == _selfId)
                    break;
                if (_neighbours.TryGetValue(msg.ObjectUpdate.ObjectId, out Ship? n))
                    n.MoveTo(ToScreen(msg.ObjectUpdate.Position));
                break;

            case ServerMessage.MsgOneofCase.ObjectLeave:
                if (_neighbours.Remove(msg.ObjectLeave.ObjectId, out Ship? gone))
                {
                    if (_selected == gone) Select(null);
                    gone.QueueFree();
                }
                break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_conn == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            switch (mb.ButtonIndex)
            {
                case MouseButton.Left:
                    OnLeftClick(GetGlobalMousePosition());
                    break;
                case MouseButton.WheelUp:
                    Zoom(1.1f);
                    break;
                case MouseButton.WheelDown:
                    Zoom(1f / 1.1f);
                    break;
            }
        }
    }

    // OnLeftClick rings a ship under the cursor (local-only selection), or else
    // sends a move-to-point and flashes a marker at the destination.
    private void OnLeftClick(Vector2 world)
    {
        Ship? hit = ShipAt(world);
        if (hit != null)
        {
            Select(hit);
            return;
        }
        Select(null);
        FlashMarker(world);
        _conn!.SendMove(
            (long)Mathf.Round(world.X / WorldScale),
            (long)Mathf.Round(world.Y / WorldScale));
    }

    private Ship? ShipAt(Vector2 world)
    {
        Ship? best = null;
        float bestDist = SelectRadius;
        foreach (Ship s in AllShips())
        {
            float d = s.GlobalPosition.DistanceTo(world);
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
    }

    private IEnumerable<Ship> AllShips()
    {
        if (_selfNode != null) yield return _selfNode;
        foreach (Ship s in _neighbours.Values) yield return s;
    }

    private void Select(Ship? ship)
    {
        if (_selected == ship) return;
        _selected?.SetSelected(false);
        _selected = ship;
        _selected?.SetSelected(true);
        _hud.Text = ship == null
            ? $"{Session.Email} — left-click to move, click a ship to select"
            : $"{Session.Email} — selected {(ship == _selfNode ? "own ship" : "a contact")}";
    }

    private void Zoom(float factor)
    {
        float z = Mathf.Clamp(_camera.Zoom.X * factor, 0.5f, 3.0f);
        _camera.Zoom = new Vector2(z, z);
    }

    private void FollowWithCamera(Ship target)
    {
        if (_camera.GetParent() == target) return;
        _camera.GetParent().RemoveChild(_camera);
        target.AddChild(_camera);
        _camera.Position = Vector2.Zero;
        _camera.MakeCurrent();
    }

    private void FlashMarker(Vector2 world)
    {
        var marker = new Sprite2D { Texture = Art.Load("cursor/mouse_spot.png"), GlobalPosition = world };
        AddChild(marker);
        Tween t = CreateTween();
        t.TweenProperty(marker, "modulate:a", 0f, 0.5f);
        t.Parallel().TweenProperty(marker, "scale", new Vector2(1.8f, 1.8f), 0.5f);
        t.TweenCallback(Callable.From(marker.QueueFree));
    }

    private void UpdateRadar()
    {
        if (_selfNode == null) return;
        var contacts = new List<Vector2>();
        foreach (Ship s in _neighbours.Values)
            contacts.Add(s.Position - _selfNode.Position);
        _radar.SetContacts(contacts);
    }

    private Ship GetOrSpawnNeighbour(ulong id, string typeKey)
    {
        if (_neighbours.TryGetValue(id, out Ship? existing))
            return existing;
        Ship n = SpawnShip(typeKey, ownFallback: false);
        _neighbours[id] = n;
        return n;
    }

    // SpawnShip builds a Ship node with the sprite for its class key.
    private Ship SpawnShip(string typeKey, bool ownFallback)
    {
        var ship = new Ship
        {
            TypeKey = typeKey,
            InitialTexture = Art.ShipTexture(typeKey, ownFallback ? "ships/ship1.png" : "ships/ship2.png"),
        };
        AddChild(ship);
        return ship;
    }

    private static Vector2 ToScreen(Vec2 p) =>
        new((float)p.X * WorldScale, (float)p.Y * WorldScale);
}
