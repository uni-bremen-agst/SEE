using System.IO;
using UnityEngine;
using Unity.VectorGraphics;
using System;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// A factory to generate sprites from SVG files.
    /// </summary>
    public class IconFactory
    {
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
        /// The paths to the icon prefab files.
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

        private IconFactory()
        {
            erosionToSprite = LoadAllSprites();
        }

        private static readonly IconFactory instance = new IconFactory();

        public static IconFactory Instance
        {
            get => instance;
        }

        [SerializeField]
        private readonly UnityEngine.Object[] erosionToSprite;

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
            result.name = ToString(erosion);
            result.transform.position = position;
            return result;
        }
    }
}
