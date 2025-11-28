namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Set the win pattern for the game
    /// </summary>
    public class PatternCommand : ConsoleCommand
    {
        private static readonly string[] VALID_PATTERNS = { "line", "corners", "fullcard" };

        public override string Name => "pattern";
        public override string Description => "Set win pattern (line, corners, fullcard)";
        public override string Usage => "pattern [line|corners|fullcard]";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;

            if (game == null)
            {
                console.PrintError("GameManager not found");
                return;
            }

            // No args - show help
            if (args.Length == 0)
            {
                console.Print("Available patterns:");
                console.Print("  line     - Complete a horizontal, vertical, or diagonal line");
                console.Print("  corners  - Mark all four corners");
                console.Print("  fullcard - Mark the entire card");
                console.PrintInfo("Use 'pattern <name>' to set pattern before starting game");
                return;
            }

            string pattern = args[0].ToLower();

            // Validate pattern
            bool isValid = false;
            foreach (var p in VALID_PATTERNS)
            {
                if (p == pattern)
                {
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                console.PrintError($"Invalid pattern: {pattern}");
                console.Print("Valid patterns: line, corners, fullcard");
                return;
            }

            game.SetWinPattern(pattern);
            console.Print($"Win pattern set to: {pattern}");
            console.PrintInfo("Pattern will apply to the next game");
        }
    }
}
