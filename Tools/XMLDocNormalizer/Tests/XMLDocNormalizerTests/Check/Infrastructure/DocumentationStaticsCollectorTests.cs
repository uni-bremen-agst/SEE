using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Checks.Infrastructure
{
    /// <summary>
    /// Tests for <see cref="DocumentationStatisticsCollector"/>.
    /// </summary>
    public sealed class DocumentationStatisticsCollectorTests
    {
        /// <summary>
        /// Ensures that the collector counts common declaration kinds and related totals correctly.
        /// </summary>
        [Fact]
        public void Collect_CountsDeclarationsAndTotalsCorrectly()
        {
            string source =
                "namespace N\n" +
                "{\n" +
                "    public enum E { A, B }\n" +
                "\n" +
                "    public interface IFoo { void M(); }\n" +
                "\n" +
                "    public class C<T>\n" +
                "    {\n" +
                "        public int this[int i, string s] { get { return 0; } }\n" +
                "\n" +
                "        public C(int x) { }\n" +
                "\n" +
                "        public int M(int a, int b) { return 0; }\n" +
                "    }\n" +
                "\n" +
                "    public delegate int D<U>(U u);\n" +
                "}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "InMemory.cs");

            IReadOnlyDictionary<string, int> totals = DocumentationStatisticsCollector.Collect(tree);

            Assert.Equal(1, Get(totals, StatisticsKeys.NamespaceDeclarationsTotal));

            Assert.Equal(1, Get(totals, StatisticsKeys.EnumDeclarationsTotal));
            Assert.Equal(2, Get(totals, StatisticsKeys.EnumMembersTotal));

            Assert.Equal(1, Get(totals, StatisticsKeys.InterfaceDeclarationsTotal));
            Assert.Equal(1, Get(totals, StatisticsKeys.ClassDeclarationsTotal));
            Assert.Equal(1, Get(totals, StatisticsKeys.DelegateDeclarationsTotal));

            // Generic type parameters:
            // class C<T> => 1
            // delegate D<U> => 1
            Assert.Equal(2, Get(totals, StatisticsKeys.TypeParametersTotal));

            // Member counts
            Assert.Equal(1, Get(totals, StatisticsKeys.IndexersTotal));
            Assert.Equal(1, Get(totals, StatisticsKeys.ConstructorsTotal));
            Assert.Equal(2, Get(totals, StatisticsKeys.MethodsTotal));

            // Parameter totals:
            // indexer: 2
            // ctor: 1
            // method: 2
            // delegate: 1
            Assert.Equal(6, Get(totals, StatisticsKeys.ParametersTotal));

            // Returns required:
            // method returns int => 1
            // delegate returns int => 1
            // indexer returns int => not counted as "returns required" in this collector (by design)
            Assert.Equal(2, Get(totals, StatisticsKeys.ReturnsRequiredTotal));
        }

        /// <summary>
        /// Ensures that void-returning methods are not counted as requiring a &lt;returns&gt; tag.
        /// </summary>
        [Fact]
        public void Collect_DoesNotCountVoidMethodsAsReturnsRequired()
        {
            string source =
                "namespace N\n" +
                "{\n" +
                "    public class C\n" +
                "    {\n" +
                "        public void M(int x) { }\n" +
                "    }\n" +
                "}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "InMemory.cs");

            IReadOnlyDictionary<string, int> totals = DocumentationStatisticsCollector.Collect(tree);

            Assert.Equal(1, Get(totals, StatisticsKeys.MethodsTotal));
            Assert.Equal(1, Get(totals, StatisticsKeys.ParametersTotal));
            Assert.Equal(0, Get(totals, StatisticsKeys.ReturnsRequiredTotal));
        }

        /// <summary>
        /// Reads a total value from the dictionary, returning zero if the key is missing.
        /// </summary>
        /// <param name="totals">The totals dictionary.</param>
        /// <param name="key">The statistics key.</param>
        /// <returns>The stored value or zero.</returns>
        private static int Get(IReadOnlyDictionary<string, int> totals, string key)
        {
            if (totals == null)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            if (!totals.TryGetValue(key, out int value))
            {
                return 0;
            }

            return value;
        }
    }
}