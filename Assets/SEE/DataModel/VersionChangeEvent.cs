using System;

namespace SEE.DataModel
{
    /// <summary>
    /// An event representing a new version being introduced.
    /// Events following this one will have the new <see cref="VersionId"/>, while events before this
    /// (up until the last <see cref="VersionChangeEvent"/>) will have <see cref="oldVersion"/>.
    /// </summary>
    public class VersionChangeEvent : GraphEvent
    {
        /// <summary>
        /// The version before this one.
        /// </summary>
        private readonly Guid oldVersion;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="newVersion">New version ID.</param>
        /// <param name="oldVersion">Old version ID.</param>
        public VersionChangeEvent(Guid newVersion, Guid oldVersion) : base(newVersion)
        {
            this.oldVersion = oldVersion;
        }

        /// <summary>
        /// Returns a description of the version change event naming the old and new version IDs.
        /// </summary>
        /// <returns>A string describing the version change.</returns>
        protected override string Description() => $"Changed version from {oldVersion} to {VersionId}.";
    }
}