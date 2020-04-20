using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Devices
{
    public class NullSelection : Selection
    {

        public override Vector3 Value
        {
            get => Vector3.zero;
        }

        public override bool Activated
        {
            get => false;
        }
    }
}