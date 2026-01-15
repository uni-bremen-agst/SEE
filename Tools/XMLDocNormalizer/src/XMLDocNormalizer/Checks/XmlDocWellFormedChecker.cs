using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects malformed XML documentation tags in a syntax tree.
    /// </summary>
    internal static class XmlDocWellFormedChecker
    {
        /// <summary>
        /// Scans the syntax tree and returns findings for malformed XML doc tags.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindMalformedTags(SyntaxTree tree, string filePath)
        {
            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            IEnumerable<DocumentationCommentTriviaSyntax> docTrivias =
                root.DescendantTrivia(descendIntoTrivia: true)
                    .Select(t => t.GetStructure())
                    .OfType<DocumentationCommentTriviaSyntax>();

            foreach (DocumentationCommentTriviaSyntax doc in docTrivias)
            {
                IEnumerable<XmlElementSyntax> elements =
                    doc.DescendantNodes()
                        .OfType<XmlElementSyntax>();

                foreach (XmlElementSyntax element in elements)
                {
                    string tagName = element.StartTag.Name.LocalName.Text;

                    // Unknown or misspelled XML doc tag.
                    if (!XmlDocTagDefinitions.KnownTags.Contains(tagName))
                    {
                        AddFinding(
                            tree,
                            findings,
                            filePath,
                            element,
                            tagName,
                            $"Unknown XML documentation tag <{tagName}>.");
                        continue;
                    }

                    // Missing end tag.
                    if (element.EndTag == null)
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "Missing end tag (unclosed XML element).");
                        continue;
                    }

                    // Common mistake: <paramref> and <typeparamref> should be empty elements (<.../>).
                    if (tagName == "paramref" || tagName == "typeparamref")
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "This tag should be an empty element, e.g. <paramref name=\"x\"/>.");
                        continue;
                    }

                    if (tagName == "param" && !HasAttribute<XmlNameAttributeSyntax>(element, "name"))
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "<param> tag is missing required 'name' attribute.");
                        continue;
                    }

                    if (tagName == "exception" && !HasAttribute<XmlCrefAttributeSyntax>(element, "cref"))
                    {
                        AddFinding(tree, findings, filePath, element, tagName,
                            "<exception> tag is missing required 'cref' attribute.");
                        continue;
                    }
                }
            }

            return findings;
        }

        /// <summary>
        /// Checks whether an XML element has a specific attribute of a given type and name.
        /// </summary>
        /// <typeparam name="T">The type of XML attribute syntax (e.g., XmlNameAttributeSyntax, XmlCrefAttributeSyntax).</typeparam>
        /// <param name="element">The XML element to check.</param>
        /// <param name="attributeName">The name of the attribute to look for.</param>
        /// <returns>True if the element has the attribute; otherwise, false.</returns>
        private static bool HasAttribute<T>(XmlElementSyntax element, string attributeName)
            where T : XmlAttributeSyntax
        {
            return element.StartTag.Attributes
                .OfType<T>()
                .Any(a => a.Name.LocalName.Text == attributeName);
        }


        /// <summary>
        /// Creates and adds a <see cref="Finding"/> describing a malformed XML documentation element.
        /// </summary>
        /// <param name="tree">The syntax tree containing the node.</param>
        /// <param name="findings">The collection to which the finding will be added.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="node">The syntax node representing the malformed XML element.</param>
        /// <param name="tagName">The name of the XML tag associated with the finding.</param>
        /// <param name="message">A human-readable description of the issue.</param>
        private static void AddFinding(
            SyntaxTree tree,
            List<Finding> findings,
            string filePath,
            SyntaxNode node,
            string tagName,
            string message)
        {
            FileLinePositionSpan span = tree.GetLineSpan(node.Span);
            int line = span.StartLinePosition.Line + 1;
            int column = span.StartLinePosition.Character + 1;

            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            findings.Add(new Finding(filePath, tagName, line, column, message, snippet));
        }
    }
}
