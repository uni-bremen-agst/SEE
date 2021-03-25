using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decorates each block with an assigned texture
/// </summary>
public class NodeDecorationController : MonoBehaviour
{
    /// <summary>
    /// The gameobject to be decorated
    /// </summary>
    private GameObject nodeObject;

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
