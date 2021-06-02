using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model
{
    /// <summary>
    /// Describes the version of analysis data of a certain Project.
    /// </summary>
    [Serializable]
    public class AnalysisVersion
    {
        /// <summary>
        /// The 0-based index of all the known analysis versions of a project.
        /// The version with index 0 never contains actual analysis data,
        /// but always refers to a fictional version without any issues that happened before version 1.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int index;

        /// <summary>
        /// The display-name of a version.
        /// Don’t expect this to be parseable in any way but use it to represent the version
        /// as it may contain descriptive information like a version control system tag name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string name;

        /// <summary>
        /// The date of this version containing time zone information.
        /// This is the preferred way of referring to a specific version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly DateTime date;

        /// <summary>
        /// The unix-representation of date without time zone information (always UTC)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly ulong millis;

        /// <summary>
        /// For every Issue Kind contains some Issue counts.
        /// Namely the Total count, as well as the newly Added and newly Removed issues in comparison with
        /// the version before. N.B. The Bauhaus Version used to analyze the project must be at least
        /// 6.5.0 in order for these values to be available.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly IDictionary<string, VersionKindCount> issueCounts;

        /// <summary>
        /// Version information of the Axivion Suite used to do this analysis run.
        /// Note that this field is only available when the analysis was done with at least version 6.5.0.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly ToolsVersion toolsVersion;

        /// <summary>
        /// The total lines of code of the project at the current version if available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint? linesOfCode;

        /// <summary>
        /// The clone ratio of the project at the current version if available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly float? cloneRatio;

        public AnalysisVersion(int index, string name, DateTime date, ulong millis,
                               IDictionary<string, VersionKindCount> issueCounts, ToolsVersion toolsVersion,
                               uint? linesOfCode, float? cloneRatio)
        {
            this.index = index;
            this.name = name;
            this.date = date;
            this.millis = millis;
            this.issueCounts = issueCounts;
            this.toolsVersion = toolsVersion;
            this.linesOfCode = linesOfCode;
            this.cloneRatio = cloneRatio;
        }

        /// <summary>
        /// Refers to a specific version of the Axivion Suite.
        /// </summary>
        [Serializable]
        public class ToolsVersion
        {
            /// <summary>
            /// Version number for display purposes.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string name;

            /// <summary>
            /// Parseable, numeric version number suitable for version comparisons.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string number;

            /// <summary>
            /// Build date.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly DateTime buildDate;

            public ToolsVersion(string name, string number, DateTime buildDate)
            {
                this.name = name;
                this.number = number;
                this.buildDate = buildDate;
            }
        }

        /// <summary>
        /// Kind-specific issue count statistics that are cheaply available.
        /// </summary>
        [Serializable]
        public class VersionKindCount
        {
            /// <summary>
            /// The number of issues of a kind in a version.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly uint Total;

            /// <summary>
            /// The number of issues of a kind present in a version that were not present in the previous version.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly uint Added;

            /// <summary>
            /// The number of issues of a kind that were present in the previous version
            /// and are not present in the current version any more.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly uint Removed;

            public VersionKindCount(uint total, uint added, uint removed)
            {
                Total = total;
                Added = added;
                Removed = removed;
            }
        }
    }
}