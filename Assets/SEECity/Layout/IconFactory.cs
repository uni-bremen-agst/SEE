using System.IO;
using UnityEngine;
using System;
using SEE.DataModel;

namespace SEE.Layout
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
        public static string ToString(Erosion erosion)
        {
            switch (erosion)
            {
                case Erosion.Architecture_Violation: return "Architecture_Violation";
                case Erosion.Clone: return "Clone";
                case Erosion.Cycle: return "Cycle";
                case Erosion.Dead_Code: return "Dead_Code";
                case Erosion.Metric: return "Metric";
                case Erosion.Style: return "Style";
                case Erosion.Universal: return "Universal";
                default: return "UNDEFINED";
            }
        }

        /// <summary>
        /// The paths to the sprite prefab files. 
        /// The viewBox of the original SVG files from which those prefabs were
        /// create is 0 0 1194.11 1161.28. Thus, the aspect ratio is roughly 1194:1161,
        /// or even more roughly 1:1.
        /// </summary>
        private static readonly string[] paths = new string[]
            {
            "Assets/Resources/Icons/architectureSprite.prefab",
            "Assets/Resources/Icons/cloneSprite.prefab",
            "Assets/Resources/Icons/cycleSprite.prefab",
            "Assets/Resources/Icons/deadcodeSprite.prefab",
            "Assets/Resources/Icons/metricSprite.prefab",
            "Assets/Resources/Icons/stilSprite.prefab",
            "Assets/Resources/Icons/universalSprite.prefab"
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
        private static readonly IconFactory instance = new IconFactory();

        /// <summary>
        /// The single instance of this class.
        /// </summary>
        public static IconFactory Instance
        {
            get => instance;
        }

        [SerializeField]
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
        /// <returns></returns>
        private static UnityEngine.Object LoadSprite(string filename)
        {
            Debug.LogFormat("Load sprite prefab from file {0}\n", filename);
            try
            {
                if (File.Exists(filename))
                {
                    UnityEngine.Object prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(filename, typeof(GameObject));
                    if (prefab == null)
                    {
                        Debug.LogErrorFormat("Loading sprite prefab from file {0} failed.\n", filename);
                    }
                    return prefab;
                }
                else
                {
                    Debug.LogErrorFormat("Sprite prefab file does not exist: {0}.\n", filename);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Loading Sprite prefab from file {0} failed: {1}", filename, e.ToString());
            }
            return null;
        }

        /// <summary>
        /// Generates a sprite game object for the given kind of Erosion at the given
        /// location. The name of the game object is the kind of Erosion. May return
        /// null, if a prefab could not be loaded previously. This function may be
        /// called in editor mode.
        /// </summary>
        /// <param name="position">the location for positioning the new game object</param>
        /// <param name="erosion">the kind of Erosion for which to generate the game object</param>
        /// <returns>a new game object for this type of erosion (may be null)</returns>
        public GameObject GetIcon(Vector3 position, Erosion erosion)
        {
            GameObject result;
            UnityEngine.Object prefab = erosionToSprite[(int)erosion];
            if (prefab == null)
            {
                result = new GameObject();
            }
            else
            {
                result = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
            }
            result.tag = Tags.Erosion;
            result.name = ToString(erosion);
            result.transform.position = position;
            return result;
        }
    }
}
