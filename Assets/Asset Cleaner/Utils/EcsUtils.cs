using System;
using System.Collections.Generic;
using System.Linq;
using Leopotam.Ecs;

namespace Asset_Cleaner {
    static class EcsUtils {
        public static IEnumerable<(T Group, IEnumerable<int> Indices)> GroupBy1<T, T1, T2>(this EcsFilter<T, T1, T2> f, IEqualityComparer<T> comp)
            where T : class
            where T1 : class
            where T2 : class {
            foreach (var group in Inner().GroupBy(tuple => tuple.Group, comp))
                yield return (group.Key, group.Select(g => g.EcsIndex));

            IEnumerable<(T Group, int EcsIndex)> Inner() {
                var get1 = f.Get1;
                foreach (var i in f) yield return (get1[i], i);
            }
        }

        public static EcsFilter<T> Out<T>(this EcsFilter<T> filter, out T[] get1, out EcsEntity[] entities) where T : class {
            get1 = filter.Get1;
            entities = filter.Entities;
            return filter;
        }

        public static EcsFilter<T1, T2> Out<T1, T2>(this EcsFilter<T1, T2> filter, out T1[] get1, out T2[] get2, out EcsEntity[] entities)
            where T1 : class where T2 : class {
            get1 = filter.Get1;
            get2 = filter.Get2;
            entities = filter.Entities;
            return filter;
        }

        public static EcsFilter<T1, T2, T3> Out<T1, T2, T3>(this EcsFilter<T1, T2, T3> filter, out T1[] get1, out T2[] get2, out T3[] get3, out EcsEntity[] entities)
            where T1 : class where T2 : class where T3 : class {
            get1 = filter.Get1;
            get2 = filter.Get2;
            get3 = filter.Get3;
            entities = filter.Entities;
            return filter;
        }

        public static void AllDestroy(this EcsFilter f) {
            var ecsEntities = f.Entities;
            foreach (var i in f)
                ecsEntities[i].Destroy();
        }

        public static void AllUnset<T>(this EcsFilter f) where T : class {
            var e = f.Entities;
            foreach (var i in f)
                e[i].Unset<T>();
        }
    }
}