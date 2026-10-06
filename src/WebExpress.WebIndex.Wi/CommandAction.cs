namespace WebExpress.WebIndex.Wi
{
    /// <summary>
    /// Enum for the different types of commands.
    /// </summary>
    internal enum CommandAction
    {
        /// <summary>
        /// No command was specified.
        /// </summary>
        None,

        /// <summary>
        /// Lists all available data.
        /// </summary>
        All,

        /// <summary>
        /// A empty command.
        /// </summary>
        Empty,

        /// <summary>
        /// Lists the index files from the currently open directory.
        /// </summary>
        ShowIndexFile,

        /// <summary>
        /// Lists the index fields from the current open index file.
        /// </summary>
        ShowIndexField,

        /// <summary>
        /// Lists the terms from the current index field.
        /// </summary>
        ShowIndexTerm,

        /// <summary>
        /// Opens an index file for access.
        /// </summary>
        OpenIndexFile,

        /// <summary>
        /// Opens an index field for access.
        /// </summary>
        OpenIndexField,

        /// <summary>
        /// Stops accessing an index file.
        /// </summary>
        CloseIndexFile,

        /// <summary>
        /// Stops accessing an index field.
        /// </summary>
        CloseIndexField,

        /// <summary>
        /// Creates an index file.
        /// </summary>
        CreateIndexFile,

        /// <summary>
        /// Removes an index file.
        /// </summary>
        DropIndexFile,

        /// <summary>
        /// Export to a file.
        /// </summary>
        Export,

        /// <summary>
        /// Import from a file.
        /// </summary>
        Import,

        /// <summary>
        /// Inserting an item into the index.
        /// </summary>
        Insert,

        /// <summary>
        /// Update an item of the index.
        /// </summary>
        Update,

        /// <summary>
        /// Returns the number of items of the index.
        /// </summary>
        Count,

        /// <summary>
        /// Delete an item from the index.
        /// </summary>
        Delete,

        /// <summary>
        /// Displays info about the application.
        /// </summary>
        Info,

        /// <summary>
        /// Displays help with the currently available commands.
        /// </summary>
        Help,

        /// <summary>
        /// Exits the program.
        /// </summary>
        Exit,

        /// <summary>
        /// A wql command.
        /// </summary>
        WQL
    }

    /// <summary>
    /// Provides extension methods for the CommandAction enum.
    /// </summary>
    internal static class CommandActionExtention
    {
        /// <summary>
        /// Converts the action value to a string.
        /// </summary>
        /// <param name="action">The CommandAction value.</param>
        /// <returns>A string representation of the CommandAction value.</returns>
        public static string GetDescription(this CommandAction action)
        {
            return action switch
            {
                CommandAction.All => "Lists all available data.",
                CommandAction.ShowIndexFile => "Lists the index files from the currently open directory.",
                CommandAction.ShowIndexField => "Lists the index fields from the current open index file.",
                CommandAction.ShowIndexTerm => "Lists the terms from the current index field.",
                CommandAction.OpenIndexFile => "Opens an index file for access.",
                CommandAction.OpenIndexField => "Opens an attribute for access.",
                CommandAction.CloseIndexFile => "Stops accessing an index file.",
                CommandAction.CloseIndexField => "Stops accessing an index attribute.",
                CommandAction.CreateIndexFile => "Creates an index file.",
                CommandAction.DropIndexFile => "Removes an index file.",
                CommandAction.Export => "Export to a file.",
                CommandAction.Import => "Import from a file.",
                CommandAction.Insert => "Inserting an item into the index.",
                CommandAction.Update => "Update an item of the index.",
                CommandAction.Count => "Counts the number of items of the index.",
                CommandAction.Delete => "Delete an item from the index.",
                CommandAction.Info => "Displays info about the application.",
                CommandAction.Help => "Displays help with the currently available commands.",
                CommandAction.Exit => "Quits the program.",
                _ => ""
            };
        }

        /// <summary>
        /// Converts the action value to a parameter description string.
        /// </summary>
        /// <param name="action">The CommandAction value.</param>
        /// <returns>A string representation of the CommandAction value.</returns>
        public static string GetParameterDescription(this CommandAction action)
        {
            return action switch
            {
                CommandAction.OpenIndexFile => "'index file'",
                CommandAction.OpenIndexField => "'attribute'",
                CommandAction.CreateIndexFile => "'name'",
                CommandAction.Export => "'file'",
                CommandAction.Import => "'file' [--replace]",
                CommandAction.Insert => "'field1, field2, ...'",
                CommandAction.Update => "'id' 'field1, field2, ...'",
                CommandAction.Delete => "'id'",
                _ => ""
            };
        }

        /// <summary>
        /// Determines the number of parameters for the action value.
        /// </summary>
        /// <param name="action">The CommandAction value.</param>
        /// <returns>A string representation of the CommandAction value.</returns>
        public static uint GetParameterCount(this CommandAction action)
        {
            return action switch
            {
                CommandAction.OpenIndexFile => 1,
                CommandAction.OpenIndexField => 1,
                CommandAction.CreateIndexFile => 1,
                CommandAction.Export => 1,
                CommandAction.Import => 1,
                CommandAction.Insert => 1,
                CommandAction.Update => 2,
                CommandAction.Delete => 1,
                _ => 0
            };
        }
    }
}
