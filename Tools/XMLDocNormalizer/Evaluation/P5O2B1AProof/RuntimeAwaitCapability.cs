using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.B1AProof;

/// <summary>
/// Experimental light-up of exactly one API; not a productive compatibility owner.
/// Absence is unsupported, not a semantic null: historical runtime async exists.
/// </summary>
internal static class RuntimeAwaitCapability
{
    private delegate IMethodSymbol? RuntimeAwaitGetter(ref AwaitExpressionInfo info);

    private static readonly RuntimeAwaitGetter? getter = CreateGetter();

    internal static bool IsSupported => getter != null;

    internal static IMethodSymbol? GetMethod(AwaitExpressionInfo info)
        => Read(info, getter);

    private static IMethodSymbol? Read(AwaitExpressionInfo info, RuntimeAwaitGetter? accessor)
    {
        if (accessor == null)
        {
            throw new NotSupportedException(
                "RuntimeAwaitMethod is not exposed by this Roslyn. "
                + "Runtime async may still exist; absence cannot be treated as semantic null.");
        }

        return accessor(ref info);
    }

    private static RuntimeAwaitGetter? CreateGetter()
    {
        PropertyInfo? property = typeof(AwaitExpressionInfo).GetProperty(
            "RuntimeAwaitMethod", BindingFlags.Instance | BindingFlags.Public);
        if (property == null)
        {
            return null;
        }

        MethodInfo? method = property.GetMethod;
        if (property.PropertyType != typeof(IMethodSymbol)
            || property.GetIndexParameters().Length != 0
            || method == null || method.IsStatic || !method.IsPublic)
        {
            throw new NotSupportedException("Unexpected RuntimeAwaitMethod metadata contract.");
        }

        return method.CreateDelegate<RuntimeAwaitGetter>();
    }
}
