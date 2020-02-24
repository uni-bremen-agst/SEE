using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    public class ButtonEvent : UnityEvent
    {

    }

    public class AxisEvent : UnityEvent<float>
    {

    }

    public class VectorEvent : UnityEvent<Vector3>
    {

    }
}
