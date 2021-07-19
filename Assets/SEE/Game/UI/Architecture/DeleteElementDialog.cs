using Michsky.UI.ModernUIPack;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.Architecture
{
    
    /// <summary>
    /// Component that handles the modal dialog for the delete action approval.
    /// Used by Prefabs/Architecture/UI/DeleteElementDialog
    /// 
    /// </summary>
    public class DeleteElementDialog : MonoBehaviour
    {
        /// <summary>
        /// The ModernUI <see cref="ModalWindowManager"/>. Provides API to interact with the Dialog.
        /// </summary>
        public ModalWindowManager ModalWindowManager;
        /// <summary>
        /// The game object of the graph element this dialog is shown for.
        /// </summary>
        private GameObject graphElement;


        private void Start()
        {
            // Add listener methods
            NodeContextMenuHolder.OnDeleteNodeConfirmation += OnDeleteNodeConfirmation;
            EdgeContextMenuHolder.OnDeleteEdgeConfirmation += OnDeleteEdgeConfirmation;
            ModalWindowManager.confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            ModalWindowManager.cancelButton.onClick.AddListener(OnCancelCommandClicked);
            ModalWindowManager.CloseWindow();
        }

        private void OnDeleteEdgeConfirmation(GameObject graphelement)
        {
            this.graphElement = graphelement;
            PrepareTextContent(graphelement);
            ModalWindowManager.OpenWindow();
        }

        /// <summary>
        /// OnClick listener for the cancel button.
        /// </summary>
        private void OnCancelCommandClicked()
        {
            graphElement = null;
            ModalWindowManager.CloseWindow();
        }
        
        /// <summary>
        /// OnClick listener for the confirm button.
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            SEECityArchitecture city = SceneQueries.FindArchitectureCity();
            if (graphElement.TryGetNode(out Node node))
            { 
                //Deletes the selected node and their children
                Destroyer.DestroyGameObjectWithChildren(graphElement);
                city.LoadedGraph.RemoveNode(node);
                
            }
            else if (graphElement.TryGetEdge(out Edge edge))
            {
                city.LoadedGraph.RemoveEdge(edge);
                Destroyer.DestroyGameObject(graphElement);
            }
            ModalWindowManager.CloseWindow();
        }
        
        /// <summary>
        /// Prepares and opens the dialog.
        /// </summary>
        /// <param name="graphElement"></param>
        private void OnDeleteNodeConfirmation(GameObject graphElement)
        {
            this.graphElement = graphElement;
            PrepareTextContent(graphElement);
            ModalWindowManager.OpenWindow();
        }

        /// <summary>
        /// Construct the dialog text from the <paramref name="graphElement"/> object.
        /// Precondition: The <paramref name="graphElement"/> has a <see cref="NodeRef"/> or <see cref="EdgeRef"/> attached.
        /// </summary>
        /// <param name="graphElement">The game object of the graph element.</param>
        private void PrepareTextContent(GameObject graphElement)
        {
            if (graphElement.TryGetNode(out Node node))
            {
                ModalWindowManager.windowDescription.text =
                    $"Do you really want to delete Node [{node.SourceName}]? All its childs and connected edges will be deleted!";
                ModalWindowManager.windowTitle.text = "DELETE NODE";
                ;
            }
            else if (graphElement.TryGetEdge(out Edge edge))
            {
                ModalWindowManager.windowTitle.text = "DELETE EDGE";
                ModalWindowManager.windowDescription.text =
                    $"Do you really want to delete Edge [{edge.Source.SourceName}-{edge.Target.SourceName}]?";
            }
        }
    }
}