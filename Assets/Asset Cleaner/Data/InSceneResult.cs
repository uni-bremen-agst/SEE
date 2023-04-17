using Leopotam.Ecs;

namespace Asset_Cleaner {
    class InSceneResult : IEcsAutoReset {
        public string ScenePath;

        public void Reset() {
            ScenePath = default;
        }
    }
}