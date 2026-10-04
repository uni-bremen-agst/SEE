using LibGit2Sharp;
using NUnit.Framework;
using SEE.GraphProviders.VCS;
using System;
using System.IO;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="Mailmap"/>: each of the four forms of an entry
    /// git defines, the precedence among them, and the case-insensitive
    /// lookup of the name and the address a commit records.
    /// </summary>
    internal class TestMailmap
    {
        /// <summary>
        /// The path of the .mailmap file written by <see cref="Map"/>, deleted
        /// again in <see cref="TearDown"/>.
        /// </summary>
        private string path;

        /// <summary>
        /// Chooses a fresh path for the .mailmap file of the next test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Path.GetTempPath(), $"TestMailmap-{Guid.NewGuid()}{Mailmap.Filename}");
        }

        /// <summary>
        /// Deletes the .mailmap file written by the test, if any.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// The mapping stated by a .mailmap file consisting of <paramref name="lines"/>.
        /// </summary>
        /// <param name="lines">The lines of the .mailmap file.</param>
        /// <returns>The mapping read from the file.</returns>
        private Mailmap Map(params string[] lines)
        {
            File.WriteAllLines(path, lines);
            return Mailmap.Read(path);
        }

        /// <summary>
        /// The canonical identity <paramref name="mailmap"/> gives the author
        /// a commit records as <paramref name="name"/> and <paramref name="email"/>.
        /// </summary>
        /// <param name="mailmap">The mapping to be consulted.</param>
        /// <param name="name">The name recorded in the commit.</param>
        /// <param name="email">The address recorded in the commit.</param>
        /// <returns>The canonical identity.</returns>
        private static FileAuthor AuthorOf(Mailmap mailmap, string name, string email)
        {
            return mailmap.AuthorOf(new Signature(name, email, DateTimeOffset.Now));
        }

        /// <summary>
        /// Each of the four forms maps the author it names onto the identity
        /// it states, and leaves alone what it does not replace.
        /// </summary>
        /// <param name="entry">The only entry of the .mailmap file.</param>
        /// <param name="commitName">The name recorded in the commit.</param>
        /// <param name="commitEmail">The address recorded in the commit.</param>
        /// <param name="expectedName">The canonical name expected.</param>
        /// <param name="expectedEmail">The canonical address expected.</param>
        // "Proper Name <commit@email>": the name is replaced, the address stays.
        [TestCase("Proper Name <commit@example.org>",
                  "Commit Name", "commit@example.org",
                  "Proper Name", "commit@example.org")]
        // "<proper@email> <commit@email>": the address is replaced, the name stays.
        [TestCase("<proper@example.org> <commit@example.org>",
                  "Commit Name", "commit@example.org",
                  "Commit Name", "proper@example.org")]
        // "Proper Name <proper@email> <commit@email>": both are replaced.
        [TestCase("Proper Name <proper@example.org> <commit@example.org>",
                  "Commit Name", "commit@example.org",
                  "Proper Name", "proper@example.org")]
        // "Proper Name <proper@email> Commit Name <commit@email>": both are replaced.
        [TestCase("Proper Name <proper@example.org> Commit Name <commit@example.org>",
                  "Commit Name", "commit@example.org",
                  "Proper Name", "proper@example.org")]
        public void EachFormMapsTheAuthorItNames
            (string entry, string commitName, string commitEmail, string expectedName, string expectedEmail)
        {
            FileAuthor author = AuthorOf(Map(entry), commitName, commitEmail);

            Assert.That(author, Is.EqualTo(new FileAuthor(expectedName, expectedEmail)));
        }

        /// <summary>
        /// No form maps an author whose address it does not name, and the
        /// form naming the commit's name as well maps no author recorded
        /// under another name.
        /// </summary>
        /// <param name="entry">The only entry of the .mailmap file.</param>
        /// <param name="commitName">The name recorded in the commit.</param>
        /// <param name="commitEmail">The address recorded in the commit.</param>
        [TestCase("Proper Name <commit@example.org>",
                  "Commit Name", "other@example.org")]
        [TestCase("<proper@example.org> <commit@example.org>",
                  "Commit Name", "other@example.org")]
        [TestCase("Proper Name <proper@example.org> <commit@example.org>",
                  "Commit Name", "other@example.org")]
        [TestCase("Proper Name <proper@example.org> Commit Name <commit@example.org>",
                  "Commit Name", "other@example.org")]
        [TestCase("Proper Name <proper@example.org> Commit Name <commit@example.org>",
                  "Other Name", "commit@example.org")]
        public void EachFormLeavesOtherAuthorsAlone(string entry, string commitName, string commitEmail)
        {
            FileAuthor author = AuthorOf(Map(entry), commitName, commitEmail);

            Assert.That(author, Is.EqualTo(new FileAuthor(commitName, commitEmail)));
        }

        /// <summary>
        /// The name and the address a commit records are looked up regardless
        /// of their case, as git does, for each of the four forms.
        /// </summary>
        /// <param name="entry">The only entry of the .mailmap file.</param>
        /// <param name="commitName">The name recorded in the commit.</param>
        /// <param name="commitEmail">The address recorded in the commit.</param>
        /// <param name="expectedName">The canonical name expected.</param>
        /// <param name="expectedEmail">The canonical address expected.</param>
        /// <remarks>
        /// For the form replacing the name alone, the address returned is
        /// spelt as the .mailmap file spells it, not as the commit does.
        /// </remarks>
        [TestCase("Proper Name <commit@example.org>",
                  "Commit Name", "Commit@Example.ORG",
                  "Proper Name", "commit@example.org")]
        [TestCase("<proper@example.org> <commit@example.org>",
                  "Commit Name", "Commit@Example.ORG",
                  "Commit Name", "proper@example.org")]
        [TestCase("Proper Name <proper@example.org> <commit@example.org>",
                  "Commit Name", "Commit@Example.ORG",
                  "Proper Name", "proper@example.org")]
        [TestCase("Proper Name <proper@example.org> Commit Name <commit@example.org>",
                  "COMMIT name", "Commit@Example.ORG",
                  "Proper Name", "proper@example.org")]
        public void LookupIgnoresCase
            (string entry, string commitName, string commitEmail, string expectedName, string expectedEmail)
        {
            FileAuthor author = AuthorOf(Map(entry), commitName, commitEmail);

            Assert.That(author, Is.EqualTo(new FileAuthor(expectedName, expectedEmail)));
        }

        /// <summary>
        /// An entry naming the commit's name as well as its address takes
        /// precedence over one naming the address alone, which in turn takes
        /// precedence over one replacing the address alone; the order of the
        /// entries in the file does not matter.
        /// </summary>
        /// <param name="reversed">Whether the entries are written in reverse order.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void MoreSpecificEntriesTakePrecedence(bool reversed)
        {
            string[] lines =
            {
                "<address-only@example.org> <commit@example.org>",
                "By Email <by-email@example.org> <commit@example.org>",
                "By Both <by-both@example.org> Commit Name <commit@example.org>"
            };
            if (reversed)
            {
                Array.Reverse(lines);
            }
            Mailmap mailmap = Map(lines);

            Assert.That(AuthorOf(mailmap, "Commit Name", "commit@example.org"),
                        Is.EqualTo(new FileAuthor("By Both", "by-both@example.org")),
                        "The entry naming name and address must win.");
            Assert.That(AuthorOf(mailmap, "Other Name", "commit@example.org"),
                        Is.EqualTo(new FileAuthor("By Email", "by-email@example.org")),
                        "The entry naming a name must win over the one replacing the address alone.");
        }

        /// <summary>
        /// An entry replacing the address alone applies where no entry naming
        /// a name has anything to say.
        /// </summary>
        [Test]
        public void AddressOnlyAppliesWhereNothingElseDoes()
        {
            Mailmap mailmap = Map("<address-only@example.org> <commit@example.org>",
                                  "By Both <by-both@example.org> Commit Name <commit@example.org>");

            Assert.That(AuthorOf(mailmap, "Other Name", "commit@example.org"),
                        Is.EqualTo(new FileAuthor("Other Name", "address-only@example.org")));
        }

        /// <summary>
        /// Comments, blank lines and lines naming no address state no entry;
        /// a comment after an entry does not disturb it.
        /// </summary>
        [Test]
        public void CommentsAndBlankLinesAreIgnored()
        {
            Mailmap mailmap = Map("# Proper Name <commit@example.org>",
                                  "",
                                  "   ",
                                  "Just A Name",
                                  "Unterminated <commit@example.org",
                                  "Other Proper <other@example.org> # Ignored <ignored@example.org>");

            Assert.That(AuthorOf(mailmap, "Commit Name", "commit@example.org"),
                        Is.EqualTo(new FileAuthor("Commit Name", "commit@example.org")));
            Assert.That(AuthorOf(mailmap, "Other Name", "other@example.org"),
                        Is.EqualTo(new FileAuthor("Other Proper", "other@example.org")));
            Assert.That(AuthorOf(mailmap, "Ignored Name", "ignored@example.org"),
                        Is.EqualTo(new FileAuthor("Ignored Name", "ignored@example.org")));
        }

        /// <summary>
        /// A missing .mailmap file maps nothing.
        /// </summary>
        [Test]
        public void MissingFileMapsNothing()
        {
            Mailmap mailmap = Mailmap.Read(path);

            Assert.That(AuthorOf(mailmap, "Commit Name", "commit@example.org"),
                        Is.EqualTo(new FileAuthor("Commit Name", "commit@example.org")));
        }
    }
}
