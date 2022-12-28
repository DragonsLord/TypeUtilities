using Microsoft.CodeAnalysis;
using TypeUtilities.Abstractions;
using TypeUtilities.SourceGenerators.Analyzer;
using TypeUtilities.SourceGenerators.Helpers;
using TypeUtilities.SourceGenerators.Map;

namespace TypeUtilities.SourceGenerators.Omit;

internal class OmitTypeConfig : MapTypeConfig
{
    public string[] Fields { get; }

    private OmitTypeConfig(MapTypeConfig mapConfig, string[] fields)
        : base(mapConfig)
    {
        Fields = fields;
    }

    public override IEnumerable<ISymbol> GetMembers(SourceProductionContext context, Location attributeLocation)
    {
        var allMembers = base.GetMembers(context, attributeLocation).ToArray();
        var omited = allMembers.Where(m => !Fields.Contains(m.Name)).ToArray();

        if (allMembers.Length - omited.Length < Fields.Length)
        {
            var missingFields = Fields.Except(allMembers.Select(x => x.Name));
            context.ReportMissingMembersToOmit(Source, missingFields, attributeLocation);
        }

        return omited;
    }

    public static new OmitTypeConfig? Create(INamedTypeSymbol targetTypeSymbol)
    {
        var attributeData = targetTypeSymbol.GetAttributeData<OmitAttribute>();

        if (attributeData is null)
            return null;

        var mapConfig = MapTypeConfig.Create(targetTypeSymbol, attributeData);

        if (mapConfig is null)
            return null;

        var fields = attributeData.ConstructorArguments.GetParamsArrayAt<string>(1);

        return new OmitTypeConfig(mapConfig, fields);
    }
}
