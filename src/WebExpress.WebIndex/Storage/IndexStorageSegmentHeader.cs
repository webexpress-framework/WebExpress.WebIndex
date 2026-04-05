using System;
using System.IO;
using System.Text;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Represents the header segment of the index storage.
    /// </summary>
    public class IndexStorageSegmentHeader : IndexStorageSegment
    {
        /// <summary>
        /// Returns the required on-disk size of the header segment.
        /// </summary>
        public const uint SegmentSize = 3 + sizeof(byte);

        /// <summary>
        /// Returns or sets the file identifier (must be exactly 3 ASCII characters).
        /// </summary>
        public string Identifier { get; internal set; }

        /// <summary>
        /// Returns or sets the file version.
        /// </summary>
        public byte Version { get; internal set; }

        /// <summary>
        /// Initializes a new header segment instance and allocates its storage.
        /// </summary>
        /// <param name="context">The index storage context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the context does not provide a valid index file.</exception>
        public IndexStorageSegmentHeader(IndexStorageContext context)
            : base(context, (context ?? throw new ArgumentNullException(nameof(context))).IndexFile is not null
                ? context.IndexFile.Alloc(SegmentSize)
                : throw new InvalidOperationException("Index file is not available in the provided context."))
        {
        }

        /// <summary>
        /// Initializes the header either by reading from the file or by writing the 
        /// current header to the file.
        /// </summary>
        /// <param name="initializationFromFile">
        /// If true, reads header from file; otherwise writes the current header to file.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when identifier or version is not set for write operation.
        /// </exception>
        public virtual void Initialization(bool initializationFromFile)
        {
            if (initializationFromFile)
            {
                Context.IndexFile.Read(this);
            }
            else
            {
                // validate identifier before writing
                ValidateIdentifier(Identifier);
                Context.IndexFile.Write(this);
            }
        }

        /// <summary>
        /// Reads the header from the binary stream. Expects exactly 3 ASCII bytes for 
        /// identifier and 1 byte for version.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown when insufficient bytes are available.</exception>
        /// <exception cref="IOException">
        /// Thrown when the identifier or version does not match the expected values.
        /// </exception>
        public override void Read(BinaryReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var idBytes = reader.ReadBytes(3);
            if (idBytes.Length != 3)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading header identifier.");
            }

            var version = reader.ReadByte();

            // build identifier as ASCII
            var readIdentifier = Encoding.ASCII.GetString(idBytes);

            // if expected identifier is not provided yet, assign it (optional behavior)
            if (string.IsNullOrEmpty(Identifier))
            {
                Identifier = readIdentifier;
            }

            if (!string.Equals(Identifier, readIdentifier, StringComparison.Ordinal))
            {
                throw new IOException($"A file with the identifier '{Identifier}' is expected; '{readIdentifier}' was read.");
            }

            // if expected version is 0 (default), accept and set it; otherwise verify
            if (Version == 0)
            {
                Version = version;
            }
            else if (Version != version)
            {
                throw new IOException($"The expected file version is '{Version}', but version '{version}' was read.");
            }
        }

        /// <summary>
        /// Writes the header to the binary stream using a fixed layout.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <exception cref="ArgumentNullException">Thrown when writer is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when identifier is invalid.</exception>
        public override void Write(BinaryWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            ValidateIdentifier(Identifier);

            // write exactly 3 ASCII bytes
            var idBytes = Encoding.ASCII.GetBytes(Identifier);
            if (idBytes.Length != 3)
            {
                // ensure strict 3-byte identifier
                throw new InvalidOperationException("Identifier must encode to exactly 3 ASCII bytes.");
            }

            writer.Write(idBytes);
            writer.Write(Version);
        }

        /// <summary>
        /// Validates that identifier is exactly 3 ASCII characters.
        /// </summary>
        /// <param name="identifier">The identifier to validate.</param>
        /// <exception cref="InvalidOperationException">thrown when identifier is invalid.</exception>
        private static void ValidateIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new InvalidOperationException("Identifier must be provided and cannot be empty.");
            }

            if (identifier.Length != 3)
            {
                throw new InvalidOperationException("Identifier must be exactly 3 characters long.");
            }

            // ensure ascii
            for (int i = 0; i < identifier.Length; i++)
            {
                if (identifier[i] > 0x7F)
                {
                    throw new InvalidOperationException("Identifier must contain only ASCII characters.");
                }
            }
        }
    }
}