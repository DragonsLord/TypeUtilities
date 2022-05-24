using Microsoft.CodeAnalysis;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal static class AttributeDataExtensions
    {
        public static AttributeData? GetAttributeData<T>(this INamedTypeSymbol typeSymbol)
            => GetAttributeData(typeSymbol, typeof(T).Name!);

        public static AttributeData? GetAttributeData(this INamedTypeSymbol typeSymbol, string attributeName)
        {
            return typeSymbol
               .GetAttributes()
               .FirstOrDefault(x =>
                   x.AttributeClass?.ToDisplayString() is not null &&
                   x.AttributeClass.ToDisplayString() == attributeName);
        }

        public static T[] GetParamsArrayAt<T>(this IReadOnlyList<TypedConstant> ctorArgs, int startIndex)
            where T : class
        {
            if (ctorArgs.Count > startIndex)
            {
                if (ctorArgs[startIndex].Type?.TypeKind == TypeKind.Array)
                {
                    return ctorArgs[startIndex].Values.Select(x => x.Value as T).Where(x => x is not null).ToArray()!;
                }
                else
                {
                    // In case of syntax errors params may not be recognized as a single array argument
                    return ctorArgs
                                .Skip(startIndex)
                                .Where(arg => !arg.IsNull && arg.Type?.TypeKind == TypeKind.Class)
                                .Select(arg => arg.Value as T)
                                .Where(arg => arg is not null)
                                .ToArray()!;
                }
            }

            return Array.Empty<T>();
        }

        public static T GetParamValue<T>(this IDictionary<string, TypedConstant> dict, string name, T defaultValue)
        {
            if (dict.ContainsKey(name) && dict[name].Value is not null)
            {
                return dict[name].Value is T resolved ? resolved : defaultValue;
            }
            return defaultValue;
        }

        public static T[] GetParamValues<T>(this IDictionary<string, TypedConstant> dict, string name, T[] defaultValue)
            where T : class
        {
            if (dict.ContainsKey(name))
            {
                var values = dict[name].Values
                    .Select(x => (x.Value as T)!)
                    .Where(x => x is not null)
                    .ToArray();

                return values.Length > 0 ? values! : defaultValue;
            }
            return defaultValue;
        }
    }
}
