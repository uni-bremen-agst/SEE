using System.Text;
using SEE.DataModel.DG;

namespace SEE.Game.City
{
    /// <summary>
    /// Utility class for building tooltip content based on <see cref="TooltipSettings"/>.
    /// </summary>
    public static class TooltipContentBuilder
    {
        /// <summary>
        /// Common metric names for lines of code.
        /// </summary>
        private static readonly string[] linesOfCodeMetrics =
        {
            "Metric.Lines.LOC",
            "Metric.LOC",
            "LOC",
            "Lines.LOC"
        };

        /// <summary>
        /// String used for leaf nodes.
        /// </summary>
        private const string leafNodeKind = "Leaf";

        /// <summary>
        /// String used for inner nodes.
        /// </summary>
        private const string innerNodeKind = "Inner";

        /// <summary>
        /// Separator used between tooltip entries.
        /// </summary>
        private const string separator = "\n";

        /// <summary>
        /// Builds the tooltip text for a given node based on the provided settings.
        /// </summary>
        /// <param name="node">The node for which to build the tooltip.</param>
        /// <param name="settings">The tooltip settings defining what content to display.</param>
        /// <returns>The formatted tooltip text, or <see cref="Node.Type"/> as fallback.
        /// Returns null only if <paramref name="node"/> or <paramref name="settings"/> is null.</returns>
        public static string BuildTooltip(Node node, TooltipSettings settings)
        {
            if (node == null || settings == null)
            {
                return null;
            }

            // Early exit if nothing is enabled
            if (!settings.HasAnyContentEnabled())
            {
                return node.Type;
            }

            StringBuilder builder = new();
            bool isFirst = true;

            // Add configured properties in a logical order
            AppendName(node, settings, builder, ref isFirst);
            AppendType(node, settings, builder, ref isFirst);
            AppendNodeKind(node, settings, builder, ref isFirst);
            AppendIncomingEdges(node, settings, builder, ref isFirst);
            AppendOutgoingEdges(node, settings, builder, ref isFirst);
            AppendLinesOfCode(node, settings, builder, ref isFirst);

            // Fallback to type if nothing was added
            return builder.Length > 0 ? builder.ToString() : node.Type;
        }

        /// <summary>
        /// Appends a value to the builder with proper separator handling.
        /// </summary>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="label">The label for the value.</param>
        /// <param name="value">The value to append.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void Append(StringBuilder builder, string label, object value, ref bool isFirst)
        {
            if (!isFirst)
            {
                builder.Append(separator);
            }
            builder.Append(label).Append(value);
            isFirst = false;
        }

        /// <summary>
        /// Appends the node's name to the tooltip if <see cref="TooltipSettings.ShowName"/> is enabled.
        /// </summary>
        /// <param name="node">The node to get the name from.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendName(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowName)
            {
                string name = node.SourceName ?? node.ID;
                if (!string.IsNullOrEmpty(name))
                {
                    Append(builder, "", name, ref isFirst);
                }
            }
        }

        /// <summary>
        /// Appends the node's type to the tooltip if <see cref="TooltipSettings.ShowType"/> is enabled.
        /// </summary>
        /// <param name="node">The node to get the type from.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendType(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowType && !string.IsNullOrEmpty(node.Type))
            {
                Append(builder, "Type: ", node.Type, ref isFirst);
            }
        }

        /// <summary>
        /// Appends the node kind (Leaf or Inner) to the tooltip if <see cref="TooltipSettings.ShowNodeKind"/> is enabled.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendNodeKind(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowNodeKind)
            {
                string kind = node.IsLeaf() ? leafNodeKind : innerNodeKind;
                Append(builder, "Kind: ", kind, ref isFirst);
            }
        }

        /// <summary>
        /// Appends the incoming edge count to the tooltip if <see cref="TooltipSettings.ShowIncomingEdges"/> is enabled.
        /// </summary>
        /// <param name="node">The node to get the incoming edge count from.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendIncomingEdges(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowIncomingEdges)
            {
                Append(builder, "Incoming: ", node.Incomings.Count, ref isFirst);
            }
        }

        /// <summary>
        /// Appends the outgoing edge count to the tooltip if <see cref="TooltipSettings.ShowOutgoingEdges"/> is enabled.
        /// </summary>
        /// <param name="node">The node to get the outgoing edge count from.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendOutgoingEdges(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowOutgoingEdges)
            {
                Append(builder, "Outgoing: ", node.Outgoings.Count, ref isFirst);
            }
        }

        /// <summary>
        /// Appends the lines of code metric to the tooltip if <see cref="TooltipSettings.ShowLinesOfCode"/> is enabled.
        /// </summary>
        /// <param name="node">The node to get the LOC metric from.</param>
        /// <param name="settings">The tooltip settings.</param>
        /// <param name="builder">The StringBuilder to append to.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendLinesOfCode(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (settings.ShowLinesOfCode && TryGetLinesOfCode(node, out int loc))
            {
                Append(builder, "LOC: ", loc, ref isFirst);
            }
        }

        /// <summary>
        /// Tries to get the lines of code metric from the node.
        /// </summary>
        /// <param name="node">The node to get the metric from.</param>
        /// <param name="loc">The lines of code value if found.</param>
        /// <returns>True if the metric was found.</returns>
        private static bool TryGetLinesOfCode(Node node, out int loc)
        {
            foreach (string metricName in linesOfCodeMetrics)
            {
                if (node.TryGetInt(metricName, out loc))
                {
                    return true;
                }
                if (node.TryGetFloat(metricName, out float floatLoc))
                {
                    loc = (int)floatLoc;
                    return true;
                }
            }

            loc = 0;
            return false;
        }
    }
}
