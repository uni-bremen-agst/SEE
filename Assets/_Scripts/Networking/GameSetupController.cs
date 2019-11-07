using Photon.Pun;
using SEE.DataModel;
using SEE.Layout;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE
{

    public class GameSetupController : MonoBehaviour
    {
        void Start()
        {
            PhotonNetwork.Instantiate(Path.Combine("Prefabs", "Player"), Vector3.zero, Quaternion.identity);
            GameObject hands = PhotonNetwork.Instantiate(Path.Combine("Prefabs", "Hand Models"), Vector3.zero, Quaternion.identity);
            hands.transform.parent = GameObject.Find("Leap Rig").transform;
#if true
            GraphSettings gs = new GraphSettings();
            gs.pathPrefix = Application.dataPath.Replace('/', '\\') + '\\';
            Graph g = SceneGraphs.Add(gs);
            List<string> nm = new List<string>() {
                gs.WidthMetric,
                gs.HeightMetric,
                gs.DepthMetric
            };
            ILayout l = new SEE.Layout.BalloonLayout(
                gs.ShowEdges,
                gs.WidthMetric, gs.HeightMetric, gs.DepthMetric,
                gs.IssueMap(),
                gs.InnerNodeMetrics,
                new BuildingFactory(),
                new ZScoreScale(g, gs.MinimalBlockLength, gs.MaximalBlockLength, nm),
                gs.EdgeWidth,
                gs.ShowErosions,
                gs.EdgesAboveBlocks,
                gs.ShowDonuts);
            l.Draw(g);
#endif
        }
    }

}// namespace SEE
