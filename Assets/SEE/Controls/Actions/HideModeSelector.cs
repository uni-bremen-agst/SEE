using UnityEngine;
using OdinSerializer;

namespace SEE.Controls.Actions
{
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

