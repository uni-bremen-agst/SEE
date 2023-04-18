using Leopotam.Ecs;

namespace Asset_Cleaner {
    static class AufCtx {
        public static EcsWorld World;

        public static EcsSystems UndoGroup;
        public static EcsSystems UpdateGroup;
        public static EcsSystems OnGuiGroup;

        internal static bool InitStarted { get; private set; }
        internal static bool Destroyed { get; private set; }

        internal static void TryInitWorld() {
            if (InitStarted) return;
            InitStarted = true;

            World = new EcsWorld();

            (OnGuiGroup = new EcsSystems(World)
                .Add(new SysWindowGui())).Init();

            (UndoGroup = new EcsSystems(World)
                    .Add(new SysUndoRedoSelection())
                ).Init();

            (UpdateGroup = new EcsSystems(World)
                    .Add(new SysRepaintWindow())
                    .Add(new SysProcessSearch())
                    .Add(new SysSceneCleanup())
                ).Init();
        }

        internal static void DestroyWorld() {
            if (!InitStarted) return;
            InitStarted = false;
            Destroyed = true;
            Asr.IsFalse(__GlobalsCounter.HasAnyValue());
        }
    }
}