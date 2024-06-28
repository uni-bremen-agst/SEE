using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window
{
    public class NoteButtonWindow : PlatformDependentComponent
    {
        private string windowPrefab => UIPrefabFolder + "NoteButtonWindow";
        private GameObject windowGameObject;

        protected override void StartDesktop()
        {
            windowGameObject = PrefabInstantiator.InstantiatePrefab(windowPrefab, Canvas.transform, false);

        }
    }
}
