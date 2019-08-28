using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public static class MeshFactory
    {
        private static Dictionary<PrimitiveType, GameObject> primitiveMeshes = new Dictionary<PrimitiveType, GameObject>();

        public static void Reset()
        {
            foreach (GameObject gameObject in primitiveMeshes.Values)
            {
                Destroyer.DestroyGameObject(gameObject);
            }
            primitiveMeshes = new Dictionary<PrimitiveType, GameObject>();
        }
        /*
        public static GameObject CreatePrimitive(PrimitiveType type, bool withCollider)
        {
            if (withCollider) { return GameObject.CreatePrimitive(type); }

            GameObject gameObject = new GameObject(type.ToString());
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = MeshFactory.GetPrimitiveMesh(type);
            gameObject.AddComponent<MeshRenderer>();

            return gameObject;
        }
        */

        public static void AddCube(GameObject gameObject)
        {
            AddMesh(gameObject, PrimitiveType.Cube, "BrickTextures/BricksTexture13/BricksTexture13");
        }

        public static void AddCylinder(GameObject gameObject)
        {
            AddMesh(gameObject, PrimitiveType.Cylinder, "Grass/Grass FD 1 diffuse");
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
        public static void AddMesh(GameObject gameObject, PrimitiveType type, string materialPath)
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

            // 3) Assigns a material to the object.
            Material newMat = Resources.Load<Material>(materialPath);
            if (newMat != null)
            {
                //renderer.material = newMat;
                renderer.sharedMaterial = newMat;
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
        private static GameObject GetPrimitiveMesh(PrimitiveType type)
        {
            if (!primitiveMeshes.ContainsKey(type))
            {
                CreatePrimitiveMesh(type);
            }
            return primitiveMeshes[type];
        }

        // "Steals" the necessary components from a temporarily created game object.
        private static GameObject CreatePrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = Tags.NodePrefab;
            gameObject.tag = gameObject.name;
            gameObject.isStatic = true;
            gameObject.SetActive(false); // this object should not be visible
            primitiveMeshes[type] = gameObject;
            return gameObject;
        }
    }
}

