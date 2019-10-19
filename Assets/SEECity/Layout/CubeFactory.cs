using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for cubes as visual representations of graph nodes in the scene.
    /// </summary>
    public class CubeFactory : BlockFactory
    {
        private SerializableDictionary<PrimitiveType, GameObject> primitiveMeshes = new SerializableDictionary<PrimitiveType, GameObject>();

        private const string shaderName = "Diffuse";
        private readonly Shader shader = Shader.Find(shaderName);

        ~CubeFactory()
        {
            foreach (GameObject gameObject in primitiveMeshes.Values)
            {
                Destroyer.DestroyGameObject(gameObject);
            }
        }

        public override GameObject NewBlock()
        {
            GameObject result = new GameObject();
            result.tag = Tags.Building;
            AddCubeMesh(result);
            return result;
        }

        private void AddCubeMesh(GameObject gameObject)
        {
            AddMesh(gameObject, PrimitiveType.Cube, "BrickTextures/BricksTexture13/BricksTexture13");
        }

        private void AddCylinderMesh(GameObject gameObject, string materialPath)
        {
            AddMesh(gameObject, PrimitiveType.Cylinder, materialPath);
        }

        public void AddTerrain(GameObject gameObject)
        {
            AddCylinderMesh(gameObject, "Grass/Grass FD 1 diffuse");
        }

        public void AddFrontYard(GameObject gameObject)
        {
            AddCylinderMesh(gameObject, "Grass/Sand FD 1 diffuse");
        }

        /// <summary>
        /// Creates a mesh for given object and assigning the material loaded from materialPath 
        /// to it.
        /// </summary>
        /// <param name="gameObject">The game object for which to create the mesh</param>
        /// <param name="type">The kind of mesh to be created</param>
        /// <param name="materialPath">Path to the material file. The material file must be located 
        /// in a folder Resources. There must not be any file extension in the path name passed here. 
        /// However, the actual file on the disk must have the file extension .mat.</param>
        private void AddMesh(GameObject gameObject, PrimitiveType type, string materialPath)
        {
            // So to create a cube (or sphere, or any object really) you need three things:
            // 1) A MeshFilter (The Mesh Filter takes a mesh from your assets and passes it 
            //    to the Mesh Renderer)
            // 2) A MeshRenderer
            // 3) A Material to go on the Renderer
            // The MeshRenderer and the Material are just built-in Unity classes you 
            // can easily create and add using AddComponent.
            // The MeshFilter, however, can only be created via GameObject.CreatePrimitive(),
            // which will return a game object, not just a mesh.

            // 1) Add mesh
            {
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                GameObject protoType = GetPrimitiveMesh(type);
                meshFilter.sharedMesh = protoType.GetComponent<MeshFilter>().sharedMesh;
            }

            // 2) Add mesh renderer
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            // object should not cast shadows: too expensive and may hide information
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // 3) Assigns a material to the object.
            Material newMat;
            if (string.IsNullOrEmpty(materialPath))
            {
                if (shader != null)
                {
                    newMat = new Material(shader);
                }
                else
                {
                    newMat = null;
                    Debug.LogError("Could not find shader " + shaderName + "\n");
                }
            } 
            else
            {
                newMat = Resources.Load<Material>(materialPath);
            }
            
            if (newMat != null)
            {
                renderer.material = newMat;
                //renderer.sharedMaterial = newMat;
            }
            else
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
            }

            // 4) Add collider so that we can interact with it the object
            gameObject.AddComponent<BoxCollider>();

            // Object should be static so that we save rendering time at run-time.
            gameObject.isStatic = true;
        }

        /// <summary>
        /// The first time you create a primitive of a given type without a MeshCollider 
        /// or get a mesh of a given PrimitiveType there is an overhead of a GameObject 
        /// being created and destroyed. That is why we store it for later use.
        /// </summary>
        /// <param name="type">the type of mesh to be created</param>
        /// <returns>mesh of given type</returns>
        private GameObject GetPrimitiveMesh(PrimitiveType type)
        {
            if (!primitiveMeshes.ContainsKey(type))
            {
                CreatePrimitiveMesh(type);
            }
            return primitiveMeshes[type];
        }

        // "Steals" the necessary components from a temporarily created game object.
        private GameObject CreatePrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = Tags.NodePrefab;
            gameObject.tag = Tags.Block;
            gameObject.isStatic = true;
            gameObject.SetActive(false); // this object should not be visible
            primitiveMeshes[type] = gameObject;
            return gameObject;
        }

        /// <summary>
        /// Scales the given block by the given scale. Note: The unit of scaling 
        /// are normal Unity units.
        /// 
        /// Precondition: The given block must have been generated by this factory.
        /// </summary>
        /// <param name="block">block to be scaled</param>
        /// <param name="scale">scaling factor</param>
        public override void ScaleBlock(GameObject block, Vector3 scale)
        {
            block.transform.localScale = scale;
        }

        public override void SetPosition(GameObject block, Vector3 position)
        {
            // The default position of a game object in Unity is its center.
            // Thus, the x and z co-ordinates are just fine. Yet, the y co-ordinate
            // needs adjustment. position.y specifies the ground level of the block,
            // whereas block.transform.position.y is assumed to be height center of a 
            // game object by Unity. As a consequence, we need to lift position.y
            // by half of the block's height. 

            Vector3 size = GetSize(block);
            block.transform.position = new Vector3(position.x, size.y / 2.0f, position.z);
        }
    }
}

