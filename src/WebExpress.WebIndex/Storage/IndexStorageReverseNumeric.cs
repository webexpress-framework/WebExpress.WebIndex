using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WebExpress.WebIndex.Term;

namespace WebExpress.WebIndex.Storage
{
    /// <summary>
    /// Provides a reverse index for numeric values persisted on disk. Handles 
    /// culture-aware parsing and robust conversion from tokens to decimal values.
    /// </summary>
    /// <typeparam name="TIndexItem">The index item type. Must implement IIndexItem.</typeparam>
    public class IndexStorageReverseNumeric<TIndexItem> : IndexStorageReverse<TIndexItem>, IIndexStorage
        where TIndexItem : IIndexItem
    {
        // file format constants
        private const string _extension = "wrn";
        // increment this when the on-disk format changes
        private const int _version = 1;

        /// <summary>
        /// Returns the on-disk numeric tree.
        /// </summary>
        public IndexStorageSegmentNumeric Numeric { get; private set; }

        /// <summary>
        /// Returns all unique item ids present in the numeric index.
        /// </summary>
        public override IEnumerable<Guid> All
        {
            get
            {
                return Numeric?.All?.Distinct() ?? [];
            }
        }

        /// <summary>
        /// Initializes a new instance of the numeric reverse index.
        /// Ensures directory existence, uses a stable file name, and initializes storage segments.
        /// </summary>
        /// <param name="context">The document index context.</param>
        /// <param name="field">The field descriptor.</param>
        /// <param name="culture">The culture for parsing numeric values.</param>
        public IndexStorageReverseNumeric(IIndexDocumemntContext context, IndexFieldData field, CultureInfo culture)
            : base(context, field, culture)
        {
            // ensure directory exists
            Directory.CreateDirectory(Context.IndexDirectory);

            // sanitize field name for file system
            var safeField = SanitizeFileName(Field?.Name ?? "field");
            FileName = Path.Combine(Context.IndexDirectory, $"{typeof(TIndexItem).Name}.{safeField}.{_extension}");

            var exists = File.Exists(FileName);

            // reuse a single storage context for all segments
            IndexFile = new IndexStorageFile(FileName);
            var storageContext = new IndexStorageContext(this);

            Header = new IndexStorageSegmentHeader(storageContext)
            {
                Identifier = _extension,
                Version = _version
            };
            Allocator = new IndexStorageSegmentAllocatorReverseIndex(storageContext);
            Statistic = new IndexStorageSegmentStatistic(storageContext);
            Numeric = new IndexStorageSegmentNumeric(storageContext);

            Header.Initialization(exists);
            Statistic.Initialization(exists);
            Numeric.Initialization(exists);
            Allocator.Initialization(exists);

            IndexFile.Flush();
        }

        /// <summary>
        /// Adds an item by extracting numeric tokens from the configured field and indexing them.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public override void Add(TIndexItem item)
        {
            if (item == null)
            {
                return;
            }

            var value = Field.GetPropertyValue(item)?.ToString();
            var terms = Context.TokenAnalyzer.Analyze(value, Culture);

            Add(item, terms);
        }

        /// <summary>
        /// Adds numeric tokens to the index for the given item.
        /// Non-numeric tokens, NaN, and infinity values are ignored.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="terms">The terms to index for the item.</param>
        public override void Add(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            if (item == null || terms == null)
            {
                return;
            }

            foreach (var term in terms)
            {
                if (!TryToDecimal(term?.Value, Culture, out var value))
                {
                    continue;
                }

                Numeric.AddAndBalance(item.Id, value);

                Statistic.Count++;
                IndexFile.Write(Statistic);
            }
        }

        /// <summary>
        /// Deletes numeric postings for the given item by re-tokenizing the current field value.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public override void Delete(TIndexItem item)
        {
            if (item == null)
            {
                return;
            }

            var value = Field.GetPropertyValue(item);
            var terms = Context.TokenAnalyzer.Analyze(value?.ToString(), Culture);

            Delete(item, terms);
        }

        /// <summary>
        /// Deletes the postings for numeric tokens associated with the given item.
        /// Non-numeric tokens, NaN, and infinity values are ignored.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="terms">The terms to delete from the reverse index for the item.</param>
        public override void Delete(TIndexItem item, IEnumerable<IndexTermToken> terms)
        {
            if (item == null || terms == null)
            {
                return;
            }

            foreach (var term in terms)
            {
                if (!TryToDecimal(term?.Value, Culture, out var key))
                {
                    continue;
                }

                var node = Numeric[key];
                if (node != null)
                {
                    if (node.RemovePosting(item.Id))
                    {
                        if (Statistic.Count > 0)
                        {
                            Statistic.Count--;
                        }
                        IndexFile.Write(Statistic);
                    }
                }
            }
        }

        /// <summary>
        /// Clears the index contents and reinitializes storage segments.
        /// </summary>
        public override void Clear()
        {
            IndexFile.NextFreeAddr = 0;
            IndexFile.InvalidationAll();
            IndexFile.Flush();

            var storageContext = new IndexStorageContext(this);

            Header = new IndexStorageSegmentHeader(storageContext)
            {
                Identifier = _extension,
                Version = _version
            };
            Allocator = new IndexStorageSegmentAllocatorReverseIndex(storageContext);
            Statistic = new IndexStorageSegmentStatistic(storageContext);
            Numeric = new IndexStorageSegmentNumeric(storageContext);

            Header.Initialization(initializationFromFile: false);
            Statistic.Initialization(initializationFromFile: false);
            Numeric.Initialization(initializationFromFile: false);
            Allocator.Initialization(initializationFromFile: false);

            IndexFile.Flush();
        }

        /// <summary>
        /// Deletes the on-disk reverse index file.
        /// </summary>
        public override void Drop()
        {
            IndexFile.Delete();
        }

        /// <summary>
        /// Retrieves all matching item ids for a numeric query input according to the specified options.
        /// Accepts numeric inputs or strings parsed with the configured culture.
        /// </summary>
        /// <param name="input">The input value or string.</param>
        /// <param name="options">The retrieve options.</param>
        /// <returns>An enumerable of matching item ids.</returns>
        public override IEnumerable<Guid> Retrieve(object input, IndexRetrieveOptions options)
        {
            if (!TryToDecimal(input, Culture, out var value))
            {
                return [];
            }

            switch (options.Method)
            {
                case IndexRetrieveMethod.Phrase:
                    {
                        return Numeric.Retrieve(value, options);
                    }
                case IndexRetrieveMethod.GratherThan:
                    {
                        return Numeric.Retrieve(value, options);
                    }
                case IndexRetrieveMethod.GratherThanOrEqual:
                    {
                        return Numeric.Retrieve(value, options);
                    }
                case IndexRetrieveMethod.LessThan:
                    {
                        return Numeric.Retrieve(value, options);
                    }
                case IndexRetrieveMethod.LessThanOrEqual:
                    {
                        return Numeric.Retrieve(value, options);
                    }
                default:
                    {
                        return [];
                    }
            }
        }

        /// <summary>
        /// Attempts to convert an arbitrary token value to decimal using the provided culture.
        /// Double/float infinities or NaN are rejected; strings are parsed with culture fallback to invariant.
        /// </summary>
        /// <param name="value">The token value (string or numeric).</param>
        /// <param name="culture">The culture used for parsing strings.</param>
        /// <param name="result">The decimal result if conversion succeeds.</param>
        /// <returns>True if conversion succeeded; otherwise false.</returns>
        private static bool TryToDecimal(object value, CultureInfo culture, out decimal result)
        {
            result = default;

            if (value == null)
            {
                return false;
            }

            // fast-path numeric types
            if (value is decimal dec)
            {
                result = dec;
                return true;
            }

            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d))
                {
                    return false;
                }
                try
                {
                    // convert double to decimal (may throw on overflow)
                    result = Convert.ToDecimal(d);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (value is float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f))
                {
                    return false;
                }
                try
                {
                    result = Convert.ToDecimal(f);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (value is sbyte sb)
            {
                result = sb;
                return true;
            }

            if (value is byte b)
            {
                result = b;
                return true;
            }

            if (value is short s)
            {
                result = s;
                return true;
            }

            if (value is ushort us)
            {
                result = us;
                return true;
            }

            if (value is int i)
            {
                result = i;
                return true;
            }

            if (value is uint ui)
            {
                result = ui;
                return true;
            }

            if (value is long l)
            {
                result = l;
                return true;
            }

            if (value is ulong ul)
            {
                try
                {
                    result = Convert.ToDecimal(ul);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // parse string with culture, then invariant fallback
            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var ci = culture ?? CultureInfo.InvariantCulture;

            if (decimal.TryParse(text, NumberStyles.Any, ci, out var parsed))
            {
                result = parsed;
                return true;
            }

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                result = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Produces a file-system safe name by replacing invalid file name characters with '_'.
        /// </summary>
        /// <param name="name">The input file name.</param>
        /// <returns>A sanitized file name.</returns>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "_";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            return new string(chars);
        }
    }
}