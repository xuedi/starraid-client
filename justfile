# StarRaid client — Godot 4 (.NET / C#). Run `just` to list recipes.
#
# Requires godot-mono + dotnet-sdk (not always present in a dev box — see README).
# Override the Godot binary if it is named differently or not on PATH:
#     just godot=/path/to/godot-mono run

godot := env_var_or_default("GODOT", "godot-mono")

# Where the protocol checkout lives (its `just gen-csharp` writes gen/csharp).
# Same convention as the Go components (server/npc).
protocol_path := env_var_or_default("STARRAID_PROTOCOL_PATH", "../protocol")

# List available recipes
default:
    @just --list

# Generate the protocol's C# bindings (into <protocol>/gen/csharp, gitignored).
# The csproj compiles them straight from there at build time.
gen:
    cd {{protocol_path}} && just gen-csharp

# Restore NuGet packages for the C# assembly (regenerates bindings first)
install: gen
    dotnet restore

# Build the C# assembly (regenerates bindings first)
build: gen
    dotnet build

# Run the game (opens the main scene, scenes/Main.tscn)
run:
    {{godot}} --path .

# Open the project in the Godot editor
edit:
    {{godot}} -e --path .
