using UnityEngine;
using OdinSerializer;

namespace SEE.Controls.Actions
{
    public enum HideModeSelector
    {
        HideAll,
        HideSelected,
        HideUnselected,
        HideIncoming,
        HideOutgoing,
        HideAllEdgesOfSelected,
        HideForwardTransitveClosure,
        HideBackwardTransitiveClosure,
        HideAllTransitiveClosure,
    }
}

