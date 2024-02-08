namespace SEE.VCS
{
    /// <summary>
    /// The kind of version control system we support.
    /// </summary>
    internal enum VCSKind
    {
        /// <summary>
        /// No version control system.
        /// </summary>
        None = 0,

        /// <summary>
        /// The git version control system.
        /// </summary>
        Git = 1,
    }
}
