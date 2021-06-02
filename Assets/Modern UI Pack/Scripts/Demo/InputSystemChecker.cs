using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Michsky.UI.ModernUIPack
{
    public class InputSystemChecker : MonoBehaviour
    {
        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            StandaloneInputModule tempModule = gameObject.GetComponent<StandaloneInputModule>();
            Destroy(tempModule);
            InputSystemUIInputModule newModule = gameObject.AddComponent<InputSystemUIInputModule>();
            newModule.enabled = false;
            newModule.enabled = true;
#endif
        }
    }
}