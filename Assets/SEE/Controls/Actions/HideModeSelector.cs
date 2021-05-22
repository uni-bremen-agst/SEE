namespace SEE.Controls.Actions
{
    /// <summary>
    /// Represent the various HideModes and serves to simplify selection
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
