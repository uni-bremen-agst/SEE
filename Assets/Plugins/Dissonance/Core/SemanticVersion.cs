using System;
using Dissonance.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    [Serializable]
    public class SemanticVersion
        : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        // ReSharper disable InconsistentNaming (Justification: That's the serialization format)
        [SerializeField] private int _major;
        [SerializeField] private int _minor;
        [SerializeField] private int _patch;
        [SerializeField] private string _tag;
        // ReSharper restore InconsistentNaming

        public int Major { get { return _major; } }
        public int Minor { get { return _minor; } }
        public int Patch { get { return _patch; } }
        public string Tag { get { return _tag; } }

        //ncrunch: no coverage start (blank constructor required for for Unity deserialization)
        public SemanticVersion()
        {
        }
        //ncrunch: no coverage end

        public SemanticVersion(int major, int minor, int patch, [CanBeNull] string tag = null)
        {
            _major = major;
            _minor = minor;
            _patch = patch;
            _tag = tag;
        }

        public int CompareTo([CanBeNull] SemanticVersion other)
        {
            if (other == null)
                return 1;

            //Compare to the most significant part which is different

            if (!Major.Equals(other.Major))
                return Major.CompareTo(other.Major);

            if (!Minor.Equals(other.Minor))
                return Minor.CompareTo(other.Minor);

            if (!Patch.Equals(other.Patch))
                return Patch.CompareTo(other.Patch);

            if (Tag != other.Tag)
            {
                // versions with a prerelease tag are considered older than those without
                // i.e. `1.0.0-beta` < `1.0.0`
                if (Tag != null && other.Tag == null)
                    return -1;
                if (Tag == null && other.Tag != null)
                    return 1;

                //They both have a tag, so just order them by tag
                return string.Compare(Tag, other.Tag, StringComparison.Ordinal);
            }

            return 0;
        }

        public override string ToString()
        {
            if (Tag == null)
                return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
            
            return string.Format("{0}.{1}.{2}-{3}", Major, Minor, Patch, Tag);
        }

        public bool Equals(SemanticVersion other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((SemanticVersion)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode (Justification: Cannot be readonly for Unity serialization)
                var hashCode = _major;
                hashCode = (hashCode * 397) ^ _minor;
                hashCode = (hashCode * 397) ^ _patch;
                hashCode = (hashCode * 397) ^ (_tag != null ? _tag.GetFnvHashCode() : 0);
                return hashCode;
                // ReSharper restore NonReadonlyMemberInGetHashCode
            }
        }
    }
}
