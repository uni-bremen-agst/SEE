#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// The static constructor of this class will be run as soon as the Unity
    /// editor launches. You can put it any code that needs to be run without
    /// requiring action from the user. 
    /// </summary>
    [InitializeOnLoad]
    public class EditorScriptOnLaunch
    {

        /// <summary>
        /// Static constructor. A static constructor is always guaranteed to be 
        /// called before any static function or instance of the class is used, 
        /// but the InitializeOnLoad attribute ensures that it is called as the 
        /// editor launches.
        /// </summary>
        static EditorScriptOnLaunch()
        {
            Debug.Log("The Unity editor is started.\n");
        }
    }
}

#endif