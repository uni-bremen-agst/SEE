using System;
using Dissonance.Networking;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TComms"></typeparam>
    /// <typeparam name="TServer"></typeparam>
    /// <typeparam name="TClient"></typeparam>
    /// <typeparam name="TPeer"></typeparam>
    /// <typeparam name="TClientParam"></typeparam>
    /// <typeparam name="TServerParam"></typeparam>
    public class BaseDissonnanceCommsNetworkEditor<TComms, TServer, TClient, TPeer, TClientParam, TServerParam>
        : UnityEditor.Editor
        where TComms : BaseCommsNetwork<TServer, TClient, TPeer, TClientParam, TServerParam>
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
        where TPeer : struct, IEquatable<TPeer>
    {
        private Texture2D _logo;
        protected GUIStyle ContentOutline { get; private set; }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            if (_logo == null)
            {
                _logo = Resources.Load<Texture2D>("dissonance_logo");

                ContentOutline = new GUIStyle(EditorStyles.helpBox) {
                    padding = new RectOffset(0, 0, 0, 1),
                    margin = new RectOffset(0, 0, 0, 3)
                };
            }

            GUILayout.Label(_logo);

            var network = (TComms)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Network Stats");

                using (new EditorGUI.IndentLevelScope())
                {
                    network.OnInspectorGui();
                }
            }
        }
    }
}
