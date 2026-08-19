using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Resolves documented exception contracts for callable symbols whose
    /// executable source bodies are unavailable to the analyzer.
    /// </summary>
    /// <remarks>
    /// Documentation contracts provide positive evidence that an exception
    /// may be thrown. They are intentionally treated as partial contracts:
    /// successfully resolving documented exceptions does not make the
    /// external callable itself fully analyzable.
    /// </remarks>
    internal static class ExternalDocumentationExceptionModel
    {
        /// <summary>
        /// Caches exception-contract indexes loaded from external XML
        /// documentation files.
        /// </summary>
        private static readonly ConcurrentDictionary<
            string,
            IReadOnlyDictionary<string, string[]>>
            sidecarContractCache =
                new(StringComparer.Ordinal);

        /// <summary>
        /// Represents an available but empty external documentation index.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string[]>
            emptyContractIndex =
                new Dictionary<string, string[]>(
                    StringComparer.Ordinal);

        /// <summary>
        /// Gets exception types documented for an external method.
        /// </summary>
        /// <param name="methodSymbol">
        /// The external method whose documentation should be inspected.
        /// </param>
        /// <param name="compilation">
        /// The compilation used to resolve documentation identifiers and
        /// referenced metadata.
        /// </param>
        /// <returns>
        /// The documented exception types that could be resolved.
        /// </returns>
        public static IReadOnlyList<INamedTypeSymbol>
            GetDocumentedExceptionTypes(
                IMethodSymbol methodSymbol,
                Compilation compilation)
        {
            List<INamedTypeSymbol> exceptionTypes =
                new();

            if (methodSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                IMethodSymbol originalMethod =
                    methodSymbol.OriginalDefinition;

                string? declarationId =
                    DocumentationCommentId.CreateDeclarationId(
                        originalMethod);

                if (!string.IsNullOrWhiteSpace(
                        declarationId))
                {
                    string[] exceptionCrefs =
                        GetExceptionCrefs(
                            originalMethod,
                            compilation,
                            declarationId);

                    INamedTypeSymbol? exceptionBase =
                        compilation.GetTypeByMetadataName(
                            "System.Exception");

                    if (exceptionBase != null)
                    {
                        foreach (string exceptionCref
                                 in exceptionCrefs)
                        {
                            if (!exceptionCref.StartsWith(
                                    "T:",
                                    StringComparison.Ordinal))
                            {
                                continue;
                            }

                            ISymbol? resolvedSymbol =
                                DocumentationCommentId
                                    .GetFirstSymbolForDeclarationId(
                                        exceptionCref,
                                        compilation);

                            if (resolvedSymbol
                                    is not INamedTypeSymbol exceptionType ||
                                !IsExceptionType(
                                    exceptionType,
                                    exceptionBase))
                            {
                                continue;
                            }

                            exceptionTypes.Add(
                                exceptionType);
                        }
                    }
                }
            }

            return exceptionTypes;
        }

        /// <summary>
        /// Gets documented exception cref identifiers for one external method.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method whose documentation should be inspected.
        /// </param>
        /// <param name="compilation">
        /// The compilation containing the external metadata reference.
        /// </param>
        /// <param name="declarationId">
        /// The documentation declaration identifier of the method.
        /// </param>
        /// <returns>
        /// The distinct documented exception cref identifiers.
        /// </returns>
        private static string[] GetExceptionCrefs(
            IMethodSymbol methodSymbol,
            Compilation compilation,
            string declarationId)
        {
            string? documentationXml =
                methodSymbol.GetDocumentationCommentXml();

            if (!string.IsNullOrWhiteSpace(
                    documentationXml))
            {
                return ExtractExceptionCrefs(
                    documentationXml);
            }

            string? documentationPath =
                TryGetSidecarDocumentationPath(
                    methodSymbol,
                    compilation);

            if (string.IsNullOrWhiteSpace(
                    documentationPath))
            {
                return Array.Empty<string>();
            }

            IReadOnlyDictionary<string, string[]> contractIndex =
                sidecarContractCache.GetOrAdd(
                    documentationPath,
                    static path =>
                        LoadSidecarContractIndex(
                            path));

            return contractIndex.TryGetValue(
                declarationId,
                out string[]? exceptionCrefs)
                    ? exceptionCrefs
                    : Array.Empty<string>();
        }

        /// <summary>
        /// Extracts exception cref identifiers from one member documentation
        /// fragment.
        /// </summary>
        /// <param name="documentationXml">
        /// The XML documentation fragment.
        /// </param>
        /// <returns>
        /// The distinct exception cref identifiers contained in the fragment.
        /// </returns>
        private static string[] ExtractExceptionCrefs(
            string documentationXml)
        {
            try
            {
                using StringReader textReader =
                    new(documentationXml);

                using XmlReader xmlReader =
                    XmlReader.Create(
                        textReader,
                        CreateXmlReaderSettings());

                XDocument document =
                    XDocument.Load(
                        xmlReader,
                        LoadOptions.None);

                return ExtractExceptionCrefs(
                    document);
            }
            catch (XmlException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Loads all member exception contracts from an external XML
        /// documentation file.
        /// </summary>
        /// <param name="documentationPath">
        /// The XML documentation file path.
        /// </param>
        /// <returns>
        /// A member-id-to-exception-cref index, or an empty index when the
        /// optional documentation file cannot be consumed.
        /// </returns>
        private static IReadOnlyDictionary<string, string[]>
            LoadSidecarContractIndex(
                string documentationPath)
        {
            try
            {
                using FileStream stream =
                    File.OpenRead(
                        documentationPath);

                using XmlReader xmlReader =
                    XmlReader.Create(
                        stream,
                        CreateXmlReaderSettings());

                XDocument document =
                    XDocument.Load(
                        xmlReader,
                        LoadOptions.None);

                Dictionary<string, string[]> contracts =
                    new(StringComparer.Ordinal);

                IEnumerable<XElement> memberElements =
                    document
                        .Descendants()
                        .Where(
                            static element =>
                                element.Name.LocalName ==
                                "member");

                foreach (XElement memberElement
                         in memberElements)
                {
                    XAttribute? nameAttribute =
                        memberElement.Attribute(
                            "name");

                    if (nameAttribute == null ||
                        string.IsNullOrWhiteSpace(
                            nameAttribute.Value))
                    {
                        continue;
                    }

                    string[] exceptionCrefs =
                        ExtractExceptionCrefs(
                            memberElement);

                    if (exceptionCrefs.Length > 0)
                    {
                        contracts[
                            nameAttribute.Value] =
                                exceptionCrefs;
                    }
                }

                return contracts;
            }
            catch (IOException)
            {
                return emptyContractIndex;
            }
            catch (UnauthorizedAccessException)
            {
                return emptyContractIndex;
            }
            catch (XmlException)
            {
                return emptyContractIndex;
            }
        }

        /// <summary>
        /// Extracts exception cref identifiers from an XML documentation
        /// container.
        /// </summary>
        /// <param name="container">
        /// The XML container whose exception elements should be inspected.
        /// </param>
        /// <returns>
        /// The distinct non-empty exception cref identifiers.
        /// </returns>
        private static string[] ExtractExceptionCrefs(
            XContainer container)
        {
            return container
                .Descendants()
                .Where(
                    static element =>
                        element.Name.LocalName ==
                        "exception")
                .Select(
                    static element =>
                        element.Attribute(
                            "cref")?.Value)
                .Where(
                    static cref =>
                        !string.IsNullOrWhiteSpace(
                            cref))
                .Select(
                    static cref =>
                        cref!)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(
                    static cref =>
                        cref,
                    StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Attempts to locate the XML documentation file associated with the
        /// metadata assembly containing one external method.
        /// </summary>
        /// <param name="methodSymbol">
        /// The external method.
        /// </param>
        /// <param name="compilation">
        /// The compilation whose metadata references should be inspected.
        /// </param>
        /// <returns>
        /// The sidecar XML path when available; otherwise
        /// <see langword="null"/>.
        /// </returns>
        private static string? TryGetSidecarDocumentationPath(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            foreach (MetadataReference reference
                     in compilation.References)
            {
                if (reference
                        is not PortableExecutableReference
                            portableReference ||
                    string.IsNullOrWhiteSpace(
                        portableReference.FilePath))
                {
                    continue;
                }

                ISymbol? referencedSymbol =
                    compilation.GetAssemblyOrModuleSymbol(
                        reference);

                IAssemblySymbol? referencedAssembly =
                    referencedSymbol switch
                    {
                        IAssemblySymbol assembly =>
                            assembly,

                        IModuleSymbol module =>
                            module.ContainingAssembly,

                        _ =>
                            null
                    };

                if (referencedAssembly == null ||
                    !SymbolEqualityComparer.Default.Equals(
                        referencedAssembly,
                        methodSymbol.ContainingAssembly))
                {
                    continue;
                }

                string? documentationPath =
                    Path.ChangeExtension(
                        portableReference.FilePath,
                        ".xml");

                return
                    !string.IsNullOrWhiteSpace(
                        documentationPath) &&
                    File.Exists(
                        documentationPath)
                        ? documentationPath
                        : null;
            }

            return null;
        }

        /// <summary>
        /// Determines whether a named type is
        /// <see cref="Exception"/> or derives from it.
        /// </summary>
        /// <param name="candidate">
        /// The type to inspect.
        /// </param>
        /// <param name="exceptionBase">
        /// The resolved <see cref="Exception"/> base type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the candidate is an exception type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionType(
            INamedTypeSymbol candidate,
            INamedTypeSymbol exceptionBase)
        {
            INamedTypeSymbol? current =
                candidate;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        current.OriginalDefinition,
                        exceptionBase.OriginalDefinition))
                {
                    return true;
                }

                current =
                    current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Creates XML-reader settings suitable for untrusted external
        /// documentation files.
        /// </summary>
        /// <returns>The configured XML-reader settings.</returns>
        private static XmlReaderSettings CreateXmlReaderSettings()
        {
            return new XmlReaderSettings
            {
                DtdProcessing =
                    DtdProcessing.Prohibit,

                XmlResolver =
                    null
            };
        }
    }
}
