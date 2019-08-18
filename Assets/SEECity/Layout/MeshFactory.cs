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
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(gameObject);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(gameObject);
                }
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

        public static void AddMesh(GameObject gameObject, PrimitiveType type)
        {
            // So to create a cube (or sphere, or any object really) you 3 things
            // 1) A MeshFilter
            // 2) A MeshRenderer
            // 3) A Material to go on the Renderer
            // The MeshRenderer and the Material are just built-in Unity classes you 
            // can easily create and add using AddComponent.
            // The MeshFilter, however, can only be created via GameObject.CreatePrimitive(),
            // which will return a game object, not just a mesh.

            // 1) Add mesh
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            GameObject protoType = GetPrimitiveMesh(type);
            meshFilter.sharedMesh = protoType.GetComponent<MeshFilter>().sharedMesh;

            // 2) Add mesh renderer
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            // object should not cast shadows: too expensive and may hide information
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 3) Assigns a material to the object.
            // The material file must be located in a folder Resources. There must not
            // be any file extension in the path name passed to Load(). However, the actual 
            // file on the disk must have the file extension .mat.
            string materialPath = "BrickTextures/BricksTexture13/BricksTexture13";
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
            gameObject.name = "Node Preftab";
            gameObject.isStatic = true;
            //gameObject.tag = Tags.Node;
            gameObject.SetActive(false); // this object should not be visible
            primitiveMeshes[type] = gameObject;
            return gameObject;
        }
    }
}

