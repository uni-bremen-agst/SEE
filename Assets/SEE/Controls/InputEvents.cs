using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    /// <summary>
    /// This event is called when a button is pressed.
    /// </summary>
    [System.Serializable]
    public class ButtonEvent : UnityEvent
    {
    }

    /// <summary>
    /// This event is called when a button is pressed or released.
    /// </summary>
    [System.Serializable]
    public class ButtonChangeEvent : UnityEvent<bool>
    {
    }

    /// <summary>
    /// This event is called when an input produces an axis value.
    /// </summary>
    [System.Serializable]
    public class AxisEvent : UnityEvent<float>
    {
    }

    /// <summary>
    /// This event is used when the function needs a 3D vector.
    /// </summary>
    [System.Serializable]
    public class Vector3Event : UnityEvent<Vector3>
    {
    }

    /// <summary>
    /// This event is used when the function needs the axis value and the direction.
    /// </summary>
    [System.Serializable]
    public class VectorEvent : UnityEvent<Vector3, float>
    {
    }

    /// <summary>
    /// Events where a game object is involved.
    /// </summary>
    public class GameObjectEvent : UnityEvent<GameObject>
    {
    }
}