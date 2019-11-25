using System.Collections.Generic;
using UnityEngine;
using System;
using TinySpline;

/// <summary>
/// A script to move a camera programmatically along a path. This
/// script must be added as a component to a camera.
/// </summary>
public class ScriptedCamera : MonoBehaviour
{
    /// <summary>
    /// The coordinates to interpolate. W stores the time.
    /// </summary>
    public Vector4[] locations = new Vector4[2];

    /// <summary>
    /// The interpolated spline.
    /// </summary>
    private BSpline spline;

    /// <summary>
    /// Accumulated time since game start in seconds.
    /// </summary>
    private float time = 0.0f;

    private IList<double> VectorsToList(Vector4[] vectors)
    {
        List<double> list = new List<double>();
        foreach(Vector4 vec in vectors)
        {
            list.Add(vec.x);
            list.Add(vec.y);
            list.Add(vec.z);
            list.Add(vec.w);
        }
        return list;
    }

    private Vector4[] ListToVectors(IList<double> list)
    {
        int num = list.Count / 4;
        Vector4[] vectors = new Vector4[num];
        for (int i = 0; i < num; i++)
        {
            vectors[i] = new Vector4(
                (float)list[i * 4],
                (float)list[i * 4 + 1],
                (float)list[i * 4 + 2],
                (float)list[i * 4 + 3]
                );
        }
        return vectors;
    }

    /// <summary>
    /// Is called whenever a value is changed in the editor.
    /// </summary>
    void OnValidate()
    {
        if (locations.Length < 2)
        {
            Debug.LogWarning("ScriptedCamera: Requiring at least two locations\n");
            System.Array.Resize(ref locations, 2);
        }
    }

    // Start is called before the first frame update.
    void Start()
    {
        Debug.Log("Starting ScriptedCamera\n");
        try
        {
            spline = TinySpline.Utils.interpolateCubic(VectorsToList(locations), 4);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ScriptedCamera: Interpolation failed with error '{e.Message}'\n");
            Debug.LogWarning($"ScriptedCamera: Createing spline with default location\n");
            spline = new BSpline(1, 4, 0);
            spline.controlPoints = new List<double> { 0, 0, 0, 0 };
        }
        transform.position = ListToVectors(spline.controlPointAt(0))[0];
    }

    /// <summary>
    /// Update is called once per frame and moves the camera along the
    /// timed path.
    /// </summary>
    void Update()
    {
        // Time.deltaTime is the time since the last Update() in seconds.
        time += Time.deltaTime;
        transform.position = ListToVectors(spline.bisect(time, 0.001, false, 3).result)[0];
    }
}
