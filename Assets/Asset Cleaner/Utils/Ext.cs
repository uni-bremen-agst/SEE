using System.Runtime.CompilerServices;
using Leopotam.Ecs;

namespace Asset_Cleaner {
    static class Ext {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Eq(this string s1, string s2) => (s1 == s2);
        // public static bool Eq(this string s1, string s2) => StringComparer.Ordinal.Equals(s1, s2);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSingle<T>(this EcsFilter<T> f) where T : class {
            Asr.AreEqual(f.GetEntitiesCount(), 1);
            return f.Get1[0];
        }
    }
}