using UnityEngine;

namespace SEE.GO.Factories
{
    /// <summary>
    /// Handles the properties of materials for our <see cref="MaterialsFactory.ShaderType.Edge"/>
    /// </summary>
    internal static class EdgeMaterial
    {
        /// <summary>
        /// Name of the shader property to enable the edge flow.
        /// </summary>
        private const string edgeFlowEnabledProperty = "_EdgeFlowEnabled";

        /// <summary>
        /// The id of the shader property for <see cref="edgeFlowEnabledProperty"/> shader property.
        /// </summary>
        private static readonly int edgeFlowEnabledID = Shader.PropertyToID(edgeFlowEnabledProperty);

        /// <summary>
        /// Shader property that defines the (start) color.
        /// </summary>
        private static readonly int colorProperty = Shader.PropertyToID("_Color");

        /// <summary>
        /// Shader property that defines the end color of the color gradient.
        /// </summary>
        private static readonly int endColorProperty = Shader.PropertyToID("_EndColor");

        /// <summary>
        /// Shader property that defines the start of the visible segment.
        /// </summary>
        private static readonly int visibleStartProperty = Shader.PropertyToID("_VisibleStart");

        /// <summary>
        /// Shader property that defines the end of the visible segment.
        /// </summary>
        private static readonly int visibleEndProperty = Shader.PropertyToID("_VisibleEnd");

        /// <summary>
        /// Enables or disables, respectively, the edge-flow animation for the given <paramref name="edgeMaterial"/>.
        ///
        /// The material must have a <see cref="edgeFlowEnabledProperty"/>; otherwise nothing
        /// happens. This is the case for our edge material <see cref="ShaderType.TransparentEdge"/>.
        /// </summary>
        /// <param name="edgeMaterial">The material to be animated.</param>
        /// <param name="animateFlow">Whether to enable the animation of edge flow.</param>
        internal static void SetEdgeFlow(Material edgeMaterial, bool animateFlow)
        {
            if (edgeMaterial != null && edgeMaterial.HasProperty(edgeFlowEnabledID))
            {
                edgeMaterial.SetFloat(edgeFlowEnabledID, animateFlow ? 1 : 0);
            }
        }

        /// <summary>
        /// Sets the start color of <paramref name="material"/> to <see cref="color"/>.
        /// </summary>
        /// <param name="material">Edge material to be set.</param>
        /// <param name="color">Color to be assigned.</param>
        internal static void SetStartColor(Material material, Color color)
        {
            material.SetColor(colorProperty, color);
        }

        /// <summary>
        /// Sets the end color of <paramref name="material"/> to <see cref="color"/>.
        /// </summary>
        /// <param name="material">Edge material to be set.</param>
        /// <param name="color">Color to be assigned.</param>
        internal static void SetEndColor(Material material, Color color)
        {
            material.SetColor(endColorProperty, color);
        }

        /// <summary>
        /// Sets the visible start of <paramref name="material"/> to <paramref name="visibleStart"/>.
        /// </summary>
        /// <param name="material">Edge material to be set.</param>
        /// <param name="visibleStart">Value to be assigned.</param>
        internal static void SetVisibleStart(Material material, float visibleStart)
        {
            material.SetFloat(visibleStartProperty, visibleStart);
        }

        /// <summary>
        /// Sets the visible end of <paramref name="material"/> to <paramref name="visibleEnd"/>.
        /// </summary>
        /// <param name="material">Edge material to be set.</param>
        /// <param name="visibleEnd">Value to be assigned.</param>
        internal static void SetVisibleEnd(Material material, float visibleEnd)
        {
            material.SetFloat(visibleEndProperty, visibleEnd);
        }
    }
}