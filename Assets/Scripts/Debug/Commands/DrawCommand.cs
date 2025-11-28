using PartyLoteria.Data;
using PartyLoteria.Game;

namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Manually draws the next card
    /// </summary>
    public class DrawCommand : ConsoleCommand
    {
        public override string Name => "draw";
        public override string Description => "Manually draw the next card";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;

            if (game == null)
            {
                console.PrintError("GameManager not found");
                return;
            }

            if (game.CurrentPhase != GamePhase.Playing && game.CurrentPhase != GamePhase.Paused)
            {
                console.PrintError($"Cannot draw card in phase: {game.CurrentPhase}");
                console.Print("Game must be in Playing or Paused state");
                return;
            }

            game.DrawNextCard();
            console.Print("Drew next card");
        }
    }
}
