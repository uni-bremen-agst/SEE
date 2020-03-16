using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    public abstract class ActionMapping
    {
        public abstract enum Inputs { };

        public abstract void SetAction(Inputs input, Event action);
        public abstract Event GetEvent(Inputs input);
        public abstract Event[] GetSet();
    }
}
