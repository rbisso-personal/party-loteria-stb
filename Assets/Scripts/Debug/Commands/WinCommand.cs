using PartyLoteria.Data;
using PartyLoteria.Game;

namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Force a player to win (for testing)
    /// </summary>
    public class WinCommand : ConsoleCommand
    {
        public override string Name => "win";
        public override string Description => "Force a player to win";
        public override string Usage => "win [player-index]";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;
            var network = console.NetworkManager;

            if (game == null || network == null)
            {
                console.PrintError("GameManager or NetworkManager not found");
                return;
            }

            if (game.CurrentPhase != GamePhase.Playing && game.CurrentPhase != GamePhase.Paused)
            {
                console.PrintError($"Cannot force win in phase: {game.CurrentPhase}");
                console.Print("Game must be in Playing or Paused state");
                return;
            }

            if (game.Players.Count == 0)
            {
                console.PrintError("No players in game");
                return;
            }

            // Default to player 0
            int playerIndex = 0;

            // Parse optional player index argument
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out playerIndex))
                {
                    console.PrintError($"Invalid player index: {args[0]}");
                    return;
                }
            }

            // Validate player index
            if (playerIndex < 0 || playerIndex >= game.Players.Count)
            {
                console.PrintError($"Player index out of range: {playerIndex}");
                console.Print($"Valid range: 0-{game.Players.Count - 1}");
                return;
            }

            var player = game.Players[playerIndex];
            console.Print($"Forcing win for player {playerIndex}: {player.name}");

            network.DebugForceWin(player.id);
        }
    }
}
