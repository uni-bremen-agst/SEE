using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Leopotam.Ecs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Asset_Cleaner {
    class FileResultTag { }

    enum TargetTypeEnum {
        File = 0,
        Directory = 1,
        Scene = 2,
        ObjectInScene = 3,
        ObjectInStage = 4
    }

    class SysWindowGui : IEcsRunSystem, IEcsInitSystem {
        EcsFilter<Result, SearchResultGui, InSceneResult> SceneResultRows = null;
        EcsFilter<SceneResult, SceneDetails> ScenePaths = null;
        EcsFilter<SearchArg>.Exclude<InSceneResult> SearchArgMain = null;
        EcsFilter<Result, SearchResultGui, FileResultTag> FileResultRows = null;

        public void Init() {
            BacklinkStoreDirty(true);
            VisualSettingDirty(true);
        }

        public void Run() {
            var windowData = Globals<WindowData>.Value;

            _toolbarSelection = GUILayout.Toolbar(_toolbarSelection, _toolbarStrings, GUILayout.ExpandWidth(false));
            var conf = Globals<Config>.Value;
            switch (_toolbarSelection) {
                case 0: {
                    ShowTabMain(conf, windowData);
                    break;
                }
                case 1: {
                    ShowTabSettings(conf);
                    break;
                }
            }
        }

        string[] _toolbarStrings = {"Main", "Settings"};

        const int _progressBarShowFromLevel = 10;
        int _toolbarSelection = 0;

        int _settingIgnoredPathsHash1;

        bool BacklinkStoreDirty(bool set) {
            var res = Hash() != _settingIgnoredPathsHash1;
            if (set) _settingIgnoredPathsHash1 = Hash();
            return res;

            int Hash() {
                var conf = Globals<Config>.Value;
                return DirtyUtils.HashCode(conf.IgnorePathContainsCombined,
                    conf.IgnoreMaterial,
                    conf.IgnoreScriptable,
                    conf.IgnoreSprite);
            }
        }

        int _settingCodeHash1;

        bool VisualSettingDirty(bool set) {
            var res = Hash() != _settingCodeHash1;
            if (set) _settingCodeHash1 = Hash();
            return res;

            int Hash() {
                var conf = Globals<Config>.Value;
                return DirtyUtils.HashCode(
                    conf.MarkRed,
                    conf.ShowInfoBox,
                    conf.RebuildCacheOnDemand,
                    conf.UpdateUnusedAssetsOnDemand);
            }
        }

        void ShowTabSettings(Config conf) {
            using (new EditorGUILayout.VerticalScope()) {
                EditorGUILayout.Space();

                var enabled = GUI.enabled;
                GUI.enabled = true;


                using (new EditorGUILayout.VerticalScope()) {
                    conf.MarkRed = GUILayout.Toggle(conf.MarkRed, "Display counters and red overlay in Project View");
                    conf.ShowInfoBox = GUILayout.Toggle(conf.ShowInfoBox, "Help suggestions");
                    conf.RebuildCacheOnDemand = GUILayout.Toggle(conf.RebuildCacheOnDemand, "Rebuild cache on demand (when scripts are updated often)");
                    conf.UpdateUnusedAssetsOnDemand = GUILayout.Toggle(conf.UpdateUnusedAssetsOnDemand, "Update unused assets on demand");
                    EditorGUILayout.Space();
                    conf.IgnoreMaterial = GUILayout.Toggle(conf.IgnoreMaterial, "Skip Materials");
                    conf.IgnoreScriptable = GUILayout.Toggle(conf.IgnoreScriptable, "Skip ScriptableObjects");
                    conf.IgnoreSprite = GUILayout.Toggle(conf.IgnoreSprite, "Skip Sprites");
                }

                EditorGUILayout.Space();

                GUI.enabled = enabled;

                EditorGUILayout.LabelField("Skip Path(s) contains:");

                conf.IgnorePathContainsCombined = GUILayout.TextArea(conf.IgnorePathContainsCombined);

                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false))) {
                    EditorGUILayout.Space();

                    var previous = GUI.enabled;

                    GUI.enabled = BacklinkStoreDirty(false) || VisualSettingDirty(false);
                    if (GUILayout.Button("Apply")) {
                        conf.IgnorePathContains = conf.IgnorePathContainsCombined.Split(';')
                            .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                        Apply();
                    }

                    GUI.enabled = previous;

                    var selectedGuids = Selection.assetGUIDs;
                    var assetPaths = new List<string>();
                    if (selectedGuids.Length > 0) {
                        foreach (var guid in selectedGuids) {
                            var realAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(realAssetPath);
                            var assetPath = realAssetPath.Replace("Assets/", string.Empty);
                            if (obj is DefaultAsset &&
                                !conf.IgnorePathContains.Any(p => (StringComparer.Ordinal.Equals(p, assetPath)))) {
                                assetPaths.Add(assetPath);
                            }
                        }
                    }

                    GUI.enabled = (assetPaths.Count > 0);
                    var foldersList = string.Join(", ", assetPaths);
                    if (GUILayout.Button("Add Selected Path")) {
                        var choice = EditorUtility.DisplayDialog(
                            title: "Asset Cleaner",
                            message:
                            $"Do you really want to add these folder(s) to ignored list: \"{foldersList}\"?",
                            ok: "Ignore",
                            cancel: "Cancel");
                        if (choice) {
                            conf.IgnorePathContainsCombined += $"{foldersList};";
                            conf.IgnorePathContains = conf.IgnorePathContainsCombined.Split(';')
                                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                            Apply();
                        }
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("Reset")) {
                        var choice = EditorUtility.DisplayDialog(
                            title: "Asset Cleaner",
                            message:
                            $"Do you really want to reset to the factory settings?",
                            ok: "Reset",
                            cancel: "Cancel");
                        if (choice) {
                            var serializable = AufSerializableData.Default();
                            AufSerializableData.OnDeserialize(in serializable, ref conf);
                            Apply();
                        }
                    }

                    GUI.enabled = previous;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(conf.InitializationTime);

                var buf = GUI.enabled;
                GUI.enabled = Selection.objects.Length > 0;
                if (GUILayout.Button("Reserialize selected assets", GUILayout.ExpandWidth(false))) {
                    var paths = Selection.objects.Select(AssetDatabase.GetAssetPath);
                    AssetDatabase.ForceReserializeAssets(paths);
                    EditorApplication.ExecuteMenuItem("File/Save Project");
                    AssetDatabase.Refresh();
                }

                GUI.enabled = buf;

                EditorGUILayout.Space();
            }

            void Apply() {
                var rebuild = BacklinkStoreDirty(true);
                VisualSettingDirty(true);
                PersistenceUtils.Save(in conf);
                AufCtx.World.NewEntityWith(out RequestRepaintEvt _);
                if (rebuild)
                    Globals<BacklinkStore>.Value.UpdateUnusedAssets();
                InternalEditorUtility.RepaintAllViews();
            }
        }

        void ShowTabMain(Config conf, WindowData windowData) {
            var store = Globals<BacklinkStore>.Value;
            EditorGUIUtility.labelWidth = windowData.Window.position.width * .7f;

            int Hash() => DirtyUtils.HashCode(conf.Locked);
            var active = SearchArgMain.Get1[0];
            if (conf.Locked && (windowData.FindFrom == FindModeEnum.File &&
                                (active == null || active.Main == null || !AssetDatabase.Contains(active.Main)))) {
                conf.Locked = false;
                AufCtx.World.NewEntityWith(out RequestRepaintEvt _);
            }

            var style = windowData.Style;
            var hash = Hash();
            if (hash != Hash()) {
                PersistenceUtils.Save(in conf);
                AufCtx.World.NewEntityWith(out RequestRepaintEvt _);
            }

            // if (Globals<WindowData>.Get() == null) return;
            EditorGUILayout.Space();

            SearchArg arg = default;
            foreach (var i in SearchArgMain) {
                arg = SearchArgMain.Get1[i];
                if (arg != null && arg.Main != null) {
                    break;
                }
            }

            if (arg == default) {
                GUI.enabled = false;
                GUILayout.TextArea("No items selected. Select an item in a scene or project.");
                GUI.enabled = true;
                return;
            }

            var targetTypeEnum = GetTargetType(windowData, arg?.Main);
            BacklinkStore.UnusedQty unusedQty = new BacklinkStore.UnusedQty(0, 0, 0);

            using (new EditorGUILayout.HorizontalScope()) {
                var enabledBuf = GUI.enabled;
                var selectedGuids = Selection.assetGUIDs;

                var undoRedoState = Globals<UndoRedoState>.Value;

                GUI.enabled = selectedGuids != null && !conf.Locked && undoRedoState.UndoEnabled;
                if (GUILayout.Button(style.ArrowL, style.ArrowBtn)) {
                    AufCtx.World.NewEntityWith(out UndoEvt _);
                }

                GUI.enabled = selectedGuids != null && !conf.Locked && undoRedoState.RedoEnabled;
                if (GUILayout.Button(style.ArrowR, style.ArrowBtn)) {
                    AufCtx.World.NewEntityWith(out RedoEvt _);
                }

                GUI.enabled = enabledBuf;

                if (conf.Locked) {
                    if (GUILayout.Button(style.Lock, style.LockBtn)) {
                        AufCtx.World.NewEntityWith(out SelectionChanged selectionChanged);
                        conf.Locked = false;
                        if (Selection.activeObject != arg.Target) {
                            selectionChanged.From = FindModeEnum.Scene;
                            selectionChanged.Scene = SceneManager.GetActiveScene();
                            selectionChanged.Target = Selection.activeObject;
                        }
                        else if (Selection.assetGUIDs is string[] guids) {
                            // todo show info box multiple selection is unsupported 
                            if (guids.Length > 0) {
                                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                                selectionChanged.Target = AssetDatabase.LoadAssetAtPath<Object>(path);
                                switch (Selection.selectionChanged.Target) {
                                    case DefaultAsset _:
                                        selectionChanged.From = FindModeEnum.File;
                                        break;
                                    case GameObject go when go.scene.isLoaded:
                                        selectionChanged.From = FindModeEnum.Scene;
                                        selectionChanged.Scene = SceneManager.GetActiveScene();
                                        break;
                                    default:
                                        selectionChanged.From = FindModeEnum.File;
                                        break;
                                }
                            }
                            else if (Selection.activeObject is GameObject go && go.scene.isLoaded) {
                                selectionChanged.From = FindModeEnum.Scene;
                                selectionChanged.Target = Selection.activeObject;
                                selectionChanged.Scene = SceneManager.GetActiveScene();
                            }
                        }
                    }
                }
                else {
                    var enabled = GUI.enabled;
                    GUI.enabled = selectedGuids != null && selectedGuids.Length == 1;
                    if (GUILayout.Button(style.Unlock, style.UnlockBtn)) {
                        conf.Locked = true;
                    }

                    GUI.enabled = enabled;
                }

                unusedQty = ShowObjectName(store, windowData, targetTypeEnum, arg, selectedGuids);
            }

            bool isMultiSelect = Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 1;

            if (conf.ShowInfoBox) {
                if (isMultiSelect && (unusedQty.UnusedFilesQty + unusedQty.UnusedScenesQty > 0)) {
                    var msgUnusedFiles = (unusedQty.UnusedFilesQty > 0)
                        ? $"unused files ({unusedQty.UnusedFilesQty}),"
                        : "";
                    var msgUnusedScenes = (unusedQty.UnusedScenesQty > 0)
                        ? $"unused scenes ({unusedQty.UnusedScenesQty}),"
                        : "";
                    var msgMultiSelect = $"This multi-selection contains: " +
                                         msgUnusedFiles + msgUnusedScenes +
                                         $"\nYou could delete them pressing corresponding button to the right.";
                    EditorGUILayout.HelpBox(msgMultiSelect, MessageType.Info);
                }
                else if (TryGetHelpInfo(arg, out var msg, out var msgType)) {
                    EditorGUILayout.HelpBox(msg, msgType);
                }
            }

            if (targetTypeEnum != TargetTypeEnum.Directory && !isMultiSelect) {
                var windowData2 = Globals<WindowData>.Value;

                EditorGUILayout.BeginVertical();
                {
                    windowData2.ScrollPos = EditorGUILayout.BeginScrollView(windowData2.ScrollPos);
                    {
                        RenderRows(windowData2); 
                        EditorGUILayout.Space();
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }


        static bool TryGetHelpInfo(SearchArg arg, out string msg, out MessageType msgType) {
            msgType = MessageType.Info;
            if (arg == null) {
                msg = default;
                return false;
            }

            var path = arg.FilePath;
            if (string.IsNullOrEmpty(path)) {
                msg = default;
                return false;
            }

            if (SearchUtils.IgnoredPaths(path, out var subPath)) {
                msg = $"Paths containing '{subPath}' are ignored. You could add or remove it in Settings tab";
                return true;
            }

            if (SearchUtils.IgnoredNonAssets(path) && !path.Eq("Assets")) {
                msg = $"Asset is outside of Assets folder";
                return true;
            }

            if (IgnoreTypes.Check(path, out var type)) {
                if (AssetDatabase.IsValidFolder(path)) {
                    var scenes = arg.UnusedScenesFiltered?.Count;
                    var files = arg.UnusedAssetsFiltered?.Count;
                    if (scenes == 0 && files == 0) {
                        msg = default;
                        return false;
                    }

                    var b = new StringBuilder();
                    b.Append("This directory contains: ");
                    var any = false;

                    if (files > 0) {
                        any = true;

                        b.Append($"unused files ({files})");
                    }

                    if (scenes > 0) {
                        if (any)
                            b.Append(", ");
                        b.Append($"unused scenes ({scenes})");
                    }

                    b.Append(
                        ".\nYou could delete them pressing corresponding button to the right.\nIf you don't want it to be inspected, please add it to Ignore list in the Settings tab");
                    msg = b.ToString();
                    return true;
                }

                msg = $"Assets of type '{type.Name}' are ignored. Please contact support if you need to change it";
                return true;
            }

            // if (Filters.ScenePaths.GetEntitiesCount() == 0 && Filters.FileResultRows.GetEntitiesCount() == 0) {
            if (SearchUtils.IsUnused(path)) {
                type = AssetDatabase.GetMainAssetTypeAtPath(path);
                var name = type.IsAssignableFromInverse(typeof(SceneAsset)) ? "scene" : "file";
                msg =
                    $"This {name} has no explicit serialized usages and potentially could be removed. If you don't want it to be inspected, please add the containing folder to ignore list";
                return true;
            }

            msgType = default;
            msg = default;
            return false;
        }

        static TargetTypeEnum GetTargetType(WindowData windowData1, Object obj) {
            if (obj == null) return TargetTypeEnum.File;
            var targetTypeEnum = TargetTypeEnum.Directory;
            var path = AssetDatabase.GetAssetPath(obj);
            switch (windowData1.FindFrom) {
                case FindModeEnum.File when obj is DefaultAsset:
                    targetTypeEnum = TargetTypeEnum.Directory;
                    break;
                case FindModeEnum.File when path.LastIndexOf(".unity", StringComparison.Ordinal) != -1:
                    targetTypeEnum = TargetTypeEnum.Scene;
                    break;
                case FindModeEnum.File:
                    targetTypeEnum = TargetTypeEnum.File;
                    break;
                case FindModeEnum.Scene:
                    targetTypeEnum = TargetTypeEnum.ObjectInScene;
                    break;
                case FindModeEnum.Stage:
                    targetTypeEnum = TargetTypeEnum.ObjectInStage;
                    break;
            }

            return targetTypeEnum;
        }

        GUIContent _contentBuf = new GUIContent();
        GUIContent _buf2 = new GUIContent();

        BacklinkStore.UnusedQty ShowObjectName(BacklinkStore store, WindowData windowData, TargetTypeEnum targetTypeEnum, SearchArg arg, string[] selectedGuids) {
            float TextWidth() {
                _buf2.text = _contentBuf.text;
                return 20f + GUI.skin.button.CalcSize(_buf2).x;
            }

            if (arg == null || arg.Main == null) return new BacklinkStore.UnusedQty();

            bool isMultiSelect = selectedGuids != null && selectedGuids.Length > 1;

            if (_contentBuf == null) {
                _contentBuf = new GUIContent {tooltip = $"Click to ping"};
            }

            _contentBuf.image = isMultiSelect
                ? windowData.Style.MultiSelect.image
                : AssetPreview.GetMiniThumbnail(arg.Target);
            _contentBuf.text = string.Empty;
            _contentBuf.tooltip = string.Empty;

            if (!isMultiSelect) {
                switch (targetTypeEnum) {
                    case TargetTypeEnum.Directory:
                        if (!SearchArgMain.IsEmpty()) {
                            _contentBuf.text = $"{arg.Main.name} (Folder)";
                            if (GUILayout.Button(_contentBuf, windowData.Style.CurrentBtn,
                                GUILayout.MinWidth(TextWidth()))) {
                                EditorGUIUtility.PingObject(arg.Main);
                            }

                            if (AskDeleteUnusedFiles(arg, arg.UnusedAssetsFiltered, windowData))
                                return new BacklinkStore.UnusedQty();

                            if (AskDeleteUnusedScenes(arg, arg.UnusedScenesFiltered, windowData))
                                return new BacklinkStore.UnusedQty();
                        }

                        break;
                    case TargetTypeEnum.File:
                        _contentBuf.text = $"{arg.Main.name} (File Asset)";
                        if (GUILayout.Button(_contentBuf, windowData.Style.CurrentBtn,
                            GUILayout.MinWidth(TextWidth()))) {
                            EditorGUIUtility.PingObject(arg.Main);
                        }

                        bool hasUnusedFile = SearchUtils.IsUnused(arg.FilePath);
                        var previous = GUI.enabled;
                        GUI.enabled = hasUnusedFile;

                        if (GUILayout.Button(windowData.Style.RemoveFile,
                            windowData.Style.RemoveUnusedBtn)) {
                            var choice = EditorUtility.DisplayDialog(
                                title: "Asset Cleaner",
                                message:
                                $"Do you really want to remove file: \"{arg.Main.name}\"?",
                                ok: "Remove",
                                cancel: "Cancel");
                            if (choice) {
                                EditorApplication.ExecuteMenuItem("File/Save Project");
                                DeleteWithMeta(arg.FilePath);
                                AssetDatabase.Refresh();
                                SearchUtils.Upd(arg);
                            }
                        }

                        GUI.enabled = previous;

                        break;
                    case TargetTypeEnum.Scene:
                        _contentBuf.text = $"{arg.Main.name} (Scene)";
                        if (GUILayout.Button(_contentBuf, windowData.Style.CurrentBtn,
                            GUILayout.MinWidth(TextWidth()))) {
                            EditorGUIUtility.PingObject(arg.Main);
                        }

                        bool hasUnusedScene = SearchUtils.IsUnused(arg.FilePath);
                        previous = GUI.enabled;
                        GUI.enabled = hasUnusedScene;

                        if (GUILayout.Button(windowData.Style.RemoveScene,
                            windowData.Style.RemoveUnusedBtn)) {
                            var choice = EditorUtility.DisplayDialog(
                                title: "Asset Cleaner",
                                message:
                                $"Do you really want to remove scene: {arg.Main.name}?",
                                ok: "Remove",
                                cancel: "Cancel");
                            if (choice) {
                                EditorApplication.ExecuteMenuItem("File/Save Project");
                                DeleteWithMeta(arg.FilePath);
                                AssetDatabase.Refresh();
                                SearchUtils.Upd(arg);
                            }
                        }

                        GUI.enabled = previous;
                        break;
                    case TargetTypeEnum.ObjectInScene:
                        _contentBuf.text = $"{arg.Main.name} (Object in Scene)";

                        if (GUILayout.Button(_contentBuf, windowData.Style.CurrentBtn,
                            GUILayout.MinWidth(TextWidth()))) {
                            EditorGUIUtility.PingObject(arg.Main);
                        }

                        break;
                    case TargetTypeEnum.ObjectInStage:
                        _contentBuf.image = AssetPreview.GetMiniThumbnail(arg.Target);
                        _contentBuf.text = $"{arg.Main.name} (Object in Staging)";

                        if (GUILayout.Button(_contentBuf,
                            windowData.Style.RemoveUnusedBtn)) {
                            EditorGUIUtility.PingObject(arg.Main);
                        }

                        break;
                    default:
                        if (GUILayout.Button($"{arg.Main.name} (Unknown Object Type)",
                            windowData.Style.RemoveUnusedBtn)) {
                            EditorGUIUtility.PingObject(arg.Main);
                        }

                        break;
                }
            }
            else {
                var unusedAssets = new List<string>();
                var unusedScenes = new List<string>();

                foreach (var guid in selectedGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    if (store.UnusedFiles.TryGetValue(path, out _))
                        unusedAssets.Add(path);

                    else if (store.UnusedScenes.TryGetValue(path, out _))
                        unusedScenes.Add(path);

                    if (store.FoldersWithQty.TryGetValue(path, out _)) {
                        SearchArg searchArg = new SearchArg() {
                            FilePath = path,
                            Target = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path),
                            Main = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path)
                        };
                        SearchUtils.Upd(searchArg);

                        foreach (var unusedAssetPath in searchArg.UnusedAssetsFiltered)
                            if (store.UnusedFiles.TryGetValue(unusedAssetPath, out _))
                                unusedAssets.Add(unusedAssetPath);

                        foreach (var unusedScenePath in searchArg.UnusedScenesFiltered)
                            if (store.UnusedScenes.TryGetValue(unusedScenePath, out _))
                                unusedScenes.Add(unusedScenePath);
                    }
                }

                unusedAssets = unusedAssets.Distinct().ToList();
                unusedScenes = unusedScenes.Distinct().ToList();

                var assetSize = unusedAssets.Sum(p => new FileInfo(p).Length);
                var sceneSize = unusedScenes.Sum(p => new FileInfo(p).Length);

                _contentBuf.text =
                    $"Assets: {unusedAssets.Count} ({CommonUtils.BytesToString(assetSize)}), Scenes: {unusedScenes.Count} ({CommonUtils.BytesToString(sceneSize)})";
                ;

                GUILayout.Button(_contentBuf, windowData.Style.CurrentBtn, GUILayout.MinWidth(TextWidth()));

                if (AskDeleteUnusedFiles(arg, unusedAssets, windowData))
                    return new BacklinkStore.UnusedQty();

                if (AskDeleteUnusedScenes(arg, unusedScenes, windowData))
                    return new BacklinkStore.UnusedQty();

                return new BacklinkStore.UnusedQty(unusedAssets.Count, unusedScenes.Count, assetSize + sceneSize);
            }

            return new BacklinkStore.UnusedQty();
        }

        static bool AskDeleteUnusedFiles(SearchArg arg, List<string> unusedAssets, WindowData windowData) {
            if (arg == null || unusedAssets == null) return false;
            var hasUnusedAssets = unusedAssets.Count > 0;
            var previous = GUI.enabled;
            GUI.enabled = hasUnusedAssets;

            var guiContentRemoveAssets = windowData.Style.RemoveFile;

            if (GUILayout.Button(guiContentRemoveAssets,
                windowData.Style.RemoveUnusedBtn)) {
                var choice = EditorUtility.DisplayDialog(
                    title: "Asset Cleaner",
                    message:
                    $"Do you really want to remove {unusedAssets.Count} asset(s)?",
                    ok: "Remove",
                    cancel: "Cancel");
                if (choice) {
                    EditorApplication.ExecuteMenuItem("File/Save Project");

                    var i = 0f;
                    var total = (float) unusedAssets.Count;

                    foreach (var f in unusedAssets) {
                        var path = Application.dataPath.Replace("Assets", f);
                        DeleteWithMeta(path);
                        
                        var percent = i * 100 / total;
                        if (total >= _progressBarShowFromLevel) {
                            if (Math.Abs(percent % 5f) < 0.01f) {
                                if (EditorUtility.DisplayCancelableProgressBar(
                                    "Please wait...",
                                    "Deleting assets...", percent))
                                    throw new Exception("Deleting aborted");
                            }

                            i++;
                        }
                    }

                    if (total >= _progressBarShowFromLevel) {
                        EditorUtility.ClearProgressBar();
                    }

                    AssetDatabase.Refresh();
                    SearchUtils.Upd(arg);
                }

                GUI.enabled = previous;
                return true;
            }

            GUI.enabled = previous;

            return false;
        }

        static void DeleteWithMeta(string path) {
            FileUtil.DeleteFileOrDirectory(path);
            var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
            if (!string.IsNullOrEmpty(metaPath))
                FileUtil.DeleteFileOrDirectory(metaPath);
        }

        static bool AskDeleteUnusedScenes(SearchArg arg, List<string> unusedScenes, WindowData windowData) {
            if (arg == null || unusedScenes == null) return false;
            var hasUnusedScenes = unusedScenes.Count > 0;
            var previous = GUI.enabled;
            GUI.enabled = hasUnusedScenes;

            var guiContentRemoveScenes = windowData.Style.RemoveScene;

            if (GUILayout.Button(guiContentRemoveScenes,
                windowData.Style.RemoveUnusedBtn)) {
                var choice = EditorUtility.DisplayDialog(
                    title: "Asset Cleaner",
                    message:
                    $"Do you really want to remove {unusedScenes.Count} scene(s)?",
                    ok: "Remove",
                    cancel: "Cancel");
                if (choice) {
                    EditorApplication.ExecuteMenuItem("File/Save Project");

                    var i = 0f;
                    var total = (float) unusedScenes.Count;

                    foreach (var scene in unusedScenes) {
                        var path = Application.dataPath.Replace("Assets", scene);
                        DeleteWithMeta(path);

                        if (total >= _progressBarShowFromLevel) {
                            var percent = i * 100 / total;
                            if (Math.Abs(percent % 5f) < 0.01f) {
                                if (EditorUtility.DisplayCancelableProgressBar(
                                    "Please wait...",
                                    "Deleting scenes...", percent))
                                    throw new Exception("Deleting aborted");
                            }

                            i++;
                        }
                    }

                    if (total >= _progressBarShowFromLevel) {
                        EditorUtility.ClearProgressBar();
                    }

                    AssetDatabase.Refresh();
                    SearchUtils.Upd(arg);
                }

                GUI.enabled = previous;

                return true;
            }

            GUI.enabled = previous;

            return false;
        }


        void RenderRows(WindowData windowData) {
            // todo show spinner until scene is loaded, 

            if (FileResultRows.GetEntitiesCount() > 0) {
                windowData.ExpandFiles =
                    EditorGUILayout.Foldout(windowData.ExpandFiles,
                        $"Usages in Project: {FileResultRows.GetEntitiesCount()}");
            }

            if (SearchArgMain.IsEmpty())
                return;

            if (windowData.ExpandFiles && windowData.FindFrom == FindModeEnum.File)
                foreach (var i in FileResultRows.Out(out var get1, out var get2, out _, out _))
                    DrawRowFile(get1[i], get2[i], windowData);


            var sceneMessage = $"Usages in Scenes: {ScenePaths.GetEntitiesCount()}";
            if (ScenePaths.GetEntitiesCount() > 0) {
                windowData.ExpandScenes =
                    EditorGUILayout.Foldout(windowData.ExpandScenes, sceneMessage);
            }


            if (!windowData.ExpandScenes) return;

            if (windowData.ExpandScenes && windowData.FindFrom == FindModeEnum.Scene) {
                foreach (var (grp, indices) in SceneResultRows.Out(out _, out var get2, out _, out _)
                    .GroupBy1(ResultComp.Instance)) {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                        var count = 0;
                        foreach (var i in indices) {
                            if (count++ == 0)
                                if (GUILayout.Button(get2[i].Label, windowData.Style.RowMainAssetBtn)) {
                                    if (windowData.Click.IsDoubleClick(grp.RootGo)) {
                                        // _selectionChangedByArrows = false;
                                        Selection.activeObject = grp.RootGo;
                                    }
                                    else
                                        EditorGUIUtility.PingObject(grp.RootGo);

                                    windowData.Click = new PrevClick(grp.RootGo);
                                }

                            DrawRowScene(get2[i]);
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope()) {
                GUILayout.Space(windowData.Style.SceneIndent1);
                using (new EditorGUILayout.VerticalScope()) {
                    foreach (var i1 in ScenePaths.Out(out var get1, out var get2, out _)) {
                        windowData.SceneFoldout.text = get1[i1].PathNicified;
                        var details = get2[i1];

                        details.SearchRequested = details.Scene.isLoaded;
                        details.SearchRequested = EditorGUILayout.Foldout(details.SearchRequested,
                            windowData.SceneFoldout, EditorStyles.foldout);
                        if (details.SearchRequested && details.Scene.isLoaded && !details.SearchDone) {
                            var mainArg = SearchArgMain.GetSingle();

                            mainArg.Scene = SceneManager.GetSceneByPath(details.Scene.path);

                            SearchUtils.InScene(mainArg, details.Scene);
                            details.SearchDone = true;
                        }

                        if (!details.SearchRequested) {
                            if (!details.Scene.isLoaded) continue;
                            if (!details.WasOpened) {
                                // to clean up on selection change
                                AufCtx.World.NewEntityWith(out SceneToClose comp);
                                comp.Scene = details.Scene;
                                comp.ForceClose = true;
                            }

                            foreach (var row in SceneResultRows.Out(out _, out _, out var get3, out var entities)) {
                                if (!get3[row].ScenePath.Eq(details.Path))
                                    continue;

                                entities[row].Destroy();
                            }

                            details.SearchDone = false;
                        }
                        else {
                            if (!details.Scene.isLoaded) {
                                details.Scene = EditorSceneManager.OpenScene(details.Path, OpenSceneMode.Additive);

                                // to clean up on selection change
                                AufCtx.World.NewEntityWith(out SceneToClose comp);
                                comp.Scene = details.Scene;
                                comp.SelectionId = Globals<PersistentUndoRedoState>.Value.Id;


#if UNITY_2019_1_OR_NEWER
                                EditorSceneManager.SetSceneCullingMask(details.Scene, 0);
#endif
                                details.SearchRequested = true;
                                // todo Scope component
#if later
                                     if (details.Scene.isLoaded)
                                    EditorSceneManager.CloseScene(details.Scene, false);
#endif
                            }
                            else if (SceneResultRows.IsEmpty())
                                EditorGUILayout.LabelField("No in-scene dependencies found.");
                            else
                                using (new GUILayout.HorizontalScope()) {
                                    GUILayout.Space(windowData.Style.SceneIndent2);
                                    using (new EditorGUILayout.VerticalScope())
                                        foreach (var (grp, indices) in SceneResultRows
                                            .Out(out var g1, out var g2, out var g3, out _)
                                            .GroupBy1(ResultComp.Instance)) {
                                            var any = false;
                                            foreach (var i3 in indices) {
                                                if (!g3[i3].ScenePath.Eq(details.Path))
                                                    continue;
                                                any = true;
                                                break;
                                            }

                                            if (!any)
                                                continue;

                                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                                                var count = 0;
                                                foreach (var i2 in indices) {
                                                    if (!g3[i2].ScenePath.Eq(details.Path))
                                                        continue;

                                                    if (count++ == 0) {
                                                        Result comp = g1[i2];
                                                        if (GUILayout.Button(g2[i2].Label,
                                                            windowData.Style.RowMainAssetBtn)) {
                                                            if (windowData.Click.IsDoubleClick(grp.RootGo)) {
                                                                // _selectionChangedByArrows = false;
                                                                Selection.activeObject = comp.RootGo;
                                                            }
                                                            else
                                                                EditorGUIUtility.PingObject(comp.RootGo);

                                                            windowData.Click = new PrevClick(comp.RootGo);
                                                        }
                                                    }

                                                    DrawRowScene(g2[i2]);
                                                }
                                            }
                                        }
                                }
                        }
                    }
                }
            }
        }

        class ResultComp : IEqualityComparer<Result> {
            public static ResultComp Instance { get; } = new ResultComp();
            public bool Equals(Result x, Result y) => GetHashCode(x) == GetHashCode(y);
            public int GetHashCode(Result obj) => obj.RootGo.GetInstanceID();
        }

        static void DrawRowScene(SearchResultGui gui) {
            EditorGUI.BeginChangeCheck();
            // if (data.TargetGo || data.TargetComponent)
            foreach (var prop in gui.Properties) {
                {
                    var locked = prop.Property.objectReferenceValue is MonoScript;
                    var f = GUI.enabled;

                    if (locked) GUI.enabled = false;

                    EditorGUILayout.PropertyField(prop.Property, prop.Content, false);

                    if (locked) GUI.enabled = f;
                }
            }

            if (EditorGUI.EndChangeCheck())
                gui.SerializedObject.ApplyModifiedProperties();
        }


        static void DrawRowFile(Result data, SearchResultGui gui, WindowData windowData) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                var buf = GUI.color;
                var pingGo = data.MainFile == null ? data.RootGo : data.MainFile;
                if (GUILayout.Button(gui.Label, windowData.Style.RowMainAssetBtn)) {
                    if (windowData.Click.IsDoubleClick(pingGo)) {
                        // _selectionChangedByArrows = false;
                        Selection.activeObject = pingGo;
                    }
                    else {
                        EditorGUIUtility.PingObject(pingGo);
                    }

                    windowData.Click = new PrevClick(pingGo);
                }

                GUI.color = buf;

                EditorGUI.BeginChangeCheck();
                if (data.File) {
                    foreach (var prop in gui.Properties) {
                        using (new EditorGUILayout.HorizontalScope()) {
                            var locked = prop.Property.objectReferenceValue is MonoScript;
                            var f = GUI.enabled;
                            if (locked) GUI.enabled = false;
                            EditorGUILayout.PropertyField(prop.Property, prop.Content, false);
                            if (locked) GUI.enabled = f;
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    gui.SerializedObject.ApplyModifiedProperties();
                    // dependency.SerializedObject.Update();
                }
            }
        }
    }
}