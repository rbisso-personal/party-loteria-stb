namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Toggle automatic card drawing
    /// </summary>
    public class AutoDrawCommand : ConsoleCommand
    {
        public override string Name => "autodraw";
        public override string Description => "Toggle automatic card drawing";
        public override string Usage => "autodraw [on|off]";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;

            if (game == null)
            {
                console.PrintError("GameManager not found");
                return;
            }

            // Parse on/off argument
            if (args.Length == 0)
            {
                console.Print("Usage: autodraw [on|off]");
                console.Print("  on  - Cards draw automatically on timer");
                console.Print("  off - Manual draw only (use 'draw' command)");
                return;
            }

            string value = args[0].ToLower();
            bool enable;

            if (value == "on" || value == "true" || value == "1")
            {
                enable = true;
            }
            else if (value == "off" || value == "false" || value == "0")
            {
                enable = false;
            }
            else
            {
                console.PrintError($"Invalid value: {value}");
                console.Print("Use 'on' or 'off'");
                return;
            }

            game.SetAutoDrawCards(enable);
            console.Print($"Auto-draw: {(enable ? "ON" : "OFF")}");

            if (!enable)
            {
                console.PrintInfo("Use 'draw' command to manually draw cards");
            }
        }
    }
}
