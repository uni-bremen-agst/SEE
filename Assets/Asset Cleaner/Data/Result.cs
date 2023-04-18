using Leopotam.Ecs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asset_Cleaner {
    class Result : IEcsAutoReset {
        public string FilePath;
        public Object File;
        public Object MainFile;
        public GameObject RootGo;

        public void Reset() {
            FilePath = default;
            File = default;
            MainFile = default;
            RootGo = default;
        }
    }
}