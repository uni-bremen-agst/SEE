using Leopotam.Ecs;

namespace Asset_Cleaner {
    class SceneResult : IEcsAutoReset {
        public string PathNicified;

        public void Reset() {
            PathNicified = default;
        }
    }
}