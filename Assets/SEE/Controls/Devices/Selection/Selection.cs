using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// Abstract super class for all input devices providing selection information.
    /// </summary>
    public abstract class Selection : InputDevice
    {
        /// <summary>
        /// The direction of the selection. Subclasses may provide values where
        /// not all co-ordinates of the resulting vector are relevant (e.g., for
        /// mouse directions).
        /// </summary>
        public abstract Vector3 Direction { get; }

        /// <summary>
        /// The position of the selection device at the time of selection. Subclasses 
        /// may provide values where not all co-ordinates of the resulting vector are 
        /// relevant (e.g., for mouse positions).
        /// </summary>
        public abstract Vector3 Position { get; }

        /// <summary>
        /// The degree of pulling. A value between 0 and 1.
        /// </summary>
        public abstract float Pull { get; }

        /// Note on the different actions selecting, grabbing, canceling: These actions
        /// are not mutually exclusive. The client of this API must resolve potential
        /// conflicts.

        /// <summary>
        /// True if the user wants to select (e.g., when a mouse button was pressed).
        /// This event is continuous, meaning, the same values is returned while the
        /// user requests this action.
        /// </summary>
        public abstract bool IsSelecting { get; }

        /// <summary>
        /// True if the user wants to grab a selected object (e.g., when a particular grabbing 
        /// key was pressed).
        /// This event is continuous, meaning, the same values is returned while the
        /// user requests this action.
        /// </summary>
        public abstract bool IsGrabbing { get; }

        /// <summary>
        /// True if the user wants to cancel the currently requested action.
        /// This event is a non-persistent one-time event, meaning, the value is returned only
        /// once at the moment when this property is queried.
        /// </summary>
        public abstract bool IsCanceling { get; }

        public abstract bool IsAnnotating { get;}

        /// <summary>
        /// True if the user wants to zoom into details.
        /// This event is a non-persistent one-time event, meaning, the value is returned only
        /// once at the moment when this property is queried.        
        /// </summary>
        public abstract bool IsZoomingIn { get; }

        /// <summary>
        /// True if the user wants to zoom out of details.
        /// This event is a non-persistent one-time event, meaning, the value is returned only
        /// once at the moment when this property is queried.        
        /// </summary>
        public abstract bool IsZoomingOut { get; }

        /// <summary>
        /// True if the user wants to zoom out of all details back to the top level.
        /// This event is a non-persistent one-time event, meaning, the value is returned only
        /// once at the moment when this property is queried.        
        /// </summary>
        public abstract bool IsZoomingHome { get; }
    }
}