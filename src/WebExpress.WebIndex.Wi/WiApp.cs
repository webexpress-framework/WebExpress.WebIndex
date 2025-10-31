using WebExpress.WebIndex.Wi;
using WebExpress.WebIndex.Wi.Model;
using WebExpress.WebIndex.Wql;

/// <summary>
/// Software for managing access to web index files.
/// </summary>
internal class WiApp
{
    /// <summary>
    /// Returns the view model.
    /// </summary>
    public static ViewModel ViewModel { get; } = new ViewModel();

    /// <summary>
    /// Returns or set the current state.
    /// </summary>
    private ProgrammState State { get; set; } = ProgrammState.Initial;

    /// <summary>
    /// Returns the console width.
    /// </summary>
    private int ConsoleWidth { get; set; } = 150;

    /// <summary>
    /// Entry point of wi application.
    /// </summary>
    /// <param name="args">Call arguments.</param>
    /// <returns>The return code. 0 on success. A number greater than 0 for errors.</returns>
    private static int Main(string[] args)
    {
        var app = new WiApp();
        app.Initialization(args);

        return app.Execution(args);
    }

    /// <summary>
    /// Running the application.
    /// </summary>
    /// <param name="args">Call arguments.</param>
    /// <returns>The return code. 0 on success. A number greater than 0 for errors.</returns>
    public int Execution(string[] args)
    {
        // prepare call arguments
        ArgumentParser.Current.Register(new ArgumentParserCommand() { FullName = "open", ShortName = "o", ParameterDescription = "index file", Description = "The path to the index file." });
        ArgumentParser.Current.Register(new ArgumentParserCommand() { FullName = "help", ShortName = "h", Description = "Display of the quick help with the most important commands." });

        // parsing call arguments
        var argumentDict = ArgumentParser.Current.Parse(args);

        if (argumentDict.ContainsKey("help"))
        {
            Console.WriteLine($"{ViewModel.Name}  [{ArgumentParser.Current.ToString()}]");
            Console.WriteLine(Environment.NewLine + ArgumentParser.Current.GetHelp() + Environment.NewLine);
            Console.WriteLine("Version: " + ViewModel.Version);

            return 0;
        }

        if (argumentDict.ContainsKey("open"))
        {
            ViewModel.CurrentDirectory = Directory.Exists(argumentDict["open"]) ? argumentDict["open"] : Path.GetDirectoryName(argumentDict["open"]);
            ViewModel.CurrentIndexFile = File.Exists(argumentDict["open"]) ? argumentDict["open"] : null;

            if (!(File.Exists(ViewModel.CurrentIndexFile) || Directory.Exists(ViewModel.CurrentDirectory)))
            {
                PrintError($"File not found. {ViewModel.CurrentIndexFile ?? ViewModel.CurrentDirectory}");

                return 1;
            }
            else if (File.Exists(ViewModel.CurrentIndexFile))
            {
                OnOpenIndexFileCommand(new Command() { Action = CommandAction.OpenIndexFile, Parameter1 = ViewModel.CurrentIndexFile });
            }
        }

        Start();

        return 0;
    }

    /// <summary>
    /// Called when the application is to be terminated using Ctrl+C.
    /// </summary>
    /// <param name="sender">The trigger of the event.</param>
    /// <param name="e">The event argument.</param>
    private void OnCancel(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = false;

        Exit();
    }

    /// <summary>
    /// Initialization
    /// </summary>
    /// <param name="args">The valid arguments.</param>
    private void Initialization(string[] args)
    {
        Console.CancelKeyPress += OnCancel;
    }

    /// <summary>
    /// Start the programm.
    /// </summary>
    private void Start()
    {
        var cmd = GetCommand();

        while (true)
        {
            var parser = new CommandParser();
            parser.Register("?", new CommandType() { Action = CommandAction.Help, Secret = true });
            parser.Register("h", new CommandType() { Action = CommandAction.Help, Secret = true });
            parser.Register("help", new CommandType() { Action = CommandAction.Help, Secret = false });
            parser.Register("!", new CommandType() { Action = CommandAction.Info, Secret = true });
            parser.Register("info", new CommandType() { Action = CommandAction.Info, Secret = false });
            parser.Register("q", new CommandType() { Action = CommandAction.Exit, Secret = true });
            parser.Register("exit", new CommandType() { Action = CommandAction.Exit, Secret = true });
            parser.Register("quit", new CommandType() { Action = CommandAction.Exit, Secret = false });
            parser.Register("bye", new CommandType() { Action = CommandAction.Exit, Secret = true });

            switch (State)
            {
                case ProgrammState.Initial:
                    {
                        parser.Register(".", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("s", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("l", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("ll", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("ls", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("dir", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("list", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = true });
                        parser.Register("show", new CommandType() { Action = CommandAction.ShowIndexFile, Secret = false });
                        parser.Register("o", new CommandType() { Action = CommandAction.OpenIndexFile, Secret = true });
                        parser.Register("open", new CommandType() { Action = CommandAction.OpenIndexFile, Secret = false });
                        parser.Register("c", new CommandType() { Action = CommandAction.CreateIndexFile, Secret = true });
                        parser.Register("create", new CommandType() { Action = CommandAction.CreateIndexFile, Secret = false });
                        parser.Register("import", new CommandType() { Action = CommandAction.Import, Secret = false });
                        break;
                    }
                case ProgrammState.OpenIndexFile:
                    {
                        parser.Register(".", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("s", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("l", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("ll", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("ls", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("dir", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("list", new CommandType() { Action = CommandAction.ShowIndexField, Secret = true });
                        parser.Register("show", new CommandType() { Action = CommandAction.ShowIndexField, Secret = false });
                        parser.Register("..", new CommandType() { Action = CommandAction.CloseIndexFile, Secret = true });
                        parser.Register("c", new CommandType() { Action = CommandAction.CloseIndexFile, Secret = true });
                        parser.Register("close", new CommandType() { Action = CommandAction.CloseIndexFile, Secret = false });
                        parser.Register("o", new CommandType() { Action = CommandAction.OpenIndexField, Secret = true });
                        parser.Register("open", new CommandType() { Action = CommandAction.OpenIndexField, Secret = false });
                        parser.Register("drop", new CommandType() { Action = CommandAction.DropIndexFile, Secret = false });
                        parser.Register("a", new CommandType() { Action = CommandAction.All, Secret = true });
                        parser.Register("all", new CommandType() { Action = CommandAction.All, Secret = false });
                        parser.Register("export", new CommandType() { Action = CommandAction.Export, Secret = false });
                        parser.Register("i", new CommandType() { Action = CommandAction.Insert, Secret = true });
                        parser.Register("insert", new CommandType() { Action = CommandAction.Insert, Secret = false });
                        parser.Register("u", new CommandType() { Action = CommandAction.Update, Secret = true });
                        parser.Register("update", new CommandType() { Action = CommandAction.Update, Secret = false });
                        parser.Register("d", new CommandType() { Action = CommandAction.Delete, Secret = true });
                        parser.Register("delete", new CommandType() { Action = CommandAction.Delete, Secret = false });
                        parser.Register("count", new CommandType() { Action = CommandAction.Count, Secret = false });
                        break;
                    }
                case ProgrammState.OpenIndexField:
                    {
                        parser.Register(".", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("s", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("l", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("ll", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("ls", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("dir", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("list", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("show", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("a", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = true });
                        parser.Register("all", new CommandType() { Action = CommandAction.ShowIndexTerm, Secret = false });
                        parser.Register("..", new CommandType() { Action = CommandAction.CloseIndexField, Secret = true });
                        parser.Register("c", new CommandType() { Action = CommandAction.CloseIndexField, Secret = true });
                        parser.Register("close", new CommandType() { Action = CommandAction.CloseIndexField, Secret = false });
                        break;
                    }
            }

            var command = parser.Parse(cmd);

            switch (command.Action)
            {
                case CommandAction.Empty:
                    {
                        break;
                    }
                case CommandAction.All:
                    {
                        OnAllCommand(command);
                        break;
                    }
                case CommandAction.ShowIndexFile:
                    {
                        OnShowIndexFileCommand(command);
                        break;
                    }
                case CommandAction.ShowIndexField:
                    {
                        OnShowIndexFieldCommand(command);
                        break;
                    }
                case CommandAction.ShowIndexTerm:
                    {
                        OnShowIndexTermCommand(command);
                        break;
                    }
                case CommandAction.OpenIndexFile:
                    {
                        OnOpenIndexFileCommand(command);
                        break;
                    }
                case CommandAction.OpenIndexField:
                    {
                        OnOpenIndexFieldCommand(command);
                        break;
                    }
                case CommandAction.CloseIndexFile:
                    {
                        OnCloseIndexFileCommand(command);
                        break;
                    }
                case CommandAction.CloseIndexField:
                    {
                        OnCloseIndexFieldCommand(command);
                        break;
                    }
                case CommandAction.CreateIndexFile:
                    {
                        OnCreateIndexFileCommand(command);
                        break;
                    }
                case CommandAction.DropIndexFile:
                    {
                        OnDropIndexFileCommand(command);
                        break;
                    }
                case CommandAction.Export:
                    {
                        OnExportCommand(command);
                        break;
                    }
                case CommandAction.Import:
                    {
                        OnImportCommand(command);
                        break;
                    }
                case CommandAction.Insert:
                    {
                        OnInsertCommand(command);
                        break;
                    }
                case CommandAction.Update:
                    {
                        OnUpdateCommand(command);
                        break;
                    }
                case CommandAction.Delete:
                    {
                        OnDeleteCommand(command);
                        break;
                    }
                case CommandAction.Count:
                    {
                        OnCountCommand(command);
                        break;
                    }
                case CommandAction.WQL:
                    {
                        OnWqlCommand(command);
                        break;
                    }
                case CommandAction.Help:
                    {
                        parser.PrintHelp();
                        break;
                    }
                case CommandAction.Info:
                    {
                        PrintInfo();
                        break;
                    }
                case CommandAction.Exit:
                    {
                        return;
                    }
                default:
                    {
                        if (int.TryParse(cmd, out int res))
                        {
                            switch (State)
                            {
                                case ProgrammState.Initial:
                                    {
                                        OnOpenIndexFileCommand(new Command() { Action = CommandAction.OpenIndexFile, Parameter1 = res });
                                    }
                                    break;
                                case ProgrammState.OpenIndexFile:
                                    {
                                        OnOpenIndexFieldCommand(new Command() { Action = CommandAction.OpenIndexField, Parameter1 = res });
                                    }
                                    break;
                                default:
                                    {
                                        PrintError($"Invalid command: {cmd}");
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            var runtimeClass = ViewModel.CurrentObjectType?.BuildRuntimeClass();
                            var statement = ViewModel.IndexManager?.Retrieve(runtimeClass, cmd);

                            if (statement != null && !statement.HasErrors)
                            {
                                OnWqlCommand(new Command() { Action = CommandAction.WQL, Parameter1 = statement });
                            }
                            else if (statement != null && statement.HasErrors)
                            {
                                PrintError($"Invalid command: {cmd} - {statement.Error}");
                            };
                        }
                    }
                    break;
            }

            cmd = GetCommand();
        }
    }

    /// <summary>
    /// Return the console command.
    /// </summary>
    /// <returns>The command.</returns>
    private string GetCommand()
    {
        var prefix = "wi";

        switch (State)
        {
            case ProgrammState.OpenIndexFile:
                {
                    prefix = Path.GetFileNameWithoutExtension(ViewModel.CurrentIndexFile);
                    break;
                }
            case ProgrammState.OpenIndexField:
                {
                    prefix = $"{Path.GetFileNameWithoutExtension(ViewModel.CurrentIndexFile)}/{ViewModel.CurrentIndexField?.Name}";
                    break;
                }
        }

        Console.Write($"{prefix}/>");
        var command = Console.ReadLine()?.ToLower().Trim();

        return command;
    }

    /// <summary>
    /// Execute the all command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnAllCommand(Command command)
    {
        var runtimeClass = ViewModel.CurrentObjectType.BuildRuntimeClass();
        var headers = runtimeClass.GetProperties().Select(x => x.Name);

        UpdateConsoleWidth();
        PrintTable(headers, ViewModel.CurrentObjectType.All.Select(x => runtimeClass.GetProperties().Select(y => y.GetValue(x)?.ToString())));
    }

    /// <summary>
    /// Execute the show index file command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnShowIndexFileCommand(Command command)
    {
        var i = 1;
        var headers = new List<string>(["Id", "Index"]);
        var files = Directory.GetFiles(ViewModel.CurrentDirectory, "*.ws", SearchOption.TopDirectoryOnly);

        Console.WriteLine($"The '{ViewModel.CurrentDirectory}' directory contains the following index files:{Environment.NewLine}");

        if (files.Length > 0)
        {
            UpdateConsoleWidth();

            PrintTableHeader(headers);

            foreach (var row in files.Select(x => new { Id = i++, Name = Path.GetFileNameWithoutExtension(x) }))
            {
                PrintTableRow(headers, [row.Id.ToString(), row.Name]);
            }

            PrintTableFooter(headers, i);

            return;
        }

        Console.WriteLine("No files available.");
    }

    /// <summary>
    /// Execute the show index field command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnShowIndexFieldCommand(Command command)
    {
        var i = 1;
        var headers = new List<string>(["Id", "Field", "Type"]);
        var fileds = ViewModel?.CurrentObjectType?.Fields.Where(x => !x.Ignore);

        Console.WriteLine($"The '{ViewModel?.CurrentObjectType?.Name}' contains the following fields:{Environment.NewLine}");

        if (fileds.Any())
        {
            UpdateConsoleWidth();

            PrintTableHeader(headers);

            foreach (var row in fileds.Select(x => new { Id = i++, x.Name, x.Type }))
            {
                PrintTableRow(headers, [row.Id.ToString(), row.Name, row.Type.ToString()]);
            }

            PrintTableFooter(headers, i);

            return;
        }

        if (!fileds.Any())
        {
            Console.WriteLine("No fields available.");
        }
    }

    /// <summary>
    /// Execute the show index term command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnShowIndexTermCommand(Command command)
    {
        var headers = new List<string>(["Term", "Fequency", "Posting tree height", "Posting balance factor", "DocumentIDs"]);
        var rows = ViewModel.GetIndexTerms();
        var i = 0;

        UpdateConsoleWidth();

        PrintTableHeader(headers);

        foreach (var row in rows)
        {
            PrintTableRow(headers, [row.Item1, row.Item2.ToString(), row.Item3.ToString(), row.Item4.ToString(), string.Join(",", row.Item5)]);
            i++;
        }

        PrintTableFooter(headers, i);
    }

    /// <summary>
    /// Execute the open index file command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnOpenIndexFileCommand(Command command)
    {
        var file = "";

        if (int.TryParse(command.Parameter1?.ToString(), out int i))
        {
            file = Directory.GetFiles(ViewModel.CurrentDirectory, "*.ws", SearchOption.TopDirectoryOnly).Skip(i - 1).FirstOrDefault();
        }
        else
        {
            file = Directory.GetFiles(ViewModel.CurrentDirectory, $"{command.Parameter1}.ws", SearchOption.TopDirectoryOnly).FirstOrDefault();
        }

        if (!File.Exists(file))
        {
            PrintError($"File '{ViewModel.CurrentIndexFile}' not found.");
            return;
        }

        if (!ViewModel.OpenIndexFile(file))
        {
            PrintError("An error occurred while opening the index file.");
            return;
        }

        State = ProgrammState.OpenIndexFile;
    }

    /// <summary>
    /// Execute the open index field command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnOpenIndexFieldCommand(Command command)
    {
        var field = default(Field);

        if (int.TryParse(command.Parameter1?.ToString(), out int i))
        {
            field = ViewModel?.CurrentObjectType?.Fields.Skip(i - 1).FirstOrDefault();
        }
        else
        {
            field = ViewModel?.CurrentObjectType?.Fields.Where(x => x.Name.Contains(command.ToString(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        if (field == null)
        {
            PrintError($"Field '{field}' not found.");
            return;
        }

        if (!ViewModel.OpenIndexField(field))
        {
            PrintError("An error occurred while opening the index field.");
            return;
        }

        State = ProgrammState.OpenIndexField;
    }

    /// <summary>
    /// Execute the close index file command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnCloseIndexFileCommand(Command command)
    {
        if (!ViewModel.CloseIndexFile())
        {
            PrintError("An error occurred while shooting the index file.");
            return;
        }

        State = ProgrammState.Initial;
    }

    /// <summary>
    /// Execute the close index field command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnCloseIndexFieldCommand(Command command)
    {
        State = ProgrammState.OpenIndexFile;
    }

    /// <summary>
    /// Execute the create index file command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnCreateIndexFileCommand(Command command)
    {
        var name = command.Parameter1?.ToString();

        if (string.IsNullOrWhiteSpace(name))
        {
            PrintError("Missing parameter name.");

            return;
        }

        if (File.Exists(Path.Combine(ViewModel.CurrentDirectory, $"{name}.ws")))
        {
            PrintError($"The index file '{name}' to be created already exists.");

            return;
        }

        if (!ViewModel.CreateIndexFile(name))
        {
            PrintError("An error occurred while creating the index file.");
        }

        State = ProgrammState.OpenIndexFile;
    }

    /// <summary>
    /// Execute the drop index file command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnDropIndexFileCommand(Command command)
    {
        if (Confirm($"Are you sure you want to delete the index file '{ViewModel?.CurrentObjectType.Name}'? The action cannot be rolled back."))
        {
            if (!ViewModel.DropIndexFile())
            {
                PrintError("An error occurred while droping the index file.");
                return;
            }

            State = ProgrammState.Initial;
        }
    }

    /// <summary>
    /// Execute the export command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnExportCommand(Command command)
    {
        PrintError("Sorry! Not implemented at the moment.");
    }

    /// <summary>
    /// Execute the import command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnImportCommand(Command command)
    {
        PrintError("Sorry! Not implemented at the moment.");
    }

    /// <summary>
    /// Execute the insert command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnInsertCommand(Command command)
    {
        PrintError("Sorry! Not implemented at the moment.");
    }

    /// <summary>
    /// Execute the update command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnUpdateCommand(Command command)
    {
        PrintError("Sorry! Not implemented at the moment.");
    }

    /// <summary>
    /// Execute the delete command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnDeleteCommand(Command command)
    {
        PrintError("Sorry! Not implemented at the moment.");
    }

    /// <summary>
    /// Execute the count command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnCountCommand(Command command)
    {
        Console.WriteLine(ViewModel.CurrentObjectType.Count);
    }

    /// <summary>
    /// Execute the wql command.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    private void OnWqlCommand(Command command)
    {
        switch (State)
        {
            case ProgrammState.OpenIndexFile:
                {
                    var runtimeClass = ViewModel.CurrentObjectType.BuildRuntimeClass();
                    var headers = runtimeClass.GetProperties().Select(x => x.Name);
                    var wql = command.Parameter1 as IWqlStatement;
                    var data = wql.Apply(runtimeClass);
                    var list = new List<IEnumerable<string>>();
                    var i = 0;

                    UpdateConsoleWidth();
                    PrintTableHeader(headers);

                    foreach (var item in data)
                    {
                        PrintTableRow(headers, runtimeClass.GetProperties().Select(y => y.GetValue(item)?.ToString()));
                        i++;
                    }

                    PrintTableFooter(headers, i);
                }
                break;
            default:
                PrintError("WQL is not allowed at this point.");
                break;
        }
    }

    /// <summary>
    /// Confirmation of an action
    /// </summary>
    /// <param name="message">The confirmation message.</param>
    private bool Confirm(string message)
    {
        var col = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{message} (n)");
        Console.ForegroundColor = col;
        var cmd = Console.ReadLine().ToLower().Trim();

        return cmd.Equals("y") || cmd.Equals("yes");
    }

    /// <summary>
    /// Display a error massage.
    /// </summary>
    /// <param name="error">The error massage.</param>
    private void PrintError(string error)
    {
        var col = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ForegroundColor = col;
    }

    /// <summary>
    ///Displays info about the application.
    /// </summary>
    private void PrintInfo()
    {
        Console.WriteLine("wi is an administrative console application for creating and searching WebIndex indexes.");
        Console.WriteLine($"Version: {ViewModel.Version}");
        Console.WriteLine("License: MIT");
        Console.WriteLine("Project: https://github.com/webexpress-framework/WebExpress.WebIndex");
    }

    /// <summary>
    /// Display a table.
    /// </summary>
    /// <param name="columns">The table columns.</param>
    /// <param name="rows">The table rows.</param>
    private void PrintTable(IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows)
    {
        PrintTableHeader(columns);

        // Print rows
        foreach (var row in rows)
        {
            PrintTableRow(columns, row);
        }

        // print bottom border
        PrintTableFooter(columns, rows.Count());
    }

    /// <summary>
    /// Display the table header.
    /// </summary>
    /// <param name="columns">The table columns.</param>
    private void PrintTableHeader(IEnumerable<string> columns)
    {
        var consoleWidth = ConsoleWidth;
        var columnCount = columns.Count();
        var columnWidth = (int)Math.Round((double)consoleWidth / columnCount);
        var header = $"| {string.Join(" | ", columns.Select(x => FormatCell(x, columnWidth - 3)))}|";
        var width = columnWidth * columnCount - 2;

        // print top border
        Console.WriteLine($"┌{new string('─', width)}┐");

        // print headers
        Console.WriteLine(header);

        // print separator
        Console.WriteLine($"├{new string('─', width)}┤");
    }

    /// <summary>
    /// Display a table row.
    /// </summary>
    /// <param name="columns">The table columns.</param>
    /// <param name="row">The table row.</param>
    private void PrintTableRow(IEnumerable<string> columns, IEnumerable<string> row)
    {
        var consoleWidth = ConsoleWidth;
        var columnCount = columns.Count();
        var columnWidth = (int)Math.Round((double)consoleWidth / columnCount);

        // Print rows
        Console.WriteLine($"| {string.Join(" | ", row.Select(x => FormatCell(x, columnWidth - 3)))}|");
    }

    /// <summary>
    /// Display the table footer.
    /// </summary>
    /// <param name="columns">The table columns.</param>
    /// <param name="count">The row count.</param>
    private void PrintTableFooter(IEnumerable<string> columns, int count)
    {
        var consoleWidth = ConsoleWidth;
        var columnCount = columns.Count();
        var columnWidth = (int)Math.Round((double)consoleWidth / columnCount);
        var width = columnWidth * columnCount - 2;

        // print separator
        Console.WriteLine($"├{new string('─', width)}┤");

        // print counter
        Console.WriteLine($"|{$" Rows count: {count}".PadRight(width)}|");

        // print bottom border
        Console.WriteLine($"└{new string('─', width)}┘");
    }

    /// <summary>
    /// Formats a cell for output to the console.
    /// </summary>
    /// <param name="cell">The contents of the cell.</param>
    /// <param name="width">The width of the cell.</param>
    /// <returns>The formatted cell.</returns>
    private static string FormatCell(string cell, int width)
    {
        if (cell.Length > width - 3)
        {
            return cell.Substring(0, width - 3) + "...";
        }
        else
        {
            return cell.PadRight(width);
        }
    }

    /// <summary>
    /// Updates the width of the console.
    /// </summary>
    private void UpdateConsoleWidth()
    {
        try
        {
            ConsoleWidth = Console.WindowWidth - 4;
        }
        catch
        {
        }
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    private void Exit()
    {
    }
}