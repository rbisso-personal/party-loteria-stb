namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Reset the current game
    /// </summary>
    public class ResetCommand : ConsoleCommand
    {
        public override string Name => "reset";
        public override string Description => "Reset the current game";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;

            if (game == null)
            {
                console.PrintError("GameManager not found");
                return;
            }

            if (string.IsNullOrEmpty(game.RoomCode))
            {
                console.PrintError("No active room");
                return;
            }

            game.ResetGame();
            console.Print("Game reset requested");
        }
    }
}
