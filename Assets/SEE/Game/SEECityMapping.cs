using SEE.DataModel.DG;
using UnityEngine.Assertions;

namespace SEE.Game
{
    public class SEECityMapping : AbstractSEECity
    {
        public Graph mappingGraph;

        public void ReDrawGraph()
        {
            Assert.IsNotNull(mappingGraph);

            DeleteGraphGameObjects(); // TODO: is pooling possible here?
            DrawGraph();
        }

        public void DrawGraph()
        {
            Assert.IsNotNull(mappingGraph);

            GraphRenderer graphRenderer = new GraphRenderer(this, mappingGraph);
            graphRenderer.Draw(gameObject);
        }
    }
}
