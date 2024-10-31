namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This attract function config is used to initialize the attract function no attract. 
    /// This class does not need to hold any information because no attract always returns 0 attraction.
    /// </summary>
    public class NoAttractConfig : AttractFunctionConfig
    {
        public override AttractFunction.AttractFunctionType AttractFunctionType => AttractFunction.AttractFunctionType.NoAttract;
    }
}
