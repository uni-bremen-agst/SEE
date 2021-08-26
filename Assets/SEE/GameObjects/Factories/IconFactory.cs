using System;
using SEE.DataModel;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SEE.GO
{
    /// <summary>
    /// A factory to generate sprites from sprite prefab files for all types of
    /// software erosion kinds we currently support (architecture violation, clones,
    /// cycles, dead code, metrics, style, and universal).
    /// </summary>
    public class IconFactory
    {
        /// The sprite prefabs are already generated in the Unity editor from the
        /// original SVG files. To do that, the SVG file must first be imported
        /// in the editor and then a sprite must be generated. The sprite can then
        /// be added to the scene as a new game object, which creates an instance
        /// in the scene hierarchy. From the scene hierarchy, the instance can be
        /// moved to the Asset folder by click and drop to turn the instance into
        /// a prefab, wich generates a prefab file. This prefab file can then be
        /// loaded here.

        /// <summary>
        /// The different types of software erosions.
        /// </summary>
        public enum Erosion
        {
            Architecture_Violation = 0,
            Clone = 1,
            Cycle = 2,
            Dead_Code = 3,
            Metric = 4,
            Style = 5,
            Universal = 6
        }

        /// <summary>
        /// Returns an erosion as human readable string.
        /// </summary>
        /// <param name="erosion">erosion type for which to yield a string</param>
        /// <returns>human readable string representation</returns>
        public static string ToString(Erosion erosion) =>
            erosion switch
            {
                Erosion.Architecture_Violation => "Architecture_Violation",
                Erosion.Clone => "Clone",
                Erosion.Cycle => "Cycle",
                Erosion.Dead_Code => "Dead_Code",
                Erosion.Metric => "Metric",
                Erosion.Style => "Style",
                Erosion.Universal => "Universal",
                _ => "UNDEFINED"
            };

        // Relative screen space required so that a erosion sprite becomes visible.
        // If the size of the sprite is below this value, it will be culled.
        private const float ScreenRelativeTransitionHeight = 0.02f;

        /// <summary>
        /// The paths to the sprite prefab files.
        /// The viewBox of the original SVG files from which those prefabs were
        /// create is 0 0 1194.11 1161.28. Thus, the aspect ratio is roughly 1194:1161,
        /// or even more roughly 1:1.
        /// Note: The path is relative to any folder named Resources inside the Assets folder of your project.
        /// Note: Extensions must be omitted.
        /// Note: All asset names and paths in Unity use forward slashes, paths using backslashes will not work.
        /// </summary>
        private static readonly string[] paths = {
            "Icons/architectureSprite",
            "Icons/cloneSprite",
            "Icons/cycleSprite",
            "Icons/deadcodeSprite",
            "Icons/metricSprite",
            "Icons/stilSprite",
            "Icons/universalSprite"
        };

        /// <summary>
        /// Constructor is made private to prevent instantiations outside of
        /// this class. Loads all sprite prefabs from the Assets.
        /// </summary>
        private IconFactory()
        {
            erosionToSprite = LoadAllSprites();
        }

        // The single instance of this class.

        /// <summary>
        /// The single instance of this class.
        /// </summary>
        public static IconFactory Instance { get; } = new IconFactory();

        // A mapping of Erosions onto sprite prefabs. During start up we load the
        // sprite prefabs from the Assets for each kind of Erosion and store them
        // in this field. Later when game objects are to be created from these
        // prefabs, we look those prefabs up in this mapping.
        private readonly UnityEngine.Object[] erosionToSprite;

        /// <summary>
        /// Loads the sprite prefabs from the Assets for all kinds of Erosions.
        /// </summary>
        /// <returns>mapping of Erosions onto sprite prefabs</returns>
        private static UnityEngine.Object[] LoadAllSprites()
        {
            Erosion[] erosionIssues = (Erosion[])Enum.GetValues(typeof(Erosion));
            UnityEngine.Object[] result = new UnityEngine.Object[erosionIssues.Length];

            foreach (Erosion erosion in erosionIssues)
            {
                result[(int)erosion] = LoadSprite(paths[(int)erosion]);
            }
            return result;
        }

        /// <summary>
        /// Loads a sprite prefab from the given file. May return null if the file cannot
        /// be loaded.
        /// </summary>
        /// <param name="filename">name of the file containing the sprite prefab</param>
        /// <returns>the game objects loaded as a prefab from given file</returns>
        public static UnityEngine.Object LoadSprite(string filename)
        {
            try
            {
                UnityEngine.Object prefab = Resources.Load<GameObject>(filename);
                if (prefab == null)
                {
                    Debug.LogError($"Loading sprite prefab from file {filename} failed.\n");
                }
                return prefab;

            }
            catch (Exception e)
            {
                Debug.LogError($"Loading Sprite prefab from file {filename} failed: {e}.\n");
            }
            return null;
        }

        /// <summary>
        /// Generates a sprite game object for the given kind of Erosion at the given
        /// location. The name of the game object is the kind of Erosion. The game
        /// object returned is a composite of different level-of-detail (LOD) objects
        /// nested in an LOD group. Currently, there is only one such child object,
        /// which is the sprite for the given kind of erosion. It will be culled by
        /// ScreenRelativeTransitionHeight.
        ///
        /// Apart from the sprite itself, a TextMeshPro with the given <paramref name="metricText"/> is placed
        /// next to it, using a horizontal layout group.
        ///
        /// This function may be called in editor mode.
        /// </summary>
        /// <param name="position">the location for positioning the new game object</param>
        /// <param name="erosion">the kind of Erosion for which to generate the game object</param>
        /// <param name="metricText">Short text containing information about this erosion,
        /// e.g. number of occurrences</param>
        /// <param name="color">The color in which to render the sprite</param>
        /// <returns>a new game object for this type of erosion</returns>
        public GameObject GetIcon(Vector3 position, Erosion erosion, string metricText, Color color = default)
        {
            GameObject gameObject = new GameObject(ToString(erosion))
            {
                tag = Tags.Erosion
            };
            gameObject.transform.position = position;
            HorizontalLayoutGroup layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 7;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;

            // Programmatically create a LOD group and add LOD levels.
            LODGroup group = gameObject.AddComponent<LODGroup>();

            // Add LOD levels (currently only one).
            LOD[] lods = new LOD[1];
            
            // Add metric text label
            GameObject text = PrefabInstantiator.InstantiatePrefab("Prefabs/Metric Value Label", gameObject.transform);
            TextMeshPro MetricMesh = text.GetComponent<TextMeshPro>();
            MetricMesh.text = metricText;
            MetricMesh.enableAutoSizing = true;
            text.SetActive(false);
            
            // Add the erosion sprite.
            GameObject erosionSprite;
            UnityEngine.Object prefab = erosionToSprite[(int)erosion];
            if (prefab == null)
            {
                // Let sphere be the fallback icon if we do not have a prefab for this erosion kind
                erosionSprite = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }
            else
            {
                string prefabName = prefab.name;
                erosionSprite = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                Assert.IsNotNull(erosionSprite);
                erosionSprite.name = prefabName;
                erosionSprite.AddComponent<RectTransform>();
            }
            erosionSprite.transform.SetParent(gameObject.transform, false);
            erosionSprite.transform.SetAsFirstSibling();
            SpriteRenderer renderer = erosionSprite.GetComponent<SpriteRenderer>();
            if (color != default)
            {
                renderer.color = color;
            }
            Renderer[] renderers = { renderer };
            lods[0] = new LOD(ScreenRelativeTransitionHeight, renderers);
                
            group.SetLODs(lods);
            group.RecalculateBounds();
            return gameObject;
        }
    }
}
