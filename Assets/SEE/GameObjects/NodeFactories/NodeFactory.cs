using SEE.Controls.Interactables;
using SEE.DataModel;
using SEE.Game;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for visual representations of graph nodes in the scene.
    ///
    /// A game object created by this factory has -- as every other game
    /// object -- a scale (width X, height Y, depth Z), which can be
    /// set and manipulated by this factory. Clients of a node factory
    /// should not retrieve or set these attributes themselves using Unity's own
    /// API, thus, should not use transform.localScale or renderer.bounds.size
    /// and the like. The reason for that is that the intent of NodeFactory
    /// to abstract from the differences of the kinds of game objects we use
    /// for leaf nodes, namely, Cubes and CScape buildings. The latter have
    /// their own idea of scaling and size, which differs from the normal
    /// Unity way.
    ///
    /// In addition to scale, a node can have another kind of visual attribute
    /// that is offered by a node factory. Concretely, Cubes offer a color gradient
    /// and CScape buildings different styles of buildings. As a shared
    /// term that abstracts from those concrete styles, we call this attribute
    /// Style.
    /// </summary>
    public abstract class NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public NodeFactory(Materials.ShaderType shaderType, ColorRange colorRange)
        {
            materials = new Materials(shaderType, colorRange);
        }

        /// <summary>
        /// Creates and returns a new block representation of a graph node.
        /// The interpretation of the given <paramref name="style"/> depends upon
        /// the subclasses. It can be used to specify a visual property of the
        /// objects such as the color. The allowed range of a style index depends
        /// upon the  subclasses, too, but must be in [0, NumberOfStyles()-1].
        /// The <paramref name="renderQueueOffset"/> specifies the offset of the render
        /// queue of the new block. The higher the value, the later the object
        /// will be drawn. Objects drawn later will cover objects drawn earlier.
        /// This parameter can be used for the rendering of transparent objects,
        /// where the inner nodes must be rendered before the leaves to ensure
        /// correct sorting.
        /// </summary>
        /// <param name="style">specifies an additional visual style parameter of
        /// the object</param>
        /// <returns>new node representation</returns>
        /// <param name="renderQueueOffset">offset in the render queue</param>
        public virtual GameObject NewBlock(int style = 0, int renderQueueOffset = 0)
        {
            GameObject result = CreateBlock();
            MeshRenderer renderer = result.AddComponent<MeshRenderer>();
            materials.SetSharedMaterial(renderer, renderQueueOffset: renderQueueOffset, index: style);
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return result;
        }

        /// <summary>
        /// Returns a new game object to represent a node.
        /// </summary>
        /// <returns>new game object for a node</returns>
        private GameObject CreateBlock()
        {
            GameObject gameObject = new GameObject() { tag = Tags.Node };
            AddCollider(gameObject);
            // A MeshFilter is necessary for the gameObject to hold a mesh.
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetMesh();
            return gameObject;
        }

        /// <summary>
        /// Returns a mesh for a node.
        /// </summary>
        /// <returns>mesh for a node</returns>
        protected abstract Mesh GetMesh();

        /// <summary>
        /// Adds an appropriate collider to <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">the game object receiving the collider</param>
        protected abstract void AddCollider(GameObject gameObject);

        /// <summary>
        /// The collection of materials to be used as styles by this node factory.
        /// </summary>
        private readonly Materials materials;

        /// <summary>
        /// The default height of an inner node in world space Unity unit.
        /// </summary>
        protected const float DefaultHeight = 0.000001f;

        /// <summary>
        /// Sets the style as the given <paramref name="style"/>
        /// for <paramref name="gameNode"/>. The value used will be clamped
        /// into [0, NumberOfStyles()-1].
        /// </summary>
        /// <param name="style">the index of the requested material</param>
        public void SetStyle(GameObject gameNode, int style)
        {
            if (gameNode.TryGetComponent(out Renderer renderer))
            {
                UnityEngine.Assertions.Assert.IsNotNull(gameNode.GetComponent<NodeRef>());
                UnityEngine.Assertions.Assert.IsNotNull(gameNode.GetComponent<NodeRef>().Value);
                int level = gameNode.GetComponent<NodeRef>().Value.Level;
                materials.SetSharedMaterial(renderer, renderQueueOffset: level, index: style);
            }

            if (gameNode.TryGetComponent(out Outline outline))
            {
                outline.UpdateRenderQueue(true);
            }
        }

        /// <summary>
        /// The number of styles offered. A style index must be in the range
        /// [0, NumberOfStyles()-1].
        /// </summary>
        /// <returns>number of materials offered</returns>
        public uint NumberOfStyles()
        {
            return materials.NumberOfMaterials;
        }

        /// <summary>
        /// Returns the size of the block generated by this factory in Unity units
        /// in world space.
        /// Precondition: The given block must have been generated by this factory.
        /// </summary>
        /// <param name="block">block whose size is to be returned</param>
        /// <returns>size of the block</returns>
        public virtual Vector3 GetSize(GameObject block)
        {
            // Nodes represented by cubes have a renderer from which we can derive the
            // extent.
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                // This is the axis-aligned bounding box fully enclosing the object in world space.
                return renderer.bounds.size;
            }
            else
            {
                Debug.LogErrorFormat("Node {0} (tag: {1}) without renderer.\n", block.name, block.tag);
                return Vector3.one;
            }
        }

        /// <summary>
        /// Sets the size (its scale) of the given block by the given size. Note: The unit of
        /// size is Unity worldspace units.
        /// </summary>
        /// <param name="block">block to be scaled</param>
        /// <param name="size">new size in worldspace</param>
        public virtual void SetSize(GameObject block, Vector3 size)
        {
            Transform parent = block.transform.parent;
            block.transform.SetParent(null);
            block.transform.localScale = size;
            block.transform.SetParent(parent);
        }

        /// <summary>
        /// Sets the width of the object (x axis) to the given value in Unity worldspace units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for width in worldspace</param>
        public virtual void SetWidth(GameObject block, float value)
        {
            SetSize(block, new Vector3(value, block.transform.lossyScale.y, block.transform.lossyScale.z));
        }

        /// <summary>
        /// Sets the height of the object (y axis) to the given value in Unity worldspace units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for height in worldspace</param>
        public virtual void SetHeight(GameObject block, float value)
        {
            SetSize(block, new Vector3(block.transform.lossyScale.x, value, block.transform.lossyScale.z));
        }

        /// <summary>
        /// Sets the depth of the object (z axis) to the given value in Unity worldspace units.
        /// </summary>
        /// <param name="block">block to be adjusted</param>
        /// <param name="value">new value for depth in worldspace</param>
        public virtual void SetDepth(GameObject block, float value)
        {
            SetSize(block, new Vector3(block.transform.lossyScale.x, block.transform.lossyScale.y, value));
        }

        /// <summary>
        /// Sets the position of the current block. The given position is
        /// interpreted as the center (x,z) of the block on the ground (y).
        /// </summary>
        /// <param name="block">block to be positioned</param>
        /// <param name="position">where to position the block (its center) on the ground y</param>
        public virtual void SetGroundPosition(GameObject block, Vector3 position)
        {
            Vector3 extent = GetSize(block) / 2.0f;
            block.transform.position = new Vector3(position.x, position.y + extent.y, position.z);
        }

        /// <summary>
        /// Sets the local position of the current block within its parent object.
        /// The given position is interpreted as the center (x,z) of the block on the ground (y).
        /// </summary>
        /// <param name="block">block to be positioned</param>
        /// <param name="position">where to position the block (its center)</param>
        public virtual void SetLocalGroundPosition(GameObject block, Vector3 position)
        {
            Vector3 extent = GetSize(block) / 2.0f;
            block.transform.localPosition = new Vector3(position.x, position.y + extent.y, position.z);
        }

        /// <summary>
        /// Returns the center of the roof of the given block in world space.
        /// </summary>
        /// <param name="block">block for which to determine the roof position</param>
        /// <returns>roof position</returns>
        public virtual Vector3 Roof(GameObject block)
        {
            Vector3 result = block.transform.position;
            result.y += GetSize(block).y / 2.0f;
            return result;
        }

        /// <summary>
        /// Returns the center of the ground of a block in world space.
        /// </summary>
        /// <param name="block">block for which to determine the ground position</param>
        /// <returns>ground position</returns>
        public virtual Vector3 Ground(GameObject block)
        {
            Vector3 result = block.transform.position;
            result.y -= GetSize(block).y / 2.0f;
            return result;
        }

        /// <summary>
        /// The center position of the block in world space.
        /// </summary>
        /// <param name="block">block for which to retrieve the center position</param>
        /// <returns>center position of the block in world space</returns>
        public virtual Vector3 GetCenterPosition(GameObject block)
        {
            // The center position in Unity is normally its transform.position
            // (as opposed to CScape buildings).
            return block.transform.position;
        }

        /// <summary>
        /// Rotates the given object by the given degree along the y axis (i.e., relative to the ground).
        /// </summary>
        /// <param name="gameNode">object to be rotated</param>
        /// <param name="degree">degree of rotation</param>
        public virtual void Rotate(GameObject block, float degree)
        {
            Quaternion rotation = Quaternion.Euler(0, degree, 0);
            block.transform.rotation = rotation;
        }

        /// <summary>
        /// Model mesh for a game object to be re-used for all instances.
        /// It will be ceated in <see cref="GetMesh()"/> on demand.
        /// </summary>
        protected static Mesh modelMesh;
    }
}
