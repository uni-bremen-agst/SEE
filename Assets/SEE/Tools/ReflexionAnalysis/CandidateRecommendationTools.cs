using MoreLinq;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public static class CandidateRecommendationTools
    {
        public static List<Edge> GetImplementationEdges(this Node implNode)
        {
            List<Edge> edges = new List<Edge>();
            edges.AddRange(implNode.Incomings);
            edges.AddRange(implNode.Outgoings);
            edges = edges.Distinct().Where(x => x.IsInImplementation()).ToList();
            return edges;
        }

        public static void AddToMappingSilent(this ReflexionGraph reflexionGraph, Node cluster, Node candidate)
        {
            bool suppressNotifications = reflexionGraph.SuppressNotifications;
            reflexionGraph.SuppressNotifications = true;
            reflexionGraph.AddToMapping(candidate, cluster);
            reflexionGraph.SuppressNotifications = suppressNotifications;
        }

        public static void RemoveFromMappingSilent(this ReflexionGraph reflexionGraph, Node candidate)
        {
            bool suppressNotifications = reflexionGraph.SuppressNotifications;
            reflexionGraph.SuppressNotifications = true;
            reflexionGraph.RemoveFromMapping(candidate);
            reflexionGraph.SuppressNotifications = suppressNotifications;
        }

        public static void SelectChilds(this Node node, List<Node> entities, Func<Node,bool> selector)
        {
            if (selector.Invoke(node))
            {
                entities.Add(node);
            }
            foreach (Node child in node.Children())
            {
                SelectChilds(child, entities, selector);
            }
            return;
        }
    }
}