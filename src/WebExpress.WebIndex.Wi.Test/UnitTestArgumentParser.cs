using Xunit;

namespace WebExpress.WebIndex.Wi.Test
{
    /// <summary>
    /// Protects startup option aliases and repeated option handling.
    /// </summary>
    public class UnitTestArgumentParser
    {
        /// <summary>
        /// Verifies that aliases share one result and the final occurrence wins.
        /// </summary>
        [Fact]
        public void ParseUsesLastAliasValue()
        {
            // arrange
            var parser = new ArgumentParser();
            parser.Register(new ArgumentParserCommand { FullName = "open", ShortName = "o" });

            // act
            var result = parser.Parse(["-O", "first", "-OPEN", "Data Files", "example.ws"]);

            // validation
            Assert.Single(result);
            Assert.Equal("Data Files example.ws", result["open"]);
        }

        /// <summary>
        /// Prevents unrelated leading tokens from becoming an option value.
        /// </summary>
        [Fact]
        public void ParseIgnoresLeadingValuesAndComments()
        {
            // arrange
            var parser = new ArgumentParser();
            parser.Register(new ArgumentParserCommand { FullName = "help", ShortName = "h" });

            // act
            var result = parser.Parse(["unrelated", "--comment", "-h"]);

            // validation
            Assert.Equal("", result["help"]);
        }

        /// <summary>
        /// Allows a command to omit its optional short alias.
        /// </summary>
        [Fact]
        public void ParseSupportsLongOnlyCommands()
        {
            // arrange
            var parser = new ArgumentParser();
            parser.Register(new ArgumentParserCommand { FullName = "help" });

            // act
            var result = parser.Parse(["-unknown", "value", "-help"]);

            // validation
            Assert.Single(result);
            Assert.Contains("-help", parser.GetHelp());
        }
    }
}
