using System;
using System.Collections.Generic;

namespace SEE.Net.Dashboard.Model
{
    [Serializable]
    public class AnalysisVersion
    {
        public readonly int index;
        public readonly string name;
        public readonly DateTime date;
        public readonly ulong millis;
        public readonly IDictionary<string, VersionKindInfo> issueCounts;
        public readonly ToolsVersion toolsVersion;
        public readonly uint linesOfCode;
        public readonly float cloneRatio;

        public AnalysisVersion(int index, string name, DateTime date, ulong millis, 
                               IDictionary<string, VersionKindInfo> issueCounts, ToolsVersion toolsVersion, 
                               uint linesOfCode, float cloneRatio)
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

        [Serializable]
        public class ToolsVersion
        {
            public readonly string name;
            public readonly string number;
            public readonly string buildDate;

            public ToolsVersion(string name, string number, string buildDate)
            {
                this.name = name;
                this.number = number;
                this.buildDate = buildDate;
            }
        }

        [Serializable]
        public class VersionKindInfo
        {
            public readonly uint Total;
            public readonly uint Added;
            public readonly uint Removed;

            public VersionKindInfo(uint total, uint added, uint removed)
            {
                Total = total;
                Added = added;
                Removed = removed;
            }
        }
    }
}