#if !NCRUNCH

using Dissonance.Audio.Playback;
using Dissonance.Networking;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (DissonanceComms))]
    public class DissonanceCommsEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly TokenControl _tokenEditor = new TokenControl("These access tokens are used by broadcast/receipt triggers to determine if they should function");

        private readonly HashSet<string> _showRoomMembership = new HashSet<string>();
        private readonly HashSet<string> _showSpeakingChannels = new HashSet<string>();

        private SerializedProperty _lastPrefabError;

        #region initialisation
        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public void OnEnable()
        {
            _lastPrefabError = serializedObject.FindProperty("_lastPrefabError");
        }
        #endregion

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            var comm = (DissonanceComms) target;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                CommsNetworkGui();
                DissonanceCommsGui();

                PlaybackPrefabGui(comm);

                comm.ChangeWithUndo(
                    "Changed Dissonance Mute",
                    EditorGUILayout.Toggle("Mute", comm.IsMuted),
                    comm.IsMuted,
                    a => comm.IsMuted = a
                );

                comm.ChangeWithUndo(
                    "Changed Dissonance Deafen",
                    EditorGUILayout.Toggle("Deafen", comm.IsDeafened),
                    comm.IsDeafened,
                    a => comm.IsDeafened = a
                );

                if (Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    StatusGui(comm);
                }

                _tokenEditor.DrawInspectorGui(comm, comm);

                if (GUILayout.Button("Voice Settings"))
                    VoiceSettingsEditor.GoToSettings();

                if (GUILayout.Button("Configure Rooms"))
                    ChatRoomSettingsEditor.GoToSettings();

                if (GUILayout.Button("Diagnostic Settings"))
                    DebugSettingsEditor.GoToSettings();

                Undo.FlushUndoRecordObjects();

                if (changed.changed)
                    EditorUtility.SetDirty(comm);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void PlaybackPrefabGui([NotNull] DissonanceComms comm)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var prefab = EditorGUILayout.ObjectField("Playback Prefab", comm.PlaybackPrefab, typeof(GameObject), false);
                if (!Application.isPlaying)
                {
                    // Check if we're in the special case of setting to nothing, when it's already nothing
                    if (prefab == null && comm.PlaybackPrefab == null)
                    {
                        //Display the last error
                        if (!string.IsNullOrEmpty(_lastPrefabError.stringValue))
                            EditorGUILayout.HelpBox(_lastPrefabError.stringValue, MessageType.Error);

                        return;
                    }

                    GameObject newPrefab = null;
                    if (prefab == null)
                    {
                        //Setting to null, no error involved with this
                        _lastPrefabError.stringValue = null;

                    }
#if UNITY_2018_3_OR_NEWER
                    else if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Regular)
#else
                    else if (PrefabUtility.GetPrefabType(prefab) == PrefabType.Prefab)
#endif
                    {
                        //Check that the prefab is valid
                        newPrefab = (GameObject)prefab;
                        if (newPrefab.GetComponent<VoicePlayback>() == null)
                        {
                            newPrefab = null;
                            _lastPrefabError.stringValue = "Playback Prefab must contain a VoicePlayback component";
                        }
                        else
                            _lastPrefabError.stringValue = null;
                    }
                    else
                    {
                        _lastPrefabError.stringValue = "Playback Prefab type must be user created prefab asset";
                    }

                    if (!string.IsNullOrEmpty(_lastPrefabError.stringValue))
                        EditorGUILayout.HelpBox(_lastPrefabError.stringValue, MessageType.Error);

                    comm.ChangeWithUndo(
                        "Changed Dissonance Playback Prefab",
                        ReferenceEquals(newPrefab, null) ? null : newPrefab.gameObject,
                        comm.PlaybackPrefab,
                        a => comm.PlaybackPrefab = a
                    );
                }
            }
        }

        private void CommsNetworkGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<ICommsNetwork>();
            if (nets == null || nets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Please attach a Comms Network component appropriate to your networking system to the entity.",
                    MessageType.Error
                );
            }
            else if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the ICommsNetwork components attached to this entity.",
                    MessageType.Error
                );
            }
        }

        private void DissonanceCommsGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<DissonanceComms>();
            if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the DissonanceComms components attached to this entity.",
                    MessageType.Error
                );
            }
            else
            {
                var comms = FindObjectsOfType<DissonanceComms>();
                if (comms.Length > 1)
                {
                    EditorGUILayout.HelpBox(
                        string.Format("Found {0} DissonanceComms components in scene, please remove all but one", comms.Length),
                        MessageType.Error
                    );
                }
            }
        }

        private void StatusGui([NotNull] DissonanceComms comm)
        {
            EditorGUILayout.LabelField("Estimated Packet Loss", (comm.PacketLoss).ToString("0%"));

            var count = comm.Players.Count - 1;
            EditorGUILayout.LabelField("Peers: (" + (count == 0 ? "none" : count.ToString()) + ")");

            for (var i = 0; i < comm.Players.Count; i++)
            {
                var p = comm.Players[i];
                PlayerGui(p);
            }
        }

        private void PlayerGui([NotNull] VoicePlayerState p)
        {
            var message = string.Format("{0} {1} {2} {3} {4}",
                p.Name,
                p is LocalVoicePlayerState ? "(local)" : "",
                p.IsSpeaking ? "(speaking)" : "",
                p.Tracker != null && p.Tracker.IsTracking ? "(positional)" : "",
                !p.IsConnected ? "(disconnected)" : ""
            );

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool showListeningRooms;
                bool showSpeakingChannels;

                EditorGUILayout.LabelField(message);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledGroupScope(p.IsLocalPlayer))
                        p.IsLocallyMuted = GUILayout.Toggle(p.IsLocallyMuted, new GUIContent("Mute", "Prevent this player from being heard locally"));

                    showListeningRooms = GUILayout.Toggle(_showRoomMembership.Contains(p.Name), new GUIContent("Show Rooms", "Show the set of rooms this player is listening to"));
                    if (showListeningRooms)
                        _showRoomMembership.Add(p.Name);
                    else
                        _showRoomMembership.Remove(p.Name);

                    showSpeakingChannels = GUILayout.Toggle(_showSpeakingChannels.Contains(p.Name), new GUIContent("Show Channels", "Show the set of channels this player is speaking to the local player through"));
                    if (showSpeakingChannels)
                        _showSpeakingChannels.Add(p.Name);
                    else
                        _showSpeakingChannels.Remove(p.Name);
                }

                if (showListeningRooms)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Listening To:");
                        foreach (var room in p.Rooms)
                            EditorGUILayout.LabelField(" - " + room);
                    }
                }

                if (showSpeakingChannels)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var l = new List<RemoteChannel>();
                        p.GetSpeakingChannels(l);

                        EditorGUILayout.LabelField("Speaking Through:");
                        foreach (var channel in l.OrderByDescending(a => a.Type))
                            EditorGUILayout.LabelField(string.Format(" - {0}: {1}", channel.Type, channel.TargetName));
                    }
                }
            }
        }
    }
}

#endif