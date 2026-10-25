namespace WebExpress.WebIndex.Wi
{
    /// <summary>
    /// Parse the handoff arguments.
    /// </summary>
    internal class ArgumentParser
    {
        private readonly List<ArgumentParserCommand> _commands = [];

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ArgumentParser()
        {
        }

        /// <summary>
        /// Registers a command.
        /// </summary>
        /// <param name="command">The command to register.</param>
        public void Register(ArgumentParserCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentException.ThrowIfNullOrWhiteSpace(command.FullName);
            _commands.Add(command);
        }

        /// <summary>
        /// Prepare program arguments.
        /// A program argument consists of a command and a value. The value can 
        /// contain the empty string, for example, -help. Commands beginning 
        /// with -- are considered comments and are not considered further.
        /// </summary>
        /// <param name="args">The program arguments.</param>
        /// <returns>A list of prepared program arguments.</returns>
        public ArgumentParserResult Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            var argsDict = new ArgumentParserResult();

            var key = "";
            var value = "";

            foreach (var s in args)
            {
                if (s.StartsWith("--") == true)
                {
                }
                else if (s.StartsWith("-") == true)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        Accept(argsDict, key, value);
                    }
                    value = "";
                    key = s;
                }
                else if (!string.IsNullOrEmpty(key))
                {
                    value += " " + s;
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                Accept(argsDict, key, value);
            }

            return argsDict;
        }

        /// <summary>
        /// Stores a parsed argument under the full name of its command, so the short and the long
        /// form of an argument end up under the same key.
        /// </summary>
        /// <param name="argsDict">The arguments recognized so far.</param>
        /// <param name="key">The argument as given on the command line, including the leading dash.</param>
        /// <param name="value">The collected value of the argument.</param>
        private void Accept(ArgumentParserResult argsDict, string key, string value)
        {
            var command = _commands.FirstOrDefault(x => string.Equals(x.FullName, key[1..], StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(x.ShortName, key[1..], StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                // a repeated argument (or its short and long form together) must not abort the program;
                // the last occurrence wins, as is common for command lines
                argsDict[command.FullName.ToLowerInvariant()] = value.Trim();
            }
        }

        /// <summary>
        /// Returns the recognized arguments.
        /// </summary>
        /// <param name="args">The program arguments.</param>
        /// <returns>The recognized arguments.</returns>
        public string GetValidArguments(string[] args)
        {
            var argumentDict = Parse(args);

            var v = from x in argumentDict
                    select "-" + x.Key + (string.IsNullOrWhiteSpace(x.Value) ? "" : " " + x.Value);

            return string.Join(' ', v);
        }

        /// <summary>
        /// Returns a help string.
        /// </summary>
        /// <returns>The help text of the commands, one line per command.</returns>
        public string GetHelp()
        {
            return string.Join(Environment.NewLine, _commands.Select(x => string.Join(" ", $"-{x.FullName}{(string.IsNullOrWhiteSpace(x.ShortName) ? "" : $" (or -{x.ShortName})")} {x.ParameterDescription}".Trim(), $": {x.Description}")));
        }

        /// <summary>
        /// Converts the commands to a help string.
        /// </summary>
        /// <returns>The short forms of the commands with their parameters, separated by vertical bars.</returns>
        public override string ToString()
        {
            return string.Join(" | ", _commands.Select(x => $"-{(string.IsNullOrWhiteSpace(x.ShortName) ? x.FullName : x.ShortName)} {x.ParameterDescription}".Trim()));
        }
    }
}
