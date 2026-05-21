using TMPro;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Scales the text area and the paper background so that a given text fits into.
    /// We assume this component is attached to a game object that contains two more
    /// child game objects: one named "Text" having a TextMeshPro component
    /// used to show the text and one named "Paper" which provides the background
    /// of the text to be shown.
    ///
    /// This scaling works both in the editor mode and during the game.
    /// </summary>
    [ExecuteInEditMode]
    public class TextGUIAndPaperResizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Text to be shown")]
        private string text = "";

        /// <summary>
        /// The text to be shown. When new text is assigned, the object resizes
        /// so that the text fits.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                text = value;
                Resize();
            }
        }

        [SerializeField, Tooltip("Text margin")]
        public Vector2 Margin = new Vector2(1, 1); // default margin

        [Tooltip("Scale of the font")]
        public float FontScale = 1.0f;

        private const float distanceBetweenPaperAndText = 0.0001f;

        /// <summary>
        /// Scaling factor for line width of 89 chars on a 1 m line (without margins).
        /// </summary>
        private const float fontScaleFactor = 0.00458f;

        /// <summary>
        /// The name of the game object representing the background paper (paperObject).
        /// </summary>
        private const string paperName = "Paper";
        /// <summary>
        /// The child game object used as a background for the text to be shown.
        /// </summary>
        protected GameObject PaperObject = null;
        /// <summary>
        /// The bounds of the mesh of paperObject. It is used to scale the background.
        /// </summary>
        protected Bounds PaperObjectMeshBounds = new Bounds();

        /// <summary>
        /// The name of the game object representing the text (textObject).
        /// </summary>
        private const string textName = "Text";
        /// <summary>
        /// The child game object containing a TextMeshPro component for showing the text.
        /// </summary>
        protected GameObject TextObject = null;
        /// <summary>
        /// The TextMeshPro component contained in textObject, which is used to show the text.
        /// </summary>
        protected TMP_Text TextComponent = null;

        /// <summary>
        /// Sets paperObject, paperObjectMeshBounds, textObject, and textComponent.
        /// textComponent is set to resize to its content automatically.
        /// </summary>
        private void OnEnable()
        {
            PaperObject = transform.Find(paperName).gameObject;
            if (PaperObject == null)
            {
                Debug.LogErrorFormat("Game object {0} does not have a child named {1}.\n", gameObject.name, paperName);
            }
            Mesh paperObjectMesh = PaperObject.GetComponent<MeshFilter>().sharedMesh;
            PaperObjectMeshBounds = paperObjectMesh.bounds;
            TextObject = transform.Find(textName).gameObject;
            if (TextObject == null)
            {
                Debug.LogErrorFormat("Game object {0} does not have a child named {1}.\n", gameObject.name, textName);
            }
            TextComponent = TextObject.GetComponent<TMP_Text>();
            if (TextComponent == null)
            {
                Debug.LogErrorFormat("The child {0} of game object {0} does not have a TextMeshPro component.\n", textName, gameObject.name);
            }
            TextComponent.autoSizeTextContainer = true;
            Resize();
        }

        /// <summary>
        /// This method will be called in the editor mode by TextGUIAndPaperResizerEditor when
        /// the user enters a text. Then the textComponent, paperObject, and textObject
        /// are adjusted so that the text fits.
        /// </summary>
        public void OnGuiChangedHandler()
        {
            Resize();
        }

        /// <summary>
        /// Resizes textComponent, paperObject, and textObject so that the text fits.
        /// </summary>
        private void Resize()
        {
            TextComponent.margin = new Vector4(Margin[0], Margin[1], Margin[0], Margin[1]);
            TextComponent.SetText(text, true);
            TextComponent.transform.localScale = new Vector3(FontScale * fontScaleFactor, FontScale * fontScaleFactor, 1);
            TextComponent.ComputeMarginSize();
            TextComponent.ClearMesh();

            float overallScale = FontScale * fontScaleFactor;

            // Set preferredWidth and preferredHeight including margins.
            // x = paper width, y = paper depth, z = paper height
            Vector3 newPaperScale = new Vector3(TextComponent.preferredWidth * overallScale / PaperObjectMeshBounds.size.x,
                                                0.000001f,
                                                TextComponent.preferredHeight * overallScale / PaperObjectMeshBounds.size.z);
            PaperObject.transform.localScale = newPaperScale;

            // set new rect width and height for fitting together with paper
            TextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(TextComponent.preferredWidth, TextComponent.preferredHeight);
        }

        /// <summary>
        /// Returns the height (y axis) of the given <paramref name="textOnPaper"/>.
        ///
        /// Precondition: <paramref name="textOnPaper"/> must meet the assumption
        /// described above: it must have a child named "Paper" with a MeshRenderer
        /// from which the height can be derived.
        /// </summary>
        /// <param name="textOnPaper">Object whose height is requested.</param>
        /// <returns>Height.</returns>
        public static float Height(GameObject textOnPaper)
        {
            GameObject paper = textOnPaper.transform.Find(paperName).gameObject;
            MeshRenderer paperRenderer = paper.GetComponent<MeshRenderer>();
            return paperRenderer != null ? paperRenderer.bounds.size.y : 0.0f;
        }
    }
}