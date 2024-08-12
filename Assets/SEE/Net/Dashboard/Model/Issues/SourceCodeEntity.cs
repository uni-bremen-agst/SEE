using System;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An entity in the source code, consisting of a <see cref="Path"/>, a <see cref="Line"/>,
    /// an optional <see cref="EndLine"/> and an optional <see cref="Content"/>.
    /// The <see cref="Content"/> will usually be the "entity" as described in the Axivion
    /// Dashboard documentation.
    /// </summary>
    public class SourceCodeEntity
    {
        /// <summary>
        /// The path of the file of the entity.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// The line number of the entity.
        /// If <see cref="EndLine"/> is specified, this will be the beginning of the line
        /// range of this entity.
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// The optional end line number of the entity.
        /// While the documentation doesn't specify this, it is assumed to be inclusive.
        /// </summary>
        public readonly int? EndLine;

        /// <summary>
        /// The optional content of the entity.
        /// </summary>
        public readonly string Content;

        public SourceCodeEntity(string path, int line, int? endLine = null, string content = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Line = line;
            EndLine = endLine;
            Content = string.IsNullOrEmpty(content) ? null : content;
        }
    }
}
