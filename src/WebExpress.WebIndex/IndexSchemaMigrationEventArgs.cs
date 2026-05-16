using System;
using System.Threading.Tasks;

namespace WebExpress.WebIndex
{
    /// <summary>
    /// Event arguments used when a schema migration is needed.
    /// </summary>
    public class IndexSchemaMigrationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of schema that has changed.
        /// </summary>
        public Type SchemaType { get; internal set; }

        /// <summary>
        /// Delegate for the migration function.
        /// </summary>
        public Func<bool> PerformMigration { get; internal set; }

        /// <summary>
        /// Asynchon delegate for the migration function.
        /// </summary>
        public Func<Task<bool>> PerformMigrationAsync { get; internal set; }
    }
}
