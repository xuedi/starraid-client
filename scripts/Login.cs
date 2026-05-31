using Godot;
using Starraid.Net;
using Starraid.Protocol.V1;

// Login scene: the version handshake + authentication flow (session lifecycle
// steps 1–2, ../docs/protocol.md). On success it carries the live Connection into
// the Game scene via Session. The UI is built in code so the .tscn stays a bare
// root node (no editor round-trip needed for a placeholder form).
public partial class Login : Control
{
    private const uint ProtocolVersion = 1;

    private LineEdit _host = null!;
    private LineEdit _email = null!;
    private LineEdit _password = null!;
    private Button _connect = null!;
    private Label _status = null!;

    private Connection? _conn;
    private bool _switching;

    public override void _Ready()
    {
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(320, 0) };
        center.AddChild(vbox);

        vbox.AddChild(new Label { Text = "StarRaid — Login" });

        _host = new LineEdit { Text = "127.0.0.1:60000", PlaceholderText = "host:port" };
        vbox.AddChild(_host);

        _email = new LineEdit { Text = "test@example.org", PlaceholderText = "email" };
        vbox.AddChild(_email);

        _password = new LineEdit { Text = "1234", PlaceholderText = "password", Secret = true };
        _password.TextSubmitted += _ => OnConnectPressed();
        vbox.AddChild(_password);

        _connect = new Button { Text = "Connect" };
        _connect.Pressed += OnConnectPressed;
        vbox.AddChild(_connect);

        _status = new Label { Text = "" };
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_status);
    }

    private void OnConnectPressed()
    {
        if (_conn != null) return; // already connecting/connected

        string hostPort = _host.Text.Trim();
        string host = hostPort;
        int port = 60000;
        int colon = hostPort.LastIndexOf(':');
        if (colon >= 0)
        {
            host = hostPort[..colon];
            if (!int.TryParse(hostPort[(colon + 1)..], out port))
            {
                _status.Text = "Invalid port.";
                return;
            }
        }

        var conn = new Connection();
        if (!conn.Connect(host, port, out string err))
        {
            _status.Text = "Connection failed: " + err;
            return;
        }

        _conn = conn;
        Session.Conn = conn;
        Session.Email = _email.Text;
        _connect.Disabled = true;
        _status.Text = "Connecting…";
        conn.SendHello(ProtocolVersion);
    }

    public override void _Process(double delta)
    {
        if (_conn == null || _switching) return;

        while (_conn.TryDequeueError(out string e))
        {
            _status.Text = e;
            _connect.Disabled = false;
            _conn = null; // allow a retry
            return;
        }

        while (_conn.TryDequeue(out ServerMessage msg))
        {
            switch (msg.MsgCase)
            {
                case ServerMessage.MsgOneofCase.VersionResult:
                    if (msg.VersionResult.Accepted)
                    {
                        _status.Text = "Logging in…";
                        _conn.SendLogin(_email.Text, _password.Text);
                    }
                    else
                    {
                        _status.Text = $"Version rejected (server wants ≥ {msg.VersionResult.MinSupported}).";
                        _connect.Disabled = false;
                        _conn = null;
                    }
                    return;

                case ServerMessage.MsgOneofCase.LoginResult:
                    if (msg.LoginResult.Ok)
                    {
                        _switching = true;
                        // Defer the scene change; the Connection (and any queued
                        // SelfAssign/SelfUpdate) lives on in Session for the Game scene.
                        CallDeferred(nameof(GoToGame));
                    }
                    else
                    {
                        _status.Text = "Login failed: " + msg.LoginResult.Reason;
                        _connect.Disabled = false;
                        _conn = null;
                    }
                    return;
            }
        }
    }

    private void GoToGame()
    {
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }
}
