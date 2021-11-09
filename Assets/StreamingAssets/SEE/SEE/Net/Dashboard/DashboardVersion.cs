using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Represents a version of the Axivion dashboard, which consists of a <see cref="MajorVersion"/>, a
    /// <see cref="MinorVersion"/>, a <see cref="PatchVersion"/> and an <see cref="ExtraVersion"/>.
    /// This object can be constructed from a parseable dashboard version number string.
    /// </summary>
    public readonly struct DashboardVersion : IComparable<DashboardVersion>
    {
        /// <summary>
        /// First component of the version string. Indicates major changes.
        /// </summary>
        public readonly int MajorVersion;
        
        /// <summary>
        /// Second component of the version string. Indicates minor changes.
        /// </summary>
        public readonly int MinorVersion;
        
        /// <summary>
        /// Third component of the version string. Indicates small changes and bugfixes.
        /// </summary>
        public readonly int PatchVersion;
        
        /// <summary>
        /// Fourth component of the version string. Indicates very small changes.
        /// </summary>
        public readonly int ExtraVersion;

        /// <summary>
        /// Latest supported version of the Axivion Dashboard.
        /// Should be updated when new (supported and tested) versions come out.
        /// </summary>
        public static readonly DashboardVersion SupportedVersion = new DashboardVersion(7,1,5,6367);

        /// <summary>
        /// Represents the difference of another version in comparison to this one.
        /// This will always represent the <i>biggest</i> difference, e.g. for 7.1.5 and 7.0.2 it will
        /// be <see cref="MINOR_OLDER"/> .
        /// </summary>
        public enum Difference
        {
            /// <summary>
            /// When the major version of the other version is smaller than this one.
            /// </summary>
            MAJOR_OLDER, 
            
            /// <summary>
            /// When the minor version of the other version is smaller than this one.
            /// </summary>
            MINOR_OLDER,
            
            /// <summary>
            /// When the patch version of the other version is smaller than this one.
            /// </summary>
            PATCH_OLDER,
            
            /// <summary>
            /// When the extra version of the other version is smaller than this one.
            /// </summary>
            EXTRA_OLDER, 
            
            /// <summary>
            /// When the major version of the other version is bigger than this one.
            /// </summary>
            MAJOR_NEWER,
            
            /// <summary>
            /// When the minor version of the other version is bigger than this one.
            /// </summary>
            MINOR_NEWER,
            
            /// <summary>
            /// When the patch version of the other version is bigger than this one.
            /// </summary>
            PATCH_NEWER,
            
            /// <summary>
            /// When the extra version of the other version is bigger than this one.
            /// </summary>
            EXTRA_NEWER,
            
            /// <summary>
            /// When both versions are equal.
            /// </summary>
            EQUAL
        }

        /// <summary>
        /// Constructs a new <see cref="DashboardVersion"/> from the given version values.
        /// </summary>
        /// <param name="majorVersion">The major version.</param>
        /// <param name="minorVersion">The minor version.</param>
        /// <param name="patchVersion">The patch version.</param>
        /// <param name="extraVersion">The extra version.</param>
        public DashboardVersion(int majorVersion, int minorVersion, int patchVersion, int extraVersion)
        {
            if (new [] {majorVersion, minorVersion, patchVersion, extraVersion}.Any(x => x < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            PatchVersion = patchVersion;
            ExtraVersion = extraVersion;
        }

        /// <summary>
        /// Constructs a <see cref="DashboardVersion"/> out of the given <paramref name="versionString"/>.
        /// The version string must be of the format MAJOR.MINOR.PATCH.EXTRA, for example "7.1.5.8363" would conform.
        /// </summary>
        /// <param name="versionString">Version string of the format "MAJOR.MINOR.PATCH.EXTRA".</param>
        /// <exception cref="ArgumentException">If the given <paramref name="versionString"/> does not conform
        /// to the version format.</exception>
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

        /// <summary>
        /// Will return the difference from this version to the supported version.
        /// For example, if the difference is <see cref="Difference.MAJOR_NEWER"/>, this means that the
        /// supported version has a newer major version.
        /// </summary>
        public Difference DifferenceToSupportedVersion => GetDifference(SupportedVersion);

        /// <summary>
        /// Returns the difference from this version to the given <paramref name="other"/> version.
        /// For example, if the difference is <see cref="Difference.MAJOR_NEWER"/>, this means that the
        /// other version has a newer major version.
        /// </summary>
        /// <param name="other">The version with which this one shall be compared.</param>
        /// <returns>The difference between this version and <paramref name="other"/>.</returns>
        public Difference GetDifference(DashboardVersion other) => 
            CompareTo(other) switch
            { 
                // All of these refer to the OTHER one, i.e., the OTHER is newer than this one
                4 => Difference.MAJOR_NEWER,
                3 => Difference.MINOR_NEWER,
                2 => Difference.PATCH_NEWER,
                1 => Difference.EXTRA_NEWER,
                0 => Difference.EQUAL,
                -1 => Difference.EXTRA_OLDER,
                -2 => Difference.PATCH_OLDER,
                -3 => Difference.MINOR_OLDER,
                -4 => Difference.MAJOR_OLDER,
                _ => throw new InvalidOperationException("There appears to be an error in the CompareTo method of this class.")
            };

        /// <summary>
        /// Comparator for this struct.
        /// </summary>
        /// <param name="other">The version this one shall be compared to.</param>
        public int CompareTo(DashboardVersion other)
        {
            int majorVersionComparison = MajorVersion.CompareTo(other.MajorVersion);
            if (majorVersionComparison != 0)
            {
                return majorVersionComparison * 4;
            }

            int minorVersionComparison = MinorVersion.CompareTo(other.MinorVersion);
            if (minorVersionComparison != 0)
            {
                return minorVersionComparison * 3;
            }

            int patchVersionComparison = PatchVersion.CompareTo(other.PatchVersion);
            if (patchVersionComparison != 0)
            {
                return patchVersionComparison * 2;
            }

            return ExtraVersion.CompareTo(other.ExtraVersion);
        }

        /// <summary>
        /// Returns a human-readable string representation of this struct.
        /// The resulting string can be parsed by the constructor of this struct.
        /// </summary>
        /// <returns>A human-readable string representation of this struct.</returns>
        public override string ToString()
        {
            return $"{MajorVersion}.{MinorVersion}.{PatchVersion}.{ExtraVersion}";
        }
    }
}