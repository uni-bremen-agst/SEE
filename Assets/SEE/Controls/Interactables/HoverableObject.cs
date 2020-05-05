using SEE.DataModel;
using SEE.GO;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a game object that is hovered over.
    /// </summary>
    public class HoverableObject : InteractableObject
    {
        protected override void Start()
        {
            base.Start();
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

        public void OnHoverBegin()
        {
            //Debug.LogFormat("OnHoverBegin({0})\n", graphNode.ID);
            ShowInformation();
            HighlightMaterial();
        }

        public void OnHoverEnd()
        {
            //Debug.LogFormat("OnHoverEnd({0})\n", graphNode.ID);
            HideInformation();
            ResetMaterial();
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        //----------------------------------------------------------------

        /// <summary>
        /// Called by the Hand when that Hand starts hovering over this object.
        /// 
        /// Activates the source name and detail text and highlights the object by
        /// material with a different color.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverBegin(Hand hand)
        {
            //Debug.Log("OnHandHoverEnd");
            OnHoverBegin();
            //hand.ShowGrabHint();
        }

        /// <summary>
        /// Called by the Hand when that Hand stops hovering over this object
        /// 
        /// Deactivates the source name and detail text and restores the original material.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverEnd(Hand hand)
        {
            //Debug.Log("OnHandHoverEnd");
            OnHoverEnd();
            //hand.HideGrabHint();
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
        GameObject textOnPaper = null;

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
        private void ShowInformation()
        {
            if (textOnPaper == null)
            {
                Debug.LogFormat("New text on paper for {0}\n", gameObject.name);
                textOnPaper = Instantiate(textOnPaperPrefab, Vector3.zero, Quaternion.identity);
                textOnPaper.name = "Hovering Text Field";
                textOnPaper.GetComponent<TextGUIAndPaperResizer>().Text = GetAttributes(graphNode);

                // Now textOnPaper has been re-sized properly; so we can derive its absolute height.
                float paperHeight = TextGUIAndPaperResizer.Height(textOnPaper);

                // Absolute height of the gameObject.
                Renderer renderer = GetComponent<Renderer>();
                float gameObjectHeight = renderer != null ? renderer.bounds.size.y : 0;

                //// We put the textOnPaper above the center of roof the gameObject.
                Vector3 position = transform.position; // absolute world co-ordinates of center
                position.y += (gameObjectHeight + paperHeight) / 2.0f;
                textOnPaper.transform.position = position;

                textOnPaper.transform.SetParent(gameObject.transform);

                // Note: Here is alternative way to put textOnPaper above the roof of gameObject
                // using co-ordinates relative to textOnPaper. This works but textOnPaper becomes 
                // simply too small.
                //   textOnPaper.transform.SetParent(gameObject.transform, false);
                //   Vector3 localPosition = Vector3.zero;
                //   localPosition.y = 0.5f + textOnPaper.transform.localScale.y / 2.0f;
                //   textOnPaper.transform.localPosition = localPosition;
            }
            else
            {
                Debug.LogFormat("Re-using text on paper for {0}\n", gameObject.name);
            }
            textOnPaper.SetActive(true);
        }

        /// <summary>
        /// Deactivates textOnPaper.
        /// </summary>
        private void HideInformation()
        {
            textOnPaper.SetActive(false);
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
            //string result = "";
            //result += "ID" + graphNode.ID + "\n";
            //result += "Type" + graphNode.Type + "\n";

            //foreach (var entry in graphNode.StringAttributes)
            //{
            //    result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            //}
            //foreach (var entry in graphNode.FloatAttributes)
            //{
            //    result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            //}
            //foreach (var entry in graphNode.IntAttributes)
            //{
            //    result += string.Format("{0}: {1}\n", entry.Key, entry.Value);
            //}
            //foreach (var entry in graphNode.ToggleAttributes)
            //{
            //    result += entry + "\n";
            //}
            //return result;
        }

        // -------------------------------
        // Highlighting the hovered object
        // -------------------------------

        /// <summary>
        /// The material before the object was hovered so that it can be restored
        /// when the object is no longer hovered. While hovering, a highlighting
        /// material will be used.
        /// </summary>
        private Material oldMaterial;

        /// <summary>
        /// Highlights the hovered gameNode by assigning a particular highlight material.
        /// Stores the original material in oldMaterial.
        /// </summary>
        private void HighlightMaterial()
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            oldMaterial = renderer.sharedMaterial;
            renderer.sharedMaterial = Materials.HighlightMaterial();
        }

        /// <summary>
        /// Resets the original material of gameNode using the material stored in oldMaterial.
        /// </summary>
        private void ResetMaterial()
        {
            gameObject.GetComponent<Renderer>().sharedMaterial = oldMaterial;
        }
    }
}