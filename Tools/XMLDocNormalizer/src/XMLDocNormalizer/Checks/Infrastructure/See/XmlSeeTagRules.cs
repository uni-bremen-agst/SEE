using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.See
{
    /// <summary>
    /// Provides reusable syntax rules for <c>see</c> and <c>seealso</c> tags.
    /// </summary>
    internal static class XmlSeeTagRules
    {
        /// <summary>
        /// The set of allowed attributes for <c>see</c>.
        /// </summary>
        private static readonly HashSet<string> AllowedSeeAttributes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "cref",
                "href",
                "langword",
                "title"
            };

        /// <summary>
        /// The set of allowed attributes for <c>seealso</c>.
        /// </summary>
        private static readonly HashSet<string> AllowedSeeAlsoAttributes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "cref",
                "href",
                "title",
                "langword"
            };

        /// <summary>
        /// Determines whether the specified <c>see</c> tag has any valid target attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains <c>cref</c>, <c>href</c>, or <c>langword</c>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasValidSeeTarget(XmlNodeSyntax element)
        {
            return CountTargets(element, includeLangword: true) >= 1;
        }

        /// <summary>
        /// Determines whether the specified <c>seealso</c> tag has any valid target attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains <c>cref</c> or <c>href</c>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasValidSeeAlsoTarget(XmlNodeSyntax element)
        {
            return CountTargets(element, includeLangword: false) >= 1;
        }

        /// <summary>
        /// Determines whether the specified <c>see</c> tag combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if more than one target attribute is present; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasInvalidSeeTargetCombination(XmlNodeSyntax element)
        {
            return CountTargets(element, includeLangword: true) > 1;
        }

        /// <summary>
        /// Determines whether the specified <c>seealso</c> tag combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if more than one target attribute is present; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasInvalidSeeAlsoTargetCombination(XmlNodeSyntax element)
        {
            return CountTargets(element, includeLangword: false) > 1;
        }

        /// <summary>
        /// Determines whether the specified <c>see</c> tag contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if an invalid attribute is present; otherwise <see langword="false"/>.</returns>
        internal static bool HasInvalidSeeAttribute(XmlNodeSyntax element)
        {
            return HasInvalidAttribute(element, AllowedSeeAttributes);
        }

        /// <summary>
        /// Determines whether the specified <c>seealso</c> tag contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if an invalid attribute is present; otherwise <see langword="false"/>.</returns>
        internal static bool HasInvalidSeeAlsoAttribute(XmlNodeSyntax element)
        {
            return HasInvalidAttribute(element, AllowedSeeAlsoAttributes);
        }

        /// <summary>
        /// Determines whether the specified <c>seealso</c> tag uses the forbidden <c>langword</c> attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if <c>langword</c> is present; otherwise <see langword="false"/>.</returns>
        internal static bool HasSeeAlsoLangwordAttribute(XmlNodeSyntax element)
        {
            return HasAttributeNamed(element, "langword");
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on a <c>see</c> tag is invalid.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if the href value is invalid; otherwise <see langword="false"/>.</returns>
        internal static bool HasInvalidSeeHref(XmlNodeSyntax element)
        {
            string? value = GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on a <c>seealso</c> tag is invalid.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if the href value is invalid; otherwise <see langword="false"/>.</returns>
        internal static bool HasInvalidSeeAlsoHref(XmlNodeSyntax element)
        {
            string? value = GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>langword</c> attribute on a <c>see</c> tag is invalid.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns><see langword="true"/> if the langword value is invalid; otherwise <see langword="false"/>.</returns>
        internal static bool HasInvalidSeeLangword(XmlNodeSyntax element)
        {
            string? value = GetAttributeValue(element, "langword");

            if (value == null)
            {
                return false;
            }

            return !IsValidLangword(value);
        }

        /// <summary>
        /// Gets the normalized duplicate-comparison key of a <c>seealso</c> tag.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>A normalized target key if present; otherwise <see langword="null"/>.</returns>
        internal static string? GetSeeAlsoTargetKey(XmlNodeSyntax element)
        {
            string? cref = GetAttributeValue(element, "cref");

            if (!string.IsNullOrWhiteSpace(cref))
            {
                return "cref:" + cref;
            }

            string? href = GetAttributeValue(element, "href");

            if (!string.IsNullOrWhiteSpace(href))
            {
                return "href:" + href;
            }

            return null;
        }

        /// <summary>
        /// Validates an href value.
        /// </summary>
        /// <param name="value">The href value to validate.</param>
        /// <returns><see langword="true"/> if the value is a valid absolute URI; otherwise <see langword="false"/>.</returns>
        internal static bool IsValidHref(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Uri.TryCreate(value, UriKind.Absolute, out _);
        }

        /// <summary>
        /// Validates a langword value.
        /// </summary>
        /// <param name="value">The langword value to validate.</param>
        /// <returns><see langword="true"/> if the value is supported; otherwise <see langword="false"/>.</returns>
        internal static bool IsValidLangword(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value switch
            {
                "null" => true,
                "true" => true,
                "false" => true,
                "default" => true,
                "this" => true,
                _ => false
            };
        }

        /// <summary>
        /// Counts target attributes on a <c>see</c> or <c>seealso</c> tag.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="includeLangword">
        /// <see langword="true"/> to count <c>langword</c> as a target; otherwise <see langword="false"/>.
        /// </param>
        /// <returns>The number of present target attributes.</returns>
        private static int CountTargets(XmlNodeSyntax element, bool includeLangword)
        {
            int count = 0;

            if (HasAttributeNamed(element, "cref"))
            {
                count++;
            }

            if (HasAttributeNamed(element, "href"))
            {
                count++;
            }

            if (includeLangword && HasAttributeNamed(element, "langword"))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Determines whether the specified tag contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="allowedAttributes">The allowed local attribute names.</param>
        /// <returns><see langword="true"/> if an invalid attribute is present; otherwise <see langword="false"/>.</returns>
        private static bool HasInvalidAttribute(
            XmlNodeSyntax element,
            IReadOnlySet<string> allowedAttributes)
        {
            foreach (XmlAttributeSyntax attribute in GetAttributes(element))
            {
                string attributeName = attribute.Name.LocalName.Text;

                if (allowedAttributes.Contains(attributeName))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified XML node contains an attribute with the given local name.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns><see langword="true"/> if the attribute exists; otherwise <see langword="false"/>.</returns>
        private static bool HasAttributeNamed(XmlNodeSyntax element, string localName)
        {
            return GetAttributeValue(element, localName) != null;
        }

        /// <summary>
        /// Gets the value of an attribute from either an XML element or an empty XML element.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <param name="localName">The local name of the attribute.</param>
        /// <returns>The unquoted attribute value if present; otherwise <see langword="null"/>.</returns>
        private static string? GetAttributeValue(XmlNodeSyntax element, string localName)
        {
            if (element is XmlElementSyntax xmlElement)
            {
                return SyntaxUtils.GetAttributeValue(xmlElement, localName);
            }

            if (element is XmlEmptyElementSyntax emptyElement)
            {
                return SyntaxUtils.GetAttributeValue(emptyElement, localName);
            }

            return null;
        }

        /// <summary>
        /// Gets the attributes of either an XML element or an empty XML element.
        /// </summary>
        /// <param name="element">The XML node to inspect.</param>
        /// <returns>The XML attributes of the node.</returns>
        private static IEnumerable<XmlAttributeSyntax> GetAttributes(XmlNodeSyntax element)
        {
            if (element is XmlElementSyntax xmlElement)
            {
                return SyntaxUtils.GetAttributes(xmlElement);
            }

            if (element is XmlEmptyElementSyntax emptyElement)
            {
                return SyntaxUtils.GetAttributes(emptyElement);
            }

            return Enumerable.Empty<XmlAttributeSyntax>();
        }
    }
}
