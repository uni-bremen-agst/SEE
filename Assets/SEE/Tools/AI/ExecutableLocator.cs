using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SEE.Tools.AI
{
    /// <summary>
    /// Finds command-line programs such as Claude Code or Python on Windows, macOS and Linux.
    ///
    /// Unity started from the Hub, the Dock or a desktop launcher often gets a minimal PATH
    /// (on macOS just <c>/usr/bin:/bin:/usr/sbin:/sbin</c>), so programs installed with Homebrew,
    /// npm, nvm, conda or the Claude Code installer are not found on PATH alone. This class
    /// therefore also searches the folders those installers use, and builds a PATH for child
    /// processes that contains them (e.g. an npm-installed <c>claude</c> needs <c>node</c>).
    /// </summary>
    public static class ExecutableLocator
    {
        /// <summary>
        /// Whether we run on Windows. Determined by the path separator, so it also works outside
        /// of Unity (e.g. in tests) and does not depend on Unity's platform defines.
        /// </summary>
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';

        /// <summary>
        /// Returns the full path of the program <paramref name="nameOrPath"/>: the path itself if
        /// it names an existing file, otherwise the first match in <paramref name="folders"/>.
        /// On Windows, the extensions .exe, .cmd and .bat are tried as well; installers there
        /// usually create one of these.
        /// </summary>
        /// <param name="nameOrPath">A program name such as <c>claude</c>, or a full path.</param>
        /// <param name="folders">The folders to search, in order.</param>
        /// <param name="skip">Optional filter; candidates for which it returns true are ignored.</param>
        /// <returns>The full path, or null if the program was not found.</returns>
        public static string Find(string nameOrPath, IEnumerable<string> folders, Func<string, bool> skip = null)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
            {
                return null;
            }
            string trimmed = nameOrPath.Trim().Trim('"');
            if (Path.IsPathRooted(trimmed))
            {
                return File.Exists(trimmed) ? trimmed : null;
            }
            foreach (string folder in folders ?? Enumerable.Empty<string>())
            {
                foreach (string name in CandidateNames(trimmed))
                {
                    string candidate;
                    try
                    {
                        candidate = Path.Combine(folder.Trim().Trim('"'), name);
                    }
                    catch (ArgumentException)
                    {
                        continue; // invalid characters in a PATH entry
                    }
                    if (File.Exists(candidate) && (skip == null || !skip(candidate)))
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Finds Claude Code's command-line program.
        /// </summary>
        /// <param name="configured">The configured name or full path, usually <c>claude</c>.</param>
        /// <returns>The full path, or null if Claude Code is not installed.</returns>
        public static string FindClaude(string configured = "claude")
        {
            return Find(string.IsNullOrWhiteSpace(configured) ? "claude" : configured, SearchFolders());
        }

        /// <summary>
        /// Finds a Python 3 interpreter: <c>python3</c> or <c>python</c>; on Windows <c>python</c>
        /// or the <c>py</c> launcher. The Microsoft Store placeholders in <c>WindowsApps</c> are
        /// skipped, because they open the Store instead of running Python.
        /// </summary>
        /// <returns>The full path, or null if no Python was found.</returns>
        public static string FindPython()
        {
            string[] names = IsWindows ? new[] { "python", "py" } : new[] { "python3", "python" };
            IList<string> folders = SearchFolders();
            return names.Select(name => Find(name, folders, IsStorePlaceholder)).FirstOrDefault(path => path != null);
        }

        /// <summary>
        /// The folders to search: the entries of PATH first, then the usual install folders of
        /// the current operating system. Duplicates and missing folders are removed.
        /// </summary>
        /// <returns>Existing folders, in search order.</returns>
        public static IList<string> SearchFolders()
        {
            IEnumerable<string> path = (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(Path.PathSeparator);
            return path.Concat(KnownFolders())
                       .Where(folder => !string.IsNullOrWhiteSpace(folder))
                       .Select(folder => folder.Trim().Trim('"'))
                       .Where(SafeDirectoryExists)
                       .Distinct(IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                       .ToList();
        }

        /// <summary>
        /// A PATH value for a child process: the folder of <paramref name="executable"/> first,
        /// then <see cref="SearchFolders"/>. This way, a program started from a minimal PATH still
        /// finds the tools it needs itself, e.g. <c>node</c> for an npm-installed Claude Code.
        /// </summary>
        /// <param name="executable">Full path of the program to be started; may be null.</param>
        /// <returns>The PATH value.</returns>
        public static string ChildPath(string executable)
        {
            IEnumerable<string> first = string.IsNullOrEmpty(executable)
                ? Enumerable.Empty<string>()
                : new[] { Path.GetDirectoryName(executable) };
            return string.Join(Path.PathSeparator.ToString(),
                               first.Concat(SearchFolders())
                                    .Distinct(IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal));
        }

        /// <summary>
        /// The program and arguments to start <paramref name="executable"/> with
        /// <c>UseShellExecute = false</c> (needed to redirect its input and output).
        /// On Windows, a command script (.cmd or .bat, e.g. the <c>claude.cmd</c> that npm creates)
        /// cannot be started directly that way; it is run through <c>cmd.exe /d /s /c</c> instead.
        /// Everywhere else, and for real programs, both are returned unchanged.
        /// </summary>
        /// <param name="executable">Full path of the program or script.</param>
        /// <param name="arguments">The arguments, already quoted as needed.</param>
        /// <returns>The file name and the arguments for <see cref="System.Diagnostics.ProcessStartInfo"/>.</returns>
        public static (string FileName, string Arguments) CommandLineFor(string executable, string arguments)
        {
            string extension = Path.GetExtension(executable ?? "");
            bool isScript = extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
                            || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase);
            if (!IsWindows || !isScript)
            {
                return (executable, arguments);
            }
            string shell = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrEmpty(shell))
            {
                shell = "cmd.exe";
            }
            // With /s, cmd removes only the outermost quotes, so the quoted script path and
            // the arguments inside them reach the script unchanged. /d skips AutoRun commands.
            return (shell, $"/d /s /c \"\"{executable}\" {arguments}\"");
        }

        /// <summary>
        /// The file names to try for <paramref name="name"/>: on Windows with .exe, .cmd and .bat
        /// (unless it already has an extension), elsewhere just the name.
        /// </summary>
        /// <param name="name">A program name.</param>
        /// <returns>The candidate file names, in order.</returns>
        private static IEnumerable<string> CandidateNames(string name)
        {
            if (IsWindows && !Path.HasExtension(name))
            {
                return new[] { name + ".exe", name + ".cmd", name + ".bat", name };
            }
            return new[] { name };
        }

        /// <summary>
        /// The folders the usual installers of Claude Code, Node.js and Python use on the current
        /// operating system. Folders that do not exist are filtered out later.
        /// </summary>
        /// <returns>Candidate folders.</returns>
        private static IEnumerable<string> KnownFolders()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (IsWindows)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                List<string> folders = new()
                {
                    Path.Combine(home, ".local", "bin"),                       // Claude Code installer
                    Path.Combine(localAppData, "Microsoft", "WinGet", "Links"),
                    Path.Combine(appData, "npm"),                              // npm install -g
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs"),
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows) // py.exe
                };
                folders.AddRange(SubfoldersOf(Path.Combine(localAppData, "Microsoft", "WinGet", "Packages"),
                                              "Anthropic.ClaudeCode*"));
                folders.AddRange(SubfoldersOf(Path.Combine(localAppData, "Programs", "Python"), "Python3*")
                                     .OrderByDescending(folder => folder, StringComparer.OrdinalIgnoreCase));
                return folders;
            }

            List<string> unix = new()
            {
                Path.Combine(home, ".local", "bin"),       // Claude Code installer, pipx
                Path.Combine(home, ".claude", "local"),    // older Claude Code installer
                "/opt/homebrew/bin",                       // Homebrew on Apple silicon
                "/usr/local/bin",                          // Homebrew on Intel, manual installs
                Path.Combine(home, ".npm-global", "bin"),  // npm with a user prefix
                Path.Combine(home, ".bun", "bin"),
                Path.Combine(home, ".volta", "bin"),
                Path.Combine(home, "bin"),
                Path.Combine(home, ".pyenv", "shims"),
                Path.Combine(home, "anaconda3", "bin"),
                Path.Combine(home, "miniconda3", "bin"),
                "/snap/bin",                               // Ubuntu snaps
                "/usr/bin",
                "/bin"
            };
            // Node versions installed with nvm; newest version first.
            unix.InsertRange(5, SubfoldersOf(Path.Combine(home, ".nvm", "versions", "node"), "*")
                                .OrderByDescending(folder => folder, StringComparer.Ordinal)
                                .Select(folder => Path.Combine(folder, "bin")));
            return unix;
        }

        /// <summary>
        /// The subfolders of <paramref name="folder"/> matching <paramref name="pattern"/>, or none
        /// if <paramref name="folder"/> does not exist or cannot be read.
        /// </summary>
        /// <param name="folder">The parent folder.</param>
        /// <param name="pattern">A search pattern such as <c>Python3*</c>.</param>
        /// <returns>The matching subfolders.</returns>
        private static IEnumerable<string> SubfoldersOf(string folder, string pattern)
        {
            try
            {
                return Directory.Exists(folder) ? Directory.GetDirectories(folder, pattern) : Array.Empty<string>();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Whether <paramref name="folder"/> exists; false for invalid paths.
        /// </summary>
        /// <param name="folder">A folder path.</param>
        /// <returns>True if the folder exists.</returns>
        private static bool SafeDirectoryExists(string folder)
        {
            try
            {
                return Directory.Exists(folder);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether <paramref name="path"/> is one of the Microsoft Store placeholders for Python,
        /// which open the Store instead of running Python.
        /// </summary>
        /// <param name="path">A candidate path.</param>
        /// <returns>True for a Store placeholder.</returns>
        private static bool IsStorePlaceholder(string path)
        {
            return IsWindows && path.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
