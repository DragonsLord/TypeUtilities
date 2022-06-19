using Microsoft.CodeAnalysis;
using TypeUtilities.SourceGenerators.Diagnostics;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Map;

namespace TypeUtilities.SourceGenerators.Pick;

internal class PickTypeConfig : MapTypeConfig
{
    public string[] Fields { get; }

    private PickTypeConfig(MapTypeConfig mapConfig, string[] fields)
        : base(mapConfig)
    {
        Fields = fields;
    }

    public override IEnumerable<ISymbol> GetMembers(SourceProductionContext context, Location attributeLocation)
    {
        var members = base.GetMembers(context, attributeLocation).Where(m => Fields.Contains(m.Name)).ToArray();

        if (members.Length < Fields.Length)
        {
            var missingFields = Fields.Except(members.Select(x => x.Name));
            context.ReportMissingMembersToPick(missingFields, attributeLocation);
        }

        return members;
    }

    public static new PickTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        var attributeData = targetTypeSymbol.GetAttributeData<PickAttribute>();

        if (attributeData is null)
            return null;

        var mapConfig = MapTypeConfig.Create(targetTypeSymbol, attributeData);

        if (mapConfig is null)
            return null;

        var fields = attributeData.ConstructorArguments.GetParamsArrayAt<string>(1);

        return new PickTypeConfig(mapConfig, fields);
    }
}
