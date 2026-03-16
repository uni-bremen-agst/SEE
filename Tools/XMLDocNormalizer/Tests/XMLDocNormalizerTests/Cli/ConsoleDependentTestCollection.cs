namespace XMLDocNormalizerTests.Cli
{
    /// <summary>
    /// Groups tests that depend on global console state and therefore must not run in parallel.
    /// </summary>
    [CollectionDefinition("Console-dependent tests", DisableParallelization = true)]
    public sealed class ConsoleDependentTestCollection
    {
    }
}
