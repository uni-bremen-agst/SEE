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

        /// <summary>
        /// A text consisting of annotations only is empty after the
        /// annotations were removed, and it is dropped.
        /// </summary>
        [TestCase("[Klack]")]
        [TestCase("(Husten)")]
        [TestCase("*klopft*")]
        [TestCase("[Klack] (Husten) *klopft*")]
        [TestCase(" [Klack][klopfen] ")]
        public void TestAnnotationsOnly(string text)
        {
            string cleaned = HallucinationFilter.RemoveAnnotations(text);
            Assert.That(cleaned, Is.EqualTo(""));
            Assert.That(HallucinationFilter.IsHallucination(cleaned), Is.True);
        }

        /// <summary>
        /// Annotations are removed from a sentence; the rest of the sentence is kept.
        /// </summary>
        [TestCase("Hallo [Husten] zusammen", "Hallo zusammen")]
        [TestCase("Hallo (Husten) zusammen", "Hallo zusammen")]
        [TestCase("Hallo *hustet* zusammen", "Hallo zusammen")]
        [TestCase("[Musik] Hallo zusammen.", "Hallo zusammen.")]
        [TestCase("Hallo zusammen. [Klack]", "Hallo zusammen.")]
        [TestCase("Hallo[Husten]zusammen", "Hallo zusammen")]
        [TestCase("[Klack] Hallo (Husten) zusammen *klopft*", "Hallo zusammen")]
        public void TestAnnotationsAreRemovedFromSentence(string text, string expected)
        {
            string cleaned = HallucinationFilter.RemoveAnnotations(text);
            Assert.That(cleaned, Is.EqualTo(expected));
            Assert.That(HallucinationFilter.IsHallucination(cleaned), Is.False);
        }

        /// <summary>
        /// No space is left in front of a punctuation mark that followed an annotation.
        /// </summary>
        [TestCase("Hallo [Husten], zusammen", "Hallo, zusammen")]
        [TestCase("Hallo zusammen [Klack].", "Hallo zusammen.")]
        [TestCase("Wirklich (Husten)?", "Wirklich?")]
        public void TestNoSpaceBeforePunctuation(string text, string expected)
        {
            Assert.That(HallucinationFilter.RemoveAnnotations(text), Is.EqualTo(expected));
        }

        /// <summary>
        /// A sentence without annotations is not changed, except for needless white spaces.
        /// </summary>
        [TestCase("Hallo zusammen.", "Hallo zusammen.")]
        [TestCase("Ja, das stimmt: 3 Klassen!", "Ja, das stimmt: 3 Klassen!")]
        [TestCase("  Hallo   zusammen.  ", "Hallo zusammen.")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void TestTextWithoutAnnotations(string text, string expected)
        {
            Assert.That(HallucinationFilter.RemoveAnnotations(text), Is.EqualTo(expected));
        }

        /// <summary>
        /// If only punctuation is left after the annotations were removed, the text is dropped.
        /// </summary>
        [TestCase("[Klack].")]
        [TestCase("(Husten) ...")]
        [TestCase("*klopft* - [Klack]!")]
        public void TestOnlyPunctuationLeft(string text)
        {
            string cleaned = HallucinationFilter.RemoveAnnotations(text);
            Assert.That(HallucinationFilter.IsHallucination(cleaned), Is.True);
        }

        /// <summary>
        /// The known inventions of Whisper are looked for in the cleaned text.
        /// </summary>
        [TestCase("[Musik] Vielen Dank.")]
        [TestCase("Thank you. (applause)")]
        public void TestHallucinationAfterAnnotationIsDropped(string text)
        {
            string cleaned = HallucinationFilter.RemoveAnnotations(text);
            Assert.That(HallucinationFilter.IsHallucination(cleaned), Is.True);
        }
    }
}
