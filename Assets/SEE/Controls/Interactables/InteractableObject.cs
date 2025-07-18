﻿using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
#if INCLUDE_STEAM_VR
using Valve.VR.InteractionSystem;
#endif
using SEE.Net.Actions;
using SEE.Audio;
using SEE.Game;
using SEE.Game.Operator;
using SEE.Game.Avatars;

namespace SEE.Controls
{
    /// <summary>
    /// InteractableObject components can be attached to different kinds of objects such as game
    /// nodes or edges but also markers or scroll views in metric charts. A HoverFlag indicates
    /// to which kind of game object the hovering event relates to. A HoverFlag is used as a single bit
    /// that occurs in hovering flags.
    /// </summary>
    public enum HoverFlag
    {
        None = 0x0, // nothing is being hovered over
        World = 0x1, // an object in the city world is being hovered over (game nodes or edges and the like)
        ChartMarker = 0x2, // a marker in a chart is being hovered over
        ChartMultiSelect = 0x4, // multiple markers in a chart are being hovered over (within a rectangular bound)
        ChartScrollViewToggle = 0x8 // the scroll view of a metric chart is being hovered over
    }

    /// <summary>
    /// User-interactable graph elements.
    /// </summary>
    public sealed class InteractableObject : InteractableObjectBase
    {
        /// <inheritdoc />
        public override int InteractableLayer => Layers.InteractableGraphObjects;

        /// <inheritdoc />
        public override int NonInteractableLayer => Layers.NonInteractableGraphObjects;

        /// <inheritdoc />
        public override Color? HitColor => hitColor;

        /// <summary>
        /// Backing field for the <see cref="HitColor"/> property.
        /// </summary>
        private Color hitColor = LaserPointer.HitColor;

        /// <summary>
        /// The color of the laser pointer when it is hovering over a node.
        /// </summary>
        private static Color nodeHitColor = Color.green;

        /// <summary>
        /// The color of the laser pointer when it is hovering over an edge.
        /// </summary>
        private static Color edgeHitColor = Color.blue;

        // Tutorial on grabbing objects:
        // https://www.youtube.com/watch?v=MKOc8J877tI&t=15s

        // These are the messages the hand sends to objects that it is interacting with:
        //
        // OnHandHoverBegin:       Sent when the hand first starts hovering over the object
        // HandHoverUpdate:        Sent every frame that the hand is hovering over the object
        // OnHandHoverEnd:         Sent when the hand stops hovering over the object
        // OnAttachedToHand:       Sent when the object gets attached to the hand
        // HandAttachedUpdate:     Sent every frame while the object is attached to the hand
        // OnDetachedFromHand:     Sent when the object gets detached from the hand
        // OnHandFocusLost:        Sent when an attached object loses focus because something else has been attached to the hand
        // OnHandFocusAcquired:    Sent when an attached object gains focus because the previous focus object has been detached from the hand
        //
        // See https://valvesoftware.github.io/steamvr_unity_plugin/articles/Interaction-System.html

        /// <summary>
        /// The interactable objects.
        /// </summary>
        private static readonly Dictionary<string, InteractableObject> idToInteractableObjectDict = new();

        /// <summary>
        /// The hovered objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> HoveredObjects = new();

        /// <summary>
        /// The object, that is currently hovered by this player.
        /// <para>
        /// There is always at most one object hovered by this player with the flag
        /// <see cref="HoverFlag.World"/> set.
        /// </para><para>
        /// The object is tested against <see cref="IsInteractable"/> before being set.
        /// </para>
        /// </summary>
        public static InteractableObject HoveredObjectWithWorldFlag { get; private set; }

        /// <summary>
        /// The selected objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> SelectedObjects = new();

        /// <summary>
        /// The grabbed objects.
        /// </summary>
        public static readonly HashSet<InteractableObject> GrabbedObjects = new();

        /// <summary>
        /// If multiple objects should be selectable at the same time.
        /// </summary>
        public static bool MultiSelectionAllowed = true;

        /// <summary>
        /// The selected objects per graph.
        /// </summary>
        private static readonly Dictionary<Graph, HashSet<InteractableObject>> graphToSelectedIOs = new();

        /// <summary>
        /// The graph element this interactable object is attached to.
        /// </summary>
        public GraphElementRef GraphElemRef { get; private set; }

        /// <summary>
        /// A bit vector for hovering flags. Each flag is a bit as defined in <see cref="HoverFlag"/>.
        /// If the bit is set, this <see cref="InteractableObject"/> is to be considered hovered over for interaction
        /// events in the respective scope of interactable objects. For instance, if this <see cref="InteractableObject"/>
        /// is attached to a game object representing a graph node in a code city, then <see cref="HoverFlag.World"/>
        /// would be set and whenever a hovering event occurs relating to <see cref="HoverFlag.World"/> objects,
        /// the graph node will be considered being hovered over. If instead this <see cref="InteractableObject"/>
        /// is attached to a marker in a metric chart, then <see cref="HoverFlag.ChartMarker"/> will be set.
        /// If a hovering event occurs relating to <see cref="HoverFlag.World"/> objects, the marker will not
        /// be considered being hovered over.
        /// </summary>
        public uint HoverFlags { get; private set; } = 0;

        /// <summary>
        /// Whether the object is currently hovered over by e.g. the mouse or the VR
        /// controller (no matter what kind of element it is, e.g. city object,
        /// metric marker, etc.).
        /// </summary>
        public bool IsHovered => HoverFlags != 0;

        /// <summary>
        /// Whether the given hover flag is set.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns><code>true</code> if the given flag is set, <code>false</code>
        /// otherwise.</returns>
        public bool IsHoverFlagSet(HoverFlag flag) => (HoverFlags & (uint)flag) != 0;

        /// <summary>
        /// Whether the object is currently selected by e.g. the mouse or the VR
        /// controller.
        /// </summary>
        public bool IsSelected { get; private set; }

        /// <summary>
        /// Whether the object is currently grabbed by e.g. the mouse or the VR
        /// controller.
        /// </summary>
        public bool IsGrabbed { get; private set; }

#if INCLUDE_STEAM_VR
        /// <summary>
        /// The interactable component, that is used by SteamVR. The interactable
        /// component is attached to <code>this.gameObject</code>.
        /// </summary>
        private Interactable interactable;
#endif

        /// <summary>
        /// The synchronizer is attached to <code>this.gameObject</code>, iff it is
        /// grabbed.
        /// </summary>
        public Synchronizer InteractableSynchronizer { get; private set; }

        private void Awake()
        {
#if INCLUDE_STEAM_VR
            gameObject.TryGetComponentOrLog(out interactable);
#endif
            GraphElemRef = GetComponent<GraphElementRef>();

            hitColor = gameObject.IsNode() ? nodeHitColor : edgeHitColor;
        }

        private void OnDestroy()
        {
            if (IsHovered)
            {
                SetHoverFlags(0, true);
            }

            if (IsSelected)
            {
                SetSelect(false, true);
            }

            if (IsGrabbed)
            {
                SetGrab(false, true);
            }

            GraphElemRef = null;
#if INCLUDE_STEAM_VR
            interactable = null;
#endif
        }

        /// <summary>
        /// Returns the interactable object of given id or <code>null</code>, if it does
        /// not exist.
        /// </summary>
        /// <param name="id">The id of the interactable object.</param>
        /// <returns>the interactable with the given <paramref name="id"/>; null if none exists</returns>
        public static InteractableObject Get(string id)
        {
            idToInteractableObjectDict.TryGetValue(id, out InteractableObject result);
            return result;
        }

        /// <summary>
        /// Returns the currently selected objects of given graph.
        /// </summary>
        /// <param name="graph">The graph, that the selected objects must be contained
        /// by.</param>
        /// <returns>The currently selected objects of given graph.</returns>
        public static HashSet<InteractableObject> GetSelectedObjectsOfGraph(Graph graph)
        {
            if (!graphToSelectedIOs.ContainsKey(graph))
            {
                graphToSelectedIOs[graph] = new HashSet<InteractableObject>();
            }

            return graphToSelectedIOs[graph];
        }

        #region Interaction

        /// <summary>
        /// Whether the object is currently interactable (at the given hit point).
        /// <para>
        /// If the object is grabbed, it is not interactable.
        /// </para><para>
        /// See <see cref="InteractableAuxiliaryObject.IsInteractable(Vector3?)"/> for inherited base functionality.
        /// </para>
        /// </summary>
        /// <param name="point">The hit point on the object.</param>
        new public bool IsInteractable(Vector3? point = null)
        {
            if (IsGrabbed) return false;
            return base.IsInteractable(point);
        }

        /// <summary>
        /// Sets <see cref="HoverFlags"/> to given <paramref name="hoverFlags"/>. Then if
        /// the object is being hovered over (<see cref="IsHovered"/>), the <see cref="HoverIn"/>
        /// and <see cref="AnyHoverIn"/> events are triggered with this <see cref="InteractableObject"/>
        /// and <paramref name="isInitiator"/> as arguments. If <paramref name="isInitiator"/>, the <see cref="LocalHoverIn"/>
        /// and <see cref="LocalAnyHoverIn"/> events are triggered with this <see cref="InteractableObject"/>
        /// additionally. This <see cref="InteractableObject"/> will be added to the set of <see cref="HoveredObjects"/>.
        ///
        /// If instead the object is NOT being hovered over (<see cref="IsHovered"/>), the <see cref="HoverOut"/>
        /// and <see cref="AnyHoverOut"/> events are triggered with this <see cref="InteractableObject"/>
        /// and <paramref name="isInitiator"/> as arguments. If <paramref name="isInitiator"/>, the <see cref="LocalHoverOut"/>
        /// and <see cref="LocalAnyHoverOut"/> events are triggered with this <see cref="InteractableObject"/>
        /// additionally. This <see cref="InteractableObject"/> will be removed from the set of <see cref="HoveredObjects"/>.
        ///
        /// At any rate, if we are running in multiplayer mode and <paramref name="isInitiator"/> is true,
        /// <see cref="Net.SetHoverAction"/> will be called with the given <paramref name="hoverFlags"/>
        /// and this <see cref="InteractableObject"/>.
        /// </summary>
        /// <param name="hoverFlags">New value for <see cref="HoverFlags"./></param>
        /// <param name="isInitiator">Whether this client is initiating the hovering action.</param>
        public void SetHoverFlags(uint hoverFlags, bool isInitiator)
        {
#if UNITY_EDITOR
            if (hoverFlags == HoverFlags)
            {
                string message = "Flags to be set are identical to the active flags. Active flags:";
                HoverFlag[] flags = (HoverFlag[])Enum.GetValues(typeof(HoverFlag));
                foreach (HoverFlag flag in flags)
                {
                    message += $"\n\t{flag}: {(IsHoverFlagSet(flag) ? "Yes" : "No")}";
                }

                Debug.LogWarning(message);
                return;
            }
#endif
            uint prevHoverFlags = HoverFlags;
            HoverFlags = hoverFlags;

            if (IsHovered)
            {
                HoverIn?.Invoke(this, isInitiator);
                AnyHoverIn?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has hovered on this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalHoverIn?.Invoke(this);
                    LocalAnyHoverIn?.Invoke(this);
                    AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.HoverSound, this.gameObject);

                    if (IsHoverFlagSet(HoverFlag.World))
                    {
                        HoveredObjectWithWorldFlag = this;
                    }
                }

                HoveredObjects.Add(this);
            }
            else
            {
                HoverOut?.Invoke(this, isInitiator);
                AnyHoverOut?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has finished hovering on this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalHoverOut?.Invoke(this);
                    LocalAnyHoverOut?.Invoke(this);
                }

                HoveredObjects.Remove(this);
                if ((prevHoverFlags & (uint)HoverFlag.World) != 0)
                {
                    // FIXME: This assertion is often violated. I (RK) don't know why. This needs further
                    // investigation.
                    //if (HoveredObjectWithWorldFlag != null)
                    //{
                    //    Debug.LogWarning($"HoveredObjectWithWorldFlag was expected to be null.\n.");
                    //}
                    HoveredObjectWithWorldFlag = null;
                }
            }

            if (isInitiator)
            {
                new SetHoverNetAction(this, hoverFlags).Execute();
            }
        }

        /// <summary>
        /// Runs <see cref="SetHoverFlags(uint, bool)"/> with the first parameter, say H,
        /// that equals <see cref="HoverFlags"/> with the <paramref name="hoverFlag"/> bit
        /// turned on if <paramref name="setFlag"/> or turned off if not <paramref name="setFlag"/>.
        /// The second parameter is <paramref name="isInitiator"/>.
        ///
        /// Note: This method may be called locally when a local user interacts with the
        /// object or remotely when a remote user has interacted with the object. In the
        /// former case, <paramref name="isInitiator"/> will be true. In the
        /// latter case, it will be called via <see cref="SEE.Net.SetHoverAction.ExecuteOnClient()"/>
        /// where <paramref name="isInitiator"/> is false.
        /// </summary>
        /// <param name="hoverFlag">The flag to be set or unset.</param>
        /// <param name="setFlag">Whether this object should be hovered.</param>
        /// <param name="isInitiator">Whether this client is initiating the hovering action.</param>
        public void SetHoverFlag(HoverFlag hoverFlag, bool setFlag, bool isInitiator)
        {
            uint hoverFlags;
            if (setFlag)
            {
                // hoverFlag will be "turned on" in HoverFlags if not already set
                hoverFlags = HoverFlags | (uint)hoverFlag;
            }
            else
            {
                // hoverFlag will be "turned off" in HoverFlags
                hoverFlags = HoverFlags & ~(uint)hoverFlag;
            }

            SetHoverFlags(hoverFlags, isInitiator);
        }

        /// <summary>
        /// Unhovers all objects.
        /// </summary>
        /// <param name="isInitiator">Whether this client is initiating the grabbing action.</param>
        public static void UnhoverAll(bool isInitiator)
        {
            while (HoveredObjects.Count != 0)
            {
                HoveredObjects.ElementAt(HoveredObjects.Count - 1).SetHoverFlags(0, isInitiator);
            }
        }

        /// <summary>
        /// Marks the game object this <see cref="InteractableObject"/> is attached to for selection
        /// and triggers the necessary events accordingly.
        ///
        /// As a side effect, this <see cref="InteractableObject"/> will be added or removed,
        /// respectively, to <see cref="SelectedObjects"/> depending upon <paramref name="select"/>.
        /// </summary>
        /// <param name="select">Whether this object should be selected.</param>
        /// <param name="isInitiator">Whether this client is initiating the selection action.</param>
        public void SetSelect(bool select, bool isInitiator)
        {
            Assert.IsTrue(IsSelected != select);

            if (select && !MultiSelectionAllowed && SelectedObjects.Count > 0)
            {
                return;
            }

            IsSelected = select;

            if (select)
            {
                // Update all selected object list
                SelectedObjects.Add(this);

                // Update all selected object list per graph
                Graph graph = GraphElemRef.Elem.ItsGraph;
                if (!graphToSelectedIOs.ContainsKey(graph))
                {
                    graphToSelectedIOs[graph] = new HashSet<InteractableObject>();
                }

                graphToSelectedIOs[graph].Add(this);

                // Start blinking indefinitely.
                GraphElementOperator op = gameObject.Operator();
                op.Blink(-1);
                if (op is EdgeOperator eop)
                {
                    eop.AnimateDataFlow(true);
                }

                // Invoke events
                SelectIn?.Invoke(this, isInitiator);
                AnySelectIn?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has selected this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalSelectIn?.Invoke(this);
                    LocalAnySelectIn?.Invoke(this);
                }
            }
            else
            {
                // Update all selected object list
                SelectedObjects.Remove(this);

                // Update all selected object list per graph
                graphToSelectedIOs[GraphElemRef.Elem.ItsGraph].Remove(this);

                // Stop blinking.
                GraphElementOperator op = gameObject.Operator();
                op.Blink(0);
                if (op is EdgeOperator eop)
                {
                    eop.AnimateDataFlow(false);
                }

                // Invoke events
                SelectOut?.Invoke(this, isInitiator);
                AnySelectOut?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has deselected this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalSelectOut?.Invoke(this);
                    LocalAnySelectOut?.Invoke(this);
                }
            }

            if (isInitiator)
            {
                new SetSelectNetAction(this, select).Execute();
            }
        }

        /// <summary>
        /// Deselects all currently selected interactable objects and invokes the
        /// <see cref="ReplaceSelect"/> event.
        /// </summary>
        /// <param name="isInitiator">Whether this client is initiating the action.</param>
        public static void UnselectAll(bool isInitiator) => UnselectAllInternal(isInitiator, true);

        /// <summary>
        /// Deselects all currently selected interactable objects and invokes the
        /// <see cref="ReplaceSelect"/> event, if <paramref name="invokeReplaceEvent"/> is true.
        /// </summary>
        /// <param name="isInitiator">Whether this client is initiating the action.</param>
        /// <param name="invokeReplaceEvent">Whether the replace event should be invoked.</param>
        private static void UnselectAllInternal(bool isInitiator, bool invokeReplaceEvent)
        {
            List<InteractableObject> replaced = SelectedObjects.ToList();
            List<InteractableObject> by = new();
            if (replaced.Count > 0 || by.Count > 0)
            {
                // Note: This is no endless loop because SetSelect will remove this
                // InteractableObject from SelectedObjects.
                while (SelectedObjects.Count != 0)
                {
                    SelectedObjects.ElementAt(SelectedObjects.Count - 1).SetSelect(false, isInitiator);
                }

                if (invokeReplaceEvent)
                {
                    ReplaceSelect?.Invoke(replaced, by, isInitiator);
                }
            }
        }

        /// <summary>
        /// Replaces current selection by <paramref name="interactableObject"/> and invokes the
        /// replace event.
        /// </summary>
        /// <param name="interactableObject">The new selected object.</param>
        /// <param name="isInitiator">Whether this client is initiating the action.</param>
        public static void ReplaceSelection(InteractableObject interactableObject, bool isInitiator)
        {
            List<InteractableObject> replaced = SelectedObjects.ToList();
            List<InteractableObject> by = new(1);
            if (interactableObject)
            {
                by.Add(interactableObject);
            }

            if (replaced.Count > 0 || by.Count > 0)
            {
                UnselectAllInternal(isInitiator, false);
                if (interactableObject)
                {
                    interactableObject.SetSelect(true, isInitiator);
                }

                ReplaceSelect?.Invoke(replaced, by, isInitiator);
            }
        }

        /// <summary>
        /// Deselects all currently selected interactable objects within given
        /// <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph of which the objects are to be deselected.</param>
        /// <param name="isInitiator">Whether this client is initiating the action.</param>
        public static void UnselectAllInGraph(Graph graph, bool isInitiator)
        {
            if (graphToSelectedIOs.TryGetValue(graph, out HashSet<InteractableObject> s))
            {
                HashSet<InteractableObject>.Enumerator e;
                while (s.Count != 0)
                {
                    e = s.GetEnumerator();
                    e.MoveNext();
                    e.Current.SetSelect(false, isInitiator);
                }
            }
        }

        /// <summary>
        /// Visually emphasizes this object for grabbing.
        /// </summary>
        /// <param name="grab">Whether this object should be grabbed.</param>
        /// <param name="isInitiator">Whether this client is initiating the grabbing action.</param>
        public void SetGrab(bool grab, bool isInitiator)
        {
            //Assert.IsTrue(IsGrabbed != grab);

            IsGrabbed = grab;

            if (grab)
            {
                GrabIn?.Invoke(this, isInitiator);
                AnyGrabIn?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has grabbed this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalGrabIn?.Invoke(this);
                    LocalAnyGrabIn?.Invoke(this);
                }

                GrabbedObjects.Add(this);
            }
            else
            {
                GrabOut?.Invoke(this, isInitiator);
                AnyGrabOut?.Invoke(this, isInitiator);
                if (isInitiator)
                {
                    // The local player has finished grabbing this object and needs to be informed about it.
                    // Non-local player are not concerned here.
                    LocalGrabOut?.Invoke(this);
                    LocalAnyGrabOut?.Invoke(this);
                }

                GrabbedObjects.Remove(this);
            }

            if (isInitiator)
            {
                new SetGrabNetAction(this, grab).Execute();
                if (grab)
                {
#if INCLUDE_STEAM_VR
                    InteractableSynchronizer = interactable.gameObject.AddComponent<Synchronizer>();
#endif
                }
                else
                {
                    Destroyer.Destroy(InteractableSynchronizer);
                    InteractableSynchronizer = null;
                }
            }
        }

        /// <summary>
        /// Ungrabs all objects.
        /// </summary>
        /// <param name="isInitiator">Whether this client is initiating the grabbing action.</param>
        public static void UngrabAll(bool isInitiator)
        {
            while (GrabbedObjects.Count != 0)
            {
                GrabbedObjects.ElementAt(GrabbedObjects.Count - 1).SetGrab(false, isInitiator);
            }
        }

        #endregion

        #region Events

        ///------------------------------------------------------------------
        /// Actions can register on selection, hovering, and grabbing events.
        /// Then they will be invoked if those events occur.
        ///------------------------------------------------------------------
        ///
        /// ----------------------------
        /// Hovering event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a hovering event has happened (hover over
        /// or hover off the game object) in circumstances where a distinction between
        /// remote or local players must be made.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over (or no longer being hovered over)</param>
        /// <param name="isInitiator">true if a local player initiated this call</param>
        public delegate void MultiPlayerHoverAction(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// A delegate to be called when a hovering event has happened (hover over
        /// or hover off the game object). This delegate is intended to be used in
        /// circumstances where no distinction between remote or local players needs
        /// to be made.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over (or no longer being hovered over)</param>
        public delegate void LocalPlayerHoverAction(InteractableObject interactableObject);

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is
        /// being hovered over. Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerHoverAction HoverIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is
        /// no longer hovered over. Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerHoverAction HoverOut;

        /// <summary>
        /// Event to be triggered when any <see cref="InteractableObject"/> is being hovered over.
        /// It can be used for actions of a player. Rather than requiring a player to register
        /// for all existing instances of <see cref="InteractableObject"/ it is interested in,
        /// the player just registers on this event here and gets notified whenever any
        /// <see cref="InteractableObject"/> is hovered over. The player must make the distinction
        /// whether it is interested in this <see cref="InteractableObject"/> at all.
        /// Intended for multiplayer actions.
        ///
        /// Note: This event is declared static so that it is independent of a particular
        /// <see cref="InteractableObject"/.
        /// </summary>
        public static event MultiPlayerHoverAction AnyHoverIn;

        /// <summary>
        /// Event to be triggered when any <see cref="InteractableObject"/> is no longer being hovered over.
        /// It can be used for actions of a player. Rather than requiring a player to register
        /// for all existing instances of <see cref="InteractableObject"/ it is interested in,
        /// the player just registers on this event here and gets notified whenever any
        /// <see cref="InteractableObject"/> is no longer being hovered over. The player must make the distinction
        /// whether it is interested in this <see cref="InteractableObject"/> at all.
        /// Intended for multiplayer actions.
        ///
        /// Note: This event is declared static so that it is independent of a particular
        /// <see cref="InteractableObject"/.
        /// </summary>
        public static event MultiPlayerHoverAction AnyHoverOut;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is
        /// being hovered over. Intended for actions to be executed only locally.
        /// </summary>
        public event LocalPlayerHoverAction LocalHoverIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is
        /// no longer hovered over. Intended for actions to be executed only locally.
        /// </summary>
        public event LocalPlayerHoverAction LocalHoverOut;

        /// <summary>
        /// Event to be triggered when any <see cref="InteractableObject"/> is being hovered over.
        /// It can be used for actions of a player. Rather than requiring a player to register
        /// for all existing instances of <see cref="InteractableObject"/ it is interested in,
        /// the player just registers on this event here and gets notified whenever any
        /// <see cref="InteractableObject"/> is hovered over. The player must make the distinction
        /// whether it is interested in this <see cref="InteractableObject"/> at all.
        /// Intended for actions to be executed only locally.
        ///
        /// Note: This event is declared static so that it is independent of a particular
        /// <see cref="InteractableObject"/.
        /// </summary>
        public static event LocalPlayerHoverAction LocalAnyHoverIn;

        /// <summary>
        /// Event to be triggered when any <see cref="InteractableObject"/> is no longer being hovered over.
        /// It can be used for actions of a player. Rather than requiring a player to register
        /// for all existing instances of <see cref="InteractableObject"/ it is interested in,
        /// the player just registers on this event here and gets notified whenever any
        /// <see cref="InteractableObject"/> is no longer being hovered over. The player must make the distinction
        /// whether it is interested in this <see cref="InteractableObject"/> at all.
        /// Intended for actions to be executed only locally.
        ///
        /// Note: This event is declared static so that it is independent of a particular
        /// <see cref="InteractableObject"/.
        /// </summary>
        public static event LocalPlayerHoverAction LocalAnyHoverOut;

        /// ----------------------------
        /// Selection event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a selection event has happened (selecting
        /// or deselecting the game object). Intended for multiplayer actions.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        public delegate void MultiPlayerSelectAction(InteractableObject interactableObject, bool isInitiator);

        public delegate void MulitPlayerReplaceSelectAction(List<InteractableObject> replaced,
            List<InteractableObject> by, bool isInitiator);

        /// <summary>
        /// A delegate to be called when a selection event has happened (selecting
        /// or deselecting the game object). Intended for actions of a local player only.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        public delegate void LocalPlayerSelectAction(InteractableObject interactableObject);

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is being selected.
        /// Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerSelectAction SelectIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is no longer selected.
        /// Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerSelectAction SelectOut;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is being selected.
        /// Intended for multiplayer actions.
        /// </summary>
        public static event MultiPlayerSelectAction AnySelectIn;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is no longer selected.
        /// Intended for multiplayer actions.
        /// </summary>
        public static event MultiPlayerSelectAction AnySelectOut;

        public static event MulitPlayerReplaceSelectAction ReplaceSelect;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is being selected.
        /// Intended for actions of only the local player.
        /// </summary>
        public event LocalPlayerSelectAction LocalSelectIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is not longer selected.
        /// Intended for actions of only the local player.
        /// </summary>
        public event LocalPlayerSelectAction LocalSelectOut;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is being selected.
        /// Intended for actions of only the local player.
        /// </summary>
        public static event LocalPlayerSelectAction LocalAnySelectIn;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is no longer selected.
        /// Intended for actions of only the local player.
        /// </summary>
        public static event LocalPlayerSelectAction LocalAnySelectOut;

        /// ----------------------------
        /// Grabbing event system
        /// ----------------------------
        /// <summary>
        /// A delegate to be called when a grab event has happened (grabbing
        /// or releasing the game object). Intended for multiplayer actions.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed (or no longer grabbed)</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        public delegate void MultiPlayerGrabAction(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// A delegate to be called when a grab event has happened (grabbing
        /// or releasing the game object). Intended for actions of the local player only.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed (or no longer grabbed)</param>
        public delegate void LocalPlayerGrabAction(InteractableObject interactableObject);

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is being grabbed.
        /// Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerGrabAction GrabIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is no longer grabbed.
        /// Intended for multiplayer actions.
        /// </summary>
        public event MultiPlayerGrabAction GrabOut;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is being grabbed.
        /// Intended for multiplayer actions.
        /// </summary>
        public static event MultiPlayerGrabAction AnyGrabIn;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is no longer grabbed.
        /// Intended for multiplayer actions.
        /// </summary>
        public static event MultiPlayerGrabAction AnyGrabOut;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is being grabbed.
        /// Intended for actions of the local player only.
        /// </summary>
        public event LocalPlayerGrabAction LocalGrabIn;

        /// <summary>
        /// Event to be triggered when this particular <see cref="InteractableObject"/> is being grabbed.
        /// Intended for actions of the local player only.
        /// </summary>
        public event LocalPlayerGrabAction LocalGrabOut;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is being grabbed.
        /// Intended for actions of the local player only.
        /// </summary>
        public static event LocalPlayerGrabAction LocalAnyGrabIn;

        /// <summary>
        /// Event to be triggered when any instance of <see cref="InteractableObject"/> is no longer grabbed.
        /// Intended for actions of the local player only.
        /// </summary>
        public static event LocalPlayerGrabAction LocalAnyGrabOut;

        //----------------------------------------------------------------
        // Mouse actions
        //----------------------------------------------------------------

        /// <summary>
        /// The mouse cursor entered a GUIElement or Collider.
        /// </summary>
        private void OnMouseEnter()
        {
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer && !Raycasting.IsMouseOverGUI() && IsInteractable())
            {
                SetHoverFlag(HoverFlag.World, true, true);
            }
        }

        /// <summary>
        /// The mouse cursor is still positioned above a GUIElement or Collider in this frame.
        /// If the <see cref="Hoverflag.World"/> flag is set, but we are currently hovering over the GUI,
        /// we need to reset the <see cref="Hoverflag.World"/> flag to false.
        /// If the <see cref="Hoverflag.World"/> flag is not set and we are not hovering over the GUI,
        /// we need to set the <see cref="Hoverflag.World"/> flag to true again.
        /// </summary>
        private void OnMouseOver()
        {
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
            {
                bool isFlagSet = IsHoverFlagSet(HoverFlag.World);
                bool isMouseOverGUI = Raycasting.IsMouseOverGUI();
                bool isInteractable = IsInteractable();
                if (isFlagSet && (isMouseOverGUI || !isInteractable))
                {
                    // If the Hoverflag.World flag is set, but we are currently hovering over the GUI,
                    // we need to reset the Hoverflag.World flag to false.
                    SetHoverFlag(HoverFlag.World, false, true);
                }
                else if (!isFlagSet && !isMouseOverGUI && isInteractable)
                {
                    // If the Hoverflag.World flag is not set and no longer hovering over the GUI,
                    // we need to set the Hoverflag.World flag to true again.
                    SetHoverFlag(HoverFlag.World, true, true);
                }
            }
        }

        /// <summary>
        /// The mouse cursor left a GUIElement or Collider.
        /// </summary>
        private void OnMouseExit()
        {
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer
                && IsHoverFlagSet(HoverFlag.World))
            {
                SetHoverFlag(HoverFlag.World, false, true);
            }
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        // <see cref="Hand.Update"/>
        //----------------------------------------------------------------
#if INCLUDE_STEAM_VR
        private const Hand.AttachmentFlags AttachmentFlags
            = Hand.defaultAttachmentFlags
            & ~Hand.AttachmentFlags.SnapOnAttach
            & ~Hand.AttachmentFlags.DetachOthers
            & ~Hand.AttachmentFlags.VelocityMovement;

        private void OnHandHoverBegin(Hand hand) => SetHoverFlag(HoverFlag.World, true, true);
        private void OnHandHoverEnd(Hand hand) => SetHoverFlag(HoverFlag.World, false, true);
#endif

        #endregion
    }
}
