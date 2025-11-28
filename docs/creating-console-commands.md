# Creating Debug Console Commands

This guide explains how to create new commands for the in-game debug console.

## Overview

The debug console (toggle with `` ` `` key) provides a command-line interface for debugging and tweaking game parameters at runtime. Commands follow a simple class-based pattern.

## Quick Start

1. Create a new command file in `Assets/Scripts/Debug/Commands/`
2. Extend the `ConsoleCommand` base class
3. Implement the required properties and `Execute()` method
4. Register your command in `DebugConsole.cs`

## Step-by-Step Guide

### Step 1: Create Your Command File

Create a new file `Assets/Scripts/Debug/Commands/YourCommand.cs`:

```csharp
namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// YourCommand - Brief description
    /// </summary>
    public class YourCommand : ConsoleCommand
    {
        public override string Name => "yourcommand";  // Command name (lowercase)
        public override string Description => "Brief help text";
        public override string Usage => "yourcommand [args]";  // Optional

        public override void Execute(string[] args, DebugConsole console)
        {
            // Your command logic here
        }
    }
}
```

### Step 2: Implement Command Logic

Commands typically follow this pattern:

```csharp
public override void Execute(string[] args, DebugConsole console)
{
    var game = console.GameManager;

    // Validate prerequisites
    if (game == null)
    {
        console.PrintError("GameManager not found");
        return;
    }

    // No arguments? Show current state or help
    if (args.Length == 0)
    {
        console.Print("Current value: ...");
        return;
    }

    // Parse and validate arguments
    if (!int.TryParse(args[0], out int value))
    {
        console.PrintError($"Invalid value: {args[0]}");
        return;
    }

    // Apply changes
    game.SomeProperty = value;
    console.Print($"Value set to {value}");
}
```

### Step 3: Register Your Command

Edit `Assets/Scripts/Debug/DebugConsole.cs`, find `RegisterCommands()`:

```csharp
private void RegisterCommands()
{
    // Built-in commands
    RegisterCommand(new HelpCommand());
    RegisterCommand(new ClearCommand());
    RegisterCommand(new StatusCommand());

    // Game commands
    RegisterCommand(new DrawCommand());
    RegisterCommand(new SpeedCommand());
    RegisterCommand(new ResetCommand());
    RegisterCommand(new PatternCommand());
    RegisterCommand(new AutoDrawCommand());

    // Add your command here:
    RegisterCommand(new YourCommand());
}
```

### Step 4: Test Your Command

1. Start the game in Unity
2. Press `` ` `` to open the debug console
3. Type `help` to see your command listed
4. Type `yourcommand` to test it

## Console Output Methods

The `DebugConsole` provides several output methods:

```csharp
console.Print("Normal message");           // Green (default)
console.PrintError("Error message");       // Red
console.PrintWarning("Warning message");   // Yellow
console.PrintInfo("Info message");         // Light blue
console.Clear();                           // Clear all output
```

## Accessing Game State

Commands have access to managers via the console:

```csharp
var game = console.GameManager;        // GameManager instance
var network = console.NetworkManager;  // NetworkManager instance
```

### GameManager Properties

- `game.CurrentPhase` - Current game phase (Waiting, Playing, Paused, Finished)
- `game.RoomCode` - Current room code
- `game.Players` - List of connected players
- `game.CurrentCard` - Currently displayed card
- `game.CardsDrawn` / `game.TotalCards` - Progress through deck
- `game.GameWinner` - Winner info (when finished)

### GameManager Methods

- `game.StartGame()` - Start the game
- `game.DrawNextCard()` - Draw next card manually
- `game.PauseGame()` / `game.ResumeGame()` - Control game flow
- `game.ResetGame()` - Reset to lobby
- `game.SetWinPattern(string)` - Set win condition
- `game.SetDrawSpeed(int)` - Set seconds between draws
- `game.SetAutoDrawCards(bool)` - Enable/disable auto-draw

### NetworkManager Properties

- `network.IsConnected` - Connection status
- `network.CurrentRoomCode` - Room code

## Examples

### Simple Status Command

```csharp
public class PlayersCommand : ConsoleCommand
{
    public override string Name => "players";
    public override string Description => "List connected players";

    public override void Execute(string[] args, DebugConsole console)
    {
        var game = console.GameManager;

        if (game.Players.Count == 0)
        {
            console.Print("No players connected");
            return;
        }

        console.Print($"Players ({game.Players.Count}):");
        foreach (var player in game.Players)
        {
            console.Print($"  {player.name}");
        }
    }
}
```

### Command with Arguments

```csharp
public class SkipCommand : ConsoleCommand
{
    public override string Name => "skip";
    public override string Description => "Skip ahead N cards";
    public override string Usage => "skip <count>";

    public override void Execute(string[] args, DebugConsole console)
    {
        var game = console.GameManager;

        if (args.Length == 0)
        {
            console.PrintError("Usage: skip <count>");
            return;
        }

        if (!int.TryParse(args[0], out int count) || count < 1)
        {
            console.PrintError("Count must be a positive number");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            game.DrawNextCard();
        }

        console.Print($"Skipped {count} cards");
    }
}
```

### Command with Flags

```csharp
public class ForceWinCommand : ConsoleCommand
{
    public override string Name => "forcewin";
    public override string Description => "Force a player to win";
    public override string Usage => "forcewin <player-name> [--silent]";

    public override void Execute(string[] args, DebugConsole console)
    {
        if (args.Length == 0)
        {
            console.PrintError("Usage: forcewin <player-name>");
            return;
        }

        string playerName = args[0];
        bool silent = args.Contains("--silent");

        // Implementation...

        if (!silent)
        {
            console.Print($"Forced win for {playerName}");
        }
    }
}
```

## Best Practices

1. **Validate inputs** - Check for null managers and invalid arguments
2. **Show current state** - When called with no args, show current value
3. **Provide helpful errors** - Include usage hints in error messages
4. **Use appropriate colors** - Error=red, Warning=yellow, Info=blue
5. **Keep names short** - Single lowercase word for command name
6. **Document usage** - Override `Usage` property for complex commands

## Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Command name | lowercase, no spaces | `speed`, `autodraw` |
| Class name | PascalCase + "Command" | `SpeedCommand` |
| File name | Match class name | `SpeedCommand.cs` |

## Existing Commands Reference

| Command | Description |
|---------|-------------|
| `help` | Show all available commands |
| `clear` | Clear console output |
| `status` | Show current game status |
| `draw` | Manually draw next card |
| `speed [n]` | Set draw speed (seconds) |
| `pattern [name]` | Set win pattern |
| `autodraw [on/off]` | Toggle auto-draw |
| `reset` | Reset current game |
