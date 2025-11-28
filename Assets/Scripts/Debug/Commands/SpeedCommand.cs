namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Set or show the card draw speed
    /// </summary>
    public class SpeedCommand : ConsoleCommand
    {
        private const int MIN_SPEED = 1;
        private const int MAX_SPEED = 30;

        public override string Name => "speed";
        public override string Description => "Set card draw speed in seconds";
        public override string Usage => "speed [1-30]";

        public override void Execute(string[] args, DebugConsole console)
        {
            var game = console.GameManager;

            if (game == null)
            {
                console.PrintError("GameManager not found");
                return;
            }

            // No args - show current
            if (args.Length == 0)
            {
                console.PrintInfo("Use 'speed <seconds>' to change draw speed");
                console.Print($"Valid range: {MIN_SPEED}-{MAX_SPEED} seconds");
                return;
            }

            // Parse speed
            if (!int.TryParse(args[0], out int speed))
            {
                console.PrintError($"Invalid speed: {args[0]}");
                console.Print("Speed must be a number");
                return;
            }

            if (speed < MIN_SPEED || speed > MAX_SPEED)
            {
                console.PrintError($"Speed must be between {MIN_SPEED} and {MAX_SPEED}");
                return;
            }

            game.SetDrawSpeed(speed);
            console.Print($"Draw speed set to {speed} seconds");
        }
    }
}
