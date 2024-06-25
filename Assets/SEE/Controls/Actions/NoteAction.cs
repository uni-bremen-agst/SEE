using SEE.UI.Window.NoteWindow;
using SEE.Utils.History;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    internal class NoteAction : AbstractPlayerAction
    {
        private NoteManager noteManager;
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

        public override void Start()
        {
            noteManager = GameObject.Find("NoteManager").GetComponent<NoteManager>();
            OpenDialog();
        }


        public override bool Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                HideAllNotes();
            }
            return true;
        }

        private void HideAllNotes()
        {
            foreach (GameObject gameObject in noteManager.objectList)
            {
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                Material[] gameObjects = new Material[meshRenderer.materials.Length - 1];
                for (int i = 0; i < meshRenderer.materials.Length - 1; i++)
                {
                    gameObjects[i] = meshRenderer.materials[i];
                }
            }
        }

        private void OpenDialog()
        {

        }

        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
