namespace WebExpress.WebIndex.Wi
{
    /// <summary>
    /// A simple parser that handles a command line. This parser recognizes commands and optional parameters. In 
    /// the event of an invalid command or an error, it outputs an error text and help for the possible commands.
    /// </summary>
    internal class CommandParser
    {
        private readonly Dictionary<string, CommandType> _commands = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the CommandParser class.
        /// </summary>
        public CommandParser()
        {
        }

        /// <summary>
        /// Register a command.
        /// </summary>
        /// <param name="command">The command to be registered.</param>
        /// <param name="type">The command as a type.</param>
        public void Register(string command, CommandType type)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command);
            ArgumentNullException.ThrowIfNull(type);

            if (!_commands.ContainsKey(command))
            {
                _commands.Add(command, type);
            }
        }

        /// <summary>
        /// Parses the input string into a command and its parameters.
        /// </summary>
        /// <remarks>
        /// Only the command word is split off by whitespace. The rest of the line is the
        /// parameter, and for a two-parameter command the first word of the rest is the
        /// first parameter and what follows it the second - a file path or a list of field
        /// values may itself contain spaces and must reach the command whole.
        /// </remarks>
        /// <param name="input">The input string to parse.</param>
        /// <returns>The recognized action and its unmodified parameter values.</returns>
        public Command Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new Command() { Action = CommandAction.Empty };
            }

            var parts = input.Trim().Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var command = parts[0];
            var rest = parts.Length > 1 ? parts[1] : null;

            if (!_commands.TryGetValue(command, out CommandType type))
            {
                return new Command() { Action = CommandAction.None };
            }

            if (type.Action.GetParameterCount() < 2 || rest is null)
            {
                return new Command() { Action = type.Action, Parameter1 = rest };
            }

            var pair = rest.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new Command()
            {
                Action = type.Action,
                Parameter1 = pair[0],
                Parameter2 = pair.Length > 1 ? pair[1] : null
            };
        }

        /// <summary>
        /// Prints the help text to the console.
        /// </summary>
        public void PrintHelp()
        {
            Console.WriteLine("Available commands:");
            foreach (var c in _commands.Where(x => !x.Value.Secret))
            {
                Console.WriteLine($"- {$"{c.Key} {c.Value.Action.GetParameterDescription()}".Trim()} : {c.Value.Action.GetDescription()}");
            }
        }
    }
}
