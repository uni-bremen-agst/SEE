using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Inheritdoc
{
    /// <summary>
    /// Tests DOC740 – multiple possible implicit inheritance sources for <c>inheritdoc</c>.
    /// </summary>
    public sealed class DOC740_AmbiguousInheritdocSourceTests
    {
        /// <summary>
        /// Ensures that a method implementing two interfaces with the same member
        /// triggers DOC740 when <c>inheritdoc</c> is used without an explicit <c>cref</c>.
        /// </summary>
        [Fact]
        public void MethodWithTwoPossibleInterfaceSources_IsDetected()
        {
            string source =
                "public interface IA\n" +
                "{\n" +
                "    /// <summary>Interface A documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public interface IB\n" +
                "{\n" +
                "    /// <summary>Interface B documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : IA, IB\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.AmbiguousInheritdocSource.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that a property implementing two interfaces with the same member
        /// triggers DOC740 when <c>inheritdoc</c> is used without an explicit <c>cref</c>.
        /// </summary>
        [Fact]
        public void PropertyWithTwoPossibleInterfaceSources_IsDetected()
        {
            string source =
                "public interface IA\n" +
                "{\n" +
                "    /// <summary>Interface A property documentation.</summary>\n" +
                "    int P { get; }\n" +
                "}\n" +
                "\n" +
                "public interface IB\n" +
                "{\n" +
                "    /// <summary>Interface B property documentation.</summary>\n" +
                "    int P { get; }\n" +
                "}\n" +
                "\n" +
                "public class Test : IA, IB\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public int P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.AmbiguousInheritdocSource.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that an event implementing two interfaces with the same member
        /// triggers DOC740 when <c>inheritdoc</c> is used without an explicit <c>cref</c>.
        /// </summary>
        [Fact]
        public void EventWithTwoPossibleInterfaceSources_IsDetected()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public interface IA\n" +
                "{\n" +
                "    /// <summary>Interface A event documentation.</summary>\n" +
                "    event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public interface IB\n" +
                "{\n" +
                "    /// <summary>Interface B event documentation.</summary>\n" +
                "    event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public class Test : IA, IB\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public event EventHandler Changed;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.AmbiguousInheritdocSource.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that a single interface source does not trigger DOC740.
        /// </summary>
        [Fact]
        public void SingleInterfaceSource_DoesNotTriggerFinding()
        {
            string source =
                "public interface IA\n" +
                "{\n" +
                "    /// <summary>Interface documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : IA\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an explicit <c>cref</c> avoids DOC740 even when multiple
        /// implicit interface sources would otherwise exist.
        /// </summary>
        [Fact]
        public void ExplicitCref_AvoidsAmbiguityFinding()
        {
            string source =
                "public interface IA\n" +
                "{\n" +
                "    /// <summary>Interface A documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public interface IB\n" +
                "{\n" +
                "    /// <summary>Interface B documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : IA, IB\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"IA.M\"/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
