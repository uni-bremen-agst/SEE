using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Leopotam.Ecs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Asset_Cleaner.AufCtx;
using Object = UnityEngine.Object;

namespace Asset_Cleaner {
	static class SearchUtils {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAssignableFromInverse(this Type lhs, Type rhs) {
			if (lhs == null || rhs == null)
				return false;

			return rhs.IsAssignableFrom(lhs);
		}

		static Queue<SerializedProperty> _tmp = new Queue<SerializedProperty>();

		public static void Upd(SearchArg arg) {
			if (arg.Target is DefaultAsset folder) {
				var path = AssetDatabase.GetAssetPath(folder);
				var store = Globals<BacklinkStore>.Value;
				arg.UnusedAssetsFiltered = store.UnusedFiles.Where(p => p.Key.StartsWith(path)).Select(p => p.Key).ToList();
				arg.UnusedScenesFiltered = store.UnusedScenes.Where(p => p.Key.StartsWith(path)).Select(p => p.Key).ToList(); 
			}
		}

		public static void Init(SearchArg arg, Object target, Scene scene = default) {
			Asr.IsNotNull(target, "Asset you're trying to search is corrupted");

			arg.Target = target;

			arg.FilePath = AssetDatabase.GetAssetPath(arg.Target);
			if (!scene.IsValid()) {
				Upd(arg);

				arg.Main = AssetDatabase.LoadMainAssetAtPath(arg.FilePath);
				if (AssetDatabase.IsSubAsset(arg.Target)) { }
				else {
					switch (target) {
						case SceneAsset _:
							// todo support cross-scene references?
							// nested = all assets
							break;
						default:
							// AssetDatabase.IsMainAssetAtPathLoaded()
							var subAssets = AssetDatabase.LoadAllAssetsAtPath(arg.FilePath).Where(Predicate).ToArray();
							arg.SubAssets = subAssets.Length == 0 ? default(Option<Object[]>) : subAssets;

							bool Predicate(Object s) {
								if (!s)
									return false;
								return s.GetInstanceID() != arg.Target.GetInstanceID();
							}

							break;
					}
				}
			}
			else {
				switch (arg.Target) {
					case GameObject gg:
						arg.Main = gg;
						arg.Scene = scene;
						arg.SubAssets = gg.GetComponents<Component>().OfType<Object>().ToArray();
						break;
					case Component component: {
						// treat like subAsset
						arg.Main = component.gameObject;
						arg.Scene  = scene;
						break;
					}
					default:
						// project asset such as Material
						arg.Main = arg.Target;
						arg.Scene = scene;
						break;
				}
			}
		}

		static bool SearchInChildProperties(SearchArg arg, Object suspect, bool scene, out EcsEntity entity) {
			if (IsTargetOrNested(arg, suspect)) {
				entity = default;
				return false;
			}

			if (!suspect) {
				entity = default;
				return false;
			}

			var so = new SerializedObject(suspect);
			_tmp.Clear();
			var queue = _tmp;
			var propIterator = so.GetIterator();

			var prefabInstance = false;

			if (scene && !string.IsNullOrEmpty(arg.FilePath) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(suspect) == arg.FilePath) {
				prefabInstance = true;
				while (propIterator.NextVisible(true)) {
					if (propIterator.propertyType != SerializedPropertyType.ObjectReference)
						continue;
					if (!IsTargetOrNested(arg, propIterator.objectReferenceValue))
						continue;

					queue.Enqueue(propIterator.Copy());
				}
			}
			else {
				while (propIterator.Next(true)) {
					if (propIterator.propertyType != SerializedPropertyType.ObjectReference)
						continue;
					if (!IsTargetOrNested(arg, propIterator.objectReferenceValue))
						continue;

					queue.Enqueue(propIterator.Copy());
				}
			}

			if (queue.Count == 0 && !prefabInstance) {
				entity = default;
				return false;
			}

			entity = World.NewEntityWith(out Result data);

			var gui = entity.Set<SearchResultGui>();
			gui.Properties = new List<SearchResultGui.PropertyData>();
			gui.SerializedObject = so;
			gui.Label = new GUIContent();

			// init header
			Texture2D miniTypeThumbnail = null;
			if (scene) {
				switch (suspect) {
					case Component component:
						data.RootGo = component.gameObject;
						gui.TransformPath = AnimationUtility.CalculateTransformPath(component.transform, null);
						gui.Label.image = AssetPreview.GetMiniThumbnail(data.RootGo);
						gui.Label.text = gui.TransformPath;
						break;
					case GameObject go:
						data.RootGo = go;
						gui.Label.image = AssetPreview.GetMiniThumbnail(data.RootGo);
						gui.TransformPath = AnimationUtility.CalculateTransformPath(go.transform, null);
						gui.Label.text = gui.TransformPath;
						break;
					default:
						throw new NotImplementedException();
				}

				miniTypeThumbnail = data.RootGo.GetInstanceID() == suspect.GetInstanceID()
					? null
					: AssetPreview.GetMiniThumbnail(suspect);
			}
			else {
				data.File = suspect;
				data.FilePath = AssetDatabase.GetAssetPath(data.File);
				data.MainFile = AssetDatabase.LoadMainAssetAtPath(data.FilePath);

				// todo
				var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(data.MainFile);
				switch (prefabInstanceStatus) {
					case PrefabInstanceStatus.Connected:
					case PrefabInstanceStatus.Disconnected:
						switch (data.File) {
							case Component comp:
								// transformPath = $"{AnimationUtility.CalculateTransformPath(comp.transform, null)}/".Replace("/", "/\n");
								gui.TransformPath =
									$"{AnimationUtility.CalculateTransformPath(comp.transform, null)}";
								break;
							case GameObject go:
								// transformPath = $"{AnimationUtility.CalculateTransformPath(go.transform, null)}/".Replace("/", "/\n");
								gui.TransformPath =
									$"{AnimationUtility.CalculateTransformPath(go.transform, null)}";
								break;
							default:
								// Assert.Fail("Not a component"); //todo
								break;
						}

						break;
					case PrefabInstanceStatus.NotAPrefab:
					case PrefabInstanceStatus.MissingAsset:
						if (!AssetDatabase.IsMainAsset(data.File)) {
							// {row.Main.name}
							gui.TransformPath = $"/{data.File.name}";
						}

						break;
				}

				gui.Label.text = data.FilePath.Replace(AssetsRootPath, string.Empty);
				gui.Label.image = AssetDatabase.GetCachedIcon(data.FilePath);
			}

			gui.Label.tooltip = gui.TransformPath;


			// init properties (footer)
			while (queue.Count > 0) {
				var prop = queue.Dequeue();
				var targetObject = prop.serializedObject.targetObject;
				var item = new SearchResultGui.PropertyData {
					Property = prop,
					Content = new GUIContent()
				};
				item.Content.image = miniTypeThumbnail;
				item.Content.text = Nicify(prop, targetObject, gui.TransformPath);
				item.Content.tooltip = gui.TransformPath;
				var typeName = targetObject.GetType().Name;
				if (StringComparer.Ordinal.Equals(typeName, targetObject.name))
					item.Content.tooltip = $"{gui.TransformPath}.{prop.propertyPath}";
				else
					item.Content.tooltip = $"{gui.TransformPath}({typeName}).{prop.propertyPath}";
				gui.Properties.Add(item: item);
			}

			return true;
		}

		public static bool IsFileIgrnoredBySettings(string path) {
			if (IgnoreTypes.Check(path, out _)) return true;
			if (IgnoredNonAssets(path)) return true;
			if (IgnoredPaths(path, out _)) return true;

			return false;
		}

		public static bool IgnoredPaths(string path, out string str) {
			var conf = Globals<Config>.Value;
			str = "";
			if (conf == null) return false;
			foreach (var substr in conf.IgnorePathContains) {
				Asr.IsNotNull(path);
				Asr.IsNotNull(substr);
				if (!path.Contains(substr)) continue;
				str = substr;
				return true;
			}

			str = default;
			return false;
		}

		public static bool IgnoredNonAssets(string path) {
			return !path.Contains("Assets/");
		}

		#region Project

		public static bool IsUnused(string path) {
			if (IsFileIgrnoredBySettings(path))
				return false;
			return !AnyDependencies(path);
		}

		static bool AnyDependencies(string path) {
			var store = Globals<BacklinkStore>.Value;
			if (store.UnusedFiles.Select(p => p.Key).Contains(path))
				return false;
			if (store.UnusedScenes.Select(p => p.Key).Contains(path))
				return false;
			return true;
		}

		public static void FilesThatReference(SearchArg arg) {
			var store = Globals<BacklinkStore>.Value;
			var path1 = AssetDatabase.GetAssetPath(arg.Target);

			if (!store.Backward.TryGetValue(path1, out var dep))
				return;

			foreach (var path in dep.Lookup) {
				var mainAsset = AssetDatabase.GetMainAssetTypeAtPath(path);
				if (mainAsset.IsAssignableFromInverse(typeof(SceneAsset)))
					continue;

				var any = false;
				if (mainAsset.IsAssignableFromInverse(typeof(GameObject))) { }
				else {
					var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(path);
					foreach (var suspect in allAssetsAtPath) {
						if (suspect is DefaultAsset || suspect is Transform || !suspect) continue;

						if (!SearchInChildProperties(arg, suspect, false, out var entity))
							continue;

						entity.Set<FileResultTag>();
						any = true;
					}
				}

				if (any) continue;

				// failed to find any property - just show main asset
				var e = World.NewEntity();
				var gui = e.Set<SearchResultGui>();
				gui.Properties = new List<SearchResultGui.PropertyData>();
				var main = AssetDatabase.LoadMainAssetAtPath(path);
				gui.Label = new GUIContent() {
					image = AssetPreview.GetMiniThumbnail(main),
					text = path.Replace(AssetsRootPath, string.Empty)
				};
				var res = e.Set<Result>();
				res.MainFile = main;
				e.Set<FileResultTag>();
			}
		}

		public static void ScenesThatContain(Object activeObject) {
			var store = Globals<BacklinkStore>.Value;
			var path1 = AssetDatabase.GetAssetPath(activeObject);
			if (!store.Backward.TryGetValue(path1, out var dep))
				return;

			foreach (var path in dep.Lookup) {
				if (!AssetDatabase.GetMainAssetTypeAtPath(path).IsAssignableFromInverse(typeof(SceneAsset)))
					continue;

				World.NewEntityWith(out SceneResult sp, out SceneDetails sceneDetails);
				sp.PathNicified = path.Replace("Assets/", string.Empty);

				// heavy
				sceneDetails.Path = path;
				var alreadyOpened = false;
				for (var i = 0; i < EditorSceneManager.loadedSceneCount; i++) {
					var cur = SceneManager.GetSceneAt(i);
					if (!cur.path.Eq(sceneDetails.Path)) continue;
					alreadyOpened = true;
					sceneDetails.Scene = cur;
				}

				sceneDetails.WasOpened = alreadyOpened;
			}
		}

		#endregion

		#region Scene

		static Pool<List<Component>> ComponentListPool = new Pool<List<Component>>(() => new List<Component>(), list => list.Clear());

		// todo provide explicit scene arg 
		public static void InScene(SearchArg arg, Scene currentScene) {
			var rootGameObjects = currentScene.GetRootGameObjects();

			foreach (var suspect in Traverse(rootGameObjects)) {
				if (!SearchInChildProperties(arg, suspect, scene: true, out var entity))
					continue;
				var s = entity.Set<InSceneResult>();
				s.ScenePath = arg.Scene.path;
			}

			IEnumerable<Object> Traverse(GameObject[] roots) {
				foreach (var rootGo in roots)
				foreach (var comp in GoAndChildComps(rootGo)) {
					yield return comp;
				}
			}

			IEnumerable<Object> GoAndChildComps(GameObject rr) {
				using (ComponentListPool.GetScoped(out var pooled)) {
					rr.GetComponents(pooled);
					foreach (var component in pooled) {
						if (component is Transform)
							continue;
						yield return component;
					}
				}

				var transform = rr.transform;
				var childCount = transform.childCount;
				for (int i = 0; i < childCount; i++) {
					var g = transform.GetChild(i).gameObject;
					foreach (var res in GoAndChildComps(g))
						yield return res;
				}
			}
		}
#if backup
     		static void InScene(SearchArg arg, Scene currentScene) {
			var allObjects = currentScene
				.GetRootGameObjects()
				.SelectMany(g => g.GetComponentsInChildren<Component>(true)
					.Where(c => c && !(c is Transform))
					.Union(Enumerable.Repeat(g as Object, 1))
				)
				.ToArray();

			var total = allObjects.Length;
			for (var i = 0; i < total; i++) {
				var suspect = allObjects[i];

				if (SearchInChildProperties(arg, suspect, true, out var entity)) {
					var s = entity.Set<InSceneResult>();
					s.ScenePath = arg.Scene.path;
				}
			}
		}
#endif

		static bool IsTargetOrNested(SearchArg target, Object suspect) {
			if (!suspect)
				return false;

			if (target.Target.GetInstanceID() == suspect.GetInstanceID() || target.Main.GetInstanceID() == (suspect).GetInstanceID())
				return true;

			if (target.SubAssets.TryGet(out var subassets))
				foreach (var asset in subassets) {
					if (asset.GetInstanceID() == (suspect).GetInstanceID())
						return true;
				}

			return false;
		}

		static string Nicify(SerializedProperty sp, Object o, string transformPath) {
			//            return sp.propertyPath;

			string nice;
			switch (o) {
				case AnimatorState animatorState:
					return animatorState.name;
				case Material material:
					nice = material.name;
					break;
				default: {
					nice = sp.propertyPath.Replace(".Array.data", string.Empty);
					if (nice.IndexOf(".m_PersistentCalls.m_Calls", StringComparison.Ordinal) > 0) {
						nice = nice.Replace(".m_PersistentCalls.m_Calls", string.Empty)
							.Replace(".m_Target", string.Empty);
					}

					nice = nice.Split('.').Select(t => ObjectNames.NicifyVariableName(t).Replace(" ", string.Empty))
						.Aggregate((a, b) => a + "." + b);
					break;
				}
			}

			// nice = $"{transformPath}({o.GetType().Name}).{nice}";
			nice = $"({o.GetType().Name}).{nice}";
			return nice;
		}

		const string AssetsRootPath = "Assets/";

		#endregion
	}
}