using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Represents the author of a file in a git repository.
    /// </summary>
    [Serializable]
    public class GitFileAuthor
    {
        /// <summary>
        /// A string representing the name of the author.
        /// </summary>
        [SerializeField]
        public string Name;

        /// <summary>
        /// A string representing the email address of the author.
        /// </summary>
        [SerializeField]
        public string Email;

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

        /// <summary>
        /// Empty constructor for serialization purposes.
        /// </summary>
        GitFileAuthor()
        {
        }

        /// <summary>
        /// Returns a string representation of the <see cref="GitFileAuthor"/> instance,
        /// combining the author's name and email in the format "Name&lt;Email&gt;".
        /// </summary>
        /// <returns>A string representation of the author's name and email.</returns>
        public override string ToString()
        {
            return $"{Name}<{Email}>";
        }
    }
}
