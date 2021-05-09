using UnityEngine;
using OdinSerializer;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Represent the various HideModes and serve to simplify selection
    /// </summary>
    public enum HideModeSelector
    {
        Select,
        SelectSingleHide,
        SelectMultipleHide,
        SelectHighlight,
        HideAll,
        HideSelected,
        HideUnselected,
        HideIncoming,
        HideOutgoing,
        HideAllEdgesOfSelected,
        HideForwardTransitveClosure,
        HideBackwardTransitiveClosure,
        HideAllTransitiveClosure,
        HighlightEdges,
    }
}

