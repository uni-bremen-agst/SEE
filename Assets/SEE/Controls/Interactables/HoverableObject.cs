using SEE.DataModel;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a game object that is hovered over.
    /// </summary>
    public class HoverableObject : HighlightableObject
    {
        protected override void Awake()
        {
            base.Awake();
            if (textOnPaperPrefab == null)
            {
                // Filename of the prefab for the text on paper excluding its file extension .prefab
                string path = "Prefabs/TextOnPaper";
                textOnPaperPrefab = Resources.Load<GameObject>(path);
                if (textOnPaperPrefab == null)
                {
                    Debug.LogErrorFormat("Prefab {0} not found.\n", path);
                }
            }
        }

        //------------------------------------------
        // Public actions when the object is hovered.
        //-------------------------------------------

        public override void Hovered(bool isOwner)
        {
            base.Hovered(isOwner);
            ShowInformation();
        }

        public override void Unhovered()
        {
            HideInformation();
            base.Unhovered();
        }

        //-------------------------------------------------
        // Showing additional information when hovered over
        //-------------------------------------------------

        /// <summary>
        /// The local position of the textFieldObject relative to gameObject.
        /// </summary>
        private readonly Vector3 localPositionOfTextFieldObject = new Vector3(0.0f, 1.025f, 0.0f);

        /// <summary>
        /// A child object of gameObject to show textual information when the object
        /// is being hovered over. It is an instantiation of the textOnPaperPrefab.
        /// It will be activated during hovering and deactived when hovering ends.
        /// </summary>
        public GameObject textOnPaper = null;

       /// GameObject textOnPaperInteractable = null;

        /// <summary>
        /// The prefab used to instantiate text-on-paper objects. It is the same for every one,
        /// hence, the attribute is declared static.
        /// </summary>
        private static GameObject textOnPaperPrefab;

        /// <summary>
        /// Shows information about the currently grabbed object using textOnPaper.
        /// If the textOnPaper does not exist, it will be instantiated from textOnPaperPrefab
        /// first. If it already existed, it will just be activated again.
        /// </summary>
        public virtual void ShowInformation()
        {
            if (textOnPaper == null)
            {
                textOnPaper = Instantiate(textOnPaperPrefab, Vector3.zero, Quaternion.identity);
                textOnPaper.name = "Hovering Text Field";
                textOnPaper.GetComponent<TextGUIAndPaperResizer>().Text = GetAttributes(graphNode);
                // We do not want textOnPaper to collide with the selection raycast.
                textOnPaper.layer = Physics.IgnoreRaycastLayer;

                // Now textOnPaper has been re-sized properly; so we can derive its absolute height.
                float paperHeight = TextGUIAndPaperResizer.Height(textOnPaper);

                // We want to put the label above the roof of the gameObject. The gameObject,
                // however, could be composed of multiple child objects of different height
                // (e.g., an inner node which typically has a very low height because it is
                // visualized as an area, but the area contains many child objects).
                // That is why we gather the roof of the complete object hierarchy rooted
                // by gameObject.
                Vector3 position = transform.position; // absolute world co-ordinates of center
                position.y = BoundingBox.GetRoof(GameObjectHierarchy.Descendants(gameObject, Tags.Node)) + paperHeight / 2.0f;
                textOnPaper.transform.position = position;

                textOnPaper.transform.SetParent(gameObject.transform);
            }
            textOnPaper.SetActive(true);
        }

        /// <summary>
        /// Deactivates textOnPaper.
        /// </summary>
        public virtual void HideInformation()
        {
            if (textOnPaper)
            {
                textOnPaper.SetActive(false);
            }
            // We keep textOnPaper for later use. Chances are that an object is hovered
            // over again when it was hovered once.
        }

        //-----------------
        // Helper functions
        //-----------------

        /// <summary>
        /// Retrieves the attributes and their values of <paramref name="graphNode"/>.
        /// </summary>
        /// <param name="graphNode">graph node whose attributes are requested</param>
        /// <returns>attribute names and values</returns>
        private string GetAttributes(Node graphNode)
        {
            if (graphNode == null)
            {
                return gameObject.name;
            }
            return graphNode.ID;
        }
    }
}