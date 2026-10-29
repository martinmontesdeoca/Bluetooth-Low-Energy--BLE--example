using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConnectedRoom.CodeQuality;

/// <summary>Inspects syntax rather than counting braces or matching method declarations with regular expressions.</summary>
internal sealed class SourceAudit
{
    private readonly Dictionary<string, TypeEntry> types = new();
    private readonly Dictionary<string, MethodEntry> methods = new();
    public IEnumerable<TypeEntry> Types => types.Values;
    public IEnumerable<MethodEntry> Methods => methods.Values;

    /// <summary>Reads declarations in both Unity editor and Android conditional compilation branches.</summary>
    public void Inspect(string path, string source)
    {
        foreach (string[] symbols in new[] { Array.Empty<string>(), new[] { "UNITY_ANDROID" }, new[] { "UNITY_EDITOR" } })
        {
            var options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse,
                preprocessorSymbols: symbols);
            var root = CSharpSyntaxTree.ParseText(source, options).GetRoot();
            foreach (BaseTypeDeclarationSyntax type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                var span = type.GetLocation().GetLineSpan();
                string identity = string.Join(".", type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                    .Reverse().Select(item => item.Name.ToString()).Concat(type.AncestorsAndSelf()
                    .OfType<BaseTypeDeclarationSyntax>().Reverse().Select(item => item.Identifier.ValueText)));
                string key = path + ":" + type.SpanStart;
                types[key] = new TypeEntry(identity, path, span.StartLinePosition.Line + 1,
                    span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
            }
            foreach (BaseMethodDeclarationSyntax method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
            {
                string name = method switch
                {
                    MethodDeclarationSyntax named => named.Identifier.ValueText,
                    ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                    _ => method.Kind().ToString()
                };
                int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                string owner = method.Ancestors().OfType<BaseTypeDeclarationSyntax>().First().Identifier.ValueText;
                string key = path + ":" + method.SpanStart;
                methods[key] = new MethodEntry(path, owner, name, line, method.ParameterList.ToString(),
                    HasSummary(method));
            }
        }
    }

    /// <summary>Checks that a method has a nonempty XML summary before its attributes or declaration.</summary>
    private static bool HasSummary(SyntaxNode method) => method.GetLeadingTrivia()
        .Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>()
        .SelectMany(comment => comment.Content.OfType<XmlElementSyntax>())
        .Any(element => element.StartTag.Name.LocalName.ValueText == "summary" &&
            !string.IsNullOrWhiteSpace(string.Concat(element.Content)));

    /// <summary>Reports undocumented methods and sums every declaration of a partial type.</summary>
    public IEnumerable<string> GetIssues()
    {
        foreach (MethodEntry method in Methods.Where(method => !method.Documented))
            yield return $"{method.Path}:{method.Line}: missing summary: {method.Owner}.{method.Name}";
        foreach (var group in Types.GroupBy(type => type.Name))
        {
            int lines = group.Sum(type => type.Lines);
            if (lines > 300) yield return $"{group.Key}: {lines} lines (maximum 300, including partial declarations).";
        }
    }
}

/// <summary>Captures the source extent used to enforce a type's maximum size.</summary>
internal sealed record TypeEntry(string Name, string Path, int Line, int Lines);

/// <summary>Captures the declaration and documentation state of a method or constructor.</summary>
internal sealed record MethodEntry(string Path, string Owner, string Name, int Line, string Parameters, bool Documented);
