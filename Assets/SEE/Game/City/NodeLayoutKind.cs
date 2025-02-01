namespace SEE.Game.City
{
    /// <summary>
    /// The kinds of node layouts available.
    /// </summary>
    public enum NodeLayoutKind : byte
    {
        EvoStreets,
        Balloon,
        RectanglePacking,
        Treemap,
        CirclePacking,
        Reflexion,
        Manhattan,
        // CompoundSpringEmbedder,
        IncrementalTreeMap,
        FromFile
    }
}
