using System;
using System.Collections.Generic;

namespace SEE.Net.Dashboard.Model
{
    /// <summary>
    /// Contains information about an issue in the source code.
    /// </summary>
    [Serializable]
    public class Issue
    {
        // A note: Due to how the JSON serializer works with inheritance, fields in here can't be readonly.
        
        /// <summary>
        /// A kind-wide Id identifying the issue across analysis versions
        /// </summary>
        public int id;

        /// <summary>
        /// In diff-queries, this indicates whether the issue is “Removed”,
        /// i.e. contained in the base-version but not any more in the current version or “Added”,
        /// i.e. it was not contained in the base-version but is contained in the current version
        /// </summary>
        public string state;

        /// <summary>
        /// Whether or not the issue is suppressed or disabled via a control comment.
        /// </summary>
        /// <remarks>
        /// This column is only available for projects where importing of suppressed issues is configured
        /// </remarks>
        public bool suppressed;

        /// <summary>
        /// The justification provided in the source-code via a control comment
        /// </summary>
        public string justification;

        /// <summary>
        /// Tags that are attached to the issue.
        /// </summary>
        public IList<IssueTag> tag;

        /// <summary>
        /// The comments that were made on the issue.
        /// </summary>
        /// <remarks>
        /// This column is not available for CSV output.
        /// This column is not returned by default and must be explicitly requested via the “columns” parameter.
        /// </remarks>
        public IList<IssueComment> comments;

        /// <summary>
        /// The dashboard users associated with the issue via VCS blaming, CI path mapping,
        /// CI user name mapping and dashboard user name mapping.
        /// </summary>
        public IList<UserRef> owners;

        protected Issue()
        {
            
        }

        public Issue(int id, string state, bool suppressed, string justification, 
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
        /// An issue tag as returned by the Issue-List API.
        /// </summary>
        [Serializable]
        public readonly struct IssueTag
        {
            /// <summary>
            /// Use this for displaying the tag.
            /// </summary>
            public readonly string tag;

            /// <summary>
            /// An RGB hex color in the form #RRGGBB directly usable by css.
            /// The colors are best suited to draw a label on bright background and to contain white letters for labeling.
            /// </summary>
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
            public readonly string username;

            /// <summary>
            /// The recommended display name of the user that wrote the comment.
            /// </summary>
            public readonly string userDisplayName;

            /// <summary>
            /// The Date when the comment was created.
            /// </summary>
            public readonly DateTime date;

            /// <summary>
            /// The Date when the comment was created for UI-display.
            /// It is formatted as a human-readable string relative to query time, e.g. 2 minutes ago.
            /// </summary>
            public readonly string displayDate;

            /// <summary>
            /// The comment text.
            /// </summary>
            public readonly string text;

            /// <summary>
            /// The id for comment deletion.
            /// When the requesting user is allowed to delete the comment,
            /// contains an id that can be used to mark the comment as deleted using another API. 
            /// </summary>
            /// <remarks>
            /// This is never set when the Comment is returned as the result of an Issue-List query.
            /// </remarks>
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
    }
}