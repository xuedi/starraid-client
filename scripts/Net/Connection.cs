using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using Starraid.Protocol.V1;

namespace Starraid.Net;

// Connection is the client's hand-rolled wire codec: a TcpClient carrying
// length-prefixed Protobuf envelopes (see ../docs/protocol.md). Framing mirrors
// server/internal/wire/frame.go — a 4-byte big-endian uint32 length followed by a
// marshalled envelope.
//
// A background thread reads frames and enqueues decoded ServerMessages onto a
// thread-safe queue; the Godot main thread drains it (TryDequeue) inside _Process,
// so all node mutation stays on the main thread. Sends are serialized under a lock.
// (A shared client SDK could later absorb this; for now it mirrors the npc bot.)
public sealed class Connection
{
    private const int MaxFrameSize = 1 << 20; // must match server wire.MaxFrameSize

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private Thread? _reader;
    private volatile bool _closed;
    private readonly object _writeLock = new();

    private readonly ConcurrentQueue<ServerMessage> _inbox = new();
    private readonly ConcurrentQueue<string> _errors = new();

    // Connect dials host:port synchronously and starts the read loop. Returns false
    // (with a human-readable error) if the dial fails.
    public bool Connect(string host, int port, out string error)
    {
        try
        {
            _tcp = new TcpClient();
            _tcp.Connect(host, port);
            _tcp.NoDelay = true;
            _stream = _tcp.GetStream();
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
        _reader = new Thread(ReadLoop) { IsBackground = true, Name = "starraid-net-read" };
        _reader.Start();
        error = "";
        return true;
    }

    // TryDequeue pops the next decoded server message, if any (main-thread drain).
    public bool TryDequeue(out ServerMessage msg) => _inbox.TryDequeue(out msg!);

    // TryDequeueError pops the next connection error string, if any.
    public bool TryDequeueError(out string err) => _errors.TryDequeue(out err!);

    public void SendHello(uint version) =>
        Send(new ClientMessage { Hello = new Hello { ProtocolVersion = version } });

    public void SendLogin(string username, string secret) =>
        Send(new ClientMessage { Login = new LoginRequest { Username = username, Secret = secret } });

    public void SendMove(long x, long y) =>
        Send(new ClientMessage { Move = new Move { Target = new Vec2 { X = x, Y = y } } });

    public void SendStop() =>
        Send(new ClientMessage { Stop = new Stop() });

    private void Send(ClientMessage m)
    {
        if (_stream == null) return;
        byte[] payload = m.ToByteArray();
        if (payload.Length > MaxFrameSize)
        {
            _errors.Enqueue("outgoing frame exceeds max size");
            return;
        }
        Span<byte> hdr = stackalloc byte[4];
        hdr[0] = (byte)(payload.Length >> 24);
        hdr[1] = (byte)(payload.Length >> 16);
        hdr[2] = (byte)(payload.Length >> 8);
        hdr[3] = (byte)payload.Length;
        lock (_writeLock)
        {
            try
            {
                _stream.Write(hdr);
                _stream.Write(payload, 0, payload.Length);
            }
            catch (Exception e)
            {
                if (!_closed) _errors.Enqueue("send failed: " + e.Message);
            }
        }
    }

    private void ReadLoop()
    {
        try
        {
            byte[] hdr = new byte[4];
            while (!_closed)
            {
                ReadFull(hdr, 4);
                int n = (hdr[0] << 24) | (hdr[1] << 16) | (hdr[2] << 8) | hdr[3];
                if (n < 0 || n > MaxFrameSize)
                    throw new IOException("frame exceeds max size");
                byte[] buf = new byte[n];
                ReadFull(buf, n);
                _inbox.Enqueue(ServerMessage.Parser.ParseFrom(buf));
            }
        }
        catch (Exception e)
        {
            if (!_closed) _errors.Enqueue("connection lost: " + e.Message);
        }
    }

    private void ReadFull(byte[] buf, int n)
    {
        int off = 0;
        while (off < n)
        {
            int r = _stream!.Read(buf, off, n - off);
            if (r <= 0) throw new EndOfStreamException();
            off += r;
        }
    }

    public void Close()
    {
        _closed = true;
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _tcp?.Close(); } catch { /* ignore */ }
    }
}
