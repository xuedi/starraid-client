# client

The StarRaid native desktop game client — **Godot with C#**. See
[../docs/client.md](../docs/client.md).

Thin and server-authoritative: it renders only what the server sends (interest-managed,
sensor-gated), takes player input, runs client-side automation (AI modules), and sends
**intent** over the wire [`protocol`](../protocol) (Protobuf over TCP).

## Prerequisites

Not buildable until these are installed (not present in the current dev environment):

```sh
# Arch / CachyOS
paru -S godot-mono        # Godot 4 with the Mono/.NET runtime
paru -S dotnet-sdk        # .NET SDK for C#
```

## Getting started

Open the project in the Godot editor (it restores NuGet + builds the C# assembly), or use the
justfile:

```sh
just install     # dotnet restore
just build       # dotnet build
just run         # godot-mono --path .  (runs scenes/Main.tscn)
just edit        # open in the Godot editor
```

If your Godot binary isn't `godot-mono` on PATH, override it: `just godot=/path/to/godot run`.

The C# client will consume the protocol's generated **C# bindings** (`gen/csharp` in the
[`protocol`](../protocol) repo) once wire integration begins; that path will be made
configurable here, mirroring how the Go components point at the protocol checkout.

Layout: `project.godot` (project), `scenes/` (scenes), `scripts/` (C# code),
`Starraid.Client.csproj` (the C# assembly).
