using System.Text.Json;

namespace ConnectedRoom.CodeQuality;

/// <summary>Runs repository source checks using the compiler bundled with the installed .NET SDK.</summary>
internal static class Program
{
    /// <summary>Reports policy violations, or prints a method inventory for documentation maintenance.</summary>
    private static int Main(string[] args)
    {
        if (args.Contains("--self-test")) return SelfTest();
        string root = args.FirstOrDefault(argument => !argument.StartsWith("--")) ?? Directory.GetCurrentDirectory();
        var audit = new SourceAudit();
        foreach (string folder in new[] { "Assets/DoorDemo", "Native", "Tools/CodeQuality" })
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Split('/').Any(segment => segment is "bin" or "obj")) continue;
                audit.Inspect(relative, File.ReadAllText(file));
            }
        }
        if (args.Contains("--inventory"))
        {
            Console.WriteLine(JsonSerializer.Serialize(audit.Methods, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        string[] issues = audit.GetIssues().ToArray();
        foreach (string issue in issues) Console.Error.WriteLine(issue);
        int largest = audit.Types.GroupBy(type => type.Name).Max(group => group.Sum(type => type.Lines));
        Console.WriteLine($"C#: {audit.Types.Count()} declarations, {audit.Methods.Count()} methods, largest type {largest} lines; {issues.Length} issues.");
        return issues.Length == 0 ? 0 : 1;
    }

    /// <summary>Verifies positive and negative cases of the documentation and partial-type size checks.</summary>
    private static int SelfTest()
    {
        var valid = new SourceAudit();
        valid.Inspect("valid.cs", "class A {\n/// <summary>Runs the sample.</summary>\npublic void Run() {}\n}");
        var missing = new SourceAudit();
        missing.Inspect("missing.cs", "class A { public void Run() {} }");
        var partial = new SourceAudit();
        partial.Inspect("part1.cs", "partial class A {\n" + new string('\n', 150) + "}");
        partial.Inspect("part2.cs", "partial class A {\n" + new string('\n', 150) + "}");
        bool passed = !valid.GetIssues().Any() && missing.GetIssues().Count() == 1 && partial.GetIssues().Count() == 1;
        Console.WriteLine(passed ? "Source audit self-tests passed." : "Source audit self-tests failed.");
        return passed ? 0 : 1;
    }
}
