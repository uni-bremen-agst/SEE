using System;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// Represents a class member
    /// </summary>
    public class ClassMember : MonoBehaviour
    {
        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/ClassMember";
        private const string CLASS_MEMBER_OBJECT_PATH = "Scroll View/Viewport/Content/MemberText";

        public string Text { get; set; }

        public void Start()
        {
            GameObject classMember =
                PrefabInstantiator.InstantiatePrefab(PREFAB_NAME, transform, false);

            TextMeshProUGUI tm = classMember.transform.Find(CLASS_MEMBER_OBJECT_PATH).gameObject
                .GetComponent<TextMeshProUGUI>();
            tm.text = Text;

            //RectTransform rt = classMember.GetComponent<RectTransform>();
            //     rt.anchorMin = new Vector2(0, 0);
            //     rt.anchorMax = new Vector2(1, 1);
            //     rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}