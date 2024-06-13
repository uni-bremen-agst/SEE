using SEE.Net.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window
{
    public class NoteNetAction : AbstractNetAction
    {
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {

            }
            //throw new NotImplementedException();
        }

        protected override void ExecuteOnServer()
        {
            //Intentionally left blank
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
