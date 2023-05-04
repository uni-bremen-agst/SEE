using System;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using SEE.Utils;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// Represents a class member FFFFFF0F
    /// </summary>
    public class ClassMember : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Constants

        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/ClassMember";
        private const string CLASS_MEMBER_OBJECT_PATH = "Scroll View/Viewport/Content/MemberText";

        private readonly Color HighlightedColor = new Color(1, 0.2862745f, 0.1490196f, 0.509804f);

        private readonly Color UnHighlightedColor = new Color(1, 1, 1, 0.3882353f);

        #endregion

        public string Text { get; set; }

        public int LineNumber { get; set; }

        public bool HighlightAnimationRunning { get; set; } = false;

        public delegate void ClickLink(string path);

        public delegate void Click(ClassMember cm);

        public event ClickLink OnLinkClicked;
        
        public UnityEvent<ClassMember> OnClicked = new UnityEvent<ClassMember>();


        private TextMeshProUGUI Mesh;

        private RectTransform TextRectTransform;

        private GameObject ClassMemberField;

        private Image ClassMemberImage;

        public void Start()
        {
            GameObject classMember =
                PrefabInstantiator.InstantiatePrefab(PREFAB_NAME, transform, false);

            ClassMemberField = classMember;
            Mesh = classMember.transform.Find(CLASS_MEMBER_OBJECT_PATH).gameObject
                .GetComponent<TextMeshProUGUI>();
            Mesh.text = Text;

            TextRectTransform = classMember.transform.Find(CLASS_MEMBER_OBJECT_PATH).gameObject
                .GetComponent<RectTransform>();

            classMember.TryGetComponentOrLog(out ClassMemberImage);


            //RectTransform rt = classMember.GetComponent<RectTransform>();
            //     rt.anchorMin = new Vector2(0, 0);
            //     rt.anchorMax = new Vector2(1, 1);
            //     rt.pivot = new Vector2(0.5f, 0.5f);
        }
        

        public void Update()
        {
            // When the user has clicked on the class member list item
            if (Input.GetMouseButtonDown(0))
            {
                int linkId = TMP_TextUtilities.FindIntersectingLink(Mesh, Input.mousePosition, null);
                // When a link was found at the current position
                if (linkId != -1)
                {
                    OnLinkClicked.Invoke(Mesh.textInfo.linkInfo[linkId].GetLinkID());
                }

                if (TMP_TextUtilities.IsIntersectingRectTransform(TextRectTransform, Input.mousePosition, null))
                {
                    OnClicked.Invoke(this);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ClassMemberImage.color = HighlightedColor;

        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClassMemberImage.color = UnHighlightedColor;
        }
    }
}