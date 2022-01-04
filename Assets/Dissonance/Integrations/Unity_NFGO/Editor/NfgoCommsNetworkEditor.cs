using UnityEditor;

#if UNITY_EDITOR

    namespace Dissonance.Integrations.Unity_NFGO.Editor
    {
        [CustomEditor(typeof(NfgoCommsNetwork))]
        public class NfgoCommsNetworkEditor
            : Dissonance.Editor.BaseDissonnanceCommsNetworkEditor<
                NfgoCommsNetwork,
                NfgoServer,
                NfgoClient,
                NfgoConn,
                Unit,
                Unit
            >
        {
        }
    }
#endif