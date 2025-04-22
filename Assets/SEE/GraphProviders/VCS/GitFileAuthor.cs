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
