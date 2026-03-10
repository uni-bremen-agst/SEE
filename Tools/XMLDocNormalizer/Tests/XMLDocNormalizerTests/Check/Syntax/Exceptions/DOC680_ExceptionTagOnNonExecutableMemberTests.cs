using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Exception
{
    /// <summary>
    /// Tests for DOC680 (ExceptionTagOnNonExecutableMember).
    /// </summary>
    public sealed class DOC680_ExceptionTagOnNonExecutableMemberTests
    {
        /// <summary>
        /// Ensures that an exception tag on an abstract method triggers DOC680.
        /// </summary>
        [Fact]
        public void ExceptionTagOnAbstractMethod_IsDetected()
        {
            string source =
                "public abstract class TestClass\n" +
                "{\n" +
                "    /// <summary>Does something.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "    public abstract void M();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagOnNonExecutableMember.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that an exception tag on an interface member triggers DOC680.
        /// </summary>
        [Fact]
        public void ExceptionTagOnInterfaceMethod_IsDetected()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    /// <summary>Does something.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "    void M();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagOnNonExecutableMember.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that an exception tag on an extern method triggers DOC680.
        /// </summary>
        [Fact]
        public void ExceptionTagOnExternMethod_IsDetected()
        {
            string source =
                "public static class Native\n" +
                "{\n" +
                "    /// <summary>Calls native code.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "    public static extern void M();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionTagOnNonExecutableMember.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that an exception tag on an implemented method does not trigger DOC680.
        /// </summary>
        [Fact]
        public void ExceptionTagOnMethodWithBody_IsNotDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagOnNonExecutableMember.ID);
        }

        /// <summary>
        /// Ensures that an exception tag on an expression-bodied property is not reported as DOC680.
        /// </summary>
        [Fact]
        public void ExceptionTagOnExpressionBodiedProperty_IsNotDetected()
        {
            string member =
                "/// <summary>Gets something.</summary>\n" +
                "/// <exception cref=\"System.InvalidOperationException\">Invalid.</exception>\n" +
                "public int P => 42;\n";

            List<Finding> findings = CheckAssert.FindExceptionFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ExceptionTagOnNonExecutableMember.ID);
        }
    }
}
