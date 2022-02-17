#if !NCRUNCH
using System;
using System.Collections.Generic;
using System.Linq;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceBroadcastTrigger))]
    public class VoiceBroadcastTriggerEditor
        : UnityEditor.Editor
    {
        private Texture2D _logo;
        private ChatRoomSettings _roomSettings;

        private readonly TokenControl _tokenEditor = new TokenControl("This broadcast trigger will only send voice if the local player has at least one of these access tokens", false);

        private SerializedProperty _channelTypeExpanded;
        private SerializedProperty _metadataExpanded;
        private SerializedProperty _activationModeExpanded;
        private SerializedProperty _tokensExpanded;
        private SerializedProperty _ampExpanded;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
            _roomSettings = ChatRoomSettings.Load();
        }

        private void OnEnable()
        {
            _channelTypeExpanded = serializedObject.FindProperty("_channelTypeExpanded");
            _metadataExpanded = serializedObject.FindProperty("_metadataExpanded");
            _activationModeExpanded = serializedObject.FindProperty("_activationModeExpanded");
            _tokensExpanded = serializedObject.FindProperty("_tokensExpanded");
            _ampExpanded = serializedObject.FindProperty("_ampExpanded");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                var transmitter = (VoiceBroadcastTrigger)target;

                FoldoutBoxGroup(_channelTypeExpanded, "Channel Type", ChannelTypeGui, transmitter);
                FoldoutBoxGroup(_metadataExpanded, "Channel Metadata", MetadataGui, transmitter);
                FoldoutBoxGroup(_activationModeExpanded, "Activation Mode", ActivationModeGui, transmitter);
                FoldoutBoxGroup(_tokensExpanded, "Access Tokens", TokenGui, transmitter);
                FoldoutBoxGroup(_ampExpanded, "Amplitude Faders", VolumeGui, transmitter);

                Undo.FlushUndoRecordObjects();

                if (changed.changed)
                    EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void FoldoutBoxGroup([NotNull] SerializedProperty expanded, string title, Action<VoiceBroadcastTrigger> gui, VoiceBroadcastTrigger trigger)
        {
            expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, title);
            if (expanded.boolValue)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    gui(trigger);
        }

        private void ChannelTypeGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Type",
                (CommTriggerTarget)EditorGUILayout.EnumPopup(new GUIContent("Channel Type", "Where this trigger sends voice to"), transmitter.ChannelType),
                transmitter.ChannelType,
                a => transmitter.ChannelType = a
            );

            if (transmitter.ChannelType == CommTriggerTarget.Player)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Channel Transmitter Player Name",
                    EditorGUILayout.TextField(new GUIContent("Recipient Player Name", "The name of the player receiving voice from this trigger"), transmitter.PlayerId),
                    transmitter.PlayerId,
                    a => transmitter.PlayerId = a
                );

                EditorGUILayout.HelpBox("Player mode sends voice data to the specified player.", MessageType.None);
            }

            if (transmitter.ChannelType == CommTriggerTarget.Room)
            {
                var roomNames = _roomSettings.Names;

                var haveRooms = roomNames.Count > 0;
                if (haveRooms)
                {
                    var roomList = new List<string>(roomNames);
                    var roomIndex = roomList.IndexOf(transmitter.RoomName);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Detect if the room name is not null, and is also not in the list. This implies the room has been deleted from the room list.
                        // If this is the case insert it into our temporary copy of the room names list
                        if (roomIndex == -1 && !string.IsNullOrEmpty(transmitter.RoomName))
                        {
                            roomList.Insert(0, transmitter.RoomName);
                            roomIndex = 0;
                        }

                        transmitter.ChangeWithUndo(
                            "Changed Dissonance Transmitter Room",
                            EditorGUILayout.Popup(new GUIContent("Chat Room", "The room to send voice to"), roomIndex, roomList.Select(a => new GUIContent(a)).ToArray()),
                            roomIndex,
                            a => transmitter.RoomName = roomList[a]
                        );

                        if (GUILayout.Button("Config Rooms"))
                            ChatRoomSettingsEditor.GoToSettings();
                    }

                    if (string.IsNullOrEmpty(transmitter.RoomName))
                        EditorGUILayout.HelpBox("No chat room selected", MessageType.Error);
                }
                else
                {
                    if (GUILayout.Button("Create New Rooms"))
                        ChatRoomSettingsEditor.GoToSettings();
                }

                EditorGUILayout.HelpBox("Room mode sends voice data to all players in the specified room.", MessageType.None);

                if (!haveRooms)
                    EditorGUILayout.HelpBox("No rooms are defined. Click 'Create New Rooms' to configure chat rooms.", MessageType.Warning);
            }

            if (transmitter.ChannelType == CommTriggerTarget.Self)
            {
                EditorGUILayout.HelpBox(
                    "Self mode sends voice data to the DissonancePlayer attached to this game object.",
                    MessageType.None
                );

                var player = transmitter.GetComponent<IDissonancePlayer>() ?? transmitter.GetComponentInParent<IDissonancePlayer>();
                if (player == null)
                {
                    EditorGUILayout.HelpBox(
                        "This GameObject (and it's parent) does not have a Dissonance player component!",
                        MessageType.Error
                    );
                }
                else
                {
                    if (EditorApplication.isPlaying)
                    {
                        if (!player.IsTracking)
                        {
                            EditorGUILayout.HelpBox(
                                "The trigger is disabled because the player tracker script is not yet tracking the player",
                                MessageType.Warning
                            );
                        }

                        if (player.Type == NetworkPlayerType.Local)
                        {
                            EditorGUILayout.HelpBox(
                                "This trigger is disabled because the player tracker script represents the local player (cannot send voice to yourself).",
                                MessageType.Info
                            );
                        }

                        if (player.IsTracking && player.Type == NetworkPlayerType.Unknown)
                        {
                            EditorGUILayout.HelpBox(
                                "This trigger is disabled because the player tracker script is tracking an 'Unknown' player type. This is probably a bug in your player tracker script.",
                                MessageType.Error
                            );
                        }
                    }
                }
            }
        }

        private static void MetadataGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Positional Audio",
                EditorGUILayout.Toggle(new GUIContent("Use Positional Data", "If voices sent with this trigger should be played with 3D playback"), transmitter.BroadcastPosition),
                transmitter.BroadcastPosition,
                a => transmitter.BroadcastPosition = a
            );

            if (!transmitter.BroadcastPosition)
            {
                EditorGUILayout.HelpBox(
                    "Send audio on this channel with positional data to allow 3D playback if set up on the receiving end. There is no performance cost to enabling this.\n\n" +
                    "Please see the Dissonance documentation for instructions on how to set your project up for playback of 3D voice comms.",
                    MessageType.Info);
            }

            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Priority",
                (ChannelPriority)EditorGUILayout.EnumPopup(new GUIContent("Priority", "Priority for speech sent through this trigger"), transmitter.Priority),
                transmitter.Priority,
                a => transmitter.Priority = a
            );

            if (transmitter.Priority == ChannelPriority.None)
            {
                EditorGUILayout.HelpBox(
                    "Priority for the voice sent from this room. Voices will mute all lower priority voices on the receiver while they are speaking.\n\n" +
                    "'None' means that this room specifies no particular priority and the priority of this player will be used instead",
                    MessageType.Info);
            }
        }

        private static void ActivationModeGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Broadcast Trigger Mute",
                EditorGUILayout.Toggle(new GUIContent("Mute", "If this trigger is prevented from sending any audio"), transmitter.IsMuted),
                transmitter.IsMuted,
                a => transmitter.IsMuted = a
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Activation Mode",
                (CommActivationMode)EditorGUILayout.EnumPopup(new GUIContent("Activation Mode", "How the user should indicate an intention to speak"), transmitter.Mode),
                transmitter.Mode,
                a => transmitter.Mode = a
            );

            if (transmitter.Mode == CommActivationMode.None)
            {
                EditorGUILayout.HelpBox(
                    "While in this mode no voice will ever be transmitted",
                    MessageType.Info
                );
            }

            if (transmitter.Mode == CommActivationMode.PushToTalk)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Push To Talk Axis",
                    EditorGUILayout.TextField(new GUIContent("Input Axis Name", "Which input axis indicates the user is speaking"), transmitter.InputName),
                    transmitter.InputName,
                    a => transmitter.InputName = a
                );

                EditorGUILayout.HelpBox(
                    "Define an input axis in Unity's input manager if you have not already.",
                    MessageType.Info
                );
            }

            VolumeTriggerActivationGui(transmitter);
        }

        private static void VolumeTriggerActivationGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            using (var toggle = new EditorGUILayout.ToggleGroupScope(new GUIContent("Collider Volume Activation", "Only allows speech when the user is inside a collider"), transmitter.UseColliderTrigger))
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Trigger Activation",
                    toggle.enabled,
                    transmitter.UseColliderTrigger,
                    a => transmitter.UseColliderTrigger = a
                );

                if (transmitter.UseColliderTrigger)
                {
                    var triggers2D = transmitter.gameObject.GetComponents<Collider2D>().Any(c => c.isTrigger);
                    var triggers3D = transmitter.gameObject.GetComponents<Collider>().Any(c => c.isTrigger);
                    if (!triggers2D && !triggers3D)
                        EditorGUILayout.HelpBox("Cannot find any collider triggers attached to this entity.", MessageType.Warning);
                }
            }

            if (!transmitter.UseColliderTrigger)
            {
                EditorGUILayout.HelpBox(
                    "Use trigger activation to only broadcast when the player is inside a trigger volume.",
                    MessageType.Info
                );
            }
        }

        private void TokenGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            _tokenEditor.DrawInspectorGui(transmitter, transmitter);
        }

        private static void VolumeGui([NotNull] VoiceBroadcastTrigger transmitter)
        {
            if (EditorApplication.isPlaying)
            {
                var currentDb = Helpers.ToDecibels(transmitter.CurrentFaderVolume);
                EditorGUILayout.Slider("Current Gain (dB)", currentDb, Helpers.MinDecibels, Math.Max(10, currentDb));
                //EditorGUILayout.Slider("Current Attenuation (VMUL)", transmitter.CurrentFaderVolume, 0, Math.Max(1, transmitter.CurrentFaderVolume));
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField(new GUIContent(string.Format("{0} Fade", transmitter.Mode), string.Format("Fade when {0} mode changes", transmitter.Mode)));
            SingleFaderGui(transmitter, transmitter.ActivationFader);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(!transmitter.UseColliderTrigger))
            {
                EditorGUILayout.LabelField(new GUIContent("Volume Trigger Fade", "Fade when when entering/exiting collider volume trigger"));
                SingleFaderGui(transmitter, transmitter.ColliderTriggerFader);
            }
        }

        private static void SingleFaderGui([NotNull] VoiceBroadcastTrigger transmitter, [NotNull] VolumeFaderSettings settings)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Volume",
                Helpers.FromDecibels(EditorGUILayout.Slider(new GUIContent("Channel Volume (dB)", "Amplification for voice sent from this trigger"), Helpers.ToDecibels(settings.Volume), Helpers.MinDecibels, 10)),
                //EditorGUILayout.Slider(new GUIContent("Channel Volume (VMUL)", "Volume multiplier for voice sent from this trigger"), settings.Volume, 0, 4),
                settings.Volume,
                a => settings.Volume = a
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade In Time",
                EditorGUILayout.Slider(new GUIContent("Fade In Time", "Duration (seconds) for voice take to reach full volume"), (float)settings.FadeIn.TotalSeconds, 0, 3),
                settings.FadeIn.TotalSeconds,
                a => settings.FadeIn = TimeSpan.FromSeconds(a)
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade Out Time",
                EditorGUILayout.Slider(new GUIContent("Fade Out Time", "Duration (seconds) for voice to fade to silent and stop transmitting"), (float)settings.FadeOut.TotalSeconds, 0, 3),
                settings.FadeOut.TotalSeconds,
                a => settings.FadeOut = TimeSpan.FromSeconds(a)
            );
        }
    }
}
#endif
