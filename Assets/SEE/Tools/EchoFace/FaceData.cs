using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single frame of face-tracking data, including blendshape
/// weights, facial landmarks, and a timestamp.
/// </summary>
[Serializable]
public class FaceData
{
    // A nested class to represent the 'x', 'y', 'z' landmark coordinates
    [Serializable]
    public class LandmarkCoordinates
    {
        public float x;
        public float y;
        public float z;
    }

    public Dictionary<string, float> blendshapes;
    public Dictionary<string, LandmarkCoordinates> landmarks;

    public long ts;
}
