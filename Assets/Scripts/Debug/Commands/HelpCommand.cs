namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Shows all available commands
    /// </summary>
    public class HelpCommand : ConsoleCommand
    {
        public override string Name => "help";
        public override string Description => "Show available commands";

        public override void Execute(string[] args, DebugConsole console)
        {
            console.Print("=== DEBUG CONSOLE ===");
            console.Print("Available commands:");
            console.Print("");

            foreach (var cmd in console.Commands.Values)
            {
                string usage = cmd.Usage != cmd.Name ? $" ({cmd.Usage})" : "";
                console.Print($"  {cmd.Name}{usage} - {cmd.Description}");
            }

            console.Print("");
            console.Print("Press ` to toggle console, ESC to close");
        }
    }
}
