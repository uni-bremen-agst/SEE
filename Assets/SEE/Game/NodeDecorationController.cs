using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;

/// <summary>
/// Decorates each block with an assigned texture
/// </summary>
public class NodeDecorationController : MonoBehaviour
{
    /// <summary>
    /// Whether or not the block contains other nodes 
    /// </summary>
    public bool foldedBlock = false;

    /// <summary>
    /// Enables debug and runs tests
    /// </summary>
    public bool debug = false;

    /// <summary>
    /// Number of rows for test block
    /// </summary>
    public int testBlockRowCount;

    /// <summary>
    /// Number of columns for test block
    /// </summary>
    public int testBlockColumnCount;

    /// <summary>
    /// The gameNode to be decorated
    /// </summary>
    public GameObject nodeObject;

    /// <summary>
    /// The gameNode's bounds' size
    /// </summary>
    private Vector3 nodeSize;

    /// <summary>
    /// The gameNode's location
    /// </summary>
    private Vector3 nodeLocation;

    /// <summary>
    /// To use treemap decoration/regular wall decorations
    /// </summary>
    public bool decorateUsingTreemap = false;

    /// <summary>
    /// Treemap layout settings for the block's side decorations
    /// </summary>
    public AbstractSEECity sideTreemapSettings;

    /// <summary>
    /// Treemap layout settings for the block's top decorator
    /// </summary>
    public AbstractSEECity surfaceTreemapSettings;

    /// <summary>
    /// Treemap graph
    /// </summary>
    private Graph treemapGraph;

    /// <summary>
    /// Roof type dropdown menu items
    /// </summary>
    public enum RoofType
    {
        Rectangular,
        Tetrahedron,
        Dome
    }

    /// <summary>
    /// Roof dropdown menu selector (inspector)
    /// </summary>
    public RoofType selectedRoofType;

    /// <summary>
    /// The Height-Percentage the bottom floor should have in
    /// contrast to the building height
    /// </summary>
    public float floorHightPercentage
    {
        get
        {
            return _floorHightPercentage;
        }
        set
        {
            _floorHightPercentage = Mathf.Clamp(value, 0f, 1f);
        }
    }

    /// <summary>
    /// How far out the lobby should be from the building, percentage
    /// is in contrast to the building width
    /// </summary>
    public float lobbySpanPercentage
    {
        get
        {
            return _lobbySpanPercentage;
        }
        set
        {
            _lobbySpanPercentage = Mathf.Clamp(value, 0f, 1f);
        }
    }

    /// <summary>
    /// The Height-Percentage the roof should have in contrast
    /// to the building height
    /// </summary>
    public float roofHeightPercentage
    {
        get
        {
            return _roofHeightPercentage;
        }
        set
        {
            _roofHeightPercentage = Mathf.Clamp(value, 0f, 1f);
        }
    }

    /// <summary>
    /// How far out/in the roof should be in contast to the building, percentage
    /// is in contrast to building width
    /// </summary>
    public float roofSpanPercentage
    {
        get
        {
            return _roofSpanPercentage;
        }
        set
        {
            _roofSpanPercentage = Mathf.Clamp(value, 0f, 1f);
        }
    }

    /// <summary>
    /// Contain the values of the above declared variables, limited to values between 0 and 1
    /// </summary>
    [SerializeField, Range(0f, 1f)]
    private float _floorHightPercentage, _lobbySpanPercentage, _roofHeightPercentage, _roofSpanPercentage;

    /// <summary>
    /// Tile-texture used to decorate the block around it's sides
    /// </summary>
    public Texture2D blockTexture
    {
        get;
        set;
    }

    /// <summary>
    /// Bottom floor texture
    /// </summary>
    public Texture2D bottomFloorTexture
    {
        get;
        set;
    }

    /// <summary>
    /// Roof texture
    /// </summary>
    public Texture2D roofTexture
    {
        get;
        set;
    }

    /// <summary>
    /// Get the gameNode's different properties
    /// </summary>
    private void fetchNodeDetails()
    {
        nodeSize = nodeObject.transform.localScale;
        nodeLocation = nodeObject.transform.position;
    }

    /// <summary>
    /// Renders the bottom floor of a building
    /// </summary>
    private void renderLobby()
    {
        float lobbySizeX = nodeSize.x + nodeSize.x * _lobbySpanPercentage;
        float lobbySizeZ = nodeSize.z + nodeSize.z * _lobbySpanPercentage;
        float lobbyHeight = nodeSize.y * _floorHightPercentage;
        // Create lobby gameObject
        GameObject lobby = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lobby.name = "Lobby";
        lobby.transform.localScale = new Vector3(lobbySizeX, lobbyHeight, lobbySizeZ);
        // *** Note: setting the lobby as a child object needs to be done after the transform, otherwise the size
        //     is relative to the parent object ***
        lobby.transform.SetParent(nodeObject.transform);
        // Get the point on the Y axis at the bottom of the building
        float buildingGroundFloorHeight = nodeLocation.y - (nodeSize.y / 2);
        // Set the lobby to be at buildingGroundFloorHeight + half the height of the lobby (so its floor touches the building floor)
        float lobbyGroundFloorHeight = buildingGroundFloorHeight + (lobby.transform.localScale.y / 2);
        // Move the lobby object to te correct location
        lobby.transform.position = new Vector3(nodeLocation.x, lobbyGroundFloorHeight, nodeLocation.z);
    }

    /// <summary>
    /// Renders the tetrahedron roof of a building
    /// *** Percentages are supplied as values between 0 and 1 ***
    /// </summary>
    private void renderRoof()
    {
        float roofSizeX = nodeSize.x + nodeSize.x * _roofSpanPercentage;
        float roofSizeZ = nodeSize.z + nodeSize.z * _roofSpanPercentage;
        float roofHeight = nodeSize.y * _roofHeightPercentage;
        // Create roof GameObject
        switch (selectedRoofType)
        {
            case RoofType.Tetrahedron:
                GameObject tetrahedron = createPyramid(roofSizeX, roofHeight, roofSizeZ);
                tetrahedron.transform.SetParent(nodeObject.transform);
                // Move tetrahedron to top of building, tetrahedron is moved with the bottom left corner
                tetrahedron.transform.position = new Vector3(nodeLocation.x - roofSizeX / 2, nodeSize.y / 2 + nodeLocation.y, nodeLocation.z - roofSizeZ / 2);
                break;
            case RoofType.Rectangular:
                renderRectangularRoof(roofSizeX, roofHeight, roofSizeZ);
                break;
            case RoofType.Dome:
                renderDomeRoof(roofSizeX, roofHeight, roofSizeZ);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Renders a rectangular roof for the node object
    /// <param name="roofSizeX">The roof's size on the X-axis</param>
    /// <param name="roofHeight">The roof's height on the Y-axis</param>
    /// <param name="roofSizeZ">The roof's size on the Z-axis</param>
    /// </summary>
    private void renderRectangularRoof(float roofSizeX, float roofHeight, float roofSizeZ)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "RectangularRoof";
        go.transform.localScale = new Vector3(roofSizeX, roofHeight, roofSizeZ);
        float nodeRoofHeight = nodeObject.transform.position.y + nodeSize.y / 2;
        go.transform.position = new Vector3(nodeLocation.x, nodeRoofHeight, nodeLocation.z);
        go.transform.SetParent(nodeObject.transform);
    }

    /// <summary>
    /// Renders a dome roof for the node object
    /// <param name="roofSizeX">The roof's size on the X-axis</param>
    /// <param name="roofHeight">The roof's height on the Y-axis</param>
    /// <param name="roofSizeZ">The roof's size on the Z-axis</param>
    /// </summary>
    private void renderDomeRoof(float roofSizeX, float roofHeight, float roofSizeZ)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SphericalRoof";
        go.transform.localScale = new Vector3(roofSizeX, roofHeight, roofSizeZ);
        float nodeRoofHeight = nodeObject.transform.position.y + nodeSize.y / 2;
        go.transform.position = new Vector3(nodeLocation.x, nodeRoofHeight, nodeLocation.z);
        go.transform.SetParent(nodeObject.transform);
    }

    /// <summary>
    /// <author name="Leonard Haddad"/>
    /// Generates a 4-faced pyramid at the given coordinates
    /// Inspired by an article by <a href="https://blog.nobel-joergensen.com/2010/12/25/procedural-generated-mesh-in-unity/">Morten Nobel-Jørgensen</a>,
    /// </summary>
    public GameObject createPyramid(float sizeX, float height, float sizeZ)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Tetrahedron";
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        // Tetrahedron floor nodes
        Vector3 p0 = new Vector3(0, 0, 0);
        Vector3 p1 = new Vector3(sizeX, 0, 0);
        Vector3 p2 = new Vector3(sizeX, 0, sizeZ);
        Vector3 p3 = new Vector3(0, 0, sizeZ);
        // Tetrahedron top node
        Vector3 p4 = new Vector3(sizeX / 2, height, sizeZ / 2);
        // Create gameObject mesh
        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = new Vector3[] {
            p0,p1,p3, // Bottom vertex #1
            p2,p3,p1, // Bottom vertex #2
            p1,p4,p2,
            p2,p4,p3,
            p3,p4,p0,
            p0,p4,p1
        };
        mesh.triangles = new int[]
        {
            0,1,2,
            3,4,5,
            6,7,8,
            9,10,11,
            12,13,14,
            15,16,17
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
        meshFilter.mesh = mesh;
        return go;
    }

    /// <summary>
    /// Decorates the block
    /// <param name="hiddenObjects">The list of gamenodes that are hidden inside the packed block</param>
    /// <param name="packedBlock">The packed block</param>
    /// </summary>
    private void decoratePackedBlock(List<GameObject> hiddenObjects, GameObject packedBlock)
    {
        GameObject topDecorator = new GameObject("TopDecorator");
        for (int i = 0; i < hiddenObjects.Count; i++)
        {
            GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clone.transform.localPosition = new Vector3(hiddenObjects[i].transform.position.x, packedBlock.transform.position.y + packedBlock.transform.localScale.y/2, hiddenObjects[i].transform.position.z);
            clone.transform.localScale = new Vector3(hiddenObjects[i].transform.localScale.x, 0.01f, hiddenObjects[i].transform.localScale.z);
            clone.GetComponent<Renderer>().material = hiddenObjects[i].GetComponent<Renderer>().material;
            clone.name = hiddenObjects[i].name + "-TopDecorator";
            clone.transform.SetParent(topDecorator.transform);
        }
        topDecorator.transform.SetParent(packedBlock.transform);
        decoratePackedBlockWalls(hiddenObjects, packedBlock);
    }

    /// <summary>
    /// Decorates the top of the packed block using a treemap
    /// <param name="settings">The settings to be applied to the layout</param>
    /// <param name="hiddenNodesGraph">Graph containing the hidden nodes</param>
    /// <param name="packedBlockLocation">The location of the packed block</param>
    /// <param name="packedBlockDimensions">The dimensions of the packed block</param>
    /// <param name="treemapParent">The parent gameobject that holds all decorators</param>
    /// </summary>
    private void decoratePackedBlockSurfaceWithTreemap(AbstractSEECity settings, Graph hiddenNodesGraph, Vector3 packedBlockLocation, Vector3 packedBlockDimensions, GameObject treemapParent)
    {
        GraphRenderer renderer = new GraphRenderer(settings, hiddenNodesGraph);
        GameObject empty = new GameObject("TopSurfaceTreemap");
        renderer.Draw(empty);
        empty.transform.localScale = new Vector3(packedBlockDimensions.x, 0.1f, packedBlockDimensions.z);
        empty.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y + packedBlockDimensions.y / 2 - empty.transform.localScale.y / 2, packedBlockLocation.z);
        empty.transform.SetParent(treemapParent.transform);
    }

    /// <summary>
    /// Decorates the walls of the packed block using a treemap
    /// <param name="settings">The settings to be applied to the layout</param>
    /// <param name="hiddenNodesGraph">Graph containing the hidden nodes</param>
    /// <param name="packedBlock">The packed block</param>
    /// </summary>
    private void decoratePackedBlockWithTreemap(AbstractSEECity settings, Graph hiddenNodesGraph, GameObject packedBlock)
    {
        // Graph renderer to render treemaps
        GraphRenderer renderer = new GraphRenderer(settings, hiddenNodesGraph);
        
        // Get packed block dimensions and location, North - Positive X, West - Positive Z
        Vector3 packedBlockDimensions = packedBlock.transform.localScale;
        Vector3 packedBlockLocation = packedBlock.transform.localPosition;

        // Render treemaps on each surface
        GameObject treemapParent = new GameObject("Treemap-Decorators");
        treemapParent.transform.SetParent(nodeObject.transform);
        decoratePackedBlockSurfaceWithTreemap(surfaceTreemapSettings, hiddenNodesGraph, packedBlockLocation, packedBlockDimensions, treemapParent);
        // North
        GameObject planeN = new GameObject();
        planeN.name = "northTreemap";
        renderer.Draw(planeN);
        planeN.transform.localScale = new Vector3(packedBlockDimensions.y, 0.1f, packedBlockDimensions.z);
        planeN.transform.localPosition = new Vector3(packedBlockLocation.x + packedBlockDimensions.x / 2 - planeN.transform.localScale.y / 2, packedBlockLocation.y, packedBlockLocation.z);
        planeN.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
        planeN.transform.SetParent(treemapParent.transform);
        // South
        GameObject planeS = new GameObject();
        planeS.name = "southTreemap";
        renderer.Draw(planeS);
        planeS.transform.localScale = new Vector3(packedBlockDimensions.y, 0.1f, packedBlockDimensions.z);
        planeS.transform.localPosition = new Vector3(packedBlockLocation.x - packedBlockDimensions.x / 2 + planeN.transform.localScale.y / 2, packedBlockLocation.y, packedBlockLocation.z);
        planeS.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        planeS.transform.SetParent(treemapParent.transform);
        // West
        GameObject planeW = new GameObject();
        planeW.name = "westTreemap";
        renderer.Draw(planeW);
        planeW.transform.localScale = new Vector3(packedBlockDimensions.z, 0.1f, packedBlockDimensions.y);
        planeW.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y, packedBlockLocation.z + packedBlockDimensions.z / 2 - planeN.transform.localScale.y / 2);
        planeW.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        planeW.transform.SetParent(treemapParent.transform);
        // East
        GameObject planeE = new GameObject();
        planeE.name = "eastTreemap";
        renderer.Draw(planeE);
        planeE.transform.localScale = new Vector3(packedBlockDimensions.z, 0.1f, packedBlockDimensions.y);
        planeE.transform.localPosition = new Vector3(packedBlockLocation.x, packedBlockLocation.y, packedBlockLocation.z - packedBlockDimensions.z / 2 + planeN.transform.localScale.y / 2);
        planeE.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        planeE.transform.SetParent(treemapParent.transform);
        // TODO Test if needed with actual gameNodes
        Portal.SetInfinitePortal(packedBlock);
    }

    /// <summary>
    /// Decorates the walls of the packed block
    /// <param name="hiddenObjects">The list of gamenodes that are hidden inside the packed block</param>
    /// <param name="packedBlock">The packed block</param>
    /// </summary>
    private void decoratePackedBlockWalls(List<GameObject> hiddenObjects, GameObject packedBlock)
    {
        // Get packed block dimensions and corners, North - Positive X, West - Positive Z
        Vector3 packedBlockDimensions = packedBlock.transform.localScale;
        Vector3 packedBlockLocation = packedBlock.transform.position;

        // Corners of the block computed with simple geometry
        Vector3 northWestTopCorner = new Vector3(packedBlockLocation.x + 0.5f * packedBlockDimensions.x, packedBlockLocation.y + 0.5f * packedBlockDimensions.y, packedBlockLocation.z + 0.5f * packedBlockDimensions.z);
        Vector3 northEastTopCorner = new Vector3(packedBlockLocation.x + 0.5f * packedBlockDimensions.x, packedBlockLocation.y + 0.5f * packedBlockDimensions.y, packedBlockLocation.z - 0.5f * packedBlockDimensions.z);
        Vector3 southWestTopCorner = new Vector3(packedBlockLocation.x - 0.5f * packedBlockDimensions.x, packedBlockLocation.y + 0.5f * packedBlockDimensions.y, packedBlockLocation.z + 0.5f * packedBlockDimensions.z);
        Vector3 southEastTopCorner = new Vector3(packedBlockLocation.x - 0.5f * packedBlockDimensions.x, packedBlockLocation.y + 0.5f * packedBlockDimensions.y, packedBlockLocation.z - 0.5f * packedBlockDimensions.z);

        // Compute sum of block heights
        float totalBlocksHeight = 0f;
        foreach (GameObject o in hiddenObjects)
        {
            totalBlocksHeight += packedBlockDimensions.y;
        }

        // How much empty space to show between the block decorations, tweak the 1% at will
        float freeSpaceX = 0.01f * packedBlock.transform.localScale.x;
        float freeSpaceY = 0.01f * packedBlock.transform.localScale.y;
        float freeSpaceZ = 0.01f * packedBlock.transform.localScale.z;

        // Compute block grid
        float roundedRoot = Mathf.Ceil(Mathf.Sqrt(hiddenObjects.Count));
        float blocksHorizontalAxis = roundedRoot;
        float blocksVerticalAxis = roundedRoot;
        Debug.Log("Horizontal: " + blocksHorizontalAxis + ", Vertical: " + blocksVerticalAxis);
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

        // Location on each surface, used to align clones on block surface, top-down left-right approach
        Vector3 currentPosN = new Vector3(northEastTopCorner.x, northEastTopCorner.y - freeSpaceY, northEastTopCorner.z + freeSpaceZ); // Blocks placed from east to west on northern surface, top to bottom
        Vector3 currentPosW = new Vector3(northWestTopCorner.x - freeSpaceX, northWestTopCorner.y - freeSpaceY, northWestTopCorner.z); // Blocks placed from north to south on western surface, top to bottom
        Vector3 currentPosS = new Vector3(southWestTopCorner.x, southWestTopCorner.y - freeSpaceY, southWestTopCorner.z - freeSpaceZ); // Blocks placed from west to east on southern surface, top to bottom
        Vector3 currentPosE = new Vector3(southEastTopCorner.x + freeSpaceX, southEastTopCorner.y - freeSpaceY, southEastTopCorner.z); // Blocks placed from south to north on eastern surface, top to bottom

        // Create gameobject clones and set them on the walls of the packed block
        List<GameObject> clones = new List<GameObject>();
        float maxCloneHeightY = (packedBlockDimensions.y - freeSpaceY * (blocksVerticalAxis + 1)) / blocksVerticalAxis;
        int currentClone = 0;
        for (int i = 0; i < blocksVerticalAxis; i++)
        {
            // Used to determine how much space every block is allowed to take up on the horizontal axis
            float blockHeightHorizontal = 0f;
            for (int j = 0; j < blocksHorizontalAxis; j++)
            {
                if (i + j >= hiddenObjects.Count - 1)
                {
                    break;
                }
                blockHeightHorizontal += hiddenObjects[i + j].transform.localScale.y;
            }
            // Used to determine how much space every block is allowed to take up on the vertical axis
            Dictionary<float, float> blockHeightVertical = new Dictionary<float, float>();
            // Draw the blocks
            for (int j = 0; j < blocksHorizontalAxis; j++)
            {
                // The grid isn't necessarily filled in all spots
                if (i + j >= hiddenObjects.Count - 1)
                {
                    break;
                }
                // Compute height percentage for current column if it isn't already computed
                if (!blockHeightVertical.ContainsKey(j))
                {
                    float totalHeight = 0f;
                    for (int k = 0; k < blocksVerticalAxis; k++)
                    {
                        if (k+j >= hiddenObjects.Count-1)
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
                cloneN.transform.localScale = new Vector3(0.1f * packedBlockDimensions.x, nodeSizeVertical, nodeSizeHorizontalZ);
                cloneN.transform.localPosition = currentPosN + new Vector3(0, -(maxCloneHeightY / 2), cloneN.transform.localScale.z / 2);
                cloneN.transform.SetParent(northClones.transform);

                // Create south clone
                GameObject cloneS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloneS.name = currentClone + "-SouthDecorator";
                cloneS.GetComponent<Renderer>().material = blockMaterial;
                cloneS.transform.localScale = new Vector3(0.1f * packedBlockDimensions.x, nodeSizeVertical, nodeSizeHorizontalZ);
                cloneS.transform.localPosition = currentPosS + new Vector3(0, -(maxCloneHeightY / 2), -(cloneS.transform.localScale.z / 2));
                cloneS.transform.SetParent(southClones.transform);

                // Create west clone
                GameObject cloneW = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloneW.name = currentClone + "-WestDecorator";
                cloneW.GetComponent<Renderer>().material = blockMaterial;
                cloneW.transform.localScale = new Vector3(nodeSizeHorizontalX, nodeSizeVertical, 0.1f * packedBlockDimensions.z);
                cloneW.transform.localPosition = currentPosW + new Vector3(-(cloneW.transform.localScale.x / 2), -(maxCloneHeightY / 2), 0);
                cloneW.transform.SetParent(westClones.transform);

                // Create east clone
                GameObject cloneE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloneE.name = currentClone + "-EastDecorator";
                cloneE.GetComponent<Renderer>().material = blockMaterial;
                cloneE.transform.localScale = new Vector3(nodeSizeHorizontalX, nodeSizeVertical, 0.1f * packedBlockDimensions.z);
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

    // Start is called before the first frame update
    void Start()
    {
        fetchNodeDetails();
        if (!foldedBlock)
        {
            renderLobby();
            renderRoof();
        }
        else
        {
            if (debug)
            {
                if (!decorateUsingTreemap)
                {
                    testImplementation(testBlockColumnCount, testBlockRowCount);
                }
                else
                {
                    testTreemapDecoration(testBlockColumnCount, testBlockRowCount);
                }
            }
            if (!decorateUsingTreemap)
            {
                decoratePackedBlock(childNodes, nodeObject);
            }
            else
            {
                decoratePackedBlockWithTreemap(sideTreemapSettings, treemapGraph, nodeObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeObject.transform.localScale != nodeSize)
        {
            // TODO remove this after testing
            Debug.Log("Node size changed, reloading data...");
            for (int i = 0; i < nodeObject.transform.childCount; ++i)
            {
                Destroy(nodeObject.transform.GetChild(i).gameObject);
            }
            fetchNodeDetails();
            if (!foldedBlock)
            {
                renderLobby();
                renderRoof();
            }
            else
            {
                if (debug)
                {
                    if (!decorateUsingTreemap)
                    {
                        testImplementation(testBlockColumnCount, testBlockRowCount);
                    }
                    else
                    {
                        testTreemapDecoration(testBlockColumnCount, testBlockRowCount);
                    }
                }
                if (!decorateUsingTreemap)
                {
                    decoratePackedBlock(childNodes, nodeObject);
                }
                else
                {
                    decoratePackedBlockWithTreemap(sideTreemapSettings, treemapGraph, nodeObject);
                }
            }
        }
    }

    /// <summary>
    /// Child nodes of this block
    /// </summary>
    private List<GameObject> childNodes = new List<GameObject>();

    /// <summary>
    /// Test the block decoration implementation both visually and mathematically
    /// </summary>
    private void testImplementation(int columns, int rows)
    {
        GameObject debugObject = new GameObject("Debug");
        debugObject.transform.SetParent(nodeObject.transform);
        // Free space inbetween child nodes
        float freeSpaceX = 0.01f * nodeSize.x;
        float freeSpaceZ = 0.01f * nodeSize.z;
        // Create a few child nodes
        for (int i = 0; i < (rows * columns); i++)
        {
            GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Max node size x = (size.x - freeSpaceCount.x * freeSpaceSize.x) / amountOfRows
            // Max node size z = (size.z - freeSpaceCount.z * freeSpaceSize.z) / amountOfColumns
            Vector3 childNodeDimensions = new Vector3((nodeSize.x - (rows + 1) * freeSpaceX) / rows,
                Random.Range(0.01f * nodeSize.y, nodeSize.y), (nodeSize.z - (columns + 1) * freeSpaceZ) / columns);
            o.transform.localScale = childNodeDimensions;
            o.name = i.ToString();
            o.GetComponent<Renderer>().material.color = Color.red;
            o.transform.SetParent(debugObject.transform);
            childNodes.Add(o);
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
                GameObject currentChild = childNodes[currentListIndex];
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
    /// Test the treemap decoration implementation
    /// </summary>
    private void testTreemapDecoration(int columns, int rows)
    {
        Graph graph = new Graph("test");
        int counter = 0;
        for (int i=0; i<columns; i++)
        {
          //  Node parent = new Node();
         //   parent.ID = i + "-DummyParentNode";
         //   graph.AddNode(parent);
            for (int j=0; j<rows; j++)
            {
                Node n = new Node();
                n.ID = counter + "-DummyNode";
                n.SetLevel(1);
            //    n.SetLevel(1);
             //   parent.AddChild(n);
             //   n.Parent = parent;
                graph.AddNode(n);
                counter++;
            }
        }
        graph.FinalizeNodeHierarchy();
        treemapGraph = graph;
    }
}
