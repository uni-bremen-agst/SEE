using System;
using System.Text.RegularExpressions;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Represents the author of a file in a git repository.
    /// </summary>
    public class GitFileAuthor
    {
        /// <summary>
        /// A string representing the name of the author.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A string representing the email address of the author.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="GitFileAuthor"/> class from a git identifier.
        ///
        /// The <paramref name="identifier"/> must be in the format "Name &lt;email&gt;".
        /// </summary>
        /// <param name="identifier">The git identifier to create the instance from</param>
        /// <exception cref="ArgumentException">When <paramref name="identifier"/> don't match the required format</exception>
        public GitFileAuthor(string identifier)
        {
            Match match = Regex.Match(identifier, @"^(?<name>.+?)<(?<email>.+?)>$");
            if (!match.Success)
            {
                throw new ArgumentException("identifier is not a valid git author string ", nameof(identifier));
            }

            Name = match.Groups["name"].Value.Trim();
            Email = match.Groups["email"].Value.Trim();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitFileAuthor"/> class.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <param name="email">The email address of the author.</param>
        public GitFileAuthor(string name, string email)
        {
            Name = name;
            Email = email;
        }

        public override string ToString()
        {
            return $"{Name}<{Email}>";
        }
    }
}
