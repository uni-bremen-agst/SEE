using UnityEngine;
using CScape;
using SEE.DataModel;

namespace SEE.Layout
{
    public class BuildingFactory : BlockFactory
    {
        private static readonly string pathPrefix = "Assets/CScape/Editor/Resources/BuildingTemplates/Buildings/";

        private static readonly string fileExtension = ".prefab";

        private static readonly string[] prefabFiles = new string[]
            {
              "CSTemplate03",
              "CSTemplate05",
              "CSTemplate06",
              "CSTemplate07",
              "CSTemplate09",
              "CSTemplate11",
              "CSTemplate12",
              "CSTemplate13",
              "CSTemplate14",
              "CSTemplate15",
              "CSTemplate16",
              "CSTemplate17",
              "CSTemplate18",
              "CSTemplate19",
              "CSTemplate20",
              "CSTemplate22",
              "CSTemplate23",
              "CSTemplate24",
              "CSTemplate25",          
              "CSTemplate26",        
              "CSTemplate27",        
              "CSTemplate28",        
              "CSTemplate29",    
              "CSTemplate30",
              "CSTemplate-SlantedRoofSmall",
              "CSTemplateForEdit",
              "Build0Small",
              "Build0SmallSlantedroof",
              "Build1"
            };

        private static readonly UnityEngine.Object[] prefabs = LoadAllPrefabs();

        private static UnityEngine.Object[] LoadAllPrefabs()
        {
            UnityEngine.Object[] result = new UnityEngine.Object[prefabFiles.Length];
            int i = 0;
            foreach (string filename in prefabFiles)
            {
                string path = pathPrefix + filename + fileExtension;
                result[i] = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));

                //result[i] = Resources.Load<UnityEngine.Object>(filename);
                if (result[i] == null)
                {
                    Debug.LogErrorFormat("[BuildingFactory] Could not load building prefab {0}.\n", path);
                }
                else
                {
                    Debug.LogFormat("[BuildingFactory] Loaded building prefab {0}.\n", path);
                }
                i++;
            }
            return result;
        }

        private static void GenerateAllBuildingPrefabs()
        {
            Vector3 position = Vector3.zero;

            int i = 0;
            foreach (UnityEngine.Object prefab in prefabs)
            {
                GameObject o = NewBuilding(prefab);
                o.name = prefabFiles[i];

                float width;
                BuildingModifier b = o.GetComponent<BuildingModifier>();
                if (b != null)
                {
                    width = b.buildingWidth * CScapeUnit;
                    Debug.LogFormat("[BuildingFactory] {0} has width {1}\n", o.name, width);

                    position += (width / 2.0f) * Vector3.right;
                    o.transform.position = position;
                    position += (width / 2.0f) * Vector3.right + 5 * Vector3.right;
                }
            }
        }

        public override GameObject NewBlock()
        {
            return NewBuilding(Random.Range(0, prefabs.Length - 1));
        }

        private static GameObject NewBuilding(int i)
        {
            return NewBuilding(prefabs[i]);
        }

        public override float Unit()
        {
            return CScapeUnit;
        }

        private const float CScapeUnit = 3.0f;

        private static GameObject NewBuilding(UnityEngine.Object prefab)
        {
            GameObject building = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
            building.tag = Tags.Building;
            building.isStatic = true;

            CSRooftops csRooftopsModifier = building.GetComponent(typeof(CSRooftops)) as CSRooftops;
            if (csRooftopsModifier != null)
            {
                csRooftopsModifier.randomSeed = UnityEngine.Random.Range(0, 1000000);
                csRooftopsModifier.lodDistance = 0.18f;
                csRooftopsModifier.instancesX = 150;
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no rooftop modifier.\n", building.name);
            }

            CSAdvertising csAdverts = building.GetComponent(typeof(CSAdvertising)) as CSAdvertising;
            if (csAdverts != null)
            {
                csAdverts.randomSeed = UnityEngine.Random.Range(0, 1000000);
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no advertising.\n", building.name);
            }

            // A building modifier allows us to modify basic properties such as the facade shape, Graffiti, etc.
            // The kinds of properties and their effect can be investigated in the Unity Inspector when a building
            // is selected.
            BuildingModifier buildingModifier = building.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
            if (buildingModifier != null)
            {
                // Set width and depth of building in terms of building units, not Unity units.
                // A city is real world scaled (1 meter = 1 unity unit) so that it looks natural in VR. 
                // CScape is using a unit of 3 meters as a standard CScape unit (CScape unit = 3m), 
                // as the developer found that that unit is a best measure for distances (3m for floor 
                // heights, 3m for street lanes). 
                buildingModifier.buildingWidth = 10;
                buildingModifier.buildingDepth = 5;
                buildingModifier.useAdvertising = true;
                buildingModifier.useGraffiti = true;
                buildingModifier.AwakeCity();
                buildingModifier.UpdateCity();
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no building modifier.\n", building.name);
            }
            Renderer renderer = building.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no renderer.\n", building.name);
            }
            return building;
        }

        /// <summary>
        /// Scales the given block by the given scale. Note: The unit of scaling 
        /// is as follows: x -> building width, y -> number of floors, z -> building depth
        /// 
        /// Precondition: The given block must have been generated by this factory.
        /// </summary>
        /// <param name="block">block to be scaled</param>
        /// <param name="scale">scaling factor</param>
        public override void ScaleBlock(GameObject block, Vector3 scale)
        {
            // Scale by the number of floors of a building.
            BuildingModifier bm = block.GetComponent<BuildingModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("CScape building {0} has no building modifier.\n", block.name);
            }
            else
            {
                bm.buildingWidth = (int)scale.x;
                bm.floorNumber = (int)scale.y;
                bm.buildingDepth = (int)scale.z;
                bm.UpdateCity();
                bm.AwakeCity();
            }
        }

        public override void SetPosition(GameObject block, Vector3 position)
        {
            // the default position of a game object in Unity is its center, but the
            // position of a CScape building is its left corner
            Vector3 extent = GetSize(block) / 2.0f;
            Vector3 newPosition = position - extent;
            newPosition.y = position.y;
            block.transform.position = newPosition;
        }

        public override void SetLocalPosition(GameObject block, Vector3 position)
        {
            Vector3 extent = GetSize(block) / 2.0f;
            Vector3 newPosition = position - extent;
            block.transform.localPosition = newPosition;
        }

        /// <summary>
        /// Returns the center of the roof of the given block.
        /// </summary>
        /// <param name="block">block for which to determine the roof position</param>
        /// <returns>roof position</returns>
        public override Vector3 Roof(GameObject block)
        {
            // block.transform.position of a building is the left front lower corner of the building
            Vector3 result = block.transform.position;
            Vector3 extent = GetSize(block) / 2.0f;
            // transform to center
            result += extent;
            // top above the center
            result.y += extent.y;
            return result;
        }

        /// <summary>
        /// Returns the center of the ground of a block.
        /// </summary>
        /// <param name="block">block for which to determine the ground position</param>
        /// <returns>ground position</returns>
        public override Vector3 Ground(GameObject block)
        {
            // block.transform.position of a building is the left front lower corner of the building 
            Vector3 result = block.transform.position;
            Vector3 extent = GetSize(block) / 2.0f;
            // transform to center
            result += extent;
            // bottom below the center
            result.y = block.transform.position.y;
            return result;
        }
    }
}
