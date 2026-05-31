# StarRaid client — Godot 4 (.NET / C#). Run `just` to list recipes.
#
# Requires godot-mono + dotnet-sdk (not always present in a dev box — see README).
# Override the Godot binary if it is named differently or not on PATH:
#     just godot=/path/to/godot-mono run

godot := env_var_or_default("GODOT", "godot-mono")

# List available recipes
default:
    @just --list

# Restore NuGet packages for the C# assembly
install:
    dotnet restore

# Build the C# assembly
build:
    dotnet build

# Run the game (opens the main scene, scenes/Main.tscn)
run:
    {{godot}} --path .

# Open the project in the Godot editor
edit:
    {{godot}} -e --path .
