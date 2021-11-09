using SEE.DataModel.DG;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// 
    /// FIXME: Should this class rather derive from <see cref="SEECity"/>?
    /// </summary>
    public class SEECityMapping : AbstractSEECity
    {
        /// <summary>
        /// The mapping of implementation nodes onto architecture nodes.
        /// </summary>
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

            new GraphRenderer(this, mappingGraph).DrawGraph(gameObject);
        }

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            // If any attribute is added to this class that should be contained in the
            // configuration file, then do not forget to add the necessary
            // statements here.
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            // If any attribute is added to this class that should be restored from the
            // configuration file, then do not forget to add the necessary
            // statements here.
        }
    }
}
