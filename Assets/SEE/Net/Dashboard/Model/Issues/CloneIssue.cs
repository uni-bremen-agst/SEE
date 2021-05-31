using System;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a clone.
    /// </summary>
    [Serializable]
    public class CloneIssue: Issue
    {
        /// <summary>
        /// The clone type
        /// </summary>
        public readonly int cloneType;
            
        /// <summary>
        /// The filename of the left clone fragment
        /// </summary>
        public readonly string leftPath;
            
        /// <summary>
        /// The start line number of the left clone fragment
        /// </summary>
        public readonly uint leftLine;

        /// <summary>
        /// The end line number of the left clone fragment
        /// </summary>
        public readonly uint leftEndLine;

        /// <summary>
        /// The number of lines of the left clone fragment
        /// </summary>
        public readonly uint leftLength;
            
        /// <summary>
        /// The weight of the left clone fragment
        /// </summary>
        public readonly int leftWeight;

        /// <summary>
        /// The filename of the right clone fragment
        /// </summary>
        public readonly string rightPath;
            
        /// <summary>
        /// The start line number of the right clone fragment
        /// </summary>
        public readonly uint rightLine;

        /// <summary>
        /// The end line number of the right clone fragment
        /// </summary>
        public readonly uint rightEndLine;

        /// <summary>
        /// The number of lines of the right clone fragment
        /// </summary>
        public readonly uint rightLength;
            
        /// <summary>
        /// The weight of the right clone fragment
        /// </summary>
        public readonly int rightWeight;

        public CloneIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public CloneIssue(int cloneType, string leftPath, uint leftLine, uint leftEndLine, uint leftLength, 
                          int leftWeight, string rightPath, uint rightLine, uint rightEndLine, uint rightLength,
                          int rightWeight)
        {
            this.cloneType = cloneType;
            this.leftPath = leftPath;
            this.leftLine = leftLine;
            this.leftEndLine = leftEndLine;
            this.leftLength = leftLength;
            this.leftWeight = leftWeight;
            this.rightPath = rightPath;
            this.rightLine = rightLine;
            this.rightEndLine = rightEndLine;
            this.rightLength = rightLength;
            this.rightWeight = rightWeight;
        }

        public override string ToString()
        {
            return $"{nameof(cloneType)}: {cloneType}, {nameof(leftPath)}: {leftPath}, {nameof(leftLine)}: {leftLine},"
                   + $" {nameof(leftEndLine)}: {leftEndLine}, {nameof(leftLength)}: {leftLength},"
                   + $" {nameof(leftWeight)}: {leftWeight}, {nameof(rightPath)}: {rightPath},"
                   + $" {nameof(rightLine)}: {rightLine}, {nameof(rightEndLine)}: {rightEndLine}, "
                   + $"{nameof(rightLength)}: {rightLength}, {nameof(rightWeight)}: {rightWeight}";
        }
    }
}