using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decorates each block with an assigned texture
/// </summary>
public class NodeDecorationController : MonoBehaviour
{
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
    private void fetchNodeDetails() {
        nodeSize = nodeObject.transform.localScale;
        nodeLocation = nodeObject.transform.position;
    }

    /// <summary>
    /// Renders the bottom floor of a building
    /// <param name="floorHightPercentage">The Height-Percentage the bottom floor should have in
    /// contrast to the building height</param>
    /// <param name="lobbySpanPercentage">How far out the lobby should be from the building, percentage
    /// is in contrast to the building width</param>
    /// *** Percentages are supplied as values between 0 and 1 ***
    /// </summary>
    private void renderLobby(float floorHightPercentage, float lobbySpanPercentage)
    {
        // ========== TODO scale these when scaling gameobject ==========
        float lobbySizeX = nodeSize.x + nodeSize.x * lobbySpanPercentage;
        float lobbySizeZ = nodeSize.z + nodeSize.z * lobbySpanPercentage;
        float lobbyHeight = nodeSize.y * floorHightPercentage;
        // Create lobby gameObject
        GameObject lobby = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lobby.transform.localScale = new Vector3(lobbySizeX, lobbyHeight, lobbySizeZ);
        // Get the point on the Y axis at the bottom of the building
        float buildingGroundFloorHeight = nodeLocation.y - (nodeSize.y / 2);
        // Set the lobby to be at buildingGroundFloorHeight + half the height of the lobby (so its floor touches the building floor)
        float lobbyGroundFloorHeight = buildingGroundFloorHeight + (lobby.transform.localScale.y / 2);
        // Move the lobby object to te correct location
        lobby.transform.position = new Vector3(nodeLocation.x, lobbyGroundFloorHeight, nodeLocation.z);
    }

    /// <summary>
    /// Renders the tetrahedron roof of a building
    /// <param name="roofHeightPercentage">The Height-Percentage the roof should have in contrast
    /// to the building height</param>
    /// <param name="roofSpanPercentage">How far out/in the roof should be in contast to the building, percentage
    /// is in contrast to building width</param>
    /// *** Percentages are supplied as values between 0 and 1 ***
    /// </summary>
    private void renderRoof(float roofHeightPercentage, float roofSpanPercentage)
    {

    }

    /// <summary>
    /// Computes how many tiles fit the given side of the gameobject block
    /// <param name="side">Side of the block, which's tile-amount to calculate</param>
    /// </summary>
    // TODO side 0-3, clockwise
    private int GetTilesPerSide(int side)
    {
        return 0;
    }

    /// <summary>
    /// Decorates the block
    /// </summary>
    private void decorateBlock()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
