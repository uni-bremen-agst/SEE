using NUnit.Framework;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Tests for <see cref="HallucinationFilter"/>.
    /// </summary>
    internal class TestHallucinationFilter
    {
        /// <summary>
        /// Texts known to be invented by Whisper are dropped, no matter
        /// how they are capitalized or punctuated.
        /// </summary>
        [TestCase("Thank you.")]
        [TestCase(" thank you ")]
        [TestCase("THANK YOU!")]
        [TestCase("Thanks for watching!")]
        [TestCase("Vielen Dank.")]
        [TestCase("[Music]")]
        [TestCase("*Musik*")]
        [TestCase("(Musik)")]
        [TestCase("[BLANK_AUDIO]")]
        [TestCase("Untertitel im Auftrag des ZDF")]
        [TestCase("Untertitel im Auftrag des ZDF, 2020")]
        [TestCase("Untertitel von Stephanie Geiges")]
        [TestCase("Untertitel der Amara.org-Community")]
        [TestCase("Untertitelung des ZDF, 2020")]
        public void TestKnownHallucinations(string text)
        {
            Assert.That(HallucinationFilter.IsHallucination(text), Is.True);
        }

        /// <summary>
        /// Results without any letter or digit are dropped.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(".")]
        [TestCase("...")]
        [TestCase("?!")]
        [TestCase(" - ")]
        public void TestEmptyOrPunctuationOnly(string text)
        {
            Assert.That(HallucinationFilter.IsHallucination(text), Is.True);
        }

        /// <summary>
        /// Real sentences are kept, even if they contain a text from the list.
        /// </summary>
        [TestCase("Hallo zusammen.")]
        [TestCase("Thank you for the review, I will fix it.")]
        [TestCase("Vielen Dank, das schaue ich mir an.")]
        [TestCase("Die Musik ist zu laut.")]
        [TestCase("Wir brauchen Untertitel im Video.")]
        [TestCase("Ja.")]
        [TestCase("42")]
        public void TestRealSpeechIsKept(string text)
        {
            Assert.That(HallucinationFilter.IsHallucination(text), Is.False);
        }

        /// <summary>
        /// Normalizing removes the surrounding punctuation and the capitalization only.
        /// </summary>
        [Test]
        public void TestNormalize()
        {
            Assert.That(HallucinationFilter.Normalize("[Music]"), Is.EqualTo("music"));
            Assert.That(HallucinationFilter.Normalize("  Hello, World!  "), Is.EqualTo("hello, world"));
            Assert.That(HallucinationFilter.Normalize("..."), Is.EqualTo(""));
            Assert.That(HallucinationFilter.Normalize(null), Is.EqualTo(""));
        }
    }
}
