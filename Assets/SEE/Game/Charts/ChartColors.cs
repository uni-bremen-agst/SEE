using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartColors : MonoBehaviour
{
    /// <summary>
    /// The color that the Labels in the scrollview will have for new nodes
    /// </summary>
    public Color newNodesScrollviewLabelColor;

    /// <summary>
    /// The color that the Labels in the scrollview will have for changed nodes
    /// </summary>
    public Color changedNodesScrollviewLabelColor;

    /// <summary>
    /// The color that the Labels in the scrollview will have for deleted nodes
    /// </summary>
    public Color removedNodesScrollviewLabelColor;

    /// <summary>
    /// The color the Labels in the scrollview will have when hovering aboce them
    /// </summary>
    public Color hoveringScrollviewLabelColor;
}
