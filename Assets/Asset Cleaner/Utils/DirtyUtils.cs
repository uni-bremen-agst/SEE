using System.Runtime.CompilerServices;

namespace Asset_Cleaner {
    static class DirtyUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1>(in T1 v1) {
            var hash = v1.GetHashCode();
            hash = (hash * 397);
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2>(in T1 v1, in T2 v2) {
            var hash = v1.GetHashCode();
            hash = (hash * 397) ^ v2.GetHashCode();
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3>(in T1 v1, in T2 v2, in T3 v3) {
            var hash = v1.GetHashCode();
            hash = (hash * 397) ^ v2.GetHashCode();
            hash = (hash * 397) ^ v3.GetHashCode();
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4>(in T1 v1, in T2 v2, in T3 v3, in T4 v4) {
            var hash = v1.GetHashCode();
            hash = (hash * 397) ^ v2.GetHashCode();
            hash = (hash * 397) ^ v3.GetHashCode();
            hash = (hash * 397) ^ v4.GetHashCode();
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HashCode<T1, T2, T3, T4, T5>(in T1 v1, in T2 v2, in T3 v3, in T4 v4, in T5 v5) {
            var hash = v1.GetHashCode();
            hash = (hash * 397) ^ v2.GetHashCode();
            hash = (hash * 397) ^ v3.GetHashCode();
            hash = (hash * 397) ^ v4.GetHashCode();
            hash = (hash * 397) ^ v5.GetHashCode();
            return hash;
        }
    }
}