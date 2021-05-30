using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Net.Dashboard
{
    public readonly struct DashboardVersion : IComparable<DashboardVersion>
    {
        public readonly int MajorVersion;
        public readonly int MinorVersion;
        public readonly int PatchVersion;
        public readonly int ExtraVersion;

        public enum Difference
        {
            MAJOR_OLDER, MINOR_OLDER, PATCH_OLDER, EXTRA_OLDER, MAJOR_NEWER, MINOR_NEWER, PATCH_NEWER, EXTRA_NEWER, SAME
        }

        public DashboardVersion(int majorVersion, int minorVersion, int patchVersion, int extraVersion)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            PatchVersion = patchVersion;
            ExtraVersion = extraVersion;
        }

        public DashboardVersion(string versionString)
        {
            IList<int> versionList = versionString.Split('.').Select(int.Parse).ToList();
            if (versionList.Count != 4)
            {
                throw new ArgumentException("The given string does not conform to the MAJOR.MINOR.PATCH.EXTRA "
                                            + "version format!");
            }

            MajorVersion = versionList[0];
            MinorVersion = versionList[1];
            PatchVersion = versionList[2];
            ExtraVersion = versionList[3];
        }

        public override string ToString()
        {
            return $"{MajorVersion}.{MinorVersion}.{PatchVersion}.{ExtraVersion}";
        }

        public Difference GetDifference(DashboardVersion other) => 
            CompareTo(other) switch
            { 
                // All of these refer to the OTHER one, i.e., the OTHER is newer than this one
                4 => Difference.MAJOR_NEWER,
                3 => Difference.MINOR_NEWER,
                2 => Difference.PATCH_NEWER,
                1 => Difference.EXTRA_NEWER,
                0 => Difference.SAME,
                -1 => Difference.EXTRA_OLDER,
                -2 => Difference.PATCH_OLDER,
                -3 => Difference.MINOR_OLDER,
                -4 => Difference.MAJOR_OLDER,
                _ => throw new InvalidOperationException("There appears to be an error in the CompareTo method of this class.")
            };

        public int CompareTo(DashboardVersion other)
        {
            int majorVersionComparison = MajorVersion.CompareTo(other.MajorVersion);
            if (majorVersionComparison != 0)
            {
                return majorVersionComparison*4;
            }

            int minorVersionComparison = MinorVersion.CompareTo(other.MinorVersion);
            if (minorVersionComparison != 0)
            {
                return minorVersionComparison*3;
            }

            int patchVersionComparison = PatchVersion.CompareTo(other.PatchVersion);
            if (patchVersionComparison != 0)
            {
                return patchVersionComparison*2;
            }

            return ExtraVersion.CompareTo(other.ExtraVersion);
        }
    }
}