namespace SEE.VCS
{
    /// <summary>
    /// Thrown in cases where a commit ID cannot be resolved in a version control system.
    /// </summary>
    public class UnknownCommitID : System.Exception
    {
        /// <summary>
        /// Constructor allowing to specify a <paramref name="message"/>.
        /// </summary>
        /// <param name="message">Descriptive message for the exception.</param>
        public UnknownCommitID(string message) : base(message)
        {
        }
    }
}
