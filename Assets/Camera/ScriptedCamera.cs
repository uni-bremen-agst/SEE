using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script to move a camera programmatically along a path. This
/// script must be added as a component to a camera.
/// </summary>
public class ScriptedCamera : MonoBehaviour
{
    /// <summary>
    /// Where to start.
    /// </summary>
    public Vector3 initialPosition = new Vector3(-3.5f, 15.0f, -112.0f);
    /// <summary>
    /// Where to finish.
    /// </summary>
    public Vector3 finalPosition = new Vector3(-3.5f, 15.0f, 15.0f);
    /// <summary>
    /// The unit of timing in seconds.
    /// </summary>
    public float tickUnit = 0.25f;
    /// <summary>
    /// The distance to be moved for every tick.
    /// </summary>
    public float distancePerTick = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        // Initial position of the camera.
        transform.position = initialPosition;
        path = CreatePath();
        current = 0;
        time = 0.0f;
        Debug.Log("ScriptedCamera started.\n");
    }

    /// <summary>
    /// Timed path along which the camera should be moved.
    /// </summary>
    private List<Vector4> path;

    /// <summary>
    /// Adds timing parameter <paramref name="time"/> to position.
    /// </summary>
    /// <param name="position">position to be reached</param>
    /// <param name="time">time to be added</param>
    /// <returns>vector containing position and time (the latter in w co-ordinate)</returns>
    private static Vector4 Add_Time(Vector3 position, float time)
    {
        // Vector3s can be implicitly converted to Vector4(w is set to zero in the result).
        Vector4 result = position;
        result.w = time;
        return result;
    }

    /// <summary>
    /// Creates a timed path from initialPosition to finalPosition.
    /// </summary>
    /// <returns>path from initialPosition to finalPosition</returns>
    private List<Vector4> CreatePath()
    {
        List<Vector4> path = new List<Vector4>();

        Vector3 position = initialPosition;
        float t = 0.0f;
        path.Add(Add_Time(position, t));

        while (position.z <= finalPosition.z)
        {
            t += tickUnit;
            position.z += distancePerTick;
            path.Add(Add_Time(position, t));
        }
        return path;
    }

    /// <summary>
    /// Accumulated time since game start in seconds.
    /// </summary>
    private float time = 0.0f;

    /// <summary>
    /// The current position in the path.
    /// </summary>
    private int current = 0;

    /// <summary>
    /// Update is called once per frame and moves the camera along the
    /// timed path.
    /// </summary>
    void Update()
    {
        if (current < path.Count)
        {
            // Time.deltaTime is the time since the last Update() in seconds.
            time += Time.deltaTime;
            // Are we to reach the next position in the path?
            if (time >= path[current].w)
            { 
                //Debug.LogFormat("Moving ScriptedCamera {0}.\n", path[current]);

                // Implicit conversion from Vector4 to Vector3 where the w co-ordinate is dropped.
                // Thus, we keeop only the position data.
                transform.position = path[current];
                current++;
            }
        }
        else
        {
            Debug.Log("ScriptedCamera stopped.\n");
        }
    }
}
