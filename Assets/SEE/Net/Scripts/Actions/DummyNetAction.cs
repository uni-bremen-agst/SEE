using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;
using SEE.Net;
using SEE.Controls.Actions;

/// <summary>
/// Creates a new edge through the network on each client.
/// </summary>
public class DummyNetAction : AbstractAction
    {
        string action;
        string gameObjectID;
        float posx;
        float posy;
        float posz;

        public DummyNetAction(string action, string gameObjectID, float posx, float posy, float posz)
        {
            this.action = action;
            this.gameObjectID = gameObjectID;
            this.posx = posx;
            this.posy = posy;
            this.posz = posz;

        }

        /// <summary>
        /// Stuff to execute on the Server. Nothing to be done here.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Creates the new edge on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (action == "create")
                {

                    DummyAction dummy = new DummyAction();
                    dummy.CreateObjectAt(new Vector3(posx, posy, posz));

                }
                else if (action == "undo")
                {

                }
                else if (action == "redo")
                {

                }
            }
        }
    }
