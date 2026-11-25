using System;
using System.IO;
using NUnit.Framework;

namespace SEE.Tools.AI
{
    /// <summary>
    /// Unit tests for <see cref="ExecutableLocator"/>. They create fake programs in temporary
    /// folders, so they behave the same on Windows, macOS and Linux.
    /// </summary>
    internal class TestExecutableLocator
    {
        /// <summary>
        /// A temporary folder that is deleted after each test.
        /// </summary>
        private string folder;

        /// <summary>
        /// A second temporary folder, inside <see cref="folder"/>.
        /// </summary>
        private string otherFolder;

        /// <summary>
        /// Creates the temporary folders.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "SEE-ExecutableLocator-" + Guid.NewGuid().ToString("N"));
            otherFolder = Path.Combine(folder, "other");
            Directory.CreateDirectory(otherFolder);
        }

        /// <summary>
        /// Deletes the temporary folders.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Directory.Delete(folder, recursive: true);
        }

        /// <summary>
        /// Creates an empty file for the program <paramref name="name"/> in <paramref name="parent"/>,
        /// named as an installer on the current operating system would name it.
        /// </summary>
        private static string CreateProgram(string parent, string name)
        {
            string path = Path.Combine(parent, ExecutableLocator.IsWindows ? name + ".exe" : name);
            File.WriteAllText(path, "");
            return path;
        }

        [Test]
        public void TestFindsProgramInGivenFolders()
        {
            string program = CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }), Is.EqualTo(program));
        }

        [Test]
        public void TestFirstFolderWins()
        {
            string first = CreateProgram(folder, "tool");
            CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }), Is.EqualTo(first));
        }

        [Test]
        public void TestFullPathIsReturnedIfItExists()
        {
            string program = CreateProgram(folder, "tool");

            Assert.That(ExecutableLocator.Find(program, Array.Empty<string>()), Is.EqualTo(program));
            Assert.That(ExecutableLocator.Find(program + "-missing", new[] { folder }), Is.Null);
        }

        [Test]
        public void TestMissingOrBlankNameFindsNothing()
        {
            Assert.That(ExecutableLocator.Find("tool", new[] { folder }), Is.Null);
            Assert.That(ExecutableLocator.Find(" ", new[] { folder }), Is.Null);
            Assert.That(ExecutableLocator.Find(null, new[] { folder }), Is.Null);
        }

        [Test]
        public void TestSkippedCandidatesAreIgnored()
        {
            string skipped = CreateProgram(folder, "tool");
            string program = CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }, path => path == skipped),
                        Is.EqualTo(program));
        }

        [Test]
        public void TestInvalidFolderEntriesAreTolerated()
        {
            string program = CreateProgram(folder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { "\0invalid", "\"" + folder + "\"" }),
                        Is.EqualTo(program));
        }

        [Test]
        public void TestRealProgramsAreStartedDirectly()
        {
            (string fileName, string arguments) = ExecutableLocator.CommandLineFor("/some/claude", "-p --verbose");

            Assert.That(fileName, Is.EqualTo("/some/claude"));
            Assert.That(arguments, Is.EqualTo("-p --verbose"));
        }

        [Test]
        public void TestCommandScriptsGoThroughCmdOnWindowsOnly()
        {
            (string fileName, string arguments) = ExecutableLocator.CommandLineFor(@"C:\npm\claude.cmd", "-p");

            if (ExecutableLocator.IsWindows)
            {
                Assert.That(fileName, Does.EndWith("cmd.exe").IgnoreCase);
                Assert.That(arguments, Is.EqualTo("/d /s /c \"\"C:\\npm\\claude.cmd\" -p\""));
            }
            else
            {
                Assert.That(fileName, Is.EqualTo(@"C:\npm\claude.cmd"));
                Assert.That(arguments, Is.EqualTo("-p"));
            }
        }

        [Test]
        public void TestChildPathStartsWithFolderOfProgram()
        {
            string program = CreateProgram(otherFolder, "tool");

            string path = ExecutableLocator.ChildPath(program);

            Assert.That(path.Split(Path.PathSeparator)[0], Is.EqualTo(otherFolder));
        }
    }
}
