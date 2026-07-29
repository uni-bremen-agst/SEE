namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Identifies the role of one step in an exception-flow path.
    /// </summary>
    internal enum ExceptionFlowPathStepKind
    {
        /// <summary>
        /// The documented member from which exception-flow analysis starts.
        /// </summary>
        RootMember,

        /// <summary>
        /// A method invocation followed during transitive analysis.
        /// </summary>
        MethodCall,

        /// <summary>
        /// A directly invoked local function.
        /// </summary>
        LocalFunctionCall,

        /// <summary>
        /// An invocation through a delegate whose concrete target was
        /// resolved.
        /// </summary>
        DelegateInvocation,

        /// <summary>
        /// A constructor invocation followed during transitive analysis.
        /// </summary>
        ConstructorCall,

        /// <summary>
        /// A property getter followed during transitive analysis.
        /// </summary>
        PropertyGetter,

        /// <summary>
        /// A property setter followed during transitive analysis.
        /// </summary>
        PropertySetter,

        /// <summary>
        /// A property init accessor followed during transitive analysis.
        /// </summary>
        PropertyInit,

        /// <summary>
        /// An indexer getter followed during transitive analysis.
        /// </summary>
        IndexerGetter,

        /// <summary>
        /// An indexer setter followed during transitive analysis.
        /// </summary>
        IndexerSetter,

        /// <summary>
        /// An indexer init accessor followed during transitive analysis.
        /// </summary>
        IndexerInit,

        /// <summary>
        /// A custom event add accessor followed during transitive analysis.
        /// </summary>
        EventAdd,

        /// <summary>
        /// A custom event remove accessor followed during transitive analysis.
        /// </summary>
        EventRemove,

        /// <summary>
        /// A user-defined unary operator followed during transitive analysis.
        /// </summary>
        UnaryOperatorCall,

        /// <summary>
        /// A user-defined binary operator followed during transitive analysis.
        /// </summary>
        BinaryOperatorCall,

        /// <summary>
        /// A user-defined conversion operator followed during transitive
        /// analysis.
        /// </summary>
        ConversionOperatorCall,

        /// <summary>
        /// An implicit synchronous disposal call produced by a using
        /// statement or using declaration.
        /// </summary>
        DisposeCall,

        /// <summary>
        /// An implicit asynchronous disposal call produced by an await-using
        /// statement or declaration.
        /// </summary>
        DisposeAsyncCall,

        /// <summary>
        /// An explicit throw statement or throw expression.
        /// </summary>
        ExplicitThrow,

        /// <summary>
        /// A modeled framework helper that throws an exception.
        /// </summary>
        FrameworkThrowHelper,

        /// <summary>
        /// An exception object supplied through a delegate factory and thrown
        /// by the callee.
        /// </summary>
        DelegateExceptionFactory
    }
}
