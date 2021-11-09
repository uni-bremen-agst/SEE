using System;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An entity in the source code, consisting of a <see cref="path"/>, a <see cref="line"/>,
    /// an optional <see cref="endLine"/> and an optional <see cref="content"/>.
    /// The <see cref="content"/> will usually be the "entity" as described in the Axivion Dashboard documentation.
    /// </summary>
    public class SourceCodeEntity
    {
        /// <summary>
        /// The path of the file of the entity.
        /// </summary>
        public readonly string path;

        /// <summary>
        /// The line number of the entity.
        /// If <see cref="endLine"/> is specified, this will be the beginning of the line range of this entity.
        /// </summary>
        public readonly int line;

        /// <summary>
        /// The optional end line number of the entity.
        /// While the documentation doesn't specify this, it is assumed to be inclusive.
        /// </summary>
        public readonly int? endLine;

        /// <summary>
        /// The optional content of the entity.
        /// </summary>
        public readonly string content;

        public SourceCodeEntity(string path, int line, int? endLine = null, string content = null)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.line = line;
            this.endLine = endLine;
            this.content = string.IsNullOrEmpty(content) ? null : content;
        }
    }
}