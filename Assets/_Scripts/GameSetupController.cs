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
            InitializePlayer();

            // TODO cities must be able to be generated outside of editor
#if true
            GraphSettings gs = new GraphSettings();
            gs.pathPrefix = Application.dataPath.Replace('/', '\\') + '\\';
            Graph g = SceneGraphs.Add(gs);
            if (g != null) // TODO fix .Add function
            {
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
            }
#endif
            MenuBackdropGenerator mbg = GameObject.FindObjectOfType<MenuBackdropGenerator>();
            mbg.Initialize();

            SearchMenu sm = GameObject.FindObjectOfType<SearchMenu>();
            sm.Initialize();
            IngameMenu im = GameObject.FindObjectOfType<IngameMenu>();
            im.Initialize();

            GameStateController gsc = GameObject.FindObjectOfType<GameStateController>();
            gsc.Initialize();
        }

        private void InitializePlayer()
        {
            GameObject player = PhotonNetwork.Instantiate(Path.Combine("Prefabs", "Player"), Vector3.zero, Quaternion.identity);
            GameObject playerHead = PhotonNetwork.Instantiate(Path.Combine("Prefabs", "PlayerHead"), Vector3.zero, Quaternion.identity);
            GameObject staticPlayerHead = PlayerData.playerHead;

            playerHead.transform.parent = player.transform;
            PhotonView.Get(playerHead).RPC("InitializeMaterial", RpcTarget.All);
            PhotonView.Get(playerHead).RPC("SetTextureScaleX", RpcTarget.All, staticPlayerHead.GetComponentInChildren<MeshRenderer>().material.mainTextureScale.x);
            PhotonView.Get(playerHead).RPC("SetTextureScaleY", RpcTarget.All, staticPlayerHead.GetComponentInChildren<MeshRenderer>().material.mainTextureScale.y);
            PhotonView.Get(playerHead).RPC("SetTextureOffsetX", RpcTarget.All, staticPlayerHead.GetComponentInChildren<MeshRenderer>().material.mainTextureOffset.x);
            PhotonView.Get(playerHead).RPC("SetTextureOffsetY", RpcTarget.All, staticPlayerHead.GetComponentInChildren<MeshRenderer>().material.mainTextureOffset.y);
        }
    }

}
