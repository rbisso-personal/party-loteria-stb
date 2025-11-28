namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Clears the console output
    /// </summary>
    public class ClearCommand : ConsoleCommand
    {
        public override string Name => "clear";
        public override string Description => "Clear console output";

        public override void Execute(string[] args, DebugConsole console)
        {
            console.Clear();
        }
    }
}
