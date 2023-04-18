using System;
using Leopotam.Ecs;
using UnityEngine;
using static Asset_Cleaner.AufCtx;

namespace Asset_Cleaner {
    class SysProcessSearch : IEcsRunSystem {
        EcsFilter<SelectionChanged> _from = null;

        EcsFilter<Result, SearchResultGui, InSceneResult> SceneResultRows = null;
        EcsFilter<SceneResult, SceneDetails> ScenePaths = null;
        EcsFilter<SearchArg>.Exclude<InSceneResult> SearchArgMain = null;
        EcsFilter<Result, SearchResultGui, FileResultTag> FileResultRows = null;

        public void Run() {
            if (_from.IsEmpty())
                return;

            SearchArgMain.AllDestroy();
            ScenePaths.AllDestroy();
            FileResultRows.AllDestroy();
            SceneResultRows.AllDestroy();

            var wd = Globals<WindowData>.Value;
            if (wd.Window)
                wd.Window.Repaint();

            foreach (var i in _from.Out(out var get1, out _)) {
                var t1 = get1[i];
                if (!t1.Target) continue;
                wd.FindFrom = t1.From;

                try {
                    switch (t1.From) {
                        case FindModeEnum.Scene:
                            World.NewEntityWith(out SearchArg st);
                            SearchUtils.Init(st, t1.Target, t1.Scene);
                            SearchUtils.InScene(st, t1.Scene);
                            break;
                        case FindModeEnum.File:
                            World.NewEntityWith(out SearchArg arg);
                            SearchUtils.Init(arg, t1.Target);
                            SearchUtils.FilesThatReference(arg);
                            SearchUtils.ScenesThatContain(t1.Target);
                            break;
                    }
                }
                catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            _from.AllUnset<SelectionChanged>();
        }
    }
}