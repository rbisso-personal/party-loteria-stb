namespace PartyLoteria.DevConsole.Commands
{
    /// <summary>
    /// Base class for all debug console commands.
    /// Extend this class to create new commands.
    /// </summary>
    public abstract class ConsoleCommand
    {
        /// <summary>
        /// Command name that users type (lowercase, no spaces)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Brief description shown in help command
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Optional usage example (e.g., "speed 5")
        /// </summary>
        public virtual string Usage => Name;

        /// <summary>
        /// Execute the command
        /// </summary>
        /// <param name="args">Command arguments (space-separated)</param>
        /// <param name="console">Reference to console for output</param>
        public abstract void Execute(string[] args, DebugConsole console);
    }
}
