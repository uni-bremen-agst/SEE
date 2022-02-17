using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// Base class for dissonance room triggers
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public abstract class BaseCommsTrigger
        : MonoBehaviour, IAccessTokenCollection
    {
        #region fields and properties
        protected readonly Log Log;

        /// <summary>
        /// Get or set if this comms trigger should use a unity collider trigger volume.
        /// Override to serialise the value and to implement custom logic to enable and disable collider triggers.
        /// </summary>
        public abstract bool UseColliderTrigger { get; set; }

        /// <summary>
        /// Get or set if this comms trigger should use a unity collider trigger volume.
        /// </summary>
        [Obsolete("Replaced with UseColliderTrigger")]
        public bool UseTrigger
        {
            get { return UseColliderTrigger; }
            set { UseColliderTrigger = value; }
        }

        /// <summary>
        /// Get or set if this comms trigger can currently be activated.
        /// Override to serialise the value and to implement custom logic to disable the trigger.
        /// </summary>
        public abstract bool CanTrigger { get; }

        private bool _wasColliderTriggered;
        /// <summary>
        /// Get a value indicating if the collider is currently triggered (i.e. a valid entity is inside the collider volume)
        /// </summary>
        public bool IsColliderTriggered
        {
            get { return UseColliderTrigger && _entitiesInCollider.Count > 0; }
        }

        private readonly List<GameObject> _entitiesInCollider = new List<GameObject>(64);

        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField]private TokenSet _tokens = new TokenSet();
        private bool? _cachedTokenActivation;

        /// <summary>
        /// Get the set of tokens this trigger requires (trigger will only function if the local player knows at least one of the tokens)
        /// </summary>
        public IEnumerable<string> Tokens { get { return _tokens; } }

        private DissonanceComms _comms;
        /// <summary>
        /// Get the DissonanceComms component this trigger is controlling
        /// </summary>
        protected DissonanceComms Comms
        {
            get { return _comms; }
            private set
            {
                if (_comms != null)
                {
                    _comms.TokenAdded -= TokensModified;
                    _comms.TokenRemoved -= TokensModified;
                }

                _comms = value;

                if (_comms != null)
                {
                    _comms.TokenAdded += TokensModified;
                    _comms.TokenRemoved += TokensModified;
                }
            }
        }
        #endregion

        protected BaseCommsTrigger()
        {
            Log = Logs.Create(LogCategory.Core, GetType().Name);
        }

        #region unity lifecycle
        [UsedImplicitly] protected virtual void Awake()
        {
            _tokens.TokenAdded += TokensModified;
            _tokens.TokenRemoved += TokensModified;
            _cachedTokenActivation = null;
        }

        [UsedImplicitly] protected virtual void Start()
        {
        }

        [UsedImplicitly] protected virtual void OnEnable()
        {
            if (Comms == null)
                Comms = FindLocalVoiceComm();
        }

        [UsedImplicitly] protected virtual void Update()
        {
            //If we have no voice comms component then we can't do anything useful
            if (!CheckVoiceComm())
                return;

            //Remove items which triggered the collider trigger but died before leaving the collider
            for (var i = _entitiesInCollider.Count - 1; i >= 0; i--)
            {
                var thing = _entitiesInCollider[i];
                if (!thing || !thing.gameObject.activeInHierarchy)
                    _entitiesInCollider.RemoveAt(i);
            }

            //Invoke the triggered events if we're in collider trigger mode
            if (UseColliderTrigger)
            {
                if (_wasColliderTriggered != IsColliderTriggered)
                    ColliderTriggerChanged();

                _wasColliderTriggered = IsColliderTriggered;
            }
        }

        [UsedImplicitly] protected virtual void OnDisable()
        {
        }

        [UsedImplicitly] protected virtual void OnDestroy()
        {
            //Set comms to null, this will unsubscribe from event handlers on the comms object
            Comms = null;
        }
        #endregion

        #region tokens
        /// <summary>
        /// A token was either added or removed from this trigger or from the set of tokens the player holds
        /// </summary>
        /// <param name="token"></param>
        protected virtual void TokensModified(string token)
        {
            _cachedTokenActivation = null;
        }

        /// <summary>
        /// Get whether the trigger may be activated based on the tokens the trigger requires and the tokens the local player holds.
        /// </summary>
        protected bool TokenActivationState
        {
            get
            {
                if (!_cachedTokenActivation.HasValue)
                {
                    _cachedTokenActivation = _tokens.Count == 0 || Comms.HasAnyToken(_tokens);
                    Log.Debug("Recalculating token activation: {0} tokens, activated: {1}", _tokens.Count, _cachedTokenActivation.Value);
                }

                return _cachedTokenActivation.Value;
            }
        }

        /// <summary>
        /// Test if this trigger can be activated with the given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool ContainsToken(string token)
        {
            return _tokens.ContainsToken(token);
        }

        /// <summary>
        /// Add a token to the set for this trigger
        /// </summary>
        /// <param name="token"></param>
        /// <returns>A value indicating if the given token was added (false if the set already contained the token)</returns>
        public bool AddToken(string token)
        {
            return _tokens.AddToken(token);
        }

        /// <summary>
        /// Remove a token from the set for this trigger
        /// </summary>
        /// <param name="token"></param>
        /// <returns>A value indicating if the given token was removed (false if the set never contained the token in the first place)</returns>
        public bool RemoveToken(string token)
        {
            return _tokens.RemoveToken(token);
        }
        #endregion

        #region collider trigger
        /// <summary>
        /// Called every time the state of the collider trigger changes
        /// </summary>
        protected virtual void ColliderTriggerChanged()
        {

        }

        [UsedImplicitly] private void OnTriggerEnter2D([NotNull] Collider2D other)
        {
            if (other == null) throw new ArgumentNullException("other");

            if (ColliderTriggerFilter2D(other) && !_entitiesInCollider.Contains(other.gameObject))
            {   
                _entitiesInCollider.Add(other.gameObject);
                Log.Debug("Collider2D entered ({0})", _entitiesInCollider.Count);
            }
        }

        [UsedImplicitly] private void OnTriggerExit2D([NotNull] Collider2D other)
        {
            if (other == null) throw new ArgumentNullException("other");

            if (_entitiesInCollider.Remove(other.gameObject))
                Log.Debug("Collider2D exited ({0})", _entitiesInCollider.Count);
        }

        [UsedImplicitly] private void OnTriggerEnter([NotNull] Collider other)
        {
            if (other == null) throw new ArgumentNullException("other");

            if (ColliderTriggerFilter(other) && !_entitiesInCollider.Contains(other.gameObject))
            {   
                _entitiesInCollider.Add(other.gameObject);
                Log.Debug("Collider entered ({0})", _entitiesInCollider.Count);
            }
        }

        [UsedImplicitly] private void OnTriggerExit([NotNull] Collider other)
        {
            if (other == null) throw new ArgumentNullException("other");

            if (_entitiesInCollider.Remove(other.gameObject))
                Log.Debug("Collider exited ({0})", _entitiesInCollider.Count);
        }

        /// <summary>
        /// When something affects the trigger (enter or exit) it will only affect the trigger state of this component if this filter returns true.
        /// May be overriden to filter which entities should trigger. Default behaviour returns true if the entity is the local dissonance player, otherwise false
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this entity should affect the trigger, otherwise false</returns>
        protected virtual bool ColliderTriggerFilter([NotNull] Collider other)
        {
            if (other == null) throw new ArgumentNullException("other");

            var player = other.GetComponent<IDissonancePlayer>();
            return player != null && player.Type == NetworkPlayerType.Local;
        }

        /// <summary>
        /// When something affects the trigger (enter or exit) it will only affect the trigger state of this component if this filter returns true.
        /// May be overriden to filter which entities should trigger. Default behaviour returns true if the entity is the local dissonance player, otherwise false
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this entity should affect the trigger, otherwise false</returns>
        protected virtual bool ColliderTriggerFilter2D([NotNull] Collider2D other)
        {
            if (other == null) throw new ArgumentNullException("other");

            var player = other.GetComponent<IDissonancePlayer>();
            return player != null && player.Type == NetworkPlayerType.Local;
        }
        #endregion

        [CanBeNull] private DissonanceComms FindLocalVoiceComm()
        {
            //Assume it's attached as a sibling component and get it directly. this is fairly cheap (although obviously not going to work in a lot of cases).
            var comm = GetComponent<DissonanceComms>();

            //If we didn't manage to get it, find it instead (a much more expensive operation).
            if (comm == null)
                comm = FindObjectOfType<DissonanceComms>();

            return comm;
        }

        protected bool CheckVoiceComm()
        {
            //This is ugly, but correct. Comms == null is *not* correct!
            //Unity returns true for a null check if an object is merely disposed.
            //In some cases (disable and destroy) Comms may reasonably be disposed but not null!
            var missing = ReferenceEquals(Comms, null);

            //If we didn't find it, try to find it before sending a warning
            if (missing)
            {
                Comms = FindLocalVoiceComm();
                missing = ReferenceEquals(Comms, null);
            }

            if (missing)
            {
                Log.Error(Log.UserErrorMessage(
                    "Cannot find DissonanceComms component in scene",
                    "Created a Dissonance trigger component without putting a DissonanceComms component into the scene first",
                    "https://placeholder-software.co.uk/dissonance/docs/Basics/Getting-Started.html",
                    "FFB753E0-AC31-40AF-848B-234932B2155B"
                ));
            }

            return !missing;
        }
    }
}
