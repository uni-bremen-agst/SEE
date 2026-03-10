using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests DOC831 for value tags on unsupported member kinds that require full-source test setup.
    /// </summary>
    public sealed class DOC831_InvalidMemberSpecialCasesTests
    {
        /// <summary>
        /// Provides complete source samples whose value tags are invalid and must trigger DOC831.
        /// </summary>
        /// <returns>Full source samples expected to produce DOC831.</returns>
        public static IEnumerable<object[]> InvalidValueSourceSamples()
        {
            yield return new object[]
            {
                "namespace TestNamespace\n" +
                "{\n" +
                "    /// <summary>Represents a delegate.</summary>\n" +
                "    /// <value>Invalid.</value>\n" +
                "    public delegate void TestDelegate();\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Represents a class.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public class TestClass\n" +
                "{\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Represents a struct.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public struct TestStruct\n" +
                "{\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Represents an interface.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public interface ITest\n" +
                "{\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Represents an enum.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public enum TestEnum\n" +
                "{\n" +
                "    A\n" +
                "}\n"
            };

            yield return new object[]
            {
                "public enum TestEnum\n" +
                "{\n" +
                "    /// <summary>Represents a value.</summary>\n" +
                "    /// <value>Invalid.</value>\n" +
                "    A\n" +
                "}\n"
            };

            yield return new object[]
            {
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Finalizes the instance.</summary>\n" +
                "    /// <value>Invalid.</value>\n" +
                "    ~TestClass()\n" +
                "    {\n" +
                "    }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Adds two values.</summary>\n" +
                "    /// <value>Invalid.</value>\n" +
                "    public static TestClass operator +(TestClass left, TestClass right)\n" +
                "    {\n" +
                "        return left;\n" +
                "    }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Converts from int.</summary>\n" +
                "    /// <value>Invalid.</value>\n" +
                "    public static implicit operator TestClass(int value)\n" +
                "    {\n" +
                "        return new TestClass();\n" +
                "    }\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that a value tag on an unsupported declaration kind triggers DOC831.
        /// </summary>
        /// <param name="source">The full source code to analyze.</param>
        [Theory]
        [MemberData(nameof(InvalidValueSourceSamples))]
        public void InvalidValueSource_TriggersDoc831(string source)
        {
            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ValueOnInvalidMember.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }
    }
}
