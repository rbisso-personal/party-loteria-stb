using PartyLoteria.Data;
using PartyLoteria.Game;
using PartyLoteria.Network;

namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Shows current game status
    /// </summary>
    public class StatusCommand : ConsoleCommand
    {
        public override string Name => "status";
        public override string Description => "Show current game status";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;
            var network = console.NetworkManager;

            console.Print("=== GAME STATUS ===");

            // Network status
            if (network != null)
            {
                console.Print($"Connected: {(network.IsConnected ? "Yes" : "No")}");
                console.Print($"Room Code: {network.CurrentRoomCode ?? "None"}");
            }
            else
            {
                console.PrintWarning("NetworkManager not found");
            }

            // Game status
            if (game != null)
            {
                console.Print($"Phase: {game.CurrentPhase}");
                console.Print($"Players: {game.Players.Count}");

                if (game.Players.Count > 0)
                {
                    console.Print("Player list:");
                    foreach (var player in game.Players)
                    {
                        console.Print($"  - {player.name} ({player.id})");
                    }
                }

                if (game.CurrentPhase == GamePhase.Playing || game.CurrentPhase == GamePhase.Paused)
                {
                    console.Print($"Cards Drawn: {game.CardsDrawn}/{game.TotalCards}");
                    if (game.CurrentCard != null)
                    {
                        console.Print($"Current Card: {game.CurrentCard.name_es} ({game.CurrentCard.id})");
                    }
                }

                if (game.CurrentPhase == GamePhase.Finished && game.GameWinner != null)
                {
                    console.Print($"Winner: {game.GameWinner.name}");
                }
            }
            else
            {
                console.PrintWarning("GameManager not found");
            }
        }
    }
}
