using System;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Enables/disables game object named "CodeSpace" holding the 
    /// source-code viewer.
    /// </summary>
    [Obsolete("This class is used only for capturing videos.")]
    internal class CodeSpaceEnabler : MonoBehaviour
    {
        private const string CodeSpaceName = "CodeSpace";
        private bool showCodeWindow = true;

        private GameObject codeSpace;

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.ShowCodeWindow))
            {
                showCodeWindow = !showCodeWindow;
                if (codeSpace == null)
                {
                    codeSpace = GameObject.Find(CodeSpaceName);
                    if (codeSpace == null)
                    {
                        codeSpace = GameObject.Find(CodeSpaceName);
                        Debug.LogError($"No game object named {CodeSpaceName} found.\n");
                        enabled = false;
                    }
                }
                codeSpace.active = showCodeWindow;
            }
        }
    }
}
