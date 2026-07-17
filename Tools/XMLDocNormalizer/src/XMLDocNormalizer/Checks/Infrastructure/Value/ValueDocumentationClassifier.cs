using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Classifies value-documentation targets and policy decisions.
    /// </summary>
    internal static class ValueDocumentationClassifier
    {
        /// <summary>
        /// Determines whether missing value documentation should be reported for the current context.
        /// </summary>
        /// <param name="context">
        /// The prepared value-analysis context.
        /// </param>
        /// <param name="valueDocumentationMode">
        /// The configured value-documentation mode.
        /// </param>
        /// <returns>
        /// True if a missing value finding should be reported; otherwise false.
        /// </returns>
        public static bool ShouldReportMissingValue(
            ValueAnalysisContext context,
            ValueDocumentationMode valueDocumentationMode)
        {
            ArgumentNullException.ThrowIfNull(context);

            return valueDocumentationMode switch
            {
                ValueDocumentationMode.None => false,
                ValueDocumentationMode.AllReadableProperties => IsValueDocumentationTarget(context.TargetKind),
                ValueDocumentationMode.ExcludeDtoLikeTypes =>
                    IsValueDocumentationTarget(context.TargetKind)
                    && !IsDtoLikeContainer(context.Member),
                ValueDocumentationMode.IndexersOnly => context.TargetKind == ValueTargetKind.Indexer,
                _ => IsValueDocumentationTarget(context.TargetKind)
            };
        }

        /// <summary>
        /// Determines whether the value target can require value documentation.
        /// </summary>
        /// <param name="targetKind">
        /// The classified value target kind.
        /// </param>
        /// <returns>
        /// True if the target can require value documentation; otherwise false.
        /// </returns>
        public static bool IsValueDocumentationTarget(ValueTargetKind targetKind)
        {
            return targetKind == ValueTargetKind.ReadableProperty
                || targetKind == ValueTargetKind.Indexer;
        }

        /// <summary>
        /// Determines whether a member belongs to a DTO-like data container.
        /// </summary>
        /// <param name="member">
        /// The member to inspect.
        /// </param>
        /// <returns>
        /// True if the member belongs to a DTO-like data container; otherwise false.
        /// </returns>
        public static bool IsDtoLikeContainer(MemberDeclarationSyntax member)
        {
            ArgumentNullException.ThrowIfNull(member);

            BaseTypeDeclarationSyntax? containingType = member
                .Ancestors()
                .OfType<BaseTypeDeclarationSyntax>()
                .FirstOrDefault();

            if (containingType == null)
            {
                return false;
            }

            string typeName = containingType.Identifier.ValueText;

            if (HasDtoLikeTypeName(typeName))
            {
                return true;
            }

            string? namespaceName = containingType
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()?
                .Name
                .ToString();

            return HasDtoLikeNamespace(namespaceName);
        }

        /// <summary>
        /// Determines whether a type name represents a DTO-like data container.
        /// </summary>
        /// <param name="typeName">
        /// The type name to inspect.
        /// </param>
        /// <returns>
        /// True if the type name is DTO-like; otherwise false.
        /// </returns>
        public static bool HasDtoLikeTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            return typeName.EndsWith("Dto", StringComparison.Ordinal)
                || typeName.EndsWith("DTO", StringComparison.Ordinal)
                || typeName.EndsWith("Result", StringComparison.Ordinal)
                || typeName.EndsWith("Report", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a namespace represents DTO-like data containers.
        /// </summary>
        /// <param name="namespaceName">
        /// The namespace name to inspect.
        /// </param>
        /// <returns>
        /// True if the namespace is DTO-like; otherwise false.
        /// </returns>
        public static bool HasDtoLikeNamespace(string? namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            return namespaceName.EndsWith(".Dto", StringComparison.Ordinal)
                || namespaceName.Contains(".Dto.", StringComparison.Ordinal)
                || namespaceName.EndsWith(".DTO", StringComparison.Ordinal)
                || namespaceName.Contains(".DTO.", StringComparison.Ordinal);
        }
    }
}
