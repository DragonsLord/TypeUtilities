﻿using Microsoft.CodeAnalysis;
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

    public override IEnumerable<ISymbol> GetMembers()
    {
        return base.GetMembers().Where(m => Fields.Contains(m.Name));
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
