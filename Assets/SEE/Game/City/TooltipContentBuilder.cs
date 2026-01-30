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
        /// Builds the tooltip text for a given node based on the provided settings.
        /// </summary>
        /// <param name="node">The node for which to build the tooltip.</param>
        /// <param name="settings">The tooltip settings defining what content to display.</param>
        /// <returns>The formatted tooltip text, or null if no content is configured.</returns>
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
        /// <param name="format">The format string with {0} placeholder.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="separator">The separator to use between items.</param>
        /// <param name="isFirst">Reference to flag indicating if this is the first item.</param>
        private static void AppendFormatted(StringBuilder builder, string format, object value, string separator, ref bool isFirst)
        {
            if (!isFirst)
            {
                builder.Append(separator);
            }
            builder.AppendFormat(format, value);
            isFirst = false;
        }

        private static void AppendName(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowName)
            {
                return;
            }

            string name = node.SourceName ?? node.ID;
            if (!string.IsNullOrEmpty(name))
            {
                AppendFormatted(builder, settings.NameFormat, name, settings.Separator, ref isFirst);
            }
        }

        private static void AppendType(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowType)
            {
                return;
            }

            if (!string.IsNullOrEmpty(node.Type))
            {
                AppendFormatted(builder, settings.TypeFormat, node.Type, settings.Separator, ref isFirst);
            }
        }

        private static void AppendNodeKind(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowNodeKind)
            {
                return;
            }

            string kind = node.IsLeaf() ? leafNodeKind : innerNodeKind;
            AppendFormatted(builder, settings.NodeKindFormat, kind, settings.Separator, ref isFirst);
        }

        private static void AppendIncomingEdges(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowIncomingEdges)
            {
                return;
            }

            AppendFormatted(builder, settings.IncomingEdgesFormat, node.Incomings.Count, settings.Separator, ref isFirst);
        }

        private static void AppendOutgoingEdges(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowOutgoingEdges)
            {
                return;
            }

            AppendFormatted(builder, settings.OutgoingEdgesFormat, node.Outgoings.Count, settings.Separator, ref isFirst);
        }

        private static void AppendLinesOfCode(Node node, TooltipSettings settings, StringBuilder builder, ref bool isFirst)
        {
            if (!settings.ShowLinesOfCode)
            {
                return;
            }

            if (TryGetLinesOfCode(node, out int loc))
            {
                AppendFormatted(builder, settings.LinesOfCodeFormat, loc, settings.Separator, ref isFirst);
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
