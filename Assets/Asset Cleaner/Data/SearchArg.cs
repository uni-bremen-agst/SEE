using System.Collections.Generic;
using Leopotam.Ecs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asset_Cleaner {
    class SearchArg : IEcsAutoReset {
        public Object Target;
        public Object Main;
        public string FilePath;
        public Option<Object[]> SubAssets;
        public Scene Scene;
        public List<string> UnusedAssetsFiltered;
        public List<string> UnusedScenesFiltered;

        public void Reset() {
            UnusedAssetsFiltered = default;
            UnusedScenesFiltered = default;
            Target = default;
            Main = default;
            SubAssets = default;
            Scene = default;
            FilePath = default;
        }
    }
}