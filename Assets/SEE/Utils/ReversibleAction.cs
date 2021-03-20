namespace SEE.Utils
{
    /// <summary>
    /// Creates a new instance of <see cref="ReversibleAction"/>.
    /// </summary>
    /// <returns>new instance of <see cref="ReversibleAction"/>/returns>
    public delegate ReversibleAction CreateReversibleAction();

    /// <summary>
    /// Defines the expected operations and their protocol for actions that
    /// can be reversed (have Undo() and Redo()). Their protocol resembles 
    /// Unity's protocol for MonoBehaviours regarding <see cref="Awake"/>,
    /// <see cref="Start"/>, and <see cref="Update"/>.
    /// </summary>
    public interface ReversibleAction
    {
        // The protocol of the following operations over the lifecycle of a
        // reversible object is as follows (where "X . Y" means X is called
        // before Y and * denotes the Kleene star, i.e., refers to zero or
        // more repetitions):
        //
        //    Awake . Start . Update* . Stop
        //
        // The action signals its completion by returning true when Update is
        // called, in which case it will receive a Stop message and then
        // its lifecycle ends. If Update yields false instead, the lifecycle
        // continues.
        // The lifecycle may also be ended "from the outside", in which case
        // Stop will be called (no matter what the return value of Update is).
        //
        // When Undo is called, the action restores the state at the time
        // after Awake and before Start were called.
        //
        // When Redo is called, the action restores the state at the time
        // after Stop was called.
        //
        // Undo can be called only after Awake has been called before.
        // Redo can be called only after Undo was called.
        //
        // Let calls(X) denote the number of calls for method X, then
        //    0 <= calls(Undo) - calls(Redo) <= 1
        // holds as an invariant.

        /// <summary>
        /// Will be called exactly once before <see cref="Start"/> and any other method
        /// in this interface. Here code can be executed for intialization.
        /// </summary>
        void Awake();

        /// <summary>
        /// Will be called whenever the action is to start its execution.
        /// There are two different situations in which this is the case:
        /// (1) upon its intial start and (2) when it is resumed.
        /// Situation (1): <see cref="Start"/> will be called once after 
        /// <see cref="Awake"/> when it is executed the first time. 
        /// Situation (2): Then it will be called again each time it gets 
        /// re-activated because a previously executed action having superseding 
        /// it gets undone.
        /// Invariant: The first call to <see cref="Start"/> will take place
        /// before <see cref="Update"/> and after <see cref="Awake"/>.
        /// </summary>
        void Start();

        /// <summary>
        /// Will be called after <see cref="Start"/>. Can be called multiple times
        /// (e.g., once per frame) for continuously executing the action.
        /// </summary>
        /// <returns>true if action is completed</returns>
        bool Update();

        /// <summary>
        /// Will be called when the action is to stop executing. This is 
        /// generally the case when another action is superseding it.
        /// Invariant: A <see cref="Start"/> message has been received before.
        /// </summary>
        void Stop();

        /// <summary>
        /// Called when the effect of the action is to be reversed.
        /// </summary>
        void Undo();

        /// <summary>
        /// Called when the action was previously reversed by <see cref="Undo"/>
        /// to re-establish the effect that was undone.
        /// Precondition: <see cref="Undo"/> was called before.
        /// </summary>
        void Redo();

        /// <summary>
        /// True if this action has had an effect that may need to be undone. Actions may
        /// have been started but may not have had any effect yet because they were
        /// waiting for input, in which case they do not need to be undone. In this
        /// case, they will return false; otherwise true.
        /// </summary>
        /// <returns>if this action has had an effect that may need to be undone</returns>
        bool HadEffect();

        /// <summary>
        /// Returns a new instance of the same type as this particular type of ReversibleAction.
        /// </summary>
        /// <returns>new instance</returns>
        ReversibleAction NewInstance();
    }
}
