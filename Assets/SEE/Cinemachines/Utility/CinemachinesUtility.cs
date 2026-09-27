using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using SEE.Game;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE.Cinemachines.Utility
{
    /// <summary>
    /// Utility class for general functions shared between the custom Cinemachines components.
    /// </summary>
    internal static class CinemachinesUtility
    {
        #if UNITY_EDITOR

        #region Constant String names
        internal const string CinemachinesBrainsName = "CinemachinesBrains";
        internal const string CinemachinesScenesName = "Scenes";

        /// <summary>
        /// Name of the folder under <see cref="CinemachinesPrefabsRoot"/> holding the backups
        /// of whole Cinemachines roots, as distinct from the backups of single scenes.
        /// </summary>
        internal const string CinemachinesRootsName = "Roots";
        internal const string CinemachinesControlCameraName = "ControlCamera";
        internal const string CinemachinesMainOutputName = "CinemachinesMainOutput.renderTexture";
        internal const string CinemachinesPIPOutputName = "CinemachinesPIPOutput.renderTexture";

        internal const string CinemachinesPrefabsRoot = "Assets/Resources/Prefabs/Cinemachines";
        internal const string CinemachinesRootPrefabsRoot = "Prefabs/Cinemachines/CinemachinesRoot";
        internal const string CinemachinesAssetsRoot = "Assets/Cinemachines";

        internal const string CinemachinesPersistanceKeyName = "CinemachinesPersistanceKey";
        internal const string CinemachinesPersistanceKeyRestorableName = "CinemachinesPersistanceKeyRestorable";
        /// <summary>
        /// The name of the root GameObject, that contains all Cinemachines related GameObjects in the scene.
        /// </summary>
        private const string CinemachinesRootName = "CinemachinesRoot";
        #endregion Constant String names

        /// <summary>
        /// Returns the active CinemachineRoots-Transform if one exists in the current scene.
        /// </summary>
        /// <returns>Returns the active Transform that includes the Cinemachines root component, or null if none is found.</returns>
        internal static Transform GetCinemachinesRootInScene()
        {
            CinemachinesRoot[] CinemachinesRootComponents = FindAllCinemachinesRootsInScene();

            if (CinemachinesRootComponents.Length < 1)
            {
                return null;
            }

            return CinemachinesRootComponents[0].transform;
        }

        /// <summary>
        /// Returns the active CinemachinesRoot-Transform of <paramref name="unityScene"/>
        /// if one exists in that scene.
        /// </summary>
        /// <remarks>Unlike <see cref="GetCinemachinesRootInScene()"/>, which searches
        /// every loaded scene at once, this looks only in the one named. Several scenes
        /// can be open together, and then the root of one of them must not be taken for
        /// the root of another.</remarks>
        /// <param name="unityScene">The Unity scene to search.</param>
        /// <returns>The Transform carrying the Cinemachines root component in
        /// <paramref name="unityScene"/>, or null if there is none.</returns>
        internal static Transform GetCinemachinesRootInScene(Scene unityScene)
        {
            if (!unityScene.IsValid() || !unityScene.isLoaded)
            {
                return null;
            }
            foreach (GameObject rootGameObject in unityScene.GetRootGameObjects())
            {
                CinemachinesRoot found = rootGameObject.GetComponentInChildren<CinemachinesRoot>(false);
                if (found != null)
                {
                    return found.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all active <see cref="CinemachinesRoot"> components in the current Unity scene.
        /// </summary>
        /// <remarks>There should only be one active <see cref="CinemachinesRoot"> component in a Unity scene.</remarks>
        /// <returns>List of <see cref="CinemachinesRoot"> components in the current Unity scene.</returns>
        internal static CinemachinesRoot[] FindAllCinemachinesRootsInScene()
        {
            return UnityEngine.Object.FindObjectsByType<CinemachinesRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        /// <summary>
        /// Menu entry for adding a CinemachinesRoot to the current Unity scene.
        /// </summary>
        [MenuItem("SEE/Cinemachines/Add Cinemachines Root", false, 10)]
        [MenuItem("GameObject/SEE/Cinemachines/Add Cinemachines Root", false, 10)]
        internal static void AddCinemachinesRoot()
        {
            // Create a new CinemachinesRoot at the root of the scene
            new GameObject(CinemachinesRootName, typeof(CinemachinesRoot));
        }

        /// <summary>
        /// Menu entry bringing back a Cinemachines root stored earlier with
        /// <c>Backup Cinemachines Root</c>, with everything that was under it.
        /// </summary>
        /// <remarks>Not a button on the root, there being no root to put one on when this is
        /// wanted. It refuses where the Unity scene already holds a root, only one being
        /// supported, rather than adding a second and disabling one of them.</remarks>
        [MenuItem("SEE/Cinemachines/Add Cinemachines Root from Backup", false, 11)]
        internal static void AddCinemachinesRootFromBackup()
        {
            const string title = "Add Cinemachines Root from Backup";

            if (GetCinemachinesRootInScene() != null)
            {
                EditorUtility.DisplayDialog(title,
                                            "This Unity scene already has a Cinemachines root, and only one is "
                                            + "supported.\n\nRemove it first if you mean to replace it: select it "
                                            + "in the hierarchy and delete it. That leaves the timelines and "
                                            + "signals in the project untouched, which Delete Scene and Reset "
                                            + "Cinemachines would not.",
                                            "Okay");
                return;
            }

            string folder = $"{CinemachinesPrefabsRoot}/{CinemachinesRootsName}";
            if (!AssetDatabase.IsValidFolder(folder)
                || AssetDatabase.FindAssets("t:Prefab", new[] { folder }).Length == 0)
            {
                EditorUtility.DisplayDialog(title,
                                            "No Cinemachines root has been backed up.\n\n"
                                            + "Press Backup Cinemachines Root on a root to store it, with its "
                                            + $"brains, its control camera and all of its scenes. It is kept under "
                                            + $"{folder} until you delete it.",
                                            "Okay");
                return;
            }

            string chosen = EditorUtility.OpenFilePanel("Choose a stored Cinemachines root",
                                                        folder, "prefab");
            if (String.IsNullOrEmpty(chosen))
            {
                return;
            }

            string assetPath = ToAssetPath(chosen);
            if (assetPath == null)
            {
                EditorUtility.DisplayDialog(title,
                                            "That file is outside the project and cannot be loaded.\n\n"
                                            + $"Stored roots are kept under {folder}.",
                                            "Okay");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null || prefab.GetComponent<CinemachinesRoot>() == null)
            {
                EditorUtility.DisplayDialog(title,
                                            "That prefab is not a stored Cinemachines root.",
                                            "Okay");
                return;
            }

            GameObject restored = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (restored == null)
            {
                Debug.LogError($"The stored root {assetPath} could not be instantiated.\n");
                return;
            }
            Undo.RegisterCreatedObjectUndo(restored, title);

            // Detached from the prefab, so that working on the restored root leaves the
            // backup as it was.
            PrefabUtility.UnpackPrefabInstance(restored, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            restored.name = CinemachinesRootName;

            // Each scene owns no folder, the backup having forgotten them, so each is given
            // one of its own here.
            CinemachinesScene[] scenes = restored.GetComponentsInChildren<CinemachinesScene>(includeInactive: true);
            foreach (CinemachinesScene scene in scenes)
            {
                GenerateSceneStructure(scene.gameObject, scene.gameObject.name);

                // And its own copy of the timeline and signals, so that the backup is left
                // as it was and can be restored again.
                scene.TryGetComponent(out SignalReceiver receiver);
                CopySceneAssetsInto(AssetFolderOfBackup(assetPath, scene.name),
                                    scene,
                                    scene.GetComponent<PlayableDirector>(),
                                    receiver);
            }

            ReportLostReferences(title, "The stored root", restored, scenes);

            Selection.activeGameObject = restored;
            Debug.Log($"The Cinemachines root has been restored from {assetPath} with {scenes.Length} scene(s).\n",
                      restored);
        }

        /// <summary>
        /// The project-relative path of <paramref name="absolute"/>, or null where that file
        /// lies outside the project.
        /// </summary>
        /// <param name="absolute">An absolute path, as a file dialog returns.</param>
        /// <returns>The path below <c>Assets</c>, or null.</returns>
        private static string ToAssetPath(string absolute)
        {
            string project = Application.dataPath.Replace('\\', '/');
            string chosen = absolute.Replace('\\', '/');
            return chosen.StartsWith(project, StringComparison.OrdinalIgnoreCase)
                ? "Assets" + chosen.Substring(project.Length)
                : null;
        }

        /// <summary>
        /// Helper Function to generate the "Scene Deletion Warning" message,
        /// including the path to the respective folder.
        /// </summary>
        /// <param name="guid">The GUID of the folder for the corresponding Cinemachines scene.</param>
        /// <returns>The message generated for the Cinemachines scene.</returns>
        internal static string GetSceneDeletionWarningMessage(string guid)
        {
            return $"Deleting the scene will also delete its associated scene folder, which is \"{AssetDatabase.GUIDToAssetPath(guid)}\"";
        }

        /// <summary>
        /// Helper Function to generate the scene folder based on the currently active Unity scene.
        /// </summary>
        /// <param name="sceneName">The name of the Cinemachines scene.</param>
        /// <returns>The GUID of the Cinemachines scene folder.</returns>
        internal static string GenerateSceneFolder(string sceneName)
        {
            // create new folder for Scene in Assets/Cinemachines/Scenes
            string sceneGUID = AssetDatabase.CreateFolder($"{CinemachinesAssetsRoot}/Scenes/{SceneManager.GetActiveScene().name}", $"{sceneName}");
            // Create signals folder to store timeline signals.
            if (!AssetDatabase.IsValidFolder($"{AssetDatabase.GUIDToAssetPath(sceneGUID)}/Signals"))
            {
                AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(sceneGUID), "Signals");
            }
            return sceneGUID;
        }

        /// <summary>
        /// Creates the Cinemachines prefab structure.
        /// </summary>
        internal static void GenerateCinemachinesPrefabFolder(string leaf = CinemachinesScenesName)
        {
            // If the Directory doesn't exist, create it.
            if (!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/{leaf}"))
            {
                // Check and create Sub-Directories, if they don't exist
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
                }

                if (!AssetDatabase.IsValidFolder(CinemachinesPrefabsRoot))
                {
                    AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "Cinemachines");
                }

                if (!AssetDatabase.IsValidFolder($"{CinemachinesPrefabsRoot}/{leaf}"))
                {
                    AssetDatabase.CreateFolder(CinemachinesPrefabsRoot, leaf);
                }

                Debug.Log($"Created Folder Structure: {CinemachinesPrefabsRoot}/{leaf}.\n");
            }
        }

        /// <summary>
        /// Creates the scene structure.
        /// </summary>
        /// <param name="scene">The Cinemachine scene GameObject.</param>
        /// <param name="sceneName">The name of the Cinemachines scene.</param>
        internal static void GenerateSceneStructure(GameObject scene, string sceneName)
        {
            // Add the CinemachinesScenes Component to the newly created scene GameObject
            scene.GetComponent<CinemachinesScene>().SceneGUID = GenerateSceneFolder(sceneName);
        }

        /// <summary>
        /// Constructs the name of the object.
        /// </summary>
        /// <param name="objectType">Type of Object the name should be constructed.</param>
        /// <param name="objectCount">Amount of Objects already created.</param>
        /// <exception cref="ArgumentException">Gets thrown, if the objectType is not defined or invalid.</exception>
        /// <returns>Fully constructed name for the object.</returns>
        internal static string GetNewObjectName(string objectType, ref int objectCount, ref string suffixText)
        {
            if (string.IsNullOrEmpty(objectType))
            {
                throw new ArgumentException($"{nameof(objectType)} string must neither be empty nor null.");
            }

            // Form name based on type and count
            string newName = $"{objectType}{objectCount}";

            // add optional suffix to name, if one is defined
            if (!String.IsNullOrWhiteSpace(suffixText))
            {
                newName += $" - {suffixText}";
            }

            // Reset the text field and increment the counter
            suffixText = "";
            objectCount += 1;

            return newName;
        }

        /// <summary>
        /// Constructs any GameObject with only one component added.
        /// </summary>
        /// <param name="objectType">Type of object the name should be constructed.</param>
        /// <param name="objectCount">Amount of objects already created.</param>
        /// <param name="rootGameObject">The root GameObject, that the new
        /// GameObject should be attached to.</param>
        /// <param name="componentToAdd">The component to add to the newly created
        /// GameObject. By default, it will not add any components.</param>
        /// <param name="shouldBeFocused">Whether the newly created GameObject
        /// should be selected or not. By default, it will get selected.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="objectType"/> or
        /// <paramref name="rootGameObject"/> are not defined or invalid.</exception>
        internal static void CreateGameObject
            (string objectType,
             ref int objectCount,
             ref string suffixText,
             GameObject rootGameObject,
             System.Type componentToAdd = null,
             bool shouldBeFocused = true)
        {
            // Throw exception, if objectType is empty or null
            if (string.IsNullOrEmpty(objectType))
            {
                throw new ArgumentException($"{nameof(objectType)} string must neither be empty nor null");
            }

            // Throw exception, if rootGameObject is null
            if (rootGameObject == null)
            {
                throw new ArgumentNullException(nameof(rootGameObject), $"{nameof(rootGameObject)} cannot be null.");
            }

            string objectName = GetNewObjectName(objectType, ref objectCount, ref suffixText);

            GameObject newObject = componentToAdd == null ? new GameObject(objectName) : new GameObject(objectName, componentToAdd);
            // Create a new GameObject, and place it in under the correct GameObject
            newObject.transform.SetParent(rootGameObject.transform);

            // Select newly created GameObject
            if (shouldBeFocused)
            {
                Selection.activeGameObject = newObject;
            }

            // Sets the new Objects hideFlags to not Save into a build
            newObject.tag = Tags.EditorOnly;
        }

        #region Assets of a scene

        /// <summary>
        /// Name of the folder holding the signal assets of a Cinemachines scene.
        /// </summary>
        internal const string CinemachinesSignalsName = "Signals";

        /// <summary>
        /// Copies the assets of one Cinemachines scene — its timeline and its signals — from
        /// <paramref name="fromFolder"/> into <paramref name="toFolder"/> and points
        /// <paramref name="director"/> at the copies.
        /// </summary>
        /// <remarks>This is what makes a backup stand on its own. Without it a stored scene
        /// refers to the timeline of the scene it was copied from, so deleting that scene
        /// takes the sequence with it and two restores of one backup drive a single
        /// timeline.
        ///
        /// Three things have to be carried over by hand. Copying a folder does not point the
        /// copies at each other, so the copied timeline's signal emitters still name the
        /// original signals. The receiver's reactions are keyed by signal asset and name the
        /// originals likewise. And a director's bindings are keyed by the track object,
        /// so the copy's tracks, being other objects, arrive bound to nothing.</remarks>
        /// <param name="fromFolder">The folder holding the assets to copy.</param>
        /// <param name="toFolder">The folder to copy them into; created if absent.</param>
        /// <param name="director">The director to point at the copies.</param>
        /// <param name="receiver">The receiver whose reactions are to be re-keyed, or null.</param>
        /// <returns>True if the assets were copied and the director repointed.</returns>
        internal static bool CopySceneAssets(string fromFolder, string toFolder,
                                             PlayableDirector director, SignalReceiver receiver)
        {
            if (director == null || String.IsNullOrWhiteSpace(fromFolder)
                || !AssetDatabase.IsValidFolder(fromFolder))
            {
                return false;
            }
            if (director.playableAsset is not TimelineAsset original)
            {
                return false;
            }

            CreateFolderPath(toFolder);

            // The signals first, the copied timeline being pointed at them afterwards.
            Dictionary<string, SignalAsset> copiedSignals = new();
            string fromSignals = $"{fromFolder}/{CinemachinesSignalsName}";
            if (AssetDatabase.IsValidFolder(fromSignals))
            {
                string toSignals = $"{toFolder}/{CinemachinesSignalsName}";
                CreateFolderPath(toSignals);
                foreach (string guid in AssetDatabase.FindAssets("t:SignalAsset", new[] { fromSignals }))
                {
                    string signalPath = AssetDatabase.GUIDToAssetPath(guid);
                    string target = $"{toSignals}/{System.IO.Path.GetFileName(signalPath)}";
                    if (AssetDatabase.CopyAsset(signalPath, target)
                        && AssetDatabase.LoadAssetAtPath<SignalAsset>(target) is SignalAsset copiedSignal)
                    {
                        copiedSignals[copiedSignal.name] = copiedSignal;
                    }
                }
            }

            string timelinePath = AssetDatabase.GetAssetPath(original);
            string timelineTarget = $"{toFolder}/{System.IO.Path.GetFileName(timelinePath)}";
            if (!AssetDatabase.CopyAsset(timelinePath, timelineTarget)
                || AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineTarget) is not TimelineAsset copy)
            {
                Debug.LogError($"The timeline {timelinePath} could not be copied to {timelineTarget}.\n");
                return false;
            }

            RebindSignals(copy, receiver, copiedSignals);
            CarryBindingsOver(director, original, copy);

            director.playableAsset = copy;
            EditorUtility.SetDirty(copy);
            EditorUtility.SetDirty(director);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// Points the signal emitters of <paramref name="timeline"/>, and the reactions of
        /// <paramref name="receiver"/>, at the copied signals rather than the originals.
        /// </summary>
        /// <param name="timeline">The copied timeline.</param>
        /// <param name="receiver">The receiver whose reactions are keyed by signal, or null.</param>
        /// <param name="copiedSignals">The copied signals, by name.</param>
        private static void RebindSignals(TimelineAsset timeline, SignalReceiver receiver,
                                          Dictionary<string, SignalAsset> copiedSignals)
        {
            if (copiedSignals.Count == 0)
            {
                return;
            }

            foreach (TrackAsset track in TracksOf(timeline))
            {
                foreach (IMarker marker in track.GetMarkers())
                {
                    if (marker is SignalEmitter emitter && emitter.asset != null
                        && copiedSignals.TryGetValue(emitter.asset.name, out SignalAsset copied))
                    {
                        emitter.asset = copied;
                    }
                }
            }

            if (receiver != null)
            {
                for (int i = 0; i < receiver.Count(); i++)
                {
                    SignalAsset key = receiver.GetSignalAssetAtIndex(i);
                    if (key != null && copiedSignals.TryGetValue(key.name, out SignalAsset copied))
                    {
                        receiver.ChangeSignalAtIndex(i, copied);
                    }
                }
                EditorUtility.SetDirty(receiver);
            }
        }

        /// <summary>
        /// Gives the tracks of <paramref name="copy"/> the bindings the director held for the
        /// corresponding tracks of <paramref name="original"/>.
        /// </summary>
        /// <remarks>A binding is keyed by the track it belongs to, so a copied timeline, whose
        /// tracks are different objects, would otherwise arrive bound to nothing at all — no
        /// brain on its Cinemachine track, no receiver on its signal track. The tracks are
        /// matched by their position, a copy listing them in the order the original does.</remarks>
        /// <param name="director">The director holding the bindings.</param>
        /// <param name="original">The timeline the bindings are keyed against.</param>
        /// <param name="copy">The copied timeline.</param>
        private static void CarryBindingsOver(PlayableDirector director, TimelineAsset original, TimelineAsset copy)
        {
            List<TrackAsset> before = TracksOf(original).ToList();
            List<TrackAsset> after = TracksOf(copy).ToList();
            if (before.Count != after.Count)
            {
                Debug.LogWarning($"The copy of {original.name} has {after.Count} tracks where the original has "
                                 + $"{before.Count}. Bindings are carried over as far as they match.\n");
            }
            for (int i = 0; i < Math.Min(before.Count, after.Count); i++)
            {
                UnityEngine.Object bound = director.GetGenericBinding(before[i]);
                if (bound != null)
                {
                    director.SetGenericBinding(after[i], bound);
                }
            }
        }

        /// <summary>
        /// Every track of <paramref name="timeline"/> that can carry clips or markers.
        /// </summary>
        /// <param name="timeline">The timeline to look through.</param>
        /// <returns>The output tracks, and the timeline's own marker track where it has one.</returns>
        private static List<TrackAsset> TracksOf(TimelineAsset timeline)
        {
            List<TrackAsset> tracks = timeline.GetOutputTracks().ToList();
            if (timeline.markerTrack != null && !tracks.Contains(timeline.markerTrack))
            {
                tracks.Add(timeline.markerTrack);
            }
            return tracks;
        }

        /// <summary>
        /// Creates <paramref name="folder"/> and whatever of its parents is missing.
        /// </summary>
        /// <param name="folder">An asset path such as <c>Assets/a/b/c</c>.</param>
        internal static void CreateFolderPath(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string[] parts = folder.Split('/');
            string walked = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{walked}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(walked, parts[i]);
                }
                walked = next;
            }
        }

        /// <summary>
        /// Copies the assets stored with a backup into the folder <paramref name="scene"/>
        /// has just been given, and points <paramref name="director"/> at the copies.
        /// </summary>
        /// <remarks>Says so and carries on where the backup has none, which is what a backup
        /// taken before backups held their own assets looks like, and what one whose source
        /// scene was deleted before this was written looks like too. The restored scene then
        /// refers to whatever the prefab referred to, as it always did.</remarks>
        /// <param name="storedFolder">The folder of assets stored with the backup.</param>
        /// <param name="scene">The restored scene, already given a folder of its own.</param>
        /// <param name="director">The director of the restored scene.</param>
        /// <param name="receiver">The receiver of the restored scene, or null.</param>
        internal static void CopySceneAssetsInto(string storedFolder, CinemachinesScene scene,
                                                 PlayableDirector director, SignalReceiver receiver)
        {
            if (!AssetDatabase.IsValidFolder(storedFolder))
            {
                Debug.LogWarning($"The backup of {scene.name} holds no assets of its own, so the restored scene "
                                 + "shares the timeline the backup refers to. Editing it will edit that one.\n",
                                 scene);
                return;
            }
            string into = AssetDatabase.GUIDToAssetPath(scene.SceneGUID);
            if (String.IsNullOrWhiteSpace(into))
            {
                Debug.LogWarning($"{scene.name} has no folder to copy its assets into.\n", scene);
                return;
            }
            if (!CopySceneAssets(storedFolder, into, director, receiver))
            {
                Debug.LogWarning($"The assets stored with the backup of {scene.name} could not be copied into "
                                 + $"{into}.\n", scene);
            }
        }

        /// <summary>
        /// The folder in which the assets of a stored Cinemachines scene are kept, beside the
        /// prefab holding its game objects.
        /// </summary>
        /// <param name="prefabPath">The asset path of the prefab.</param>
        /// <param name="sceneName">The name of the Cinemachines scene, for a stored root
        /// holding several; empty for a stored single scene.</param>
        /// <returns>The folder path.</returns>
        internal static string AssetFolderOfBackup(string prefabPath, string sceneName = "")
        {
            string folder = prefabPath.EndsWith(".prefab")
                ? prefabPath.Substring(0, prefabPath.Length - ".prefab".Length)
                : prefabPath;
            return String.IsNullOrWhiteSpace(sceneName) ? folder : $"{folder}/{sceneName}";
        }

        #endregion Assets of a scene

        #region References a backup cannot keep

        /// <summary>
        /// Describes every reference within <paramref name="stored"/> that names an object of
        /// the Unity scene outside it, and which a prefab therefore cannot keep.
        /// </summary>
        /// <remarks>Taken before the prefab is written, the prefab having no way to say
        /// afterwards which of its empty properties once held something.</remarks>
        /// <param name="stored">The scene object about to be stored.</param>
        /// <returns>One line for each, naming where it sits, which property holds it, and
        /// what it referred to.</returns>
        internal static List<LostReference> ReferencesLeaving(GameObject stored)
        {
            List<LostReference> leaving = new();

            foreach (Component component in stored.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (component == null)
                {
                    // A component whose script has gone missing has nothing to read.
                    continue;
                }

                using SerializedObject serialized = new(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(enterChildren: true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }
                    UnityEngine.Object referenced = property.objectReferenceValue;
                    // An asset of the project survives in a prefab; only what lives in the
                    // Unity scene is lost, and only if it lies outside what is being stored.
                    if (referenced != null
                        && !EditorUtility.IsPersistent(referenced)
                        && !IsWithin(stored, referenced))
                    {
                        leaving.Add(new LostReference
                        {
                            Path = PathWithin(stored, component.transform),
                            Component = component.GetType().Name,
                            Property = property.displayName,
                            Target = referenced.name
                        });
                    }
                }
            }

            return leaving;
        }

        /// <summary>
        /// Whether <paramref name="referenced"/> is <paramref name="stored"/> or something
        /// below it.
        /// </summary>
        /// <param name="stored">The scene object being stored.</param>
        /// <param name="referenced">The object referred to.</param>
        /// <returns>True if the reference stays inside what is being stored.</returns>
        private static bool IsWithin(GameObject stored, UnityEngine.Object referenced)
        {
            GameObject owner = referenced as GameObject;
            if (owner == null && referenced is Component component)
            {
                owner = component.gameObject;
            }
            return owner != null && owner.transform.IsChildOf(stored.transform);
        }

        /// <summary>
        /// The path of <paramref name="transform"/> below <paramref name="stored"/>, so that
        /// a report names where in the scene the reference sits.
        /// </summary>
        /// <param name="stored">The scene object being stored.</param>
        /// <param name="transform">The transform whose path is wanted.</param>
        /// <returns>The names from <paramref name="stored"/> down, separated by slashes.</returns>
        private static string PathWithin(GameObject stored, Transform transform)
        {
            if (transform == stored.transform)
            {
                return "";
            }
            string path = transform.name;
            while (transform.parent != null && transform.parent != stored.transform)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }

        /// <summary>
        /// One reference that a backup could not keep: which game object held it, which
        /// property of which component, and what it named.
        /// </summary>
        /// <remarks>Kept in pieces rather than as a sentence so that the game object can be
        /// found again when the backup is brought back. What has to be put right is a
        /// camera or a receiver somewhere below the restored object, and saying only which
        /// scene it belongs to leaves the reader to search for it.</remarks>
        [Serializable]
        internal struct LostReference
        {
            /// <summary>
            /// The path of the game object that held the reference, below the object that
            /// was stored, in the form <see cref="Transform.Find"/> takes. Empty where the
            /// stored object itself held it.
            /// </summary>
            public string Path;

            /// <summary>
            /// The name of the type of component that held the reference.
            /// </summary>
            public string Component;

            /// <summary>
            /// The name of the property that held the reference, as the inspector shows it.
            /// </summary>
            public string Property;

            /// <summary>
            /// The name of the object that was referred to.
            /// </summary>
            public string Target;
        }

        /// <summary>
        /// Tells the user which references the given scenes lost when they were stored, and
        /// then forgets them, they having been reported.
        /// </summary>
        /// <param name="title">The title of the dialog, naming the operation reporting.</param>
        /// <param name="what">What was brought back, named for the reader.</param>
        /// <param name="scenes">The restored Cinemachines scenes.</param>
        internal static void ReportLostReferences(string title, string what, GameObject restored,
                                                  IEnumerable<CinemachinesScene> scenes)
        {
            List<LostReference> lost = new();
            List<CinemachinesScene> reported = new();
            foreach (CinemachinesScene scene in scenes)
            {
                if (scene != null && scene.LostReferences.Count > 0)
                {
                    lost.AddRange(scene.LostReferences);
                    reported.Add(scene);
                }
            }

            if (lost.Count == 0)
            {
                return;
            }

            // An entry that says nothing at all came from a backup taken before these were
            // recorded in pieces, when each was one sentence. A sentence cannot be read back
            // as the record it became, so the count survives and the content does not. Those
            // are counted and explained rather than printed as a row of blanks.
            List<LostReference> readable = lost.Where(Names).ToList();
            int unreadable = lost.Count - readable.Count;

            System.Text.StringBuilder message = new();
            if (readable.Count > 0)
            {
                message.Append($"{what} referred to {readable.Count} object(s) of the Unity scene, which a "
                               + "backup cannot hold. They have come back empty and must be named again:\n\n");
            }

            // The console keeps what the dialog does not. One entry for each reference
            // rather than one holding them all: the console lists only the first line of a
            // message, so a single entry would hide the very thing it is kept for. Apart,
            // each can be searched for, and each names and points at the game object that
            // has to be put right, so clicking it selects that object in the hierarchy.
            foreach (LostReference reference in readable)
            {
                GameObject owner = OwnerOf(restored, reference.Path);
                string where = owner != null ? PathOf(owner) : DescribePath(restored, reference.Path);

                Debug.LogWarning($"Cinemachines backup: {where} has lost the {reference.Property} of its "
                                 + $"{reference.Component}, which named \"{reference.Target}\". "
                                 + "Name it again.\n",
                                 owner != null ? owner : restored);

                message.Append($"  {where}\n      {reference.Component}.{reference.Property} "
                               + $"→ {reference.Target}\n");
            }

            if (unreadable > 0)
            {
                string note = $"This backup also noted {unreadable} reference(s) that cannot be read: it was "
                              + "taken by an older version of the framework, which recorded them as prose "
                              + "rather than in parts. Take the backup again to have them named. Until then, "
                              + "check the tracking targets and the signal reactions by hand.";
                Debug.LogWarning($"Cinemachines backup: {note}\n", restored);
                message.Append(readable.Count > 0 ? $"\n{note}\n" : $"{note}\n");
            }

            if (readable.Count > 0)
            {
                message.Append("\nEach is in the console as well, one line apiece, and will still be there "
                               + "when this window is gone. Clicking one selects the game object it is about.");
            }

            EditorUtility.DisplayDialog(title, message.ToString(), "Okay");

            foreach (CinemachinesScene scene in reported)
            {
                scene.ForgetLostReferences();
            }
        }

        /// <summary>
        /// Whether <paramref name="reference"/> says anything, which one read back from an
        /// older backup does not.
        /// </summary>
        /// <param name="reference">The recorded reference.</param>
        /// <returns>True if it names a component, a property or a target.</returns>
        private static bool Names(LostReference reference)
        {
            return !String.IsNullOrEmpty(reference.Component)
                   || !String.IsNullOrEmpty(reference.Property)
                   || !String.IsNullOrEmpty(reference.Target);
        }

        /// <summary>
        /// The game object at <paramref name="path"/> below <paramref name="restored"/>, or
        /// null where there is none.
        /// </summary>
        /// <param name="restored">The object that was stored, now brought back.</param>
        /// <param name="path">The path recorded when the reference was lost.</param>
        /// <returns>The game object that held the reference, or null.</returns>
        private static GameObject OwnerOf(GameObject restored, string path)
        {
            if (restored == null)
            {
                return null;
            }
            if (String.IsNullOrEmpty(path))
            {
                return restored;
            }
            Transform found = restored.transform.Find(path);
            return found != null ? found.gameObject : null;
        }

        /// <summary>
        /// The path of <paramref name="gameObject"/> from the root of its Unity scene, as the
        /// hierarchy shows it.
        /// </summary>
        /// <param name="gameObject">The object to name.</param>
        /// <returns>The names of it and its ancestors, separated by slashes.</returns>
        private static string PathOf(GameObject gameObject)
        {
            string path = gameObject.name;
            for (Transform parent = gameObject.transform.parent; parent != null; parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }
            return path;
        }

        /// <summary>
        /// What to call a game object that was recorded but cannot be found again, which is
        /// what a renamed or deleted one comes to.
        /// </summary>
        /// <param name="restored">The object that was stored, now brought back.</param>
        /// <param name="path">The path recorded when the reference was lost.</param>
        /// <returns>The path as it was recorded, marked as no longer findable.</returns>
        private static string DescribePath(GameObject restored, string path)
        {
            string under = restored != null ? PathOf(restored) : "the restored object";
            return String.IsNullOrEmpty(path)
                ? $"{under} (not found)"
                : $"{under}/{path} (not found)";
        }

        #endregion References a backup cannot keep

        #endif
    }
}
