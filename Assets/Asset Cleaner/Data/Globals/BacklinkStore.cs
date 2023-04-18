using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Asset_Cleaner {
    class BacklinkStore {
        public bool Initialized { get; private set; }

        public Dictionary<string, long> UnusedFiles { get; private set; }
        public Dictionary<string, long> UnusedScenes { get; private set; }
        public Dictionary<string, BwMeta> Backward { get; private set; }
        public Dictionary<string, UnusedQty> FoldersWithQty { get; private set; }

        Dictionary<string, FwMeta> _forward;
        List<string> Folders { get; set; }


        public void Init() {
            FoldersWithQty = new Dictionary<string, UnusedQty>();
            _forward = new Dictionary<string, FwMeta>();
            Backward = new Dictionary<string, BwMeta>();

            var defaultAss = typeof(DefaultAsset);
            var asmdefAss = typeof(AssemblyDefinitionAsset);

            var paths = AssetDatabase.GetAllAssetPaths()
                .Distinct()
                .Where(s => s.StartsWith("Assets") || s.StartsWith("ProjectSettings"))
                .Where(p => {
                    var t = AssetDatabase.GetMainAssetTypeAtPath(p);
                    return !t.IsAssignableFromInverse(defaultAss) && !t.IsAssignableFromInverse(asmdefAss);
                })
                .ToArray();

            var i = 0f;
            var total = (float) paths.Length;
            foreach (var path in paths) {
                _FillFwAndBacklinks(path);
                var percent = i * 100f / total;
                if (Math.Abs(percent % 5f) < 0.01f) {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Please wait...",
                        "Building the cache...", percent))
                        Debug.LogError("Cache build aborted");
                }

                i++;
            }

            EditorUtility.ClearProgressBar();

            // FillFoldersWithQtyByPaths
            List<string> foldersAll = new List<string>();
            foreach (var path in paths) {
                var folders = GetAllFoldersFromPath(path);
                foldersAll.AddRange(folders);
            }

            Folders = foldersAll.Distinct().OrderBy(p => p).ToList();
            UpdateUnusedAssets();
            Initialized = true;
        }

        void _FillFwAndBacklinks(string path) {
            var dependencies = _Dependencies(path);
            var hs = new FwMeta {Dependencies = new HashSet<string>(dependencies)};
            _forward.Add(path, hs);
            foreach (var backPath in dependencies) {
                if (!Backward.TryGetValue(backPath, out var val)) {
                    val = new BwMeta();
                    val.Lookup = new HashSet<string>();
                    Backward.Add(backPath, val);
                }

                val.Lookup.Add(path);
            }
        }


        void UpdateFoldersWithQtyByPath(string path) {
            var folders = GetAllFoldersFromPath(path);
            foreach (var folder in folders) {
                if (!Folders.Exists(p => p == folder))
                    Folders.Add(folder);
            }
        }


        static List<string> GetAllFoldersFromPath(string p) {
            var result = new List<string>();
            var i = p.IndexOf('/', 0);
            while (i > 0) {
                var item = p.Substring(0, i);
                result.Add(item);
                i = p.IndexOf('/', i + 1);
            }

            return result.Distinct().ToList();
        }

        public void UpdateUnusedAssets() {
            var all = new HashSet<string>(_forward.Keys);
            var withBacklinks =
                new HashSet<string>(Backward.Where(kv => kv.Value.Lookup.Count > 0).Select(kv => kv.Key));

            all.ExceptWith(withBacklinks);
            all.RemoveWhere(SearchUtils.IsFileIgrnoredBySettings);

            var unusedAssets = all;

            var scenes = unusedAssets.Where(s =>
                AssetDatabase.GetMainAssetTypeAtPath(s).IsAssignableFromInverse(typeof(SceneAsset))).ToArray();

            unusedAssets.ExceptWith(scenes);
            var files = unusedAssets;
            UnusedFiles = new Dictionary<string, long>();
            foreach (var file in files) UnusedFiles[file] = new FileInfo(file).Length;

            UnusedScenes = new Dictionary<string, long>();
            foreach (var scene in scenes) UnusedScenes[scene] = new FileInfo(scene).Length;

            // UpdateFoldersWithQty();
            foreach (var folder in Folders) {
                var unusedFilesQty = UnusedFiles.Count(p => p.Key.StartsWith(folder));
                var unusedScenesQty = UnusedScenes.Count(p => p.Key.StartsWith(folder));
                long size = 0;
                size = UnusedFiles.Where((p => p.Key.StartsWith(folder))).Sum(p => p.Value);
                size += UnusedScenes.Where(p => p.Key.StartsWith(folder)).Sum(p => p.Value);

                FoldersWithQty.TryGetValue(folder, out var folderWithQty);
                if (folderWithQty == null) {
                    FoldersWithQty.Add(folder, new UnusedQty(unusedFilesQty, unusedScenesQty, size));
                }
                else {
                    folderWithQty.UnusedFilesQty = unusedFilesQty;
                    folderWithQty.UnusedScenesQty = unusedScenesQty;
                    folderWithQty.UnusedSize = size;
                }
            }
        }


        public void Remove(string path) {
            if (!_forward.TryGetValue(path, out var fwMeta))
                return;

            foreach (var dependency in fwMeta.Dependencies) {
                if (!Backward.TryGetValue(dependency, out var dep)) continue;

                dep.Lookup.Remove(path);
            }

            _forward.Remove(path);
            UpdateFoldersWithQtyByPath(path);
        }

        public void Replace(string src, string dest) {
            _Upd(_forward);
            _Upd(Backward);
            UpdateFoldersWithQtyByPath(dest);

            void _Upd<T>(Dictionary<string, T> dic) {
                if (!dic.TryGetValue(src, out var refs)) return;

                dic.Remove(src);
                dic.Add(dest, refs);
            }
        }

        public void RebuildFor(string path, bool remove) {
            if (!_forward.TryGetValue(path, out var fwMeta)) {
                fwMeta = new FwMeta();
                _forward.Add(path, fwMeta);
            }
            else if (remove) {
                foreach (var dependency in fwMeta.Dependencies) {
                    if (!Backward.TryGetValue(dependency, out var backDep)) continue;

                    backDep.Lookup.Remove(path);
                }

                fwMeta.Dependencies = null;
            }

            var dependencies = _Dependencies(path);
            fwMeta.Dependencies = new HashSet<string>(dependencies);

            foreach (var backPath in dependencies) {
                if (!Backward.TryGetValue(backPath, out var bwMeta)) {
                    bwMeta = new BwMeta {Lookup = new HashSet<string>()};
                    Backward.Add(backPath, bwMeta);
                }
                else if (remove)
                    bwMeta.Lookup.Remove(path);

                bwMeta.Lookup.Add(path);
            }

            if (!remove) {
                UpdateFoldersWithQtyByPath(path);
            }
        }


        static string[] _Dependencies(string s) {
            if (s[0] == 'A')
                return AssetDatabase.GetDependencies(s, false);
            var obj = LoadAllOrMain(s)[0];
            return GetDependenciesManualPaths().ToArray();

            Object[] LoadAllOrMain(string assetPath) {
                // prevents error "Do not use readobjectthreaded on scene objects!"
                return typeof(SceneAsset) == AssetDatabase.GetMainAssetTypeAtPath(assetPath)
                    ? new[] {AssetDatabase.LoadMainAssetAtPath(assetPath)}
                    : AssetDatabase.LoadAllAssetsAtPath(assetPath);
            }

            IEnumerable<string> GetDependenciesManualPaths() {
                if (obj is EditorBuildSettings) {
                    foreach (var scene in EditorBuildSettings.scenes)
                        yield return scene.path;
                }

                using (var so = new SerializedObject(obj)) {
                    var props = so.GetIterator();
                    while (props.Next(true)) {
                        switch (props.propertyType) {
                            case SerializedPropertyType.ObjectReference:
                                var propsObjectReferenceValue = props.objectReferenceValue;
                                if (!propsObjectReferenceValue) continue;

                                var assetPath = AssetDatabase.GetAssetPath(propsObjectReferenceValue);
                                yield return assetPath;
                                break;
#if later
                        case SerializedPropertyType.Generic:
                        case SerializedPropertyType.ExposedReference:
                        case SerializedPropertyType.ManagedReference:
                            break;
#endif
                            default:
                                continue;
                        }
                    }
                }
            }
        }


        class FwMeta {
            public HashSet<string> Dependencies;
        }

        public class BwMeta {
            public HashSet<string> Lookup;
        }

        public class UnusedQty {
            public int UnusedFilesQty;
            public int UnusedScenesQty;

            public long UnusedSize;

            public UnusedQty() {
                Init(0, 0, 0);
            }

            public UnusedQty(int unusedFilesQty, int unusedScenesQty, long unusedSize) {
                Init(unusedFilesQty, unusedScenesQty, unusedSize);
            }

            private void Init(int unusedFilesQty, int unusedScenesQty, long unusedSize) {
                UnusedFilesQty = unusedFilesQty;
                UnusedScenesQty = unusedScenesQty;
                UnusedSize = unusedSize;
            }
        }
    }
}