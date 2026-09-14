using System.Collections.Immutable;
using System.Text.Json;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes validated module-level Source Link mappings without
    /// performing source acquisition.
    /// </summary>
    internal sealed class ExternalSourceLinkDescriptor
    {
        /// <summary>
        /// Initializes a Source Link descriptor.
        /// </summary>
        /// <param name="mappings">The mappings in original JSON order.</param>
        private ExternalSourceLinkDescriptor(
            ImmutableArray<ExternalSourceLinkMapping> mappings)
        {
            Mappings = mappings;
        }

        /// <summary>
        /// Gets the mappings in their original Source Link JSON order.
        /// </summary>
        /// <value>The validated mappings.</value>
        public ImmutableArray<ExternalSourceLinkMapping> Mappings { get; }

        /// <summary>
        /// Tries to parse and validate a Source Link JSON blob.
        /// </summary>
        /// <param name="sourceLinkJson">The UTF-8 Source Link JSON bytes.</param>
        /// <param name="descriptor">The validated descriptor when available.</param>
        /// <returns>
        /// <see langword="true"/> when the JSON contains one nonempty,
        /// unambiguous set of valid document mappings; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool TryCreate(
            ImmutableArray<byte> sourceLinkJson,
            out ExternalSourceLinkDescriptor descriptor)
        {
            if (sourceLinkJson.IsDefaultOrEmpty)
            {
                descriptor = null!;
                return false;
            }

            try
            {
                using JsonDocument jsonDocument = JsonDocument.Parse(
                    sourceLinkJson.ToArray());
                JsonElement root = jsonDocument.RootElement;

                if (root.ValueKind != JsonValueKind.Object
                    || !TryGetDocumentsObject(root, out JsonElement documents))
                {
                    descriptor = null!;
                    return false;
                }

                ImmutableArray<ExternalSourceLinkMapping>.Builder mappings =
                    ImmutableArray.CreateBuilder<ExternalSourceLinkMapping>();
                HashSet<string> patterns = new(StringComparer.OrdinalIgnoreCase);

                foreach (JsonProperty property in documents.EnumerateObject())
                {
                    string? documentPattern = property.Name;
                    string? target = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : null;

                    if (documentPattern == null
                        || target == null
                        || !patterns.Add(documentPattern))
                    {
                        descriptor = null!;
                        return false;
                    }

                    if (!IsValidMapping(documentPattern, target))
                    {
                        descriptor = null!;
                        return false;
                    }

                    mappings.Add(new ExternalSourceLinkMapping(documentPattern, target));
                }

                if (mappings.Count == 0)
                {
                    descriptor = null!;
                    return false;
                }

                descriptor = new ExternalSourceLinkDescriptor(mappings.ToImmutable());
                return true;
            }
            catch (JsonException)
            {
                descriptor = null!;
                return false;
            }
        }

        /// <summary>
        /// Tries to resolve a document name to its Source Link target without
        /// accessing the target.
        /// </summary>
        /// <param name="documentName">The exact document name from the PDB.</param>
        /// <param name="sourceLocator">
        /// The resolved source-locator string when a mapping matches.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an exact or wildcard mapping matches;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="documentName"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryResolveDocument(string documentName, out string sourceLocator)
        {
            ArgumentNullException.ThrowIfNull(documentName);

            ExternalSourceLinkMapping? bestWildcard = null;
            int bestPrefixLength = -1;

            foreach (ExternalSourceLinkMapping mapping in Mappings)
            {
                if (!mapping.DocumentPattern.EndsWith('*'))
                {
                    if (string.Equals(
                            documentName,
                            mapping.DocumentPattern,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        sourceLocator = mapping.Target;
                        return true;
                    }

                    continue;
                }

                int prefixLength = mapping.DocumentPattern.Length - 1;
                string prefix = mapping.DocumentPattern[..prefixLength];

                if (prefixLength > bestPrefixLength
                    && documentName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    bestWildcard = mapping;
                    bestPrefixLength = prefixLength;
                }
            }

            if (!bestWildcard.HasValue)
            {
                sourceLocator = null!;
                return false;
            }

            string suffix = documentName[bestPrefixLength..];
            sourceLocator = bestWildcard.Value.Target.Replace(
                "*",
                suffix,
                StringComparison.Ordinal);
            return true;
        }

        /// <summary>
        /// Locates the single required Source Link documents object.
        /// </summary>
        /// <param name="root">The JSON root object.</param>
        /// <param name="documents">The unique documents object.</param>
        /// <returns>
        /// <see langword="true"/> when exactly one object-valued documents
        /// property exists; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDocumentsObject(
            JsonElement root,
            out JsonElement documents)
        {
            documents = default;
            int count = 0;

            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!property.NameEquals("documents"))
                {
                    continue;
                }

                count++;
                documents = property.Value;
            }

            return count == 1 && documents.ValueKind == JsonValueKind.Object;
        }

        /// <summary>
        /// Validates Source Link wildcard placement and cardinality.
        /// </summary>
        /// <param name="documentPattern">The document pattern.</param>
        /// <param name="target">The source-locator target.</param>
        /// <returns>
        /// <see langword="true"/> when both sides satisfy Source Link wildcard
        /// rules; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsValidMapping(string documentPattern, string target)
        {
            int documentWildcardCount = documentPattern.Count(character => character == '*');
            int targetWildcardCount = target.Count(character => character == '*');

            if (documentWildcardCount == 0)
            {
                return targetWildcardCount == 0;
            }

            return documentWildcardCount == 1
                && documentPattern[^1] == '*'
                && targetWildcardCount == 1;
        }
    }
}
