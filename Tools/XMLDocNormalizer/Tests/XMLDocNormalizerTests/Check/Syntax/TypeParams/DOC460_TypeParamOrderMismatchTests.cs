using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Tests for DOC460 (TypeParamOrderMismatch): typeparam documentation tags are not ordered like declaration type parameters.
    /// </summary>
    public sealed class DOC460_TypeParamOrderMismatchTests
    {
        /// <summary>
        /// Provides supported declarations where typeparam documentation tags are ordered differently than declaration type parameters.
        /// </summary>
        /// <returns>Test cases containing code snippets, full-source flags, and expected owner kinds.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T, U, V>() { }\n",
                false,
                "Method"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T, U, V>();\n",
                false,
                "Delegate"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T, U, V>\n" +
                "{\n" +
                "}\n",
                true,
                "Class"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T, U, V>\n" +
                "{\n" +
                "}\n",
                true,
                "Struct"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T, U, V>\n" +
                "{\n" +
                "}\n",
                true,
                "Interface"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T, U, V>\n" +
                "{\n" +
                "}\n",
                true,
                "Record"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T, U, V>\n" +
                "{\n" +
                "}\n",
                true,
                "RecordStruct"
            };
        }

        /// <summary>
        /// Ensures that type parameter order mismatch is detected once for each supported declaration kind.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        /// <param name="expectedOwnerKind">The expected owner kind in the finding context.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void TypeParamOrderMismatch_IsDetected(string code, bool isFullSource, string expectedOwnerKind)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.TypeParamOrderMismatch.ID);

            Finding finding = findings.Single();

            Assert.Equal("typeparam", finding.TagName);
            Assert.Equal("<typeparam> tags should follow the declaration type parameter order.", finding.Message);
            Assert.Equal(expectedOwnerKind, finding.Context.OwnerKind);
            Assert.Equal("TypeParameterTag", finding.Context.SubjectKind);
            Assert.Null(finding.Context.TargetName);
        }

        /// <summary>
        /// Ensures that a method with three type parameters in reversed documentation order produces only one DOC460 finding.
        /// </summary>
        [Fact]
        public void TypeParamOrderMismatch_WithThreeReorderedTypeParameters_ProducesOnlyOneFinding()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T, U, V>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.TypeParamOrderMismatch.ID, 1);
            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.TypeParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that correctly ordered typeparam tags produce no findings.
        /// </summary>
        [Fact]
        public void CorrectTypeParamOrder_ProducesNoFindings()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "public void M<T, U, V>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that missing typeparam documentation does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void MissingTypeParamTag_DoesNotTriggerTypeParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T, U, V>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.MissingTypeParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.TypeParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that an unknown typeparam tag does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void UnknownTypeParamTag_DoesNotTriggerTypeParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"V\">V</typeparam>\n" +
                "public void M<T, U, V>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.UnknownTypeParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.TypeParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that duplicate typeparam documentation does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void DuplicateTypeParamTag_DoesNotTriggerTypeParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"U\">U</typeparam>\n" +
                "/// <typeparam name=\"T\">first</typeparam>\n" +
                "/// <typeparam name=\"T\">second</typeparam>\n" +
                "public void M<T, U>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.DuplicateTypeParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.TypeParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that a declaration with only one documented type parameter cannot produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void SingleTypeParameter_ProducesNoTypeParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Runs the type parameter detector on the given code snippet.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">Whether the snippet is a full source text.</param>
        /// <returns>The produced list of findings.</returns>
        private static List<Finding> Run(string code, bool isFullSource)
        {
            if (isFullSource)
            {
                return CheckAssert.FindTypeParamFindingsForSource(code);
            }

            return CheckAssert.FindTypeParamFindingsForMember(code);
        }
    }
}
