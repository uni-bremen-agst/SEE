namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// Represents a vertical strip of potential maximal empty space.
    /// Invariant: 0 <= X1 < X2
    /// </summary>
    internal class VerticalGap
    {
        public float X1 { get; set; }
        public float X2 { get; set; }
    }
}
