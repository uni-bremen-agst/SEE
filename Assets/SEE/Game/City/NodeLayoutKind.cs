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
        IncrementalRectanglePacking,
        Treemap,
        CirclePacking,
        IncrementalCirclePacking,
        Reflexion,
        IncrementalTreeMap,
        FromFile
    }
}