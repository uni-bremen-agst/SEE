using System;
using System.Collections.Generic;
using SEE.Game.City;
using SEE.Layout;

namespace SEE.Net
{
    [Serializable]
    public class SeeCitySnapshot
    {
        public CityTypes CityType { get; set; }

        public IEnumerable<SnapshotNode> Nodes { get; set; }

        public IEnumerable<SnapshotEdge> Edges { get; set; }
    }
}
