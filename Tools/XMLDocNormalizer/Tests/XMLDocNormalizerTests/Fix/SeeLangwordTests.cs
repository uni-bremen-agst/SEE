using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Fix
{
    /// <summary>
    /// Tests for <see langword="..."/> elements.
    /// </summary>
    public sealed class SeeLangwordTests
    {
        [Fact]
        public void SeeLangword_BooleanLiteral_IsConverted()
        {
            string source =
                "/// <summary>This is <see langword=\"true\"/></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>This is true</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void SeeLangword_NullLiteral_IsConverted()
        {
            string source =
                "/// <summary>Value is <see langword=\"null\"/></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>Value is null</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void SeeLangword_MixedContent_IsConverted()
        {
            string source =
                "/// <summary>Check <see langword=\"true\"/> and <see langword=\"false\"/></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>Check true and false</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }

        [Fact]
        public void SeeLangword_Casing_IsPreserved()
        {
            string source =
                "/// <summary>Value is <see langword=\"False\"/></summary>\n" +
                "void M() { }\n";

            string target =
                "/// <summary>Value is False</summary>\n" +
                "void M() { }\n";

            RewriteAssert.MemberEquals(source, target);
        }
    }
}

