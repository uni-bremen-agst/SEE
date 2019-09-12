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
        private static VectorUtils.TessellationOptions tessOptions = new VectorUtils.TessellationOptions()
        {
            StepDistance = 100.0f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

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
        /// The paths to the SVG files.
        /// </summary>
        private static readonly string[] paths = new string[]
            {
            "Assets/Resources/Icons/architecture.svg",
            "Assets/Resources/Icons/clone.svg",
            "Assets/Resources/Icons/cycle.svg",
            "Assets/Resources/Icons/deadcode.svg",
            "Assets/Resources/Icons/metric.svg",
            "Assets/Resources/Icons/stil.svg",
            "Assets/Resources/Icons/universal.svg"
            };

        private IconFactory()
        {
            erosionToSpriteGeometries = LoadAllSVGs();
        }

        private static readonly IconFactory instance = new IconFactory();

        public static IconFactory Instance
        {
            get => instance;
        }

        /// <summary>
        /// Returns a game object as place holder for sprite visualizing an
        /// architecture violation. This method can be called in both play
        /// and editor mode. The sprite, however, must be added later using
        /// GetSprite().
        /// </summary>
        /// <param name="position">the position of the newly generated object</param>
        /// <returns></returns>
        public GameObject GetArchitectureViolationIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Architecture_Violation);
        }

        public GameObject GetCloneIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Clone);
        }

        public GameObject GetDeadCodeIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Cycle);
        }

        public GameObject GetMetricIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Metric);
        }

        public GameObject GetCycleIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Cycle);
        }

        public GameObject GetStyleIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Style);
        }

        public GameObject GetUniversalIcon(Vector3 position)
        {
            return GetIcon(position, Erosion.Universal);
        }

        [SerializeField]
        private List<VectorUtils.Geometry>[] erosionToSpriteGeometries;

        private static List<VectorUtils.Geometry>[] LoadAllSVGs()
        {
            Erosion[] erosionIssues = (Erosion[])Enum.GetValues(typeof(Erosion));
            List<VectorUtils.Geometry>[] result = new List<VectorUtils.Geometry>[erosionIssues.Length];

            foreach (Erosion erosion in erosionIssues)
            {
                List<VectorUtils.Geometry> geoms = LoadSVG(paths[(int)erosion]);
                result[(int)erosion] = geoms;
            }
            return result;
        }

        /*
        private static GameObject GetIcon(Vector3 position, IconFactory.Erosion value)
        {
            UnityEngine.Object prefab = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Resources//Icons/architectureSprite.prefab", typeof(GameObject));
            GameObject clone = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
            // Modify the clone to your heart's content
            clone.transform.position = position;
            clone.name = "SPRITE";
            return clone;
        }
        */

        /// <summary>
        /// Returns a sprite for this erosion issue kind. This function can only
        /// be called in game mode, not in the editor mode.
        /// </summary>
        /// <param name="erosion"></param>
        /// <returns>sprite for give erosion kind</returns>
        public Sprite GetSprite(Erosion erosion)
        {
            List<VectorUtils.Geometry> geoms = erosionToSpriteGeometries[(int)erosion];
            // Build a sprite with the tessellated geometry.
            return VectorUtils.BuildSprite(geoms, 10.0f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
        }

        private static List<VectorUtils.Geometry> LoadSVG(string filename)
        {
            Debug.LogFormat("LoadSVG from file {0}\n", filename);
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        // Dynamically import the SVG data, and tessellate the resulting vector scene.
                        SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(sr);
                        List<VectorUtils.Geometry> geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions);
                        return geoms;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("SVG file does not exist: {0}.\n", filename);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Loading SVG {0} failed: {1}", filename, e.ToString());
            }
            return new List<VectorUtils.Geometry>();
        }

        public GameObject GetIcon(Vector3 position, Erosion erosion)
        {
            GameObject result = new GameObject();
            result.name = ToString(erosion);
            result.transform.position = position;
            ErosionCloud cloud = result.AddComponent<ErosionCloud>();
            SpriteRenderer renderer = result.AddComponent<SpriteRenderer>();
            cloud.erosion = erosion;
            return result;
        }

        private static GameObject LoadIcon(string filename)
        {
            Debug.LogFormat("LoadIcon from file {0}\n", filename);

            GameObject result = new GameObject
            {
                name = "prototype " + filename
            };
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        // Dynamically import the SVG data, and tessellate the resulting vector scene.
                        var sceneInfo = SVGParser.ImportSVG(sr);
                        var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions);

                        // Build a sprite with the tessellated geometry.
                        // Important note: VectorUtils.BuildSprite calls OverrideGeometry and
                        // OverrideGeometry is not allowed to be called outside of the player loop, and 
                        // the sprites won't allow to override their geometries in this situation. 
                        UnityEngine.Sprite sprite = VectorUtils.BuildSprite(geoms, 10.0f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
                        if (sprite == null)
                        {
                            Debug.LogErrorFormat("LoadIcon: cannot create sprite from file {0}\n", filename);
                        }
                        SpriteRenderer renderer = result.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("SVG file does not exist: {0}.\n", filename);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Loading SVG {0} failed: {1}", filename, e.ToString());
            }
            return result;
        }
    }
}
