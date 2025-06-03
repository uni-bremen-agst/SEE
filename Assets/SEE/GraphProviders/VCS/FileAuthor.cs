using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Represents the author of a file in a VCS.
    /// </summary>
    /// <remarks>The author is the person who originally wrote the work, whereas the
    /// committer is the person who last applied (committed) the work.</remarks>
    [Serializable,
     HideReferenceObjectPicker]
    public class FileAuthor : IEquatable<FileAuthor>
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
        /// Creates a new instance of the <see cref="FileAuthor"/> class from a VCS identifier.
        ///
        /// The <paramref name="identifier"/> must be in the format "Name &lt;email&gt;".
        /// </summary>
        /// <param name="identifier">The git identifier to create the instance from</param>
        /// <exception cref="ArgumentException">When <paramref name="identifier"/> doesn't match the required format</exception>
        public FileAuthor(string identifier)
        {
            Match match = Regex.Match(identifier, @"^(?<name>.+?)<(?<email>.+?)>$");
            if (!match.Success)
            {
                throw new ArgumentException("Odentifier is not a valid VCS author string ", nameof(identifier));
            }

            Name = match.Groups["name"].Value.Trim();
            Email = match.Groups["email"].Value.Trim();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAuthor"/> class.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <param name="email">The email address of the author.</param>
        public FileAuthor(string name, string email)
        {
            Name = name;
            Email = email;
        }

        /// <summary>
        /// Empty constructor for serialization purposes.
        /// </summary>
        public FileAuthor()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Determines whether the specified <see cref="FileAuthor"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The instance to check for equality.</param>
        /// <returns>True if they are equal, false otherwise.</returns>
        public bool Equals(FileAuthor other)
        {
            return Name == other.Name && Email == other.Email;
        }

        /// <summary>
        /// Generates a hash code based on the <see cref="Name"/> and <see cref="Email"/> properties.
        /// </summary>
        /// <returns>
        /// A hash code that represents the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Email);
        }

        /// <summary>
        /// Returns a string representation of the <see cref="FileAuthor"/> instance,
        /// combining the author's name and email in the format "Name&lt;Email&gt;".
        /// </summary>
        /// <returns>A string representation of the author's name and email.</returns>
        public override string ToString()
        {
            return $"{Name}<{Email}>";
        }

        #region Config I/O

        /// <summary>
        /// The label of the <see cref="Name"/> in the configuration file.
        /// </summary>
        private const string nameLabel = "Name";

        /// <summary>
        /// The label of the <see cref="Email"/> in the configuration file.
        /// </summary>
        private const string emailLabel = "Email";

        internal void Save(ConfigWriter writer, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(Name, nameLabel);
            writer.Save(Email, emailLabel);
            writer.EndGroup();
        }

        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                return Restore(values);
            }
            else
            {
                return false;
            }
        }

        public bool Restore(Dictionary<string, object> attributes)
        {
            bool result = true;
            result &= ConfigIO.Restore(attributes, nameLabel, ref Name);
            result &= ConfigIO.Restore(attributes, emailLabel, ref Email);
            return result;
        }

        #endregion Config I/O
    }
}
