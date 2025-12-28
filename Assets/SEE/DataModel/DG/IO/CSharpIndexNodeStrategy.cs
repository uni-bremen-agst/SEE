using SEE.Utils;
using System;
using System.IO;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Tool-agnostic index strategy for C# reports.
    ///
    /// Accepts file paths, fully-qualified type names, nested type encodings,
    /// and member/method identifiers (e.g. XML-doc IDs "M:...") and normalizes
    /// them to the logical TYPE identifier used for node lookup.
    /// </summary>
    public sealed class CSharpIndexNodeStrategy : IIndexNodeStrategy
    {
        /// <summary>
        /// The separator used for namespaces in C#.
        /// </summary>
        private const char namespaceSeparator = '.';

        /// <summary>
        /// The separator used for nested types in reflection (plus).
        /// </summary>
        private const char nestedTypeSeparator = '+';

        /// <summary>
        /// An alternative separator used for nested types (dollar sign),
        /// to tolerate foreign encodings.
        /// </summary>
        private const char alternativeNestedTypeSeparator = '$';

        // Common member delimiters across tools

        /// <summary>
        /// The delimiter used for methods in some tool outputs (hash).
        /// </summary>
        private const char hashMethodDelimiter = '#';

        /// <summary>
        /// The delimiter used for methods in C++ style or some doc tools (double colon).
        /// </summary>
        private const string doubleColonDelimiter = "::";

        /// <summary>
        /// The character indicating the start of a parameter list.
        /// </summary>
        private const char paramsStartDelimiter = '(';

        /// <summary>
        /// The configuration used to parse paths (e.g., source root).
        /// </summary>
        private readonly ParsingConfig parsingConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpIndexNodeStrategy"/> class.
        /// </summary>
        /// <param name="config">The configuration options for parsing paths.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
        public CSharpIndexNodeStrategy(ParsingConfig config)
        {
            parsingConfig = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Normalizes an arbitrary report identifier (path/type/member) to the TYPE identifier.
        /// </summary>
        /// <param name="rawIdentifier">The raw identifier string from the report.</param>
        /// <returns>The normalized logical identifier.</returns>
        public string ToLogicalIdentifier(string rawIdentifier)
        {
            return NormalizeToTypeIdentifier(rawIdentifier);
        }

        /// <summary>
        /// Determines the logical identifier for a graph node.
        /// Methods map to their parent type.
        /// </summary>
        /// <param name="node">The graph node to analyze.</param>
        /// <returns>The logical identifier for the node, or null if it cannot be determined.</returns>
        public string ToLogicalIdentifier(Node node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Type == NodeTypes.Package || node.Type == NodeTypes.Namespace)
            {
                return Filenames.ReplaceDirectorySeparators(node.ID, namespaceSeparator);
            }

            if (node.Type == NodeTypes.Method)
            {
                return node.Parent != null ? ToLogicalIdentifier(node.Parent) : null;
            }

            if (NodeTypeExtensions.IsTypeNode(node.Type))
            {
                // Graph IDs may include prefixes, assemblies, generics arity etc.
                // Normalize them through the same pipeline (but they are usually already "type-like").
                return NormalizeToTypeIdentifier(node.ID);
            }

            if (node.Type == NodeTypes.File)
            {
                return NormalizeToTypeIdentifier(node.ID);
            }

            return null;
        }

        /// <summary>
        /// Full identifier normalization.
        /// For lookup we normalize to TYPE identifiers; thus this equals ToLogicalIdentifier(raw).
        /// (If you ever need a "member-full" identifier, split this method into two variants.)
        /// </summary>
        /// <param name="rawIdentifier">The raw identifier string.</param>
        /// <returns>The fully normalized identifier.</returns>
        public string ToFullIdentifier(string rawIdentifier)
        {
            return NormalizeToTypeIdentifier(rawIdentifier);
        }

        // ----------------------------
        // Normalization pipeline
        // ----------------------------

        /// <summary>
        /// Normalizes the raw input string into a standard C# type identifier.
        /// This handles file paths, XML documentation IDs, and various separator styles.
        /// </summary>
        /// <param name="raw">The raw input string to normalize.</param>
        /// <returns>The normalized type identifier, or null if the input is empty.</returns>
        private string NormalizeToTypeIdentifier(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string identifier = raw.Trim();

            // 1) If it looks like a file path, make it source-root-relative and remove extension.
            //    Heuristic: contains directory separators OR ends with ".cs" (case-insensitive).
            bool looksLikePath =
                identifier.Contains(Filenames.WindowsDirectorySeparator) ||
                identifier.Contains(Filenames.UnixDirectorySeparator) ||
                identifier.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

            if (looksLikePath)
            {
                identifier = parsingConfig.SourceRootRelativePath(identifier);
                identifier = Path.ChangeExtension(identifier, null); // remove .cs
                identifier = Filenames.ReplaceDirectorySeparators(identifier, namespaceSeparator);
                identifier = identifier.Trim(namespaceSeparator);
            }

            // 2) Strip assembly qualifier (common in some graph encodings): "Namespace.Type@Assembly"
            int atIndex = identifier.IndexOf('@');
            if (atIndex >= 0)
            {
                identifier = identifier.Substring(0, atIndex);
            }

            // 3) Strip XML-doc style prefixes: "M:", "T:", "P:", "F:", "E:", "N:" ...
            //    Also strip any similar two-char prefix "X:" used by some tools.
            if (identifier.Length > 2 && identifier[1] == ':')
            {
                identifier = identifier.Substring(2);
            }

            // 4) Normalize directory separators anyway (some tools mix them into IDs)
            identifier = Filenames.ReplaceDirectorySeparators(identifier, namespaceSeparator);

            // 5) Normalize nested type separators to '.'
            identifier = identifier.Replace(nestedTypeSeparator, namespaceSeparator)
                 .Replace(alternativeNestedTypeSeparator, namespaceSeparator);

            // 6) Remove generic arity markers from .NET type names: "`1", "`2", ...
            //    Keep only the part before the first backtick.
            int tickIndex = identifier.IndexOf('`');
            if (tickIndex >= 0)
            {
                identifier = identifier.Substring(0, tickIndex);
            }

            // 7) Remove apostrophe-based suffixes seen in some encodings
            int apostropheIndex = identifier.IndexOf('\'');
            if (apostropheIndex >= 0)
            {
                identifier = identifier.Substring(0, apostropheIndex);
            }

            // 8) Cut obvious method/member delimiters.
            //    a) "#method"
            int hashIdx = identifier.IndexOf(hashMethodDelimiter);
            if (hashIdx >= 0)
            {
                identifier = identifier.Substring(0, hashIdx);
            }

            //    b) "::Method"
            int dcolonIdx = identifier.IndexOf(doubleColonDelimiter, StringComparison.Ordinal);
            if (dcolonIdx >= 0)
            {
                identifier = identifier.Substring(0, dcolonIdx);
            }

            //    c) Parameter list "Type.Method(...)" → cut at '(' then cut trailing ".Method"
            int parenIdx = identifier.IndexOf(paramsStartDelimiter);
            if (parenIdx >= 0)
            {
                identifier = identifier.Substring(0, parenIdx);
            }

            // 9) If it still looks like a member reference "Namespace.Type.Member",
            //    try to map to the containing type by dropping the last segment.
            //    We do this only if there are at least 2 dots (so we don't destroy short namespaces),
            //    and only if the last segment starts with an uppercase/lowercase letter (common member naming).
            //    This is a heuristic; it helps for tools that don't use "#", "::", or "(...)".
            int lastDot = identifier.LastIndexOf(namespaceSeparator);
            if (lastDot > 0)
            {
                // If it’s an XML-doc member ID, it will often be "Namespace.Type.Member"
                // after stripping "M:"; in that case we want just "Namespace.Type".
                // But we must not break pure type names like "Foo.Bar.Baz" (Baz is type).
                // Heuristic: if there's another dot before last, and the token after last dot looks like a member (starts lowercase),
                // drop it.
                int prevDot = identifier.LastIndexOf(namespaceSeparator, lastDot - 1);
                if (prevDot > 0)
                {
                    string tail = identifier.Substring(lastDot + 1);
                    if (!string.IsNullOrEmpty(tail) && char.IsLower(tail[0]))
                    {
                        identifier = identifier.Substring(0, lastDot);
                    }
                }
            }

            return string.IsNullOrWhiteSpace(identifier) ? null : identifier;
        }
    }
}