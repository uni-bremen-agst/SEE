using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.NoteWindow;
using SEE.Utils;
using SEE.Utils.History;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Manages actions related to notes, including highlighting and hiding nodes and edges with notes.
    /// </summary>
    internal class NoteAction : AbstractPlayerAction
    {
        /// <summary>
        /// Instance of the <see cref="NoteManager"/> class.
        /// </summary>
        private NoteManager noteManager;

        /// <summary>
        /// Window for handling note buttons.
        /// </summary>
        private NoteButtonWindow noteButtonWindow;

        /// <summary>
        /// A set of IDs representing all game objects that have been changed by this action.
        /// </summary>
        private HashSet<string> idHashSet;

        /// <summary>
        /// The material to outline the nodes and edges.
        /// </summary>
        Material noteMaterial = Resources.Load<Material>("Materials/Outliner_MAT");

        /// <summary>
        /// The material to outline the nodes and edges.
        /// </summary>
        Material noteMaterialEdge = Resources.Load<Material>("Materials/OutlinerEdge_MAT");

        /// <summary>
        /// Returns a new instance of <see cref="NoteAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="NoteAction"/></returns>
        internal static IReversibleAction CreateReversibleAction() => new NoteAction();

        /// <summary>
        /// Returns a new instance of <see cref="NoteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance() => new NoteAction();

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Notes"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Notes;
        }

        /// <summary>
        /// Initializes the note action, setting up necessary references.
        /// </summary>
        public override void Start()
        {
            noteManager = NoteManager.Instance;
        }

        /// <summary>
        /// Cleans up when the note action is stopped.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            noteButtonWindow.DestroyWindow();
        }

        /// <summary>
        /// Updates the state of the note action, handling user inputs for hiding and highlighting notes.
        /// </summary>
        /// <returns>True if an action was performed, otherwise false.</returns>
        public override bool Update()
        {
            //Hide all Outlines when middle Mousebutton is pressed
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                HideAllNotes();
                return true;
            }
            //Hide or Show highlightes Nodes/Edges one by one
            if (Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI())
            {
                HitGraphElement hitElement = Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef _);
                if (hitElement == HitGraphElement.Node || hitElement == HitGraphElement.Edge)
                {
                    Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef graphElementRef);
                    GameObject elemGameObject = graphElementRef.gameObject;
                    if (noteManager.objectList.Contains(elemGameObject))
                    {
                        HideOrHightlight(elemGameObject);
                        return true;
                    }
                }
             }
             return false;
        }

        /// <summary>
        /// Hide or highlights every Node and Edge with a note.
        /// </summary>
        private void HideAllNotes()
        {
            foreach (GameObject gameObject in noteManager.objectList)
            {
                HideOrHightlight(gameObject);
            }
        }

        /// <summary>
        /// Hides or highlights the GameObject depending if they have a note.
        /// It hides them if they have an outline active.
        /// It highlights the GameObject if they have a note but no outline active.
        /// </summary>
        /// <param name="gameObject">The game object to toggle the outline for.</param>
        private void HideOrHightlight(GameObject gameObject)
        {
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            idHashSet.Add(gameObject.ID());
            if (gameObject.IsNode())
            {
                ToggleOutline(meshRenderer, noteMaterial);
            }
            else
            {
                ToggleOutline(meshRenderer, noteMaterialEdge);
            }
        }

        /// <summary>
        /// Toggles the outline material for the specified mesh renderer.
        /// </summary>
        /// <param name="meshRenderer">The mesh renderer to toggle the outline for.</param>
        /// <param name="outlineMaterial">The outline material to use.</param>
        private void ToggleOutline(MeshRenderer meshRenderer, Material outlineMaterial)
        {
            string oldMaterialName = meshRenderer.materials[meshRenderer.materials.Length - 1].name;
            string newName = oldMaterialName.Replace(" (Instance)", "");

            if (newName != outlineMaterial.name)
            {
                Material[] materialsArray = new Material[meshRenderer.materials.Length + 1];
                Array.Copy(meshRenderer.materials, materialsArray, meshRenderer.materials.Length);
                materialsArray[meshRenderer.materials.Length] = outlineMaterial;
                meshRenderer.materials = materialsArray;
            }
            else
            {
                Material[] materialsArray = new Material[meshRenderer.materials.Length - 1];
                Array.Copy(meshRenderer.materials, materialsArray, meshRenderer.materials.Length - 1);
                meshRenderer.materials = materialsArray;
            }
        }

        /// <summary>
        /// Returns the set of IDs of all game objects changed by this action.
        /// <see cref="IReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>Returns the ID of the gameObject that either has an outline or has its outline removed.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return idHashSet;
        }
    }
}
