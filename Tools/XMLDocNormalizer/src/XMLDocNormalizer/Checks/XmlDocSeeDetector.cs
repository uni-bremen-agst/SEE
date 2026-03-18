using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects syntax-based smells for <c>see</c> and <c>seealso</c> XML documentation tags.
    /// </summary>
    /// <remarks>
    /// This initial implementation only reports:
    /// - DOC900: missing target on <c>see</c>
    /// - DOC901: missing target on <c>seealso</c>
    /// </remarks>
    internal static class XmlDocSeeDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for missing target attributes
        /// on <c>see</c> and <c>seealso</c> elements.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSeeSmells(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new List<Finding>();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<DocumentationCommentTriviaSyntax> comments =
                root.DescendantNodes(descendIntoTrivia: true)
                    .OfType<DocumentationCommentTriviaSyntax>();

            foreach (DocumentationCommentTriviaSyntax comment in comments)
            {
                AnalyzeDocumentationComment(tree, filePath, comment, findings);
            }

            return findings;
        }

        /// <summary>
        /// Analyzes a documentation comment for <c>see</c> and <c>seealso</c> elements.
        /// </summary>
        private static void AnalyzeDocumentationComment(
            SyntaxTree tree,
            string filePath,
            DocumentationCommentTriviaSyntax comment,
            List<Finding> findings)
        {
            foreach (XmlEmptyElementSyntax emptyElement in comment.DescendantNodes().OfType<XmlEmptyElementSyntax>())
            {
                AnalyzeEmptyElement(tree, filePath, emptyElement, findings);
            }

            foreach (XmlElementSyntax element in comment.DescendantNodes().OfType<XmlElementSyntax>())
            {
                AnalyzeElement(tree, filePath, element, findings);
            }
        }

        /// <summary>
        /// Analyzes an empty XML documentation element such as <c>&lt;see /&gt;</c>.
        /// </summary>
        private static void AnalyzeEmptyElement(
            SyntaxTree tree,
            string filePath,
            XmlEmptyElementSyntax element,
            List<Finding> findings)
        {
            string tagName = element.Name.LocalName.Text;

            if (tagName == "see")
            {
                if (!HasValidSeeTarget(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.SeeMissingTarget,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeTargetCombination(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeAttributeCombination,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeAttribute,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidHref(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeHref,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidLangword(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeLangword,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                return;
            }

            if (tagName == "seealso")
            {
                if (!HasValidSeeAlsoTarget(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.SeeAlsoMissingTarget,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoTargetCombination(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoAttributeCombination,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasSeeAlsoLangwordAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.SeeAlsoLangwordNotSupported,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoAttribute,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoHref(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoHref,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }
            }
        }

        /// <summary>
        /// Analyzes a non-empty XML documentation element such as <c>&lt;see&gt;...&lt;/see&gt;</c>.
        /// </summary>
        private static void AnalyzeElement(
            SyntaxTree tree,
            string filePath,
            XmlElementSyntax element,
            List<Finding> findings)
        {
            string tagName = element.StartTag.Name.LocalName.Text;

            if (tagName == "see")
            {
                if (!HasValidSeeTarget(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.SeeMissingTarget,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeTargetCombination(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeAttributeCombination,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeAttribute,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidHref(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeHref,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidLangword(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "see",
                            XmlDocSmells.InvalidSeeLangword,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                return;
            }

            if (tagName == "seealso")
            {
                if (!HasValidSeeAlsoTarget(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.SeeAlsoMissingTarget,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoTargetCombination(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoAttributeCombination,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasSeeAlsoLangwordAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.SeeAlsoLangwordNotSupported,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoAttribute(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoAttribute,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }

                if (HasInvalidSeeAlsoHref(element))
                {
                    findings.Add(
                        FindingFactory.AtSpanStart(
                            tree,
                            filePath,
                            "seealso",
                            XmlDocSmells.InvalidSeeAlsoHref,
                            element.Span,
                            SyntaxUtils.GetSnippet(element)));
                }
            }
        }

        /// <summary>
        /// Determines whether the given <c>see</c> element has any valid target attribute.
        /// </summary>
        private static bool HasValidSeeTarget(XmlElementSyntax element)
        {
            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given empty <c>see</c> element has any valid target attribute.
        /// </summary>
        private static bool HasValidSeeTarget(XmlEmptyElementSyntax element)
        {
            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given <c>seealso</c> element has any valid target attribute.
        /// </summary>
        private static bool HasValidSeeAlsoTarget(XmlElementSyntax element)
        {
            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given empty <c>seealso</c> element has any valid target attribute.
        /// </summary>
        private static bool HasValidSeeAlsoTarget(XmlEmptyElementSyntax element)
        {
            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                return true;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a <c>see</c> element combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if more than one of <c>cref</c>, <c>href</c>, or <c>langword</c> is present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeTargetCombination(XmlElementSyntax element)
        {
            int targetCount = CountSeeTargets(element);
            return targetCount > 1;
        }

        /// <summary>
        /// Determines whether an empty <c>see</c> element combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if more than one of <c>cref</c>, <c>href</c>, or <c>langword</c> is present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeTargetCombination(XmlEmptyElementSyntax element)
        {
            int targetCount = CountSeeTargets(element);
            return targetCount > 1;
        }

        /// <summary>
        /// Determines whether a <c>seealso</c> element combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if both <c>cref</c> and <c>href</c> are present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAlsoTargetCombination(XmlElementSyntax element)
        {
            int targetCount = CountSeeAlsoTargets(element);
            return targetCount > 1;
        }

        /// <summary>
        /// Determines whether an empty <c>seealso</c> element combines multiple target attributes.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if both <c>cref</c> and <c>href</c> are present;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAlsoTargetCombination(XmlEmptyElementSyntax element)
        {
            int targetCount = CountSeeAlsoTargets(element);
            return targetCount > 1;
        }

        /// <summary>
        /// Counts the number of target attributes on a <c>see</c> element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>The number of present target attributes.</returns>
        private static int CountSeeTargets(XmlElementSyntax element)
        {
            int count = 0;

            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword"))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Counts the number of target attributes on an empty <c>see</c> element.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>The number of present target attributes.</returns>
        private static int CountSeeTargets(XmlEmptyElementSyntax element)
        {
            int count = 0;

            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword"))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Counts the number of target attributes on a <c>seealso</c> element.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>The number of present target attributes.</returns>
        private static int CountSeeAlsoTargets(XmlElementSyntax element)
        {
            int count = 0;

            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Counts the number of target attributes on an empty <c>seealso</c> element.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>The number of present target attributes.</returns>
        private static int CountSeeAlsoTargets(XmlEmptyElementSyntax element)
        {
            int count = 0;

            if (SyntaxUtils.HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
            {
                count++;
            }

            if (SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "href"))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Determines whether a <c>see</c> element contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains any attribute other than
        /// <c>cref</c>, <c>href</c>, <c>langword</c>, or <c>title</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAttribute(XmlElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.StartTag.Attributes)
            {
                string attributeName = attribute.Name.LocalName.Text;

                if ((attributeName == "cref") ||
                    (attributeName == "href") ||
                    (attributeName == "langword") ||
                    (attributeName == "title"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an empty <c>see</c> element contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains any attribute other than
        /// <c>cref</c>, <c>href</c>, <c>langword</c>, or <c>title</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAttribute(XmlEmptyElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.Attributes)
            {
                string attributeName = attribute.Name.LocalName.Text;

                if ((attributeName == "cref") ||
                    (attributeName == "href") ||
                    (attributeName == "langword") ||
                    (attributeName == "title"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a <c>seealso</c> element contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains any attribute other than
        /// <c>cref</c>, <c>href</c>, or <c>title</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAlsoAttribute(XmlElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.StartTag.Attributes)
            {
                string attributeName = attribute.Name.LocalName.Text;

                if ((attributeName == "cref") ||
                    (attributeName == "href") ||
                    (attributeName == "title") ||
                    (attributeName == "langword"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an empty <c>seealso</c> element contains an invalid attribute.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the element contains any attribute other than
        /// <c>cref</c>, <c>href</c>, or <c>title</c>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasInvalidSeeAlsoAttribute(XmlEmptyElementSyntax element)
        {
            foreach (XmlAttributeSyntax attribute in element.Attributes)
            {
                string attributeName = attribute.Name.LocalName.Text;

                if ((attributeName == "cref") ||
                    (attributeName == "href") ||
                    (attributeName == "title") ||
                    (attributeName == "langword"))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a <c>seealso</c> element uses the forbidden <c>langword</c> attribute.
        /// </summary>
        /// <param name="element">The XML element to inspect.</param>
        /// <returns><see langword="true"/> if <c>langword</c> is present; otherwise <see langword="false"/>.</returns>
        private static bool HasSeeAlsoLangwordAttribute(XmlElementSyntax element)
        {
            return SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword");
        }

        /// <summary>
        /// Determines whether an empty <c>seealso</c> element uses the forbidden <c>langword</c> attribute.
        /// </summary>
        /// <param name="element">The XML empty element to inspect.</param>
        /// <returns><see langword="true"/> if <c>langword</c> is present; otherwise <see langword="false"/>.</returns>
        private static bool HasSeeAlsoLangwordAttribute(XmlEmptyElementSyntax element)
        {
            return SyntaxUtils.HasAttribute<XmlAttributeSyntax>(element, "langword");
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on a <c>see</c> element is invalid.
        /// </summary>
        private static bool HasInvalidHref(XmlElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on an empty <c>see</c> element is invalid.
        /// </summary>
        private static bool HasInvalidHref(XmlEmptyElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on a <c>seealso</c> element is invalid.
        /// </summary>
        private static bool HasInvalidSeeAlsoHref(XmlElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>href</c> attribute on an empty <c>seealso</c> element is invalid.
        /// </summary>
        private static bool HasInvalidSeeAlsoHref(XmlEmptyElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "href");

            if (value == null)
            {
                return false;
            }

            return !IsValidHref(value);
        }

        /// <summary>
        /// Determines whether the <c>langword</c> attribute on a <c>see</c> element is invalid.
        /// </summary>
        private static bool HasInvalidLangword(XmlElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "langword");

            if (value == null)
            {
                return false;
            }

            return !IsValidLangword(value);
        }

        /// <summary>
        /// Determines whether the <c>langword</c> attribute on an empty <c>see</c> element is invalid.
        /// </summary>
        private static bool HasInvalidLangword(XmlEmptyElementSyntax element)
        {
            string? value = SyntaxUtils.GetAttributeValue(element, "langword");

            if (value == null)
            {
                return false;
            }

            return !IsValidLangword(value);
        }

        /// <summary>
        /// Validates an href value.
        /// </summary>
        private static bool IsValidHref(string value)
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
        private static bool IsValidLangword(string value)
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
    }
}
