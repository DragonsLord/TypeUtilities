using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace TypeUtilities.SourceGenerators.Helpers
{
    internal class SourceBuilder
    {
        private StringBuilder _stringBuilder = new StringBuilder();
        private string _indent = string.Empty;
        private int _openScopes = 0;

        public SourceBuilder AddNamespace(string name, bool fileScoped = false)
        {
            if (fileScoped)
            {
                AddLine($"namespace {name};\n");
                return this;
            }

            return AddLine($"namespace {name}").OpenScope();
        }

        public SourceBuilder AddTypeDeclaration(SyntaxTokenList modifiers, SyntaxToken typeKind, SyntaxToken name, TypeParameterListSyntax? typeParameters, BaseListSyntax? baseList)
        {
            var baseListSrc = baseList is null ? string.Empty : " " + baseList.ToString();
            return AddLine($"{modifiers} {typeKind} {name}{typeParameters?.ToString() ?? string.Empty}{baseListSrc}").OpenScope();
        }

        public SourceBuilder AddLine(string line)
        {
            _stringBuilder.AppendLine(_indent + line);
            return this;
        }

        public SourceBuilder AddLines(params string[] lines)
        {
            foreach (var line in lines) AddLine(line);
            return this;
        }

        public SourceBuilder OpenScope()
        {
            AddLine("{");
            ++_openScopes;
            _indent = new string('\t', _openScopes);
            return this;
        }

        public SourceBuilder CloseScope()
        {
            --_openScopes;
            _indent = new string('\t', _openScopes);
            AddLine("}");
            return this;
        }

        public string Build()
        {
            while (_openScopes > 0)
            {
                CloseScope();
            }
            return _stringBuilder.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }
    }
}
