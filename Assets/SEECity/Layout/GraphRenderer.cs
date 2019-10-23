using System;
using System.Collections.Generic;
using SEE.DataModel;

namespace SEE.Layout
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public class GraphRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph">the graph to be drawn</param>
        /// <param name="settings">the settings for the visualization</param>
        public GraphRenderer(GraphSettings settings)
        {
            this.settings = settings;
            if (this.settings.CScapeBuildings)
            {
                blockFactory = new BuildingFactory();
            }
            else
            {
                blockFactory = new CubeFactory();
            }
        }

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        private readonly GraphSettings settings;

        /// <summary>
        /// The factory used to create blocks.
        /// </summary>
        private readonly BlockFactory blockFactory;

        /// <summary>
        /// Draws the graph.
        /// </summary>
        public void Draw(Graph graph)
        {  
            IScale scaler;
            {
                List<string> nodeMetrics = new List<string>() { settings.WidthMetric, settings.HeightMetric, settings.DepthMetric };
                nodeMetrics.AddRange(settings.IssueMap().Keys);
                if (settings.ZScoreScale)
                {
                    scaler = new ZScoreScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
                }
                else
                {
                    scaler = new LinearScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
                }
            }

            ILayout layout;

            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Balloon:
                    {
                        layout = new SEE.Layout.BalloonLayout(settings.ShowEdges,
                                                      settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                      settings.IssueMap(),
                                                      settings.InnerNodeMetrics,
                                                      blockFactory,
                                                      scaler,
                                                      settings.EdgeWidth,
                                                      settings.ShowErosions,
                                                      settings.EdgesAboveBlocks,
                                                      settings.ShowDonuts);
                        break;
                    }
                case GraphSettings.NodeLayouts.Manhattan:
                    {
                        layout = new SEE.Layout.ManhattenLayout(settings.ShowEdges,
                                                        settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                        settings.IssueMap(),
                                                        blockFactory,
                                                        scaler,
                                                        settings.EdgeWidth,
                                                        settings.ShowErosions,
                                                        settings.EdgesAboveBlocks);
                        break;
                    }
                case GraphSettings.NodeLayouts.CirclePacking:
                    {
                        layout = new SEE.Layout.CirclePackingLayout(settings.ShowEdges,
                                                      settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                      settings.IssueMap(),
                                                      settings.InnerNodeMetrics,
                                                      blockFactory,
                                                      scaler,
                                                      settings.EdgeWidth,
                                                      settings.ShowErosions,
                                                      settings.EdgesAboveBlocks,
                                                      settings.ShowDonuts);
                        break;
                    }
                default:
                    throw new Exception("Unhandled layout " + settings.NodeLayout.ToString());
            }
            layout.Draw(graph);
        }

        /// <summary>
        /// Returns the unit of the world helpful for scaling. This unit depends upon the
        /// kind of blocks we are using to represent nodes.
        /// </summary>
        /// <returns>unit of the world</returns>
        public float Unit()
        {
            return blockFactory.Unit();
        }
    }
}
