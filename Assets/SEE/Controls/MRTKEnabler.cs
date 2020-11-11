using Microsoft.MixedReality.Toolkit;
using UnityEngine;

namespace SEE.Controls
{
    public class MRTKEnabler : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            //TODO: MixedReality namespace seems to be missing
            MixedRealityToolkit.Instance.enabled = false;
        }
    }
}