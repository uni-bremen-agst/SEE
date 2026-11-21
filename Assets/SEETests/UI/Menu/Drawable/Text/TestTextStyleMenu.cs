using NUnit.Framework;
using FontStyles = TMPro.FontStyles;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Tests reusable font-style behavior of <see cref="TextStyleMenu"/>.
    /// </summary>
    [TestFixture]
    public class TestTextStyleMenu
    {
        /// <summary>
        /// Verifies that selecting lower case excludes upper case and small caps.
        /// </summary>
        [Test]
        public void TestLowerCaseExcludesUpperCaseAndSmallCaps()
        {
            FontStyles result =
                TextStyleMenu.GetMutuallyExclusiveFontStyles(
                    FontStyles.LowerCase);

            Assert.That(
                result,
                Is.EqualTo(
                    FontStyles.UpperCase | FontStyles.SmallCaps));
        }

        /// <summary>
        /// Verifies that selecting upper case excludes lower case and small caps.
        /// </summary>
        [Test]
        public void TestUpperCaseExcludesLowerCaseAndSmallCaps()
        {
            FontStyles result =
                TextStyleMenu.GetMutuallyExclusiveFontStyles(
                    FontStyles.UpperCase);

            Assert.That(
                result,
                Is.EqualTo(
                    FontStyles.LowerCase | FontStyles.SmallCaps));
        }

        /// <summary>
        /// Verifies that selecting small caps excludes lower and upper case.
        /// </summary>
        [Test]
        public void TestSmallCapsExcludesLowerAndUpperCase()
        {
            FontStyles result =
                TextStyleMenu.GetMutuallyExclusiveFontStyles(
                    FontStyles.SmallCaps);

            Assert.That(
                result,
                Is.EqualTo(
                    FontStyles.LowerCase | FontStyles.UpperCase));
        }

        /// <summary>
        /// Verifies that an independent font style does not exclude another style.
        /// </summary>
        [Test]
        public void TestBoldDoesNotExcludeAnotherStyle()
        {
            FontStyles result =
                TextStyleMenu.GetMutuallyExclusiveFontStyles(
                    FontStyles.Bold);

            Assert.That(result, Is.EqualTo(FontStyles.Normal));
        }

        /// <summary>
        /// Verifies that the bold label resolves to the bold font style.
        /// </summary>
        [Test]
        public void TestBoldLabelResolvesToBold()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("Bold");

            Assert.That(result, Is.EqualTo(FontStyles.Bold));
        }

        /// <summary>
        /// Verifies that the italic label resolves to the italic font style.
        /// </summary>
        [Test]
        public void TestItalicLabelResolvesToItalic()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("Italic");

            Assert.That(result, Is.EqualTo(FontStyles.Italic));
        }

        /// <summary>
        /// Verifies that the underline label resolves to the underline font style.
        /// </summary>
        [Test]
        public void TestUnderlineLabelResolvesToUnderline()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("Underline");

            Assert.That(result, Is.EqualTo(FontStyles.Underline));
        }

        /// <summary>
        /// Verifies that the strikethrough label resolves to the
        /// strikethrough font style.
        /// </summary>
        [Test]
        public void TestStrikethroughLabelResolvesToStrikethrough()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("Strikethrough");

            Assert.That(result, Is.EqualTo(FontStyles.Strikethrough));
        }

        /// <summary>
        /// Verifies that the lower-case label resolves to the lower-case style.
        /// </summary>
        [Test]
        public void TestLowerCaseLabelResolvesToLowerCase()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("LowerCase");

            Assert.That(result, Is.EqualTo(FontStyles.LowerCase));
        }

        /// <summary>
        /// Verifies that the upper-case label resolves to the upper-case style.
        /// </summary>
        [Test]
        public void TestUpperCaseLabelResolvesToUpperCase()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("UpperCase");

            Assert.That(result, Is.EqualTo(FontStyles.UpperCase));
        }

        /// <summary>
        /// Verifies that the small-caps label resolves to the small-caps style.
        /// </summary>
        [Test]
        public void TestSmallCapsLabelResolvesToSmallCaps()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("SmallCaps");

            Assert.That(result, Is.EqualTo(FontStyles.SmallCaps));
        }

        /// <summary>
        /// Verifies that an unknown label resolves to the normal font style.
        /// </summary>
        [Test]
        public void TestUnknownLabelResolvesToNormal()
        {
            FontStyles result =
                TextStyleMenu.GetFontStyleOfKey("Unknown");

            Assert.That(result, Is.EqualTo(FontStyles.Normal));
        }
    }
}
