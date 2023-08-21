using SEE.Game.UI.LiveDocumentation.Buffer;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.UI.LiveDocumentation
{
    /// <summary>
    ///     Represents a class member showed in the ListView of an <see cref="LiveDocumentationWindow" />.
    ///     Is used to display documentation of a method (when <see cref="LiveDocumentationWindow.DocumentationWindowType" />
    ///     is set to <see cref="LiveDocumentationWindowType.CLASS" />
    ///     or to display the documentation of a methods parameter when set to
    ///     <see cref="LiveDocumentationWindowType.METHOD" /> .
    /// </summary>
    public class ClassMember : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        ///     Delegate for <see cref="ClassMember.OnLinkClicked" />.
        /// </summary>
        public delegate void ClickLink(string path);

        /// <summary>
        ///     Used to change the color of the text field when the user hovers over.
        /// </summary>
        private Image ClassMemberImage;

        /// <summary>
        ///     The text view showing the documentation of a class member
        /// </summary>
        private TextMeshProUGUI Mesh;

        /// <summary>
        ///     The rect transform of the text view.
        ///     Used to determent if the user has clicked on the text view
        /// </summary>
        private RectTransform TextRectTransform;

        /// <summary>
        ///     The starting line number of the class member
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        ///     The documentation of the class member
        /// </summary>
        public LiveDocumentationBuffer MethodsBuffer { get; set; }

        /// <summary>
        ///     Event which is called, when the user clicked on the class member text view itself
        /// </summary>
        public UnityEvent<ClassMember> OnClicked { get; } = new();

        /// <summary>
        ///     Event which is called, when the user clicked on a link in the text view
        /// </summary>
        public UnityEvent<string> OnLinkClicked { get; } = new();

        /// <summary>
        ///     Start method
        /// </summary>
        public void Start()
        {
            GameObject classMember =
                PrefabInstantiator.InstantiatePrefab(PREFAB_NAME, transform, false);

            Mesh = classMember.transform.Find(CLASS_MEMBER_OBJECT_PATH).gameObject
                .GetComponent<TextMeshProUGUI>();
            Mesh.text = MethodsBuffer.PrintBuffer();
            if (MethodsBuffer is LiveDocumentationClassMemberBuffer classMemberBuffer)
            {
                Mesh.text = Mesh.text + "\n" + classMemberBuffer.Documentation.PrintBuffer();
            }

            TextRectTransform = classMember.transform.Find(ScrollViewPath).gameObject
                .GetComponent<RectTransform>();

            classMember.TryGetComponentOrLog(out ClassMemberImage);
        }

        /// <summary>
        ///     Update method
        /// </summary>
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

        /// <summary>
        ///     Is called when the user hovers over the text field
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            ClassMemberImage.color = HighlightedColor;
        }

        /// <summary>
        ///     Is called when the users mouse cursor left the text field
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            ClassMemberImage.color = UnHighlightedColor;
        }

        #region Constants

        /// <summary>
        ///     The unity path to the ClassMember prefab
        /// </summary>
        private const string PREFAB_NAME = "Prefabs/UI/LiveDocumentation/ClassMember";

        /// <summary>
        ///     The prefab internal path to the text field
        /// </summary>
        private const string CLASS_MEMBER_OBJECT_PATH = "Scroll View/Viewport/Content/MemberText";

        /// <summary>
        ///     The prefab internal path to the scroll view
        /// </summary>
        private const string ScrollViewPath = "Scroll View";

        /// <summary>
        ///     The background color, the class member should have, when hovered
        /// </summary>
        private static readonly Color HighlightedColor = new(1, 0.2862745f, 0.1490196f, 0.509804f);

        /// <summary>
        ///     The normal background color of the class member.
        /// </summary>
        private static readonly Color UnHighlightedColor = new(1, 1, 1, 0.3882353f);

        #endregion
    }
}