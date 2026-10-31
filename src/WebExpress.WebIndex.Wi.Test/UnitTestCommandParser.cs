using Xunit;

namespace WebExpress.WebIndex.Wi.Test
{
    /// <summary>
    /// Protects command boundaries while preserving user data verbatim.
    /// </summary>
    public class UnitTestCommandParser
    {
        /// <summary>
        /// Verifies that whitespace and command casing do not change parameter contents.
        /// </summary>
        /// <param name="input">The entered command line.</param>
        [Theory]
        [InlineData("UPDATE identifier Name=Hello World, Count=2")]
        [InlineData("  update   identifier   Name=Hello World, Count=2  ")]
        [InlineData("update\tidentifier\tName=Hello World, Count=2")]
        public void ParsePreservesValues(string input)
        {
            // arrange
            var parser = new CommandParser();
            parser.Register("update", new CommandType { Action = CommandAction.Update });

            // act
            var command = parser.Parse(input);

            // validation
            Assert.Equal(CommandAction.Update, command.Action);
            Assert.Equal("identifier", command.Parameter1);
            Assert.Equal("Name=Hello World, Count=2", command.Parameter2);
        }

        /// <summary>
        /// Verifies registration and lookup use the same casing rules.
        /// </summary>
        [Fact]
        public void RegisterMatchesCaseInsensitively()
        {
            // arrange
            var parser = new CommandParser();
            parser.Register("OPEN", new CommandType { Action = CommandAction.OpenIndexFile });

            // act
            var command = parser.Parse(@"open C:\Data Files\Example.ws");

            // validation
            Assert.Equal(CommandAction.OpenIndexFile, command.Action);
            Assert.Equal(@"C:\Data Files\Example.ws", command.Parameter1);
        }

        /// <summary>
        /// Distinguishes empty input from an unrecognized command.
        /// </summary>
        /// <param name="input">The entered command line.</param>
        /// <param name="expected">The expected action name.</param>
        [Theory]
        [InlineData(null, "Empty")]
        [InlineData("  ", "Empty")]
        [InlineData("unknown", "None")]
        public void ParseHandlesMissingCommands(string input, string expected)
        {
            // arrange
            var parser = new CommandParser();

            // act
            var command = parser.Parse(input);

            // validation
            Assert.Equal(expected, command.Action.ToString());
        }
    }
}
