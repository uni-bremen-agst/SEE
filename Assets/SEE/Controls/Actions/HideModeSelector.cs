namespace SEE.Controls.Actions
{
    /// <summary>
    /// Represents the various modes for hiding nodes and edges 
    /// and helps simplifying selection.
    /// </summary>
    public enum HideModeSelector
    {
        Back,
        Confirmed,
        SelectSingle,
        SelectMultiple,
        HideAll,
        HideSelected,
        HideUnselected,
        HideIncoming,
        HideOutgoing,
        HideAllEdgesOfSelected,
        HideForwardTransitiveClosure,
        HideBackwardTransitiveClosure,
        HideAllTransitiveClosure,
        HighlightEdges,
    }
}
