namespace Starraid.Net;

// Session carries the live Connection (and the logged-in email) across the
// scene change from Login to Game. The Connection's read thread + inbox queue
// keep buffering server messages during the switch, so messages that arrive
// between scenes (e.g. SelfAssign right after LoginResult) are not lost — the
// Game scene drains them on its first frames.
public static class Session
{
    public static Connection? Conn;
    public static string Email = "";
}
