using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Decorates each block with an assigned texture.
    ///
    /// The component is assumed to be attached to every node
    /// to be decorated.
    /// </summary>
    [ExecuteInEditMode]
    public class NodeDecorationController : MonoBehaviour
    {
        /// Although the component is assumed to be attached to every node
        /// to be decorated, in order to prevent errors, the
        /// component needs to get the gameObject it is attached to
        /// manually (rather than using parent). The <see cref="ChildNodes"/>
        /// need to be set manually because:
        /// <ul>
        /// <li> If using normal decoration mode: you only want to decorate
        /// the block using the upmost children (not recursively all
        /// transitive ancestors) and furthermore we need the gameObjects
        /// of the children, as using this.children might error out of there
        /// are empty objects between the node and its children.
        /// <li> If using treemap decorators: the node only knows what
        /// gameObject children it has, the graph renderer, however, needs a
        /// graph object to be able to render the treemaps, which is why it
        /// needs to be set manually.
        /// </ul>

        /// <summary>
        /// Child nodes of this block.
        /// </summary>
        public IList<GameObject> ChildNodes = new List<GameObject>();

        /// <summary>
        /// The height the treemap decorators should have, is set
        /// to 0.1f so they are basically flush with the surface of the
        /// blocks.
        /// </summary>
        private const float treemapDecoratorHeight = 0.1f;

        /// <summary>
        /// How much empty space there should be between block decorators.
        /// The number is a percentage ranging from 0% as 0.0f to 100% as 1.0f.
        /// </summary>
        private const float globalFreeSpacePercentage = 0.01f;

        /// <summary>
        /// The block is a folded block when it hides other nodes
        /// inside of it. If set to true, the block sides are decorated
        /// using treemaps to indicate the different metrics of the hidden nodes.
        /// Otherwise the block gets decorated using a roof that corresponds to the
        /// type the node has (for instance Enum > Rounded Roof).
        /// </summary>
        [Tooltip("Whether the node should be folded.")]
        public bool FoldedBlock = false;

        /// <summary>
        /// Can be used to test node decorations when adding roof types
        /// and/or modifying the treemaps.
        /// </summary>
        [Obsolete("This field can be removed when everything works.")]
        public bool Debug = false;

        /// <summary>
        /// Number of rows for test block.
        /// </summary>
        public int TestBlockRowCount;

        /// <summary>
        /// Number of columns for test block.
        /// </summary>
        public int TestBlockColumnCount;

        /// <summary>
        /// The gameNode to be decorated.
        /// </summary>
        public GameObject NodeObject;

        /// <summary>
        /// The gameNode's bounds' size.
        /// </summary>
        private Vector3 nodeSize;

        /// <summary>
        /// The gameNode's location.
        /// </summary>
        private Vector3 nodeLocation;

        /// <summary>
        /// To use treemap decoration/regular wall decorations.
        /// </summary>
        public bool DecorateUsingTreemap = false;

        /// <summary>
        /// Treemap layout settings for the block's side decorations.
        /// </summary>
        public AbstractSEECity SideTreemapSettings;

        /// <summary>
        /// Treemap layout settings for the block's top decorator.
        /// </summary>
        public AbstractSEECity SurfaceTreemapSettings;

        /// <summary>
        /// Treemap graph.
        /// </summary>
        public Graph TreemapGraph;

        /// <summary>
        /// Roof type dropdown menu items.
        /// </summary>
        public enum RoofType
        {
            Rectangular,
            Tetrahedron,
            Dome
        }

        /// <summary>
        /// Roof dropdown menu selector (inspector).
        /// </summary>
        public RoofType SelectedRoofType;

        [SerializeField, Range(0f, 1f)] private float floorHightPercentage;
        /// <summary>
        /// The height percentage the bottom floor should have in
        /// contrast to the building height.
        /// </summary>
        public float FloorHightPercentage
        {
            get
            {
                return floorHightPercentage;
            }
            set
            {
                floorHightPercentage = Mathf.Clamp(value, 0f, 1f);
            }
        }

        [SerializeField, Range(0f, 1f)] private float lobbySpanPercentage;
        /// <summary>
        /// How far out the lobby should be from the building, percentage
        /// is in contrast to the building width.
        /// </summary>
        public float LobbySpanPercentage
        {
            get
            {
                return lobbySpanPercentage;
            }
            set
            {
                lobbySpanPercentage = Mathf.Clamp(value, 0f, 1f);
            }
        }

        [SerializeField, Range(0f, 1f)] private float roofHeightPercentage;
        /// <summary>
        /// The height percentage the roof should have in contrast
        /// to the building height.
        /// </summary>
        public float RoofHeightPercentage
        {
            get
            {
                return roofHeightPercentage;
            }
            set
            {
                roofHeightPercentage = Mathf.Clamp(value, 0f, 1f);
            }
        }

        [SerializeField, Range(0f, 1f)] private float roofSpanPercentage;
        /// <summary>
        /// How far out/in the roof should be in contast to the building, percentage
        /// is in contrast to building width.
        /// </summary>
        public float RoofSpanPercentage
        {
            get
            {
                return roofSpanPercentage;
            }
            set
            {
                roofSpanPercentage = Mathf.Clamp(value, 0f, 1f);
            }
        }

        /// <summary>
        /// Get the gameNode's different properties.
        /// </summary>
        private void FetchNodeDetails()
        {
            nodeSize = NodeObject.transform.localScale;
            nodeLocation = NodeObject.transform.position;
        }

        /// <summary>
        /// Renders the bottom floor of a building.
        /// </summary>
        private void RenderLobby()
        {
            float lobbySizeX = nodeSize.x + nodeSize.x * lobbySpanPercentage;
            float lobbySizeZ = nodeSize.z + nodeSize.z * lobbySpanPercentage;
            float lobbyHeight = nodeSize.y * floorHightPercentage;
            // Create lobby gameObject
            GameObject lobby = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lobby.name = "Lobby";
            lobby.transform.localScale = new Vector3(lobbySizeX, lobbyHeight, lobbySizeZ);
            // *** Note: setting the lobby as a child object needs to be done after the transform, otherwise the size
            //     is relative to the parent object ***
            lobby.transform.SetParent(NodeObject.transform);
            // Get the point on the Y axis at the bottom of the building
            float buildingGroundFloorHeight = nodeLocation.y - (nodeSize.y / 2);
            // Set the lobby to be at buildingGroundFloorHeight + half the height of the lobby (so its floor touches the building floor)
            float lobbyGroundFloorHeight = buildingGroundFloorHeight + (lobby.transform.localScale.y / 2);
            // Move the lobby object to te correct location
            lobby.transform.position = new Vector3(nodeLocation.x, lobbyGroundFloorHeight, nodeLocation.z);
        }

        /// <summary>
        /// Renders the tetrahedron roof of a building.
        /// <remarks>Percentages are supplied as values between 0 and 1.<remarks>
        /// </summary>
        private void RenderRoof()
        {
            Vector3 roofScale = new Vector3(nodeSize.x + nodeSize.x * roofSpanPercentage,
                                            nodeSize.z + nodeSize.z * roofSpanPercentage,
                                            nodeSize.y * roofHeightPercentage);
            // Create roof GameObject
            switch (SelectedRoofType)
            {
                case RoofType.Tetrahedron:
                    GameObject tetrahedron = CreatePyramid(roofScale);
                    tetrahedron.transform.SetParent(NodeObject.transform);
                    // Move tetrahedron to top of building, tetrahedron is moved with the bottom left corner
                    tetrahedron.transform.position = new Vector3(nodeLocation.x - roofScale.x / 2,
                                                                 nodeSize.y / 2 + nodeLocation.y,
                                                                 nodeLocation.z - roofScale.z / 2);
                    break;
                case RoofType.Rectangular:
                    RenderRectangularRoof(roofScale);
                    break;
                case RoofType.Dome:
                    RenderDomeRoof(roofScale);
                    break;
                default:
                    throw new System.Exception("Unsupported Roof Type!");
            }
        }

        /// <summary>
        /// Renders a rectangular roof for the node object.
        /// </summary>
        /// <param name="roofScale">scale of the roof</param>
        private void RenderRectangularRoof(Vector3 roofScale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "RectangularRoof";
            go.transform.localScale = roofScale;
            float nodeRoofHeight = NodeObject.transform.position.y + nodeSize.y / 2;
            go.transform.position = new Vector3(nodeLocation.x, nodeRoofHeight, nodeLocation.z);
            go.transform.SetParent(NodeObject.transform);
        }

        /// <summary>
        /// Renders a dome roof for the node object.
        /// </summary>
        /// <param name="roofScale">scale of the roof</param>
        private void RenderDomeRoof(Vector3 roofScale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SphericalRoof";
            go.transform.localScale = roofScale;
            float nodeRoofHeight = NodeObject.transform.position.y + nodeSize.y / 2;
            go.transform.position = new Vector3(nodeLocation.x, nodeRoofHeight, nodeLocation.z);
            go.transform.SetParent(NodeObject.transform);
        }

        /// <summary>
        /// <author name="Leonard Haddad"/>
        /// Generates a 4-faced pyramid at the given coordinates.
        /// Inspired by an article by <a href="https://blog.nobel-joergensen.com/2010/12/25/procedural-generated-mesh-in-unity/">Morten Nobel-Jørgensen</a>.
        /// </summary>
        /// <param name="roofScale">scale of the roof</param>
        public GameObject CreatePyramid(Vector3 roofScale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tetrahedron";
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            // Tetrahedron floor nodes
            Vector3 p0 = new Vector3(0, 0, 0);
            Vector3 p1 = new Vector3(roofScale.x, 0, 0);
            Vector3 p2 = new Vector3(roofScale.x, 0, roofScale.z);
            Vector3 p3 = new Vector3(0, 0, roofScale.z);
            // Tetrahedron top node
            Vector3 p4 = new Vector3(roofScale.x / 2, roofScale.y, roofScale.z / 2);
            // Create gameObject mesh
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = new Vector3[] {
                p0, p1, p3, // Bottom vertex #1
                p2, p3, p1, // Bottom vertex #2
                p1, p4, p2,
                p2, p4, p3,
                p3, p4, p0,
                p0, p4, p1
            };
            mesh.triangles = Enumerable.Range(0, mesh.vertices.Length).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            meshFilter.mesh = mesh;
            return go;
        }

        /// <summary>
        /// Decorates the block.
        /// </summary>
        /// <param name="hiddenObjects">The list of gamenodes that are hidden inside the packed block</param>
        /// <param name="packedBlock">The packed block</param>
        private void DecoratePackedBlock(IList<GameObject> hiddenObjects, GameObject packedBlock)
        {
            GameObject topDecorator = new GameObject("TopDecorator");
            foreach (GameObject o in hiddenObjects)
            {
                GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clone.transform.localPosition = new Vector3(o.transform.position.x, packedBlock.transform.position.y + packedBlock.transform.localScale.y / 2, o.transform.position.z);
                clone.transform.localScale = new Vector3(o.transform.localScale.x, treemapDecoratorHeight, o.transform.localScale.z);
                clone.GetComponent<Renderer>().material = o.GetComponent<Renderer>().material;
                clone.name = o.name + "-TopDecorator";
                clone.transform.SetParent(topDecorator.transform);
            }
            topDecorator.transform.SetParent(packedBlock.transform);
            DecoratePackedBlockWalls(hiddenObjects, packedBlock);
        }

        /// <summary>
        /// Decorates the top of the packed block using a treemap.
        /// </summary>
        /// <param name="settings">The settings to be applied to the layout</param>
        /// <param name="hiddenNodesGraph">Graph containing the hidden nodes</param>
        /// <param name="packedBlockLocation">The location of the packed block</param>
        /// <param name="packedBlockDimensions">The dimensions of the packed block</param>
        /// <param name="treemapParent">The parent gameobject that holds all decorators</param>
        private void DecoratePackedBlockSurfaceWithTreemap(AbstractSEECity settings, Graph hiddenNodesGraph, Vector3 packedBlockLocation, Vector3 packedBlockDimensions, GameObject treemapParent)
        {
            GraphRenderer renderer = new GraphRenderer(settings, hiddenNodesGraph);
            GameObject empty = new GameObject("TopSurfaceTreemap");
            renderer.DrawGraph(empty);
            empty.transform.localScale = new Vector3(packedBlockDimensions.x, treemapDecoratorHeight, packedBlockDimensions.z);
            empty.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y + packedBlockDimensions.y / 2 - empty.transform.localScale.y / 2, packedBlockLocation.z);
            empty.transform.SetParent(treemapParent.transform);
        }

        /// <summary>
        /// Decorates the walls of the packed block using a treemap.
        /// </summary>
        /// <param name="settings">The settings to be applied to the layout</param>
        /// <param name="hiddenNodesGraph">Graph containing the hidden nodes</param>
        /// <param name="packedBlock">The packed block</param>
        private void DecoratePackedBlockWithTreemap(AbstractSEECity settings, Graph hiddenNodesGraph, GameObject packedBlock)
        {
            // Graph renderer to render treemaps
            GraphRenderer renderer = new GraphRenderer(settings, hiddenNodesGraph);

            // Get packed block dimensions and location, North - Positive X, West - Positive Z
            Vector3 packedBlockDimensions = packedBlock.transform.localScale;
            Vector3 packedBlockLocation = packedBlock.transform.localPosition;

            // Render treemaps on each surface
            GameObject treemapParent = new GameObject("Treemap-Decorators");
            treemapParent.transform.SetParent(NodeObject.transform);
            DecoratePackedBlockSurfaceWithTreemap(SurfaceTreemapSettings, hiddenNodesGraph, packedBlockLocation, packedBlockDimensions, treemapParent);
            // North
            GameObject planeN = new GameObject();
            planeN.name = "northTreemap";
            renderer.DrawGraph(planeN);
            planeN.transform.localScale = new Vector3(packedBlockDimensions.y, treemapDecoratorHeight, packedBlockDimensions.z);
            planeN.transform.localPosition = new Vector3(packedBlockLocation.x + packedBlockDimensions.x / 2 - planeN.transform.localScale.y / 2, packedBlockLocation.y, packedBlockLocation.z);
            planeN.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            planeN.transform.SetParent(treemapParent.transform);
            // South
            GameObject planeS = new GameObject();
            planeS.name = "southTreemap";
            renderer.DrawGraph(planeS);
            planeS.transform.localScale = new Vector3(packedBlockDimensions.y, treemapDecoratorHeight, packedBlockDimensions.z);
            planeS.transform.localPosition = new Vector3(packedBlockLocation.x - packedBlockDimensions.x / 2 + planeN.transform.localScale.y / 2, packedBlockLocation.y, packedBlockLocation.z);
            planeS.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            planeS.transform.SetParent(treemapParent.transform);
            // West
            GameObject planeW = new GameObject();
            planeW.name = "westTreemap";
            renderer.DrawGraph(planeW);
            planeW.transform.localScale = new Vector3(packedBlockDimensions.z, treemapDecoratorHeight, packedBlockDimensions.y);
            planeW.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y, packedBlockLocation.z + packedBlockDimensions.z / 2 - planeN.transform.localScale.y / 2);
            planeW.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            planeW.transform.SetParent(treemapParent.transform);
            // East
            GameObject planeE = new GameObject();
            planeE.name = "eastTreemap";
            renderer.DrawGraph(planeE);
            planeE.transform.localScale = new Vector3(packedBlockDimensions.z, treemapDecoratorHeight, packedBlockDimensions.y);
            planeE.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y, packedBlockLocation.z - packedBlockDimensions.z / 2 + planeN.transform.localScale.y / 2);
            planeE.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            planeE.transform.SetParent(treemapParent.transform);
            // TODO Test if needed with actual gameNodes
            Portal.SetInfinitePortal(packedBlock);
        }

        /// <summary>
        /// Decorates the walls of the packed block.
        /// </summary>
        /// <param name="hiddenObjects">The list of gamenodes that are hidden inside the packed block</param>
        /// <param name="packedBlock">The packed block</param>
        private void DecoratePackedBlockWalls(IList<GameObject> hiddenObjects, GameObject packedBlock)
        {
            // Get packed block dimensions and corners, North - Positive X, West - Positive Z
            Vector3 packedBlockDimensions = packedBlock.transform.localScale;
            Vector3 packedBlockLocation = packedBlock.transform.position;

            // Corners of the block computed with simple geometry
            Vector3 northWestTopCorner = new Vector3(packedBlockLocation.x + 0.5f * packedBlockDimensions.x,
                                                     packedBlockLocation.y + 0.5f * packedBlockDimensions.y,
                                                     packedBlockLocation.z + 0.5f * packedBlockDimensions.z);
            Vector3 northEastTopCorner = new Vector3(packedBlockLocation.x + 0.5f * packedBlockDimensions.x,
                                                     packedBlockLocation.y + 0.5f * packedBlockDimensions.y,
                                                     packedBlockLocation.z - 0.5f * packedBlockDimensions.z);
            Vector3 southWestTopCorner = new Vector3(packedBlockLocation.x - 0.5f * packedBlockDimensions.x,
                                                     packedBlockLocation.y + 0.5f * packedBlockDimensions.y,
                                                     packedBlockLocation.z + 0.5f * packedBlockDimensions.z);
            Vector3 southEastTopCorner = new Vector3(packedBlockLocation.x - 0.5f * packedBlockDimensions.x,
                                                     packedBlockLocation.y + 0.5f * packedBlockDimensions.y,
                                                     packedBlockLocation.z - 0.5f * packedBlockDimensions.z);

            // Compute sum of block heights
            float totalBlocksHeight = 0f;
            foreach (GameObject o in hiddenObjects)
            {
                totalBlocksHeight += o.transform.localScale.y;
            }

            // How much empty space to show between the block decorations, tweak the 1% at will
            float freeSpaceX = globalFreeSpacePercentage * packedBlock.transform.localScale.x;
            float freeSpaceY = globalFreeSpacePercentage * packedBlock.transform.localScale.y;
            float freeSpaceZ = globalFreeSpacePercentage * packedBlock.transform.localScale.z;

            // Compute block grid
            float roundedRoot = Mathf.Ceil(Mathf.Sqrt(hiddenObjects.Count));
            float blocksHorizontalAxis = roundedRoot;
            float blocksVerticalAxis = roundedRoot;
            // Check for an empty row (too many rows)
            // Math: Assuming 4x4 grid, if the last row is empty and we have 11 gameObjects, we calculate
            // 16 - 11 >=? 4 -> if yes then the last row is empty and can be removed. Note that 9 blocks will create a 3x3 grid
            if (blocksHorizontalAxis * blocksVerticalAxis - hiddenObjects.Count >= blocksVerticalAxis)
            {
                blocksVerticalAxis--;
            }

            // Add empty gameobject children to identify different clones
            GameObject northClones = new GameObject("northClones");
            GameObject southClones = new GameObject("southClones");
            GameObject westClones = new GameObject("westClones");
            GameObject eastClones = new GameObject("eastClones");
            northClones.transform.SetParent(packedBlock.transform);
            southClones.transform.SetParent(packedBlock.transform);
            westClones.transform.SetParent(packedBlock.transform);
            eastClones.transform.SetParent(packedBlock.transform);

            // Location on each surface, used to align clones on block surface, top-down left-right approach.
            // Blocks placed from east to west on northern surface, top to bottom
            Vector3 currentPosN = new Vector3(northEastTopCorner.x, northEastTopCorner.y - freeSpaceY, northEastTopCorner.z + freeSpaceZ);
            // Blocks placed from north to south on western surface, top to bottom
            Vector3 currentPosW = new Vector3(northWestTopCorner.x - freeSpaceX, northWestTopCorner.y - freeSpaceY, northWestTopCorner.z);
            // Blocks placed from west to east on southern surface, top to bottom
            Vector3 currentPosS = new Vector3(southWestTopCorner.x, southWestTopCorner.y - freeSpaceY, southWestTopCorner.z - freeSpaceZ);
            // Blocks placed from south to north on eastern surface, top to bottom
            Vector3 currentPosE = new Vector3(southEastTopCorner.x + freeSpaceX, southEastTopCorner.y - freeSpaceY, southEastTopCorner.z);

            // Create gameobject clones and set them on the walls of the packed block
            List<GameObject> clones = new List<GameObject>();
            float maxCloneHeightY = (packedBlockDimensions.y - freeSpaceY * (blocksVerticalAxis + 1)) / blocksVerticalAxis;
            int currentClone = 0;
            for (int i = 0; i < blocksVerticalAxis; i++)
            {
                // Used to determine how much space every block is allowed to take up on the horizontal axis
                float blockHeightHorizontal = 0f;
                for (int j = 0; j < blocksHorizontalAxis && i + j < hiddenObjects.Count - 1; j++)
                {
                    blockHeightHorizontal += hiddenObjects[i + j].transform.localScale.y;
                }
                // Used to determine how much space every block is allowed to take up on the vertical axis
                Dictionary<float, float> blockHeightVertical = new Dictionary<float, float>();
                // Draw the blocks
                for (int j = 0; j < blocksHorizontalAxis && i + j < hiddenObjects.Count - 1; j++)
                {
                    // Compute height percentage for current column if it isn't already computed
                    if (!blockHeightVertical.ContainsKey(j))
                    {
                        float totalHeight = 0f;
                        for (int k = 0; k < blocksVerticalAxis; k++)
                        {
                            if (k + j >= hiddenObjects.Count - 1)
                            {
                                break;
                            }
                            totalHeight += hiddenObjects[j + k].transform.localScale.y;
                        }
                        blockHeightVertical.Add(j, totalHeight);
                    }

                    GameObject o = hiddenObjects[i + j];
                    Material blockMaterial = o.GetComponent<Renderer>().material;

                    // Determines how much space the block is allowed to take up on the vertical Axis
                    float blockHeightPercentageY = o.transform.localScale.y / blockHeightVertical[j];
                    float blockHeightPercentageHorizontal = o.transform.localScale.y / blockHeightHorizontal;
                    float nodeSizeHorizontalZ = blockHeightPercentageHorizontal * (packedBlockDimensions.z - (blocksHorizontalAxis + 1) * freeSpaceZ);
                    float nodeSizeHorizontalX = blockHeightPercentageHorizontal * (packedBlockDimensions.x - (blocksHorizontalAxis + 1) * freeSpaceX);
                    float nodeSizeVertical = maxCloneHeightY * blockHeightPercentageY;

                    // Create north clone
                    GameObject cloneN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cloneN.name = currentClone + "-NorthDecorator";
                    cloneN.GetComponent<Renderer>().material = blockMaterial;
                    cloneN.transform.localScale = new Vector3(treemapDecoratorHeight * packedBlockDimensions.x, nodeSizeVertical, nodeSizeHorizontalZ);
                    cloneN.transform.localPosition = currentPosN + new Vector3(0, -(maxCloneHeightY / 2), cloneN.transform.localScale.z / 2);
                    cloneN.transform.SetParent(northClones.transform);

                    // Create south clone
                    GameObject cloneS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cloneS.name = currentClone + "-SouthDecorator";
                    cloneS.GetComponent<Renderer>().material = blockMaterial;
                    cloneS.transform.localScale = new Vector3(treemapDecoratorHeight * packedBlockDimensions.x, nodeSizeVertical, nodeSizeHorizontalZ);
                    cloneS.transform.localPosition = currentPosS + new Vector3(0, -(maxCloneHeightY / 2), -(cloneS.transform.localScale.z / 2));
                    cloneS.transform.SetParent(southClones.transform);

                    // Create west clone
                    GameObject cloneW = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cloneW.name = currentClone + "-WestDecorator";
                    cloneW.GetComponent<Renderer>().material = blockMaterial;
                    cloneW.transform.localScale = new Vector3(nodeSizeHorizontalX, nodeSizeVertical, treemapDecoratorHeight * packedBlockDimensions.z);
                    cloneW.transform.localPosition = currentPosW + new Vector3(-(cloneW.transform.localScale.x / 2), -(maxCloneHeightY / 2), 0);
                    cloneW.transform.SetParent(westClones.transform);

                    // Create east clone
                    GameObject cloneE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cloneE.name = currentClone + "-EastDecorator";
                    cloneE.GetComponent<Renderer>().material = blockMaterial;
                    cloneE.transform.localScale = new Vector3(nodeSizeHorizontalX, nodeSizeVertical, treemapDecoratorHeight * packedBlockDimensions.z);
                    cloneE.transform.localPosition = currentPosE + new Vector3(cloneE.transform.localScale.x / 2, -(maxCloneHeightY / 2), 0);
                    cloneE.transform.SetParent(eastClones.transform);

                    // Move position along
                    currentPosN += new Vector3(0, 0, nodeSizeHorizontalZ + freeSpaceZ);
                    currentPosS += new Vector3(0, 0, -(nodeSizeHorizontalZ + freeSpaceZ));
                    currentPosW += new Vector3(-(nodeSizeHorizontalX + freeSpaceX), 0, 0);
                    currentPosE += new Vector3(nodeSizeHorizontalX + freeSpaceX, 0, 0);
                    currentClone++;
                }
                currentPosN = new Vector3(currentPosN.x, currentPosN.y - freeSpaceY - maxCloneHeightY, northEastTopCorner.z + freeSpaceZ);
                currentPosS = new Vector3(currentPosS.x, currentPosS.y - freeSpaceY - maxCloneHeightY, southWestTopCorner.z + freeSpaceZ);
                currentPosW = new Vector3(northWestTopCorner.x + freeSpaceX, currentPosW.y - freeSpaceY - maxCloneHeightY, currentPosW.z);
                currentPosE = new Vector3(southEastTopCorner.x + freeSpaceX, currentPosE.y - freeSpaceY - maxCloneHeightY, currentPosE.z);
            }
        }

        /// <summary>
        /// Decorate nodes at runtime.
        /// </summary>
        void Start()
        {
            FetchNodeDetails();
            if (!FoldedBlock)
            {
                RenderLobby();
                RenderRoof();
            }
            else
            {
                if (Debug)
                {
                    if (!DecorateUsingTreemap)
                    {
                        TestImplementation(TestBlockColumnCount, TestBlockRowCount);
                    }
                    else
                    {
                        TestTreemapDecoration(TestBlockColumnCount, TestBlockRowCount);
                    }
                }
                if (!DecorateUsingTreemap)
                {
                    DecoratePackedBlock(ChildNodes, NodeObject);
                }
                else
                {
                    DecoratePackedBlockWithTreemap(SideTreemapSettings, TreemapGraph, NodeObject);
                }
            }
        }

        /// <summary>
        /// Tests the block decoration implementation both visually and mathematically.
        /// </summary>
        [Obsolete("This test method can be removed when everything works.")]
        private void TestImplementation(int columns, int rows)
        {
            GameObject debugObject = new GameObject("Debug");
            debugObject.transform.SetParent(NodeObject.transform);
            // Free space inbetween child nodes
            float freeSpaceX = globalFreeSpacePercentage * nodeSize.x;
            float freeSpaceZ = globalFreeSpacePercentage * nodeSize.z;
            // Create a few child nodes
            for (int i = 0; i < (rows * columns); i++)
            {
                GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // Max node size x = (size.x - freeSpaceCount.x * freeSpaceSize.x) / amountOfRows
                // Max node size z = (size.z - freeSpaceCount.z * freeSpaceSize.z) / amountOfColumns
                Vector3 childNodeDimensions = new Vector3((nodeSize.x - (rows + 1) * freeSpaceX) / rows,
                    UnityEngine.Random.Range(globalFreeSpacePercentage * nodeSize.y, nodeSize.y), (nodeSize.z - (columns + 1) * freeSpaceZ) / columns);
                o.transform.localScale = childNodeDimensions;
                o.name = i.ToString();
                o.GetComponent<Renderer>().material.color = Color.red;
                o.transform.SetParent(debugObject.transform);
                ChildNodes.Add(o);
            }
            // Find corners of parent node, parent node is moved using it's 3d center
            float parentNodeLowX = nodeLocation.x - nodeSize.x / 2;
            float parentNodeLowZ = nodeLocation.z - nodeSize.z / 2;
            float parentNodeFloorY = nodeLocation.y - nodeSize.y / 2;
            // Lay nodes out as a grid inside parent node
            int currentListIndex = 0;
            float currentLocationX = parentNodeLowX + freeSpaceX;
            float currentLocationZ = parentNodeLowZ + freeSpaceZ;
            float childWidthX = (nodeSize.x - (rows + 1) * freeSpaceX) / rows;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    GameObject currentChild = ChildNodes[currentListIndex];
                    // Location for current child
                    float childLocX = currentLocationX + currentChild.transform.localScale.x / 2;
                    float childLocZ = currentLocationZ + currentChild.transform.localScale.z / 2;
                    float childLocY = parentNodeFloorY + currentChild.transform.localScale.y / 2;
                    // Move child to new location
                    currentChild.transform.localPosition = new Vector3(childLocX, childLocY, childLocZ);
                    currentListIndex++;
                    currentLocationZ += freeSpaceZ + currentChild.transform.localScale.z;
                }
                currentLocationZ = parentNodeLowZ + freeSpaceZ;
                currentLocationX += freeSpaceX + childWidthX;
            }
        }

        /// <summary>
        /// Tests the treemap decoration implementation.
        /// </summary>
        [Obsolete("This test method can be removed when everything works.")]
        private void TestTreemapDecoration(int columns, int rows)
        {
            Graph graph = new Graph("DUMMY", name: "test");
            int counter = 0;
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Node node = new Node();
                    node.ID = counter + "-DummyNode";
                    graph.AddNode(node);
                    counter++;
                }
            }
            TreemapGraph = graph;
        }
    }
}
