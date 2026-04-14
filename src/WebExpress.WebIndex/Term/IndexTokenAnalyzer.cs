using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WebExpress.WebIndex.Term.Pipeline;

namespace WebExpress.WebIndex.Term
{
    /// <summary>
    /// Decomposes and processes an input string into a sequence of terms via a configurable pipeline.
    /// Handles resource extraction for language assets and disposes pipeline stages safely.
    /// </summary>
    public sealed class IndexTokenAnalyzer : IDisposable
    {
        private bool _disposed;
        private readonly List<IIndexPipeStage> _textProcessingPipeline = [];

        /// <summary>
        /// Gets the index context.
        /// </summary>
        public IIndexContext Context { get; private set; }

        /// <summary>
        /// Initializes a new instance of the analyzer.
        /// </summary>
        /// <param name="context">The index context holding configuration and paths.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public IndexTokenAnalyzer(IIndexContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (string.IsNullOrWhiteSpace(context.IndexDirectory))
            {
                throw new ArgumentException("Context.IndexDirectory must be a non-empty directory path.", nameof(context));
            }

            Context = context;

            Initialization();
        }

        /// <summary>
        /// Performs one-time initialization, materializing embedded resource files and 
        /// registering default pipeline stages.
        /// </summary>
        private void Initialization()
        {
            var assembly = typeof(IndexManager).Assembly;

            // resource files expected in embedded resources
            string[] fileNames =
            [
                "IrregularWords.en", "IrregularWords.de",
                "MisspelledWords.en", "MisspelledWords.de",
                "RegularWords.en", "RegularWords.de",
                "StopWords.en", "StopWords.de",
                "Synonyms.en", "Synonyms.de"
            ];

            // ensure index directory exists
            Directory.CreateDirectory(Context.IndexDirectory);

            // cache resource names once
            var resources = assembly.GetManifestResourceNames();

            foreach (var fileName in fileNames)
            {
                var path = Path.Combine(Context.IndexDirectory, fileName.ToLowerInvariant());
                var resource = resources
                    .FirstOrDefault(x => x.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase));

                if (resource is null)
                {
                    // resource not found, skip silently
                    continue;
                }

                try
                {
                    if (!File.Exists(path))
                    {
                        // open resource stream safely
                        using var contentStream = assembly.GetManifestResourceStream(resource);
                        if (contentStream is null)
                        {
                            // no content available, skip
                            continue;
                        }

                        using var sr = new StreamReader(contentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                        var content = sr.ReadToEnd();

                        // write UTF-8 without BOM to avoid encoding mismatches across platforms
                        using var sw = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                        sw.Write(content);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // cannot write into target directory, skip file to avoid blocking initialization
                }
                catch (IOException)
                {
                    // i/o error while materializing resource, skip file
                }
                catch (SystemException)
                {
                    // guard against other rare system exceptions, continue
                }
            }

            // register default pipeline stages (order matters)
            Register(new IndexPipeStageConverterTrim(Context));
            Register(new IndexPipeStageConverterLowerCase(Context));
            Register(new IndexPipeStageConverterMisspelled(Context));
            Register(new IndexPipeStageConverterNormalizer(Context));
            Register(new IndexPipeStageConverterSingular(Context));
            Register(new IndexPipeStageConverterSynonym(Context));
            Register(new IndexPipeStageFilterEmpty(Context));
            Register(new IndexPipeStageFilterSurrogateCharacter(Context));
            Register(new IndexPipeStageFilterStopWord(Context));
        }

        /// <summary>
        /// Registers a pipe stage for processing the tokens.
        /// </summary>
        /// <param name="pipeStage">The pipe stage to add.</param>
        public void Register(IIndexPipeStage pipeStage)
        {
            if (pipeStage is null)
            {
                return;
            }

            _textProcessingPipeline.Add(pipeStage);
        }

        /// <summary>
        /// Removes a pipe stage from the processing pipeline.
        /// </summary>
        /// <param name="pipeStage">The pipe stage to remove.</param>
        public void Remove(IIndexPipeStage pipeStage)
        {
            if (pipeStage is null)
            {
                return;
            }

            _textProcessingPipeline.Remove(pipeStage);
        }

        /// <summary>
        /// Analyzes the input and returns processed term tokens.
        /// </summary>
        /// <param name="input">The raw input text.</param>
        /// <param name="culture">The culture for tokenization and normalization.</param>
        /// <param name="retrieval">When true, wildcard placeholders are preserved for query parsing.</param>
        /// <returns>An enumeration of processed tokens.</returns>
        public IEnumerable<IndexTermToken> Analyze(string input, CultureInfo culture, bool retrieval = false)
        {
            var effectiveCulture = culture ?? CultureInfo.InvariantCulture;
            var wildcards = retrieval ? IndexTermTokenizer.Wildcards : null;

            var tokens = IndexTermTokenizer.Tokenize(input ?? string.Empty, effectiveCulture, wildcards);

            // pass tokens through all stages
            foreach (var pipeStage in _textProcessingPipeline)
            {
                tokens = pipeStage?.Process(tokens, effectiveCulture) ?? tokens;
            }

            return tokens ?? [];
        }

        /// <summary>
        /// Releases resources held by this analyzer, disposing pipe stages that 
        /// implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Core dispose routine with managed/unmanaged split.
        /// </summary>
        /// <param name="disposing">True when called from Dispose, false when finalizing.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose registered pipe stages if they are disposable
                foreach (var stage in _textProcessingPipeline)
                {
                    if (stage is IDisposable d)
                    {
                        try
                        {
                            d.Dispose();
                        }
                        catch
                        {
                            // swallow exceptions during dispose to avoid teardown failures
                        }
                    }
                }

                _textProcessingPipeline.Clear();
            }

            _disposed = true;
        }
    }
}