using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NoAttractConfig : AttractFunctionConfig
    {
        public override AttractFunction.AttractFunctionType AttractFunctionType => AttractFunction.AttractFunctionType.NoAttract;
    }
}
