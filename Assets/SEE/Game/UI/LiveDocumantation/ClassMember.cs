using System;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.LiveDocumantation
{
    public class ClassMember : MonoBehaviour
    {
        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/ClassMember";

        public string Text { get; set; }
        
        public void Start()
        {
            //       gameObject.AddComponent<CanvasRenderer>();

            //       Image image = gameObject.AddComponent<Image>();
            //      image.sprite = Sprite.Create();
            //     var imageColor = image.color;
            //    imageColor.a = 0.05F;
            //   image.color = imageColor;

            GameObject classMember =
                PrefabInstantiator.InstantiatePrefab("Prefabs/UI/LiveDocumentation/ClassMember", transform, false);

            TextMeshProUGUI tm = classMember.transform.Find("MemberText").gameObject.GetComponent<TextMeshProUGUI>();
            tm.text = Text;

            RectTransform rt = classMember.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            
        }
    }
}