using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SEE.Controls.Devices
{
    public class NullBoost : Boost
    {
        public override float Value
        {
            get => 0;
        }
    }
}
