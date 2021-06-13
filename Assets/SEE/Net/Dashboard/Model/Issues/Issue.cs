using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// Contains information about an issue in the source code.
    /// </summary>
    [Serializable]
    public abstract class Issue
    {
        // A note: Due to how the JSON serializer works with inheritance, fields in here can't be readonly.

        /// <summary>
        /// Represents the state of an issue.
        /// </summary>
        public enum IssueState
        {
            added, changed, removed // note: names must be lowercase for the serialization to work
        }

        /// <summary>
        /// The issue kind.
        /// </summary>
        public enum IssueKind
        {
            // Note: The abbreviations have to be used here instead of the full name, otherwise serialization
            // won't work.

            /// <summary>
            /// Special fallback value, used if issue kind could not be determined.
            /// </summary>
            Unknown,

            /// <summary>
            /// Architecture violations.
            /// </summary>
            AV,

            /// <summary>
            /// Clones.
            /// </summary>
            CL,

            /// <summary>
            /// Cycles.
            /// </summary>
            CY,

            /// <summary>
            /// Dead Entities.
            /// </summary>
            DE,

            /// <summary>
            /// Metric Violations.
            /// </summary>
            MV,

            /// <summary>
            /// Style Violations.
            /// </summary>
            SV
        }


        /// <summary>
        /// A kind-wide Id identifying the issue across analysis versions
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int id;

        /// <summary>
        /// In diff-queries, this indicates whether the issue is “Removed”,
        /// i.e. contained in the base-version but not any more in the current version or “Added”,
        /// i.e. it was not contained in the base-version but is contained in the current version
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Default)]
        public IssueState state;

        /// <summary>
        /// Whether or not the issue is suppressed or disabled via a control comment.
        /// </summary>
        /// <remarks>
        /// This column is only available for projects where importing of suppressed issues is configured
        /// </remarks>
        [JsonProperty(Required = Required.Default)]
        public bool suppressed;

        /// <summary>
        /// The justification provided in the source-code via a control comment
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public string justification;

        /// <summary>
        /// Tags that are attached to the issue.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public IList<IssueTag> tag;

        /// <summary>
        /// The comments that were made on the issue.
        /// </summary>
        /// <remarks>
        /// This column is not available for CSV output.
        /// This column is not returned by default and must be explicitly requested via the “columns” parameter.
        /// </remarks>
        [JsonProperty(Required = Required.Default)]
        public IList<IssueComment> comments;

        /// <summary>
        /// The dashboard users associated with the issue via VCS blaming, CI path mapping,
        /// CI user name mapping and dashboard user name mapping.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public IList<UserRef> owners;


        protected Issue()
        {
            // Necessary for inheritance with Newtonsoft.Json to work properly
        }

        public Issue(int id, IssueState state, bool suppressed, string justification,
                     IList<IssueTag> tag, IList<IssueComment> comments, IList<UserRef> owners)
        {
            this.id = id;
            this.state = state;
            this.suppressed = suppressed;
            this.justification = justification;
            this.tag = tag;
            this.comments = comments;
            this.owners = owners;
        }
        
        /// <summary>
        /// Number of lines to wrap the string in <see cref="ToDisplayString"/> at.
        /// </summary>
        protected const int WRAP_AT = 60;

        /// <summary>
        /// Returns a string suitable for display in a TextMeshPro which describes this issue.
        /// TextMeshPro's rich tags are used in here, so the string shouldn't be displayed elsewhere.
        /// </summary>
        /// <returns>A string describing this issue which is suitable for display in a TextMeshPro</returns>
        public virtual async UniTask<string> ToDisplayString() { return "<b>TODO</b>"; }
        //TODO: This should be an abstract method

        /// <summary>
        /// An issue tag as returned by the Issue-List API.
        /// </summary>
        [Serializable]
        public readonly struct IssueTag
        {
            /// <summary>
            /// Use this for displaying the tag.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string tag;

            /// <summary>
            /// An RGB hex color in the form #RRGGBB directly usable by css.
            /// The colors are best suited to draw a label on bright background and to contain white letters for labeling.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string color;

            public IssueTag(string tag, string color)
            {
                this.tag = tag;
                this.color = color;
            }
        }

        /// <summary>
        /// Describes an issue comment.
        /// </summary>
        [Serializable]
        public readonly struct IssueComment
        {
            /// <summary>
            /// The loginname of the user that created the comment.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string username;

            /// <summary>
            /// The recommended display name of the user that wrote the comment.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string userDisplayName;

            /// <summary>
            /// The Date when the comment was created.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly DateTime date;

            /// <summary>
            /// The Date when the comment was created for UI-display.
            /// It is formatted as a human-readable string relative to query time, e.g. 2 minutes ago.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string displayDate;

            /// <summary>
            /// The comment text.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string text;

            /// <summary>
            /// The id for comment deletion.
            /// When the requesting user is allowed to delete the comment,
            /// contains an id that can be used to mark the comment as deleted using another API. 
            /// </summary>
            /// <remarks>
            /// This is never set when the Comment is returned as the result of an Issue-List query.
            /// </remarks>
            [JsonProperty(Required = Required.Default)]
            public readonly string commentDeletionId;

            public IssueComment(string username, string userDisplayName, DateTime date, 
                                string displayDate, string text, string commentDeletionId)
            {
                this.username = username;
                this.userDisplayName = userDisplayName;
                this.date = date;
                this.displayDate = displayDate;
                this.text = text;
                this.commentDeletionId = commentDeletionId;
            }
        }
        
        
        // In this region are properties not defined in the Dashboard spec.
        // These will either be derived from existing fields or defined statically.
        #region Additional Properties
        
        /// <summary>
        /// The kind of issue this is.
        /// </summary>
        public abstract IssueKind kind { get; }

        /// <summary>
        /// The entities this issue references.
        /// </summary>
        public abstract IEnumerable<SourceCodeEntity> Entities { get; }

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
        
        #endregion

    }
}