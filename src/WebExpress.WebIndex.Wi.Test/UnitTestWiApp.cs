using System.Diagnostics;
using WebExpress.WebIndex.Wi.Model;
using Xunit;

namespace WebExpress.WebIndex.Wi.Test
{
    /// <summary>
    /// Verifies the executable with redirected streams without changing global test console state.
    /// </summary>
    public class UnitTestWiApp : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "wi-cli-tests", Guid.NewGuid().ToString("N"));

        /// <summary>
        /// Creates a process working directory isolated from repository and user files.
        /// </summary>
        public UnitTestWiApp()
        {
            Directory.CreateDirectory(_directory);
        }

        /// <summary>
        /// Ensures startup failures return an error rather than entering the command loop.
        /// </summary>
        /// <param name="argument">The missing or nonexistent startup path.</param>
        [Theory]
        [InlineData("")]
        [InlineData("missing.ws")]
        public async Task InvalidStartupPathReturnsFailure(string argument)
        {
            // arrange
            var arguments = new[] { "-open", argument };

            // act
            var result = await Run("", arguments);

            // validation
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Error);
        }

        /// <summary>
        /// Creates a missing startup directory before commands access its index files.
        /// </summary>
        /// <param name="argument">The relative or environment-based directory path.</param>
        [Theory]
        [InlineData("New Index")]
        [InlineData("New Index/")]
        [InlineData("%WI_TEST_DIRECTORY%/New Index/")]
        [InlineData("%WI_TEST_DIRECTORY%/New Index")]
        public async Task MissingStartupDirectoryIsCreated(string argument)
        {
            // arrange
            var target = Path.Combine(_directory, "New Index");

            // act
            var result = await Run("create Products\nquit\n", ["-o", argument]);

            // validation
            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Error);
            Assert.True(File.Exists(Path.Combine(target, "Products.ws")), result.Output);
        }

        /// <summary>
        /// Reports directory creation failures without terminating with an unhandled exception.
        /// </summary>
        [Fact]
        public async Task StartupDirectoryBelowFileReturnsFailure()
        {
            // arrange
            File.WriteAllText(Path.Combine(_directory, "blocked"), "file");

            // act
            var result = await Run("", ["-o", Path.Combine("blocked", "child")]);

            // validation
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(result.Error);
            Assert.Contains("could not be opened", result.Output);
        }

        /// <summary>
        /// Verifies both an explicit quit and end of input release the application.
        /// </summary>
        /// <param name="input">The redirected command stream.</param>
        [Theory]
        [InlineData("")]
        [InlineData("quit\n")]
        [InlineData("create Products\n")]
        [InlineData("create bad*name\nquit\n")]
        public async Task RedirectedInputTerminates(string input)
        {
            // arrange
            var arguments = Array.Empty<string>();

            // act
            var result = await Run(input, arguments);

            // validation
            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Error);
        }

        /// <summary>
        /// Opens a full startup path and resolves visible fields by name or displayed number.
        /// </summary>
        /// <param name="field">The requested field selection.</param>
        [Theory]
        [InlineData("Name")]
        [InlineData("1")]
        public async Task OpenPathAndFieldResolveDisplayedSelection(string field)
        {
            // arrange
            var file = CreateIndex();

            // act
            var result = await Run($"show\nopen {field}\nshow\nclose\nall\nquit\n", ["-open", file]);

            // validation
            Assert.Equal(0, result.ExitCode);
            Assert.Empty(result.Error);
            Assert.Contains("Products/Name/>", result.Output);
            Assert.Contains("hello", result.Output);
            Assert.DoesNotContain("not found", result.Output);
        }

        /// <summary>
        /// Treats end of input at a destructive confirmation as a negative response.
        /// </summary>
        [Fact]
        public async Task ConfirmationAtEndOfInputPreservesIndex()
        {
            // arrange
            var file = CreateIndex();

            // act
            var result = await Run("drop\n", ["-open", file]);

            // validation
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(file));
        }

        /// <summary>
        /// Creates a persisted index whose ignored identity precedes the visible field.
        /// </summary>
        /// <returns>The schema file to pass to the executable.</returns>
        private string CreateIndex()
        {
            using var model = new ViewModel { CurrentDirectory = _directory };
            model.CreateIndexFile(new ObjectType { Name = "Products", Fields = [
                new Field { Name = "Id", Type = FieldType.Guid, Ignore = true },
                new Field { Name = "Name", Type = FieldType.Text }] });
            model.Insert("hello");
            return model.CurrentIndexFile;
        }

        /// <summary>
        /// Runs the shipped executable with a bounded lifetime and captures both output streams.
        /// </summary>
        /// <param name="input">The commands sent to standard input.</param>
        /// <param name="arguments">The startup arguments.</param>
        /// <returns>The exit code and captured output streams.</returns>
        private async Task<(int ExitCode, string Output, string Error)> Run(string input, string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = _directory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.Environment["WI_TEST_DIRECTORY"] = _directory;
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "wi.dll"));
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            using var process = Process.Start(start);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                return (process.ExitCode, await output, await error);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync();
                }
            }
        }

        /// <summary>
        /// Removes the unique test directory after all child processes have exited.
        /// </summary>
        public void Dispose()
        {
            Directory.Delete(_directory, true);
        }
    }
}
