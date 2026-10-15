using LibGit2Sharp;
using SEE.GraphProviders.VCS;
using System;
using System.Collections.Generic;
using System.IO;

namespace SEE.VCS
{
    /// <summary>
    /// The mapping stated by a .mailmap file: the canonical identity, that is
    /// the name and the address, of an author whose commits record them under
    /// more than one. LibGit2Sharp 0.29 exposes nothing of libgit2's own
    /// mailmap support, so the file is read here.
    /// </summary>
    /// <remarks>
    /// The file lies at the root of a repository and is git's own way of
    /// stating such a mapping, honoured by <c>git log</c>, <c>git shortlog</c>
    /// and <c>git blame</c> alike. It supersedes <see cref="AuthorMapping"/>,
    /// which states the same thing in a configuration of SEE's own instead.
    ///
    /// All four forms git defines are accepted, each with the effect git gives
    /// it. <c>&lt;proper@email&gt; &lt;commit@email&gt;</c>, the one naming no
    /// name, replaces the address alone and leaves the name as the commit
    /// records it; the other three name a canonical name as well.
    /// </remarks>
    internal class Mailmap
    {
        /// <summary>
        /// The name git gives the file, which lies at the root of a repository.
        /// </summary>
        internal const string Filename = ".mailmap";

        /// <summary>
        /// Maps the address recorded in a commit onto the canonical name of
        /// its author.
        /// </summary>
        private readonly IDictionary<string, FileAuthor> byEmail
            = new Dictionary<string, FileAuthor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps the name and the address recorded in a commit, in the form
        /// produced by <see cref="Key"/>, onto the canonical name of its
        /// author. Takes precedence over <see cref="byEmail"/>.
        /// </summary>
        private readonly IDictionary<string, FileAuthor> byNameAndEmail
            = new Dictionary<string, FileAuthor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps the address recorded in a commit onto the canonical address of
        /// its author, the name being left as the commit records it. This is
        /// what an entry naming no name states. Consulted only where neither
        /// <see cref="byNameAndEmail"/> nor <see cref="byEmail"/> has anything
        /// to say, both of which name a name and so say more.
        /// </summary>
        private readonly IDictionary<string, string> addressOnly
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The mapping given by the .mailmap file at <paramref name="path"/>.
        /// If there is no such file, the result maps nothing, which leaves
        /// every author named as their commits name them.
        /// </summary>
        /// <param name="path">The path of the .mailmap file.</param>
        /// <returns>The mapping.</returns>
        internal static Mailmap Read(string path)
        {
            Mailmap result = new();
            if (!File.Exists(path))
            {
                return result;
            }
            foreach (string line in File.ReadAllLines(path))
            {
                result.Add(line);
            }
            return result;
        }

        /// <summary>
        /// Adds the entry <paramref name="line"/> states, if it states one.
        /// Comments and blank lines are ignored.
        /// </summary>
        /// <param name="line">The line of a .mailmap file to be added.</param>
        private void Add(string line)
        {
            int comment = line.IndexOf('#');
            string entry = (comment >= 0 ? line.Substring(0, comment) : line).Trim();
            if (entry.Length == 0)
            {
                return;
            }

            // An entry consists of up to two addresses in angle brackets,
            // each preceded by a name that may be empty.
            List<string> names = new();
            List<string> emails = new();
            int cursor = 0;
            while (true)
            {
                int open = entry.IndexOf('<', cursor);
                if (open < 0)
                {
                    break;
                }
                int close = entry.IndexOf('>', open + 1);
                if (close < 0)
                {
                    break;
                }
                names.Add(entry.Substring(cursor, open - cursor).Trim());
                emails.Add(entry.Substring(open + 1, close - open - 1).Trim());
                cursor = close + 1;
            }

            if (emails.Count == 1 && names[0].Length > 0)
            {
                // "Proper Name <commit@email>": the name is replaced, the
                // address is the one the commit records and stays.
                byEmail[emails[0]] = new FileAuthor(names[0], emails[0]);
            }
            else if (emails.Count == 2)
            {
                if (names[0].Length == 0)
                {
                    // "<proper@email> <commit@email>": the address is replaced
                    // and the name is left as the commit records it.
                    addressOnly[emails[1]] = emails[0];
                }
                else if (names[1].Length > 0)
                {
                    // "Proper Name <proper@email> Commit Name <commit@email>".
                    byNameAndEmail[Key(names[1], emails[1])] = new FileAuthor(names[0], emails[0]);
                }
                else
                {
                    // "Proper Name <proper@email> <commit@email>".
                    byEmail[emails[1]] = new FileAuthor(names[0], emails[0]);
                }
            }
        }

        /// <summary>
        /// The canonical identity of <paramref name="author"/>, or the name
        /// and address their commit records if this mapping has nothing to
        /// say about them.
        /// </summary>
        /// <param name="author">The author whose identity is asked for.</param>
        /// <returns>The canonical identity.</returns>
        internal FileAuthor AuthorOf(Signature author)
        {
            if (byNameAndEmail.TryGetValue(Key(author.Name, author.Email), out FileAuthor byBoth))
            {
                return byBoth;
            }
            if (byEmail.TryGetValue(author.Email, out FileAuthor byAddress))
            {
                return byAddress;
            }
            if (addressOnly.TryGetValue(author.Email, out string canonical))
            {
                return new FileAuthor(author.Name, canonical);
            }
            return new FileAuthor(author.Name, author.Email);
        }

        /// <summary>
        /// The key under which <see cref="byNameAndEmail"/> stores an entry
        /// for <paramref name="name"/> and <paramref name="email"/>.
        /// </summary>
        /// <param name="name">The name recorded in a commit.</param>
        /// <param name="email">The address recorded in a commit.</param>
        /// <returns>The key.</returns>
        private static string Key(string name, string email)
        {
            return $"{name} <{email}>";
        }
    }
}
