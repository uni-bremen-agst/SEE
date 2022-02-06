using System;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    ///     Enters and exits voice comm rooms in response to entity activation or
    ///     local player proximity.
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    [HelpURL("https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Voice-Receipt-Trigger/")]
    public class VoiceReceiptTrigger
        : BaseCommsTrigger
    {
        #region fields and properties
        private RoomMembership? _membership;

        [SerializeField]private string _roomName;
        /// <summary>
        /// Get or set the name of the room this receipt trigger is receiving from
        /// </summary>
        public string RoomName
        {
            get { return _roomName; }
            set
            {
                if (_roomName != value)
                {
                    _roomName = value;

                    //Since the room has changed we need to close the channel. Next update will open it if necessary
                    LeaveRoom();
                }
            }
        }

        private bool _scriptDeactivated;

        [SerializeField]private bool _useTrigger;
        /// <summary>
        /// Get or set if this receipt trigger should use a unity trigger volume
        /// </summary>
        public override bool UseColliderTrigger
        {
            get { return _useTrigger; }
            set { _useTrigger = value; }
        }

        /// <inheritdoc />
        public override bool CanTrigger
        {
            get
            {
                if (Comms == null || !Comms.IsStarted)
                    return false;

                if (_roomName == null)
                    return false;

                if (_scriptDeactivated)
                    return false;

                return true;
            }
        }
        #endregion

        #region manual activation
        /// <summary>
        /// Allow this receipt trigger to receive voice
        /// </summary>
        [Obsolete("This is equivalent to enabling this component")]    //Marked obsolete after v4.0.0 (2017-11-08)
        public void StartListening()
        {
            _scriptDeactivated = false;
        }

        /// <summary>
        /// Prevent this receipt trigger from receiving any voice until StartListening is called
        /// </summary>
        [Obsolete("This is equivalent to disabling this component")]    //Marked obsolete after v4.0.0 (2017-11-08)
        public void StopListening()
        {
            _scriptDeactivated = true;
        }
        #endregion

        protected override void Update()
        {
            base.Update();

            if (!CheckVoiceComm())
                return;

            var shouldActivate =
                CanTrigger                                  //Don't activate if base checks say we can't
                && (!_useTrigger || IsColliderTriggered)    //Only activate if trigger is activated (and we're using trigger activation)
                && TokenActivationState;                    //Only activate if tokens say so

            if (shouldActivate)
                JoinRoom();
            else
                LeaveRoom();
        }

        private void JoinRoom()
        {
            if (!_membership.HasValue)
                _membership = Comms.Rooms.Join(RoomName);
        }

        private void LeaveRoom()
        {
            if (_membership.HasValue)
            {
                Comms.Rooms.Leave(_membership.Value);
                _membership = null;
            }
        }

        protected override void OnDisable()
        {
            if (Comms != null)
                LeaveRoom();

            base.OnDisable();
        }
    }
}