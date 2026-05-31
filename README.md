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

## Run

Open the project in Godot (it will restore NuGet/build the C# assembly), or:

```sh
godot-mono --path .       # run the main scene (scenes/Main.tscn)
```

Layout: `project.godot` (project), `scenes/` (scenes), `scripts/` (C# code),
`Starraid.Client.csproj` (the C# assembly).
