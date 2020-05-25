using UnityEngine;
using CScape;
using SEE.DataModel;

namespace SEE.GO
{
    /// <summary>
    /// A factory for game objects represented by CScape buildings.
    /// </summary>
    public class BuildingFactory : NodeFactory
    {
        public BuildingFactory()
        {
            Unit = CScapeUnit;
        }

        /// <summary>
        /// The number of building types offered.
        /// </summary>
        /// <returns>number of building types offered</returns>
        public override int NumberOfStyles()
        {
            return prefabFiles.Length;
        }

        /// <summary>
        /// Path to the folder assumed to be contained in a folder named Resources 
        /// in the Assets directory where the prefabs for the buildings are located.
        /// </summary>
        private static readonly string pathPrefix = "BuildingTemplates/Buildings/";

        /// <summary>
        /// Filenames of the prefabs for the buildings excluding their file extension .prefab
        /// </summary>
        private static readonly string[] prefabFiles = new string[]
            {                 // floor, depth, width
              "CSTemplate30", // 1, 1, 1
              "CSTemplate03", // 3, 3, 3
              "CSTemplate05", // 4, 4, 4
              "CSTemplate06", // 4, 8, 7
              "CSTemplate07", // 5, 7, 7
              "CSTemplate09", // 3, 3, 3
              "CSTemplate11", // 3, 4, 4
              "CSTemplate12", // 4, 4, 4
              "CSTemplate13", // 8, 9, 9
              "CSTemplate14", // 8, 10, 10
              "CSTemplate15", // 7, 3, 3
              "CSTemplate16", // 7, 3, 3
              "CSTemplate17", // 4, 5, 5
              "CSTemplate18", // 3, 5, 6
              "CSTemplate19", // 12, 3, 3
              "CSTemplate20", // 3, 8, 8
              "CSTemplate22", // 8, 10, 10
              "CSTemplate23", // 8, 9, 9
              "CSTemplate24", // 4, 8, 7
              "CSTemplate25", // 8, 4, 4
              "CSTemplate26", // 7, 3, 3
              "CSTemplate27", // 3, 4, 4
              "CSTemplate28", // 6, 8, 9
              "CSTemplate29", // 4, 3, 3   
              "CSTemplate-SlantedRoofSmall", // 3, 2, 2
              "CSTemplateForEdit", // 4, 5, 5   
              "Build0Small", // 2, 1, 1
              "Build0SmallSlantedroof", // 1, 1, 1
              "Build1" // 5, 7, 7
            };

        /// <summary>
        /// The loaded prefabs for the CScape building we use for their instantiation.
        /// </summary>
        private static readonly GameObject[] prefabs = LoadAllPrefabs();

        /// <summary>
        /// Loads and returns all prefabs listed in prefabFiles.
        /// </summary>
        /// <returns>all prefabs listed in prefabFiles</returns>
        private static GameObject[] LoadAllPrefabs()
        {
            GameObject[] result = new GameObject[prefabFiles.Length];
            int i = 0;
            foreach (string filename in prefabFiles)
            {
                string path = pathPrefix + filename;
                result[i] = Resources.Load<GameObject>(path);
                if (result[i] == null)
                {
                    Debug.LogErrorFormat("[BuildingFactory] Could not load building prefab {0}.\n", path);
                }
                else
                {
                    ResetChildren(result[i]);
                }
                i++;
            }
            return result;
        }

        /// <summary>
        /// Re-sets all children to the default values concerning shadowing and collision
        /// (both are turned off).
        /// </summary>
        /// <param name="parent">parent game object whose children game object are to be reset</param>
        private static void ResetChildren(GameObject parent)
        {
            // Set default values for all children (rooftops)
            foreach (Transform child in parent.transform)
            {
                GameObject kid = child.gameObject;
                kid.isStatic = true;
                NoShadows(kid);
                // disable colliders
                Collider collider = kid.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        /// <summary>
        /// Adds one instance of each type of CScape building. This function can be
        /// used to create all buildings for inspection. It is not actually used
        /// for a real scene.
        /// </summary>
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

        /// <summary>
        /// If true, NewBlock(index) should return different types of buildings
        /// depending upon the index, otherwise the same building type is 
        /// created independent of the index.
        /// </summary>
        private const bool createDifferentBuildingTypes = true;

        public override GameObject NewBlock(int style = 0)
        {
            if (createDifferentBuildingTypes)
            {
                style = Mathf.Clamp(style, 0, prefabs.Length - 1);
                GameObject building = NewBuilding(style);
                SetStyle(building, style);
                return building;
            }
            else
            {
                // The most suitable CScape building is CSTemplate30 because that kind of
                // building can be scaled down to (1 floors, 1 depth, 1 width).
                GameObject building = NewBuilding(0);
                // Even though we are using the same type of building for all styles,
                // we can still vary its facade.
                SetStyle(building, style);
                return building;
            }
        }

        /// <summary>
        /// Returns the CScape building with given index relative to prefabs.
        /// </summary>
        /// <param name="style">style index of the building to be returned</param>
        /// <returns>CScape building with given index</returns>
        private static GameObject NewBuilding(int style)
        {
            GameObject building = NewBuilding(prefabs[style]);
            return building;
        }

        /// <summary>
        /// One CScape unit (floor level height) is three Unity units.
        /// One Unity unit represents one meter in real world. Three meters
        /// resembles the height of a floor in real world.
        /// </summary>
        private const float CScapeUnit = 3.0f;

        /// <summary>
        /// Instantiates the given prefab (a CScape building) and sets various
        /// parameters.
        /// </summary>
        /// <param name="prefab">building prefab to be instantiated</param>
        /// <returns>returns an instantiation of the given building prefab with default settings</returns>
        private static GameObject NewBuilding(UnityEngine.Object prefab)
        {
            GameObject building = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
            building.tag = Tags.Node;
            building.isStatic = true;

            CSRooftops csRooftopsModifier = building.GetComponent<CSRooftops>();
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

            CSAdvertising csAdverts = building.GetComponent<CSAdvertising>();
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
            BuildingModifier buildingModifier = building.GetComponent<BuildingModifier>();
            if (buildingModifier != null)
            {
                // Set width and depth of building in terms of building units, not Unity units.
                // A city is real world scaled (1 meter = 1 unity unit) so that it looks natural in VR. 
                // CScape is using a unit of 3 meters as a standard CScape unit (CScape unit = 3m), 
                // as the developer found that that unit is a good measure for distances (3m for floor 
                // heights, 3m for street lanes). 
                buildingModifier.buildingWidth = 10;
                buildingModifier.buildingDepth = 5;
                buildingModifier.useAdvertising = true;
                buildingModifier.useGraffiti = true;
                buildingModifier.extendFoundations = 0.0f;
                buildingModifier.AwakeCity();
                buildingModifier.UpdateCity();
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no building modifier.\n", building.name);
            }
            NoShadows(building);
            ResetChildren(building);
            return building;
        }

        /// <summary>
        /// Turns off shadow casting and receiving.
        /// </summary>
        /// <param name="gameObject">game object for which to disable shadowing</param>
        private static void NoShadows(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            else
            {
                Debug.LogWarningFormat("[BuildingFactory] {0} has no renderer.\n", gameObject.name);
            }
        }

        /// <summary>
        /// Scales the given block to the given size. Note: The unit of size 
        /// is the Unit size and mapped on building length attributes as follows:
        ///
        ///    x -> building width / Unit()
        ///    y -> number of floors / Unit()
        ///    z -> building depth / Unit()
        /// 
        /// Precondition: The given block must have been generated by this factory.
        /// </summary>
        /// <param name="block">block to be scaled</param>
        /// <param name="size">new size in Unity units</param>
        public override void SetSize(GameObject block, Vector3 size)
        {
            // Scale by the number of floors of a building.
            BuildingModifier bm = block.GetComponent<BuildingModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("CScape building {0} has no building modifier.\n", block.name);
            }
            else
            {
                bm.buildingWidth = (int)(size.x / Unit);
                bm.floorNumber = (int)(size.y / Unit);
                bm.buildingDepth = (int)(size.z / Unit);
                bm.UpdateCity();
                bm.AwakeCity();
            }
        }

        private enum Length
        {
            width,  // building width
            height, // number of floors
            depth   // building depth
        }

        /// <summary>
        /// Sets the length of the block to the given value in Unity units. Which length is set is
        /// specified by parameter length:
        /// 
        ///   width => building width  / Unit()
        ///   height => number of floors / Unit()
        ///   depth  => building depth / Unit()
        /// 
        /// Precondition: The given block must have been generated by this factory.
        /// </summary>
        /// <param name="block">block whose length is to be set</param>
        /// <param name="value">new value to be set</param>
        /// <param name="length">which length to be set</param>
        private void SetSize(GameObject block, float value, Length length)
        {
            // Scale by the number of floors of a building.
            BuildingModifier bm = block.GetComponent<BuildingModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("CScape building {0} has no building modifier.\n", block.name);
            }
            else
            {
                switch (length)
                {
                    case Length.width:
                        bm.buildingWidth = Mathf.RoundToInt(value / Unit);
                        break;
                    case Length.height:
                        bm.floorNumber = Mathf.RoundToInt(value / Unit);
                        break;
                    case Length.depth:
                        bm.buildingDepth = Mathf.RoundToInt(value / Unit);
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the width of the object (x axis) to the given value in Unity units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for width</param>
        public override void SetWidth(GameObject block, float value)
        {
            SetSize(block, value, Length.width);
        }

        /// <summary>
        /// Sets the height of the object (y axis) to the given value in Unity units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for height</param>
        public override void SetHeight(GameObject block, float value)
        {
            SetSize(block, value, Length.height);
        }

        /// <summary>
        /// Sets the depth of the object (y axis) to the given value in Unity units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for depth</param>
        public override void SetDepth(GameObject block, float value)
        {
            SetSize(block, value, Length.depth);
        }

        public override void SetGroundPosition(GameObject block, Vector3 position)
        {
            // the default position of a game object in Unity is its center, but the
            // position of a CScape building is its left front corner
            Vector3 extent = GetSize(block) / 2.0f;
            position.x -= extent.x;
            position.z -= extent.z;
            // The y position is already interpreted as the ground by CScape buildings.
            block.transform.position = position;
        }

        public override void SetLocalGroundPosition(GameObject block, Vector3 position)
        {
            Vector3 extent = GetSize(block) / 2.0f;
            block.transform.localPosition = new Vector3(position.x - extent.x, position.y, position.z - extent.z);
            //block.transform.localPosition = position - extent;
        }

        /// <summary>
        /// The center position of the block in world space.
        /// </summary>
        /// <param name="block">block for which to retrieve the center position</param>
        /// <returns>center position of the block in world space</returns>
        public override Vector3 GetCenterPosition(GameObject block)
        {
            // transform.position denotes the left front corner of a building in CScape
            Vector3 extent = GetSize(block) / 2.0f;
            return block.transform.position + extent;
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

        /// <summary>
        /// Rotates the given object by the given degree along the y axis (i.e., relative to the ground).
        /// </summary>
        /// <param name="gameNode">object to be rotated</param>
        /// <param name="degree">degree of rotation</param>
        public override void Rotate(GameObject block, float degree)
        {
            // Unlike normal Unity objects, CScape buildings are rotated relative to the left front corner
            // at ground level. That is why we need to select the ground center as the anchor of rotation.
            block.transform.RotateAround(Ground(block), Vector3.up, degree);
        }

        public override void SetStyle(GameObject block, int style)
        {
            // What would be a good option to change the style? Selecting a 
            // different type of building would be an obvious chose, in 
            // particular because the style parameter in NewBlock is
            // in fact used to determine the type of building.
            // The problem with that is that we would need to instantiate a 
            // different building prefab. As a consequence, we would create a new
            // game object that is different from 'block'. Other objects
            // may still reference the 'block', however. We need a way in which
            // the block remains the same game object, but only one of its
            // visual attributes changes. We could use the material of the
            // facade, for instance, or the facade shape. This could be achieved
            // through the BuildingModifier attached to the block.
            // The facade shape is one of the most prominent visual features
            // of a building. For this reason, we will select that for the style.
            // There are two different facade shapes: one for the front and one
            // lateral. We will change both and keep them in sync, which makes
            // sense because a building should be alike from all different
            // perspectives; this is also the case for the style for
            // cubes we offer as an alternative to CScape buildings.

            BuildingModifier modifier = block.GetComponent<BuildingModifier>();
            if (modifier == null)
            {
                Debug.LogWarningFormat("CScape building {0} does not have a BuildingModifier.\n", block.name);
            }
            else
            {
                // The facade shape has a value range [0,19].
                int index = Mathf.Clamp(style, 0, Mathf.Min(19, prefabFiles.Length));
                // front facade shape
                modifier.materialId2 = index;
                modifier.materialId3 = index;
            }
        }
    }
}
