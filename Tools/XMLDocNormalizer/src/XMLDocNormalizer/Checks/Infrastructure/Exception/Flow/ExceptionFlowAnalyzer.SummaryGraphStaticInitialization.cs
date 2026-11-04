using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

internal static partial class ExceptionFlowAnalyzer
{
    // This is coverage evidence only: no initialization state, new call edges,
    // wrapper sources, or sharing of closed generic successes.
    private static bool HasInitializationEntry(IMethodSymbol method) =>
        method.MethodKind != MethodKind.StaticConstructor &&
        (method.IsStatic || method.MethodKind == MethodKind.Constructor || method.ContainingType.IsValueType);

    private static void ObserveStaticInitializationEntry(ISymbol symbol,
        ExceptionFlowSemanticEnvironment environment, ExceptionFlowSummaryFragment fragment)
    {
        if (symbol is not IMethodSymbol method) return;
        var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
            ?? method.ContainingType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax is null || !environment.TryGetSemanticModel(syntax.SyntaxTree, out var model)) return;
        if (method.MethodKind == MethodKind.StaticConstructor)
        {
            // Its own body is already the query subject, not an external
            // invocation of .cctor. Account only for omitted preceding initializers.
            if (!SafeSourceInitialization(method.ContainingType, environment, ignoreConstructorBody: true))
                AddInitializationGap(fragment, method.ContainingType, syntax, "static initializer coverage");
        }
        else if (HasInitializationEntry(method))
            ObserveInitializationType(method.ContainingType, syntax, "callable entry", model, environment, fragment);
    }

    private static void ObserveStaticInitializationOperations(SyntaxNode node, SemanticModel model,
        ExceptionFlowSemanticEnvironment environment, ExceptionFlowSummaryFragment fragment)
    {
        IOperation? root = model.GetOperation(node);
        if (root is null) return;
        HashSet<INamedTypeSymbol> inspected = new(SymbolEqualityComparer.Default);
        Stack<IOperation> pending = new();
        pending.Push(root);
        while (pending.TryPop(out var operation))
        {
            if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation or INameOfOperation or ITypeOfOperation
                || operation is ITryOperation) continue;
            INamedTypeSymbol? type = operation switch
            {
                IFieldReferenceOperation field when field.Field.IsStatic && !field.Field.IsConst => field.Field.ContainingType,
                IInvocationOperation call when HasInitializationEntry(call.TargetMethod) => call.TargetMethod.ContainingType,
                IObjectCreationOperation creation => creation.Constructor?.ContainingType,
                IPropertyReferenceOperation property when property.Property.IsStatic => property.Property.ContainingType,
                IEventReferenceOperation @event when @event.Event.IsStatic => @event.Event.ContainingType,
                IBinaryOperation binary => binary.OperatorMethod?.ContainingType,
                IUnaryOperation unary => unary.OperatorMethod?.ContainingType,
                IConversionOperation conversion => conversion.OperatorMethod?.ContainingType,
                IIncrementOrDecrementOperation increment => increment.OperatorMethod?.ContainingType,
                ICompoundAssignmentOperation assignment => assignment.OperatorMethod?.ContainingType,
                _ => null
            };
            if (type is not null && inspected.Add(type.OriginalDefinition))
                ObserveInitializationType(type, operation.Syntax, operation.Kind.ToString(), model, environment, fragment);
            foreach (var child in operation.ChildOperations) pending.Push(child);
        }
    }

    private static void ObserveStaticInitializationTargets(ExceptionFlowSummaryFragment fragment, SemanticModel model,
        ExceptionFlowSemanticEnvironment environment)
    {
        // Includes actual dispatch/accessor/implicit targets, rather than only
        // the lexical operation target. Definition safety is universal in binders.
        HashSet<INamedTypeSymbol> inspected = new(SymbolEqualityComparer.Default);
        foreach (var edge in fragment.CallEdges)
            if (edge.Target.Symbol is IMethodSymbol method && HasInitializationEntry(method)
                && inspected.Add(method.ContainingType.OriginalDefinition)
                && !SafeInitialization(method.ContainingType, model.Compilation, environment))
                fragment.StaticInitializationGaps.Add("StaticInitializationCoverageMissing / bound target / " +
                    ExceptionFlowFiniteOrbitAdmission.BodyId(method.ContainingType) + " / " + edge.CallSiteStep.FilePath + ":" + edge.CallSiteStep.Line);
    }

    private static void ObserveInitializationType(INamedTypeSymbol type, SyntaxNode site, string kind, SemanticModel model,
        ExceptionFlowSemanticEnvironment environment, ExceptionFlowSummaryFragment fragment)
    {
        if (!SafeInitialization(type, model.Compilation, environment)) AddInitializationGap(fragment, type, site, kind);
    }

    private static void AddInitializationGap(ExceptionFlowSummaryFragment fragment, INamedTypeSymbol type, SyntaxNode site, string kind)
    {
        var position = site.GetLocation().GetLineSpan().StartLinePosition;
        fragment.StaticInitializationGaps.Add("StaticInitializationCoverageMissing / " + kind + " / " +
            ExceptionFlowFiniteOrbitAdmission.BodyId(type) + " / " + site.SyntaxTree.FilePath + ":" + position.Line + ":" + position.Character);
    }

    private static bool SafeInitialization(INamedTypeSymbol type, Compilation binding,
        ExceptionFlowSemanticEnvironment environment)
    {
        type = type.OriginalDefinition;
        if (!type.DeclaringSyntaxReferences.IsEmpty
            && environment.TryGetSemanticModel(type.DeclaringSyntaxReferences[0].SyntaxTree, out var declaredModel)
            && ReferenceEquals(declaredModel.Compilation.Assembly, type.ContainingAssembly))
            return SafeSourceInitialization(type, environment);
        // Exact supporting source wins over incomplete reference metadata.
        foreach (var scope in environment.GetAnalysisScopes())
        {
            var source = ExceptionFlowCrossCompilationResolver.ResolveNamedType(type, scope.Compilation);
            if (source is not null && ReferenceEquals(source.ContainingAssembly, scope.Compilation.Assembly)
                && !source.DeclaringSyntaxReferences.IsEmpty) return SafeSourceInitialization(source, environment);
        }
        bool absent = MetadataHasNoInitializer(type, binding);
        if (!absent) environment.ExcludeAdmissionReuse();
        return absent;
    }

    private static bool SafeSourceInitialization(INamedTypeSymbol type, ExceptionFlowSemanticEnvironment environment,
        bool ignoreConstructorBody = false)
    {
        if (type.DeclaringSyntaxReferences.IsEmpty) return false;
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            var declaration = reference.GetSyntax();
            if (!environment.TryGetSemanticModel(declaration.SyntaxTree, out var model)
                || !ReferenceEquals(model.GetDeclaredSymbol(declaration)?.ContainingAssembly, type.ContainingAssembly)) return false;
            if (declaration is not TypeDeclarationSyntax body) return false;
            foreach (var member in body.Members)
            {
                if (member is ConstructorDeclarationSyntax constructor && constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    if (!ignoreConstructorBody && (constructor.ExpressionBody is not null || constructor.Body is null
                        || constructor.Body.Statements.Any(statement => statement is not EmptyStatementSyntax))) return false;
                }
                else if (member is BaseFieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    foreach (var variable in field.Declaration.Variables)
                        if (variable.Initializer is not null && !SafeInitializationLiteral(variable.Initializer.Value,
                            model.GetDeclaredSymbol(variable) switch { IFieldSymbol symbol => symbol.Type,
                                IEventSymbol symbol => symbol.Type, _ => null }, model)) return false;
                }
                else if (member is PropertyDeclarationSyntax property && property.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && property.Initializer is not null && !SafeInitializationLiteral(property.Initializer.Value,
                        (model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type, model)) return false;
            }
        }
        return true;
    }

    private static bool SafeInitializationLiteral(ExpressionSyntax expression, ITypeSymbol? target, SemanticModel model)
    {
        // Parentheses and nullable suppression add no executable operation.
        while (expression is ParenthesizedExpressionSyntax || expression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            expression = expression is ParenthesizedExpressionSyntax parentheses ? parentheses.Expression
                : ((PostfixUnaryExpressionSyntax)expression).Operand;
        if (target?.IsReferenceType == true && (expression.IsKind(SyntaxKind.NullLiteralExpression)
            || expression.IsKind(SyntaxKind.DefaultLiteralExpression)
            || expression is DefaultExpressionSyntax && model.GetTypeInfo(expression).Type?.IsReferenceType == true))
        {
            var nullConversion = model.ClassifyConversion(expression, target);
            return nullConversion.Exists && nullConversion.IsImplicit && !nullConversion.IsUserDefined;
        }
        if (target is null || target.SpecialType is not (SpecialType.System_Boolean or SpecialType.System_Char
            or SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_String or SpecialType.System_Object)) return false;
        if (expression is not LiteralExpressionSyntax && expression is not DefaultExpressionSyntax) return false;
        var conversion = model.ClassifyConversion(expression, target);
        return conversion.Exists && !conversion.IsUserDefined && (conversion.IsIdentity || conversion.IsImplicit)
            && !conversion.IsBoxing;
    }

    private static bool MetadataHasNoInitializer(INamedTypeSymbol type, Compilation binding)
    {
        if (binding.GetMetadataReference(type.ContainingAssembly) is not PortableExecutableReference reference) return false;
        try
        {
            var metadata = reference.GetMetadata();
            var modules = metadata is AssemblyMetadata assembly ? assembly.GetModules() : default;
            if (modules.IsDefaultOrEmpty) return false;
            if (!ReferenceEquals(binding.GetAssemblyOrModuleSymbol(reference), type.ContainingAssembly)) return false;
            // Check the whole assembly marker, not a filtered symbol member list.
            var manifest = modules[0].GetMetadataReader();
            foreach (var handle in manifest.GetAssemblyDefinition().GetCustomAttributes())
                if (AttributeTypeName(manifest, manifest.GetCustomAttribute(handle).Constructor) ==
                    "System.Runtime.CompilerServices.ReferenceAssemblyAttribute") return false;
            string wanted = MetadataTypeName(type);
            int matches = 0;
            foreach (var module in modules)
            {
                var reader = module.GetMetadataReader();
                foreach (var handle in reader.TypeDefinitions)
                {
                    var definition = reader.GetTypeDefinition(handle);
                    if (reader.GetString(definition.Name) != type.MetadataName || MetadataTypeName(reader, handle) != wanted) continue;
                    matches++;
                    foreach (var method in definition.GetMethods())
                        if (reader.StringComparer.Equals(reader.GetMethodDefinition(method).Name, ".cctor")) return false;
                }
            }
            return matches == 1;
        }
        catch (BadImageFormatException) { return false; }
    }

    private static string MetadataTypeName(INamedTypeSymbol type)
        => type.ContainingType is { } parent ? MetadataTypeName(parent) + "+" + type.MetadataName
            : (type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + ".") + type.MetadataName;

    private static string MetadataTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        if (!type.GetDeclaringType().IsNil) return MetadataTypeName(reader, type.GetDeclaringType()) + "+" + reader.GetString(type.Name);
        string space = reader.GetString(type.Namespace);
        return (space.Length == 0 ? "" : space + ".") + reader.GetString(type.Name);
    }

    private static string AttributeTypeName(MetadataReader reader, EntityHandle constructor)
    {
        EntityHandle owner = constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => default
        };
        if (owner.Kind == HandleKind.TypeDefinition) return MetadataTypeName(reader, (TypeDefinitionHandle)owner);
        if (owner.Kind != HandleKind.TypeReference) return "";
        var type = reader.GetTypeReference((TypeReferenceHandle)owner);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
}

/// <summary>
/// C3G's finite source family. Normal continuation is separate from complete
/// initialization coverage. No context queue, CLR state cache or new framework contract.
/// </summary>
internal static class ExceptionFlowBoundedLowerWitness
{
    // Immutable definition recipe. It carries symbolic return and source bindings,
    // not an evaluated null argument, root path or successful CLR initialization.
    internal sealed record Recipe(IMethodSymbol Definition, Compilation Compilation,
        IFieldSymbol ReceiverField, IMethodSymbol Getter, ITypeSymbol DefaultReturn,
        IMethodSymbol Validator, InvocationExpressionSyntax ValidatorSite,
        IMethodSymbol Guard, InvocationExpressionSyntax GuardSite);

    // Fresh root evidence. This is never part of a reusable definition snapshot.
    internal sealed record Witness(Recipe Schema, ITypeSymbol ClosedReturn, ExceptionFlowSummarySource Source);

    internal static Recipe? CreateRecipe(ISymbol symbol, ExceptionFlowSemanticEnvironment environment)
    {
        if (symbol is not IMethodSymbol { IsStatic: true, ReturnsVoid: true, MethodKind: MethodKind.Ordinary,
                Parameters.Length: 0, Arity: 0, IsAsync: false } root
            || ExceptionFlowInvocationSubstitution.Bindings(root).Any()
            || !OwnedMethod(root, environment, out var method, out var model)
            || method.Body is not { } body || method.AttributeLists.Count != 0) return null;

        CatchClauseSyntax? caught = null;
        if (body.Statements.Count == 1 && body.Statements[0] is TryStatementSyntax guarded)
        {
            if (guarded.Finally != null || guarded.Catches.Count != 1) return null;
            caught = guarded.Catches[0];
            if (caught.Filter != null || caught.Block.Statements.Count != 0) return null;
            body = guarded.Block;
        }
        if (body.Statements.Count != 2 || body.Statements[0] is not LocalDeclarationStatementSyntax local
            || local.UsingKeyword != default || local.AwaitKeyword != default
            || local.Declaration.Variables.Count != 1 || local.Declaration.Variables[0] is not { Initializer: { } initializer } variable
            || body.Statements[1] is not ExpressionStatementSyntax statement
            || ReferenceValue(model.GetOperation(initializer.Value)) is not IInvocationOperation getter
            || ReferenceValue(model.GetOperation(statement.Expression)) is not IInvocationOperation validation
            || getter.Instance is not { } receiver || ReferenceValue(receiver) is not IFieldReferenceOperation fieldRead
            || getter.Syntax is not InvocationExpressionSyntax || validation.Syntax is not InvocationExpressionSyntax validationSite
            || model.GetDeclaredSymbol(variable) is not ILocalSymbol value || value.RefKind != RefKind.None) return null;

        var field = fieldRead.Field;
        var compilation = model.Compilation;
        if (!field.IsStatic || !field.IsReadOnly || !Same(field.ContainingType, root.ContainingType)
            || !OwnedType(root.ContainingType, environment, compilation, out var owner)
            || !SingleInitializer(owner, field, model, out var creation)
            || creation.Type is not INamedTypeSymbol allocated || !Same(allocated, field.Type)
            || creation.Arguments.Length != 0 || creation.Initializer != null
            || !EmptySourceAllocation(creation, environment, compilation)
            || !Same(getter.TargetMethod.ContainingType, allocated)
            || getter.TargetMethod.IsStatic || getter.TargetMethod.IsVirtual || getter.TargetMethod.IsOverride
            || getter.TargetMethod.IsAbstract || getter.TargetMethod.IsAsync || getter.TargetMethod.Arity != 0
            || getter.TargetMethod.ReturnsByRef || getter.TargetMethod.ReturnsByRefReadonly
            || getter.TargetMethod.Parameters.Any(parameter => parameter.RefKind != RefKind.None)
            || !OwnedMethod(getter.TargetMethod, environment, out var getterSource, out var getterModel)
            || !ReferenceEquals(getterModel.Compilation, compilation) || getterSource.AttributeLists.Count != 0
            || !FixedReturn(getterSource, out var returned) || getter.Arguments.Any(argument =>
                argument.Parameter == null || argument.Parameter.RefKind != RefKind.None
                || !NormalArgument(argument.Value, environment, compilation))) return null;

        if (!Same(value.Type, model.GetTypeInfo(initializer.Value).ConvertedType)
            || !ReferenceConversion(model, initializer.Value, value.Type)
            || !validation.TargetMethod.IsStatic || !validation.TargetMethod.ReturnsVoid || validation.TargetMethod.IsAsync
            || validation.TargetMethod.Arity != 0 || validation.TargetMethod.Parameters.Length != 1
            || validation.TargetMethod.Parameters[0].RefKind != RefKind.None || validation.Arguments.Length != 1
            || ReferenceValue(validation.Arguments[0].Value) is not ILocalReferenceOperation passed
            || !Same(passed.Local, value) || validation.Arguments[0].Parameter?.Ordinal != 0
            || !OwnedMethod(validation.TargetMethod, environment, out var validatorSource, out var validatorModel)
            || !ReferenceEquals(validatorModel.Compilation, compilation) || validatorSource.AttributeLists.Count != 0
            || validatorSource.Body is not { Statements.Count: 1 } validatorBody
            || validatorBody.Statements[0] is not ExpressionStatementSyntax guardStatement
            || validatorModel.GetOperation(guardStatement.Expression) is not IInvocationOperation guard
            || guard.Syntax is not InvocationExpressionSyntax guardSite
            || !KnownFrameworkExceptionModel.IsArgumentNullThrowIfNull(guard.TargetMethod, compilation)
            || !ReferenceEquals(guard.TargetMethod.ContainingAssembly, compilation.ObjectType.ContainingAssembly)
            || guard.Arguments.Count(argument => argument.Parameter?.Ordinal == 0) != 1
            || ReferenceValue(guard.Arguments.Single(argument => argument.Parameter?.Ordinal == 0).Value)
                is not IParameterReferenceOperation parameter
            || parameter.Parameter.Ordinal != 0 || !Same(parameter.Parameter.ContainingSymbol, validation.TargetMethod)
            || guard.Arguments.Where(argument => argument.Parameter?.Ordinal != 0).Any(argument =>
                !argument.Value.ConstantValue.HasValue || argument.Value.ConstantValue.Value is not (null or string))) return null;

        if (!Same(validation.TargetMethod.ContainingType, root.ContainingType)
            && (!OwnedType(validation.TargetMethod.ContainingType, environment, compilation, out var validatorOwner)
                || !NoSourceInitialization(validatorOwner))) return null;
        if (caught != null && (caught.Declaration == null
            || model.GetTypeInfo(caught.Declaration.Type).Type is not INamedTypeSymbol caughtType
            || ExceptionFlowCaughtTypeFilter.IsSameOrDerivedFrom(guard.TargetMethod.ContainingType, caughtType))) return null;
        if (!Foundation(compilation, guard.TargetMethod.ContainingType))
        {
            environment.ExcludeAdmissionReuse();
            return null;
        }
        if (HasModuleInitializer(compilation, environment)) return null;

        // The fixed return is symbolic in its own declaration's binder frame.
        // Known null is established only by fresh, exact invocation composition.
        var returnInfo = getterModel.GetTypeInfo(returned);
        ITypeSymbol? returnTerm = returnInfo.Type ?? returnInfo.ConvertedType;
        // default(int) returned as object is boxed zero, not default(object).
        // The bounded recipe accepts only the declared return's own default/null.
        if (returnTerm == null || !Same(returnTerm, getter.TargetMethod.OriginalDefinition.ReturnType)) return null;
        if (returned.IsKind(SyntaxKind.NullLiteralExpression) && !getter.TargetMethod.ReturnType.IsReferenceType) return null;
        return new(root.OriginalDefinition, compilation, field, getter.TargetMethod, returnTerm,
            validation.TargetMethod, validationSite, guard.TargetMethod, guardSite);
    }

    internal static Witness? Compose(Recipe recipe, ISymbol root,
        ExceptionFlowSemanticEnvironment environment)
    {
        if (!Same(root, recipe.Definition) || ExceptionFlowInvocationSubstitution.Bindings(root).Any()
            || !ReferenceEquals(root.ContainingAssembly, recipe.Compilation.Assembly)
            || !environment.GetAnalysisScopes().Any(scope => ReferenceEquals(scope.Compilation, recipe.Compilation))) return null;
        var returned = new ExceptionFlowInvocationSubstitution(recipe.Getter, recipe.Compilation).Apply(recipe.DefaultReturn);
        // Unknown/None and boxed value defaults are not reference-null proofs.
        if (!returned.IsReferenceType || !Same(returned, recipe.Getter.ReturnType)) return null;
        var terminal = ExceptionFlowPathFactory.CreateTerminal(ExceptionFlowPathStepKind.FrameworkThrowHelper,
            recipe.Guard, recipe.GuardSite);
        var path = terminal.Prepend(ExceptionFlowPathFactory.CreateStep(ExceptionFlowPathStepKind.MethodCall,
            recipe.Validator, recipe.ValidatorSite));
        return new(recipe, returned, new(recipe.Guard.ContainingType, path));
    }

    private static bool Same(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);

    private static bool ReferenceConversion(SemanticModel model, ExpressionSyntax expression, ITypeSymbol target)
    {
        var conversion = model.ClassifyConversion(expression, target);
        return conversion.Exists && conversion.IsImplicit && !conversion.IsUserDefined
            && (conversion.IsIdentity || conversion.IsReference
                || expression.IsKind(SyntaxKind.NullLiteralExpression) && target.IsReferenceType);
    }

    private static IOperation? ReferenceValue(IOperation? operation)
    {
        while (true)
        {
            if (operation is IParenthesizedOperation parentheses) { operation = parentheses.Operand; continue; }
            if (operation is not IConversionOperation conversion) return operation;
            if (!conversion.IsImplicit || conversion.Conversion.IsUserDefined
                || !(conversion.Conversion.IsIdentity || conversion.Conversion.IsReference
                    || conversion.Operand.ConstantValue is { HasValue: true, Value: null } && conversion.Type?.IsReferenceType == true)) return null;
            operation = conversion.Operand;
        }
    }

    private static bool OwnedMethod(IMethodSymbol method, ExceptionFlowSemanticEnvironment environment,
        out MethodDeclarationSyntax syntax, out SemanticModel model)
    {
        syntax = null!; model = null!;
        if (method.OriginalDefinition.DeclaringSyntaxReferences is not { Length: 1 } references
            || references[0].GetSyntax() is not MethodDeclarationSyntax declaration
            || !environment.TryGetSemanticModel(declaration.SyntaxTree, out model)
            || !ReferenceEquals(method.ContainingAssembly, model.Compilation.Assembly)
            || !Same(model.GetDeclaredSymbol(declaration), method.OriginalDefinition)) return false;
        syntax = declaration; return true;
    }

    private static bool OwnedType(INamedTypeSymbol type, ExceptionFlowSemanticEnvironment environment,
        Compilation compilation, out ClassDeclarationSyntax syntax)
    {
        syntax = null!;
        if (type.OriginalDefinition.DeclaringSyntaxReferences is not { Length: 1 } references
            || references[0].GetSyntax() is not ClassDeclarationSyntax declaration
            || declaration.ParameterList != null || declaration.AttributeLists.Count != 0
            || !environment.TryGetSemanticModel(declaration.SyntaxTree, out var model)
            || !ReferenceEquals(model.Compilation, compilation)
            || !ReferenceEquals(type.ContainingAssembly, compilation.Assembly)
            || !Same(model.GetDeclaredSymbol(declaration), type.OriginalDefinition)) return false;
        syntax = declaration; return true;
    }

    private static bool SingleInitializer(ClassDeclarationSyntax owner, IFieldSymbol field, SemanticModel model,
        out IObjectCreationOperation creation)
    {
        creation = null!;
        if (owner.Members.OfType<ConstructorDeclarationSyntax>().Any(constructor => constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            || owner.Members.OfType<PropertyDeclarationSyntax>().Any(property => property.Modifiers.Any(SyntaxKind.StaticKeyword) && property.Initializer != null)
            || owner.Members.OfType<BaseFieldDeclarationSyntax>().Any(member => member.AttributeLists.Count != 0)) return false;
        var initialized = owner.Members.OfType<BaseFieldDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.StaticKeyword)
                && !member.Modifiers.Any(SyntaxKind.ConstKeyword)).SelectMany(member => member.Declaration.Variables)
            .Where(variable => variable.Initializer != null).ToArray();
        if (initialized.Length != 1 || !Same(model.GetDeclaredSymbol(initialized[0]), field)
            || ReferenceValue(model.GetOperation(initialized[0].Initializer!.Value)) is not IObjectCreationOperation allocated) return false;
        creation = allocated; return true;
    }

    private static bool EmptySourceAllocation(IObjectCreationOperation creation,
        ExceptionFlowSemanticEnvironment environment, Compilation compilation)
    {
        if (creation.Constructor is not { IsImplicitlyDeclared: true, Parameters.Length: 0 }
            || creation.Arguments.Length != 0 || creation.Initializer != null
            || creation.Type is not INamedTypeSymbol { IsSealed: true, IsAbstract: false } type
            || type.BaseType?.SpecialType != SpecialType.System_Object || !type.Interfaces.IsEmpty
            || type.TypeArguments.Any(argument => !KnownTypeArgument(argument, environment, compilation))
            || !OwnedType(type, environment, compilation, out var source)) return false;
        return source.Members.All(member => member is MethodDeclarationSyntax method && method.AttributeLists.Count == 0);
    }

    private static bool KnownTypeArgument(ITypeSymbol argument, ExceptionFlowSemanticEnvironment environment, Compilation compilation)
    {
        if (argument is not INamedTypeSymbol named) return false;
        if (named.SpecialType is SpecialType.System_Object or SpecialType.System_String or SpecialType.System_Int32)
            return ReferenceEquals(named.ContainingAssembly, compilation.ObjectType.ContainingAssembly);
        return named is { IsSealed: true, IsAbstract: false, Arity: 0 } && named.Interfaces.IsEmpty
            && named.BaseType?.SpecialType == SpecialType.System_Object
            && OwnedType(named, environment, compilation, out var source) && source.Members.Count == 0;
    }

    private static bool NormalArgument(IOperation? operation, ExceptionFlowSemanticEnvironment environment, Compilation compilation)
    {
        operation = ReferenceValue(operation);
        if (operation is ILiteralOperation) return true;
        if (operation is IDefaultValueOperation value)
            return value.Type?.IsReferenceType == true || value.Type?.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Decimal;
        if (operation is IObjectCreationOperation creation) return EmptySourceAllocation(creation, environment, compilation);
        return operation is IDelegateCreationOperation { Target: IAnonymousFunctionOperation anonymous, Type: INamedTypeSymbol delegateType }
            && anonymous.Symbol.IsStatic && delegateType.TypeKind == TypeKind.Delegate
            && ReferenceEquals(delegateType.ContainingAssembly, compilation.ObjectType.ContainingAssembly);
    }

    private static bool FixedReturn(MethodDeclarationSyntax method, out ExpressionSyntax expression)
    {
        expression = method.ExpressionBody?.Expression!;
        if (method.Body != null)
        {
            if (method.Body.Statements is not { Count: 1 } statements || statements[0] is not ReturnStatementSyntax returned) return false;
            expression = returned.Expression!;
        }
        while (expression is ParenthesizedExpressionSyntax parentheses) expression = parentheses.Expression;
        return expression != null && (expression.IsKind(SyntaxKind.DefaultLiteralExpression)
            || expression.IsKind(SyntaxKind.NullLiteralExpression) || expression is DefaultExpressionSyntax);
    }

    private static bool NoSourceInitialization(ClassDeclarationSyntax type)
        => !type.Members.OfType<ConstructorDeclarationSyntax>().Any(constructor => constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
            && !type.Members.OfType<BaseFieldDeclarationSyntax>().Any(field => field.Modifiers.Any(SyntaxKind.StaticKeyword)
                && !field.Modifiers.Any(SyntaxKind.ConstKeyword) && field.Declaration.Variables.Any(variable => variable.Initializer != null))
            && !type.Members.OfType<PropertyDeclarationSyntax>().Any(property => property.Modifiers.Any(SyntaxKind.StaticKeyword) && property.Initializer != null);

    private static bool HasModuleInitializer(Compilation compilation, ExceptionFlowSemanticEnvironment environment)
    {
        var marker = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute");
        foreach (var tree in compilation.SyntaxTrees)
        {
            var attributes = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().ToArray();
            if (attributes.Length == 0) continue;
            if (!environment.TryGetSemanticModel(tree, out var model)) return true;
            foreach (var attribute in attributes)
                if (model.GetSymbolInfo(attribute).Symbol is not IMethodSymbol constructor
                    || Same(constructor.ContainingType, marker)) return true;
        }
        return false;
    }

    private static bool Foundation(Compilation compilation, INamedTypeSymbol helperOwner)
    {
        var assembly = compilation.ObjectType.ContainingAssembly;
        if (!ReferenceEquals(assembly, helperOwner.ContainingAssembly)
            || compilation.GetMetadataReference(assembly) is not PortableExecutableReference reference
            || !string.Equals(reference.FilePath, typeof(object).Assembly.Location, StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            if (reference.GetMetadata() is not AssemblyMetadata metadata || metadata.GetModules() is not { Length: 1 } modules
                || !ReferenceEquals(compilation.GetAssemblyOrModuleSymbol(reference), assembly)) return false;
            var reader = modules[0].GetMetadataReader();
            if (reader.GetGuid(reader.GetModuleDefinition().Mvid) != typeof(object).Assembly.ManifestModule.ModuleVersionId
                || !string.Equals(assembly.Identity.ToString(), typeof(object).Assembly.GetName().FullName, StringComparison.Ordinal)) return false;
            foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(handle);
                EntityHandle parent = attribute.Constructor.Kind switch
                {
                    HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
                    HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                    _ => default
                };
                if (parent.Kind == HandleKind.TypeReference)
                {
                    var type = reader.GetTypeReference((TypeReferenceHandle)parent);
                    if (reader.GetString(type.Name) == "ReferenceAssemblyAttribute"
                        && reader.GetString(type.Namespace) == "System.Runtime.CompilerServices") return false;
                }
                else if (parent.Kind == HandleKind.TypeDefinition)
                {
                    var type = reader.GetTypeDefinition((TypeDefinitionHandle)parent);
                    if (reader.GetString(type.Name) == "ReferenceAssemblyAttribute"
                        && reader.GetString(type.Namespace) == "System.Runtime.CompilerServices") return false;
                }
                else return false;
            }
            foreach (string name in new[] { "Object", "ArgumentNullException" })
            {
                var types = reader.TypeDefinitions.Where(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == name
                    && reader.GetString(reader.GetTypeDefinition(handle).Namespace) == "System").ToArray();
                if (types.Length != 1 || reader.GetTypeDefinition(types[0]).GetMethods().Any(method =>
                    reader.GetString(reader.GetMethodDefinition(method).Name) == ".cctor")) return false;
            }
            var body = typeof(object).GetConstructor(Type.EmptyTypes)?.GetMethodBody()?.GetILAsByteArray();
            return body != null && body.Where(value => value != 0x00).SequenceEqual(new byte[] { 0x2A });
        }
        catch (System.Exception exception) when (exception is BadImageFormatException or InvalidOperationException or NotSupportedException)
        {
            return false;
        }
    }
}
