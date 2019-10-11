using UnityEngine;
using UnityEditor;
using System.Collections;
using CScape;
//using UnityEditor;

namespace CScape
{


    public class CScapeMenu : MonoBehaviour
    {
        //	private GameObject VRPano;
        [MenuItem("GameObject/CScape/Create MegaCity", false, 10)]
        static void CreateVRCameraObject(MenuCommand menuCommand)
        {


            GameObject CScapeCity = PrefabUtility.InstantiatePrefab(Resources.Load("CScapeCity")) as GameObject;
            CScapeCity.name = "CScape City";
            //PrefabUtility.DisconnectPrefabInstance (VRPano);
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.UnpackPrefabInstance(CScapeCity, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
#endif
            GameObjectUtility.SetParentAndAlign(CScapeCity, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(CScapeCity, "Create " + CScapeCity.name);
            Selection.activeObject = CScapeCity;
        }

        [MenuItem("GameObject/CScape/Create MegaCity Complex", false, 10)]
        static void CreateMegacitySlanted(MenuCommand menuCommand)
        {


            GameObject CScapeCity = PrefabUtility.InstantiatePrefab(Resources.Load("CScapeCityComplex")) as GameObject;
            CScapeCity.name = "CScape City";
            //PrefabUtility.DisconnectPrefabInstance (VRPano);
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.UnpackPrefabInstance(CScapeCity, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
#endif
            GameObjectUtility.SetParentAndAlign(CScapeCity, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(CScapeCity, "Create " + CScapeCity.name);
            Selection.activeObject = CScapeCity;
        }



    }
}

