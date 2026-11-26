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
        /// <param name="parent">The folder in which the file is created.</param>
        /// <param name="name">The program name, without extension.</param>
        /// <returns>The full path of the created file.</returns>
        private static string CreateProgram(string parent, string name)
        {
            string path = Path.Combine(parent, ExecutableLocator.IsWindows ? name + ".exe" : name);
            File.WriteAllText(path, "");
            return path;
        }

        /// <summary>
        /// A program in one of the given folders is found there.
        /// </summary>
        [Test]
        public void TestFindsProgramInGivenFolders()
        {
            string program = CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }), Is.EqualTo(program));
        }

        /// <summary>
        /// If several folders contain the program, the first of them wins.
        /// </summary>
        [Test]
        public void TestFirstFolderWins()
        {
            string first = CreateProgram(folder, "tool");
            CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }), Is.EqualTo(first));
        }

        /// <summary>
        /// A full path is returned as it is if the file exists, otherwise null.
        /// </summary>
        [Test]
        public void TestFullPathIsReturnedIfItExists()
        {
            string program = CreateProgram(folder, "tool");

            Assert.That(ExecutableLocator.Find(program, Array.Empty<string>()), Is.EqualTo(program));
            Assert.That(ExecutableLocator.Find(program + "-missing", new[] { folder }), Is.Null);
        }

        /// <summary>
        /// A missing program, a blank name and null find nothing.
        /// </summary>
        [Test]
        public void TestMissingOrBlankNameFindsNothing()
        {
            Assert.That(ExecutableLocator.Find("tool", new[] { folder }), Is.Null);
            Assert.That(ExecutableLocator.Find(" ", new[] { folder }), Is.Null);
            Assert.That(ExecutableLocator.Find(null, new[] { folder }), Is.Null);
        }

        /// <summary>
        /// Candidates rejected by the skip filter are not returned.
        /// </summary>
        [Test]
        public void TestSkippedCandidatesAreIgnored()
        {
            string skipped = CreateProgram(folder, "tool");
            string program = CreateProgram(otherFolder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { folder, otherFolder }, path => path == skipped),
                        Is.EqualTo(program));
        }

        /// <summary>
        /// Invalid and quoted folder entries do not stop the search.
        /// </summary>
        [Test]
        public void TestInvalidFolderEntriesAreTolerated()
        {
            string program = CreateProgram(folder, "tool");

            Assert.That(ExecutableLocator.Find("tool", new[] { "\0invalid", "\"" + folder + "\"" }),
                        Is.EqualTo(program));
        }

        /// <summary>
        /// Programs that are not command scripts are started directly.
        /// </summary>
        [Test]
        public void TestRealProgramsAreStartedDirectly()
        {
            (string fileName, string arguments) = ExecutableLocator.CommandLineFor("/some/claude", "-p --verbose");

            Assert.That(fileName, Is.EqualTo("/some/claude"));
            Assert.That(arguments, Is.EqualTo("-p --verbose"));
        }

        /// <summary>
        /// Command scripts are run through cmd.exe on Windows, and directly elsewhere.
        /// </summary>
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

        /// <summary>
        /// The PATH for a child process starts with the folder of the program.
        /// </summary>
        [Test]
        public void TestChildPathStartsWithFolderOfProgram()
        {
            string program = CreateProgram(otherFolder, "tool");

            string path = ExecutableLocator.ChildPath(program);

            Assert.That(path.Split(Path.PathSeparator)[0], Is.EqualTo(otherFolder));
        }
    }
}
