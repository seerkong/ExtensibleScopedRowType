using RowLang.Core.Runtime;
using RowLang.Core.Scripting;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: rowlang <path>");
    Console.WriteLine("Provides <path> as either a .kon file or directory to execute run directives.");
    return;
}

var targetPath = args[0];
IReadOnlyList<FileInfo> konFiles;

if (Directory.Exists(targetPath))
{
    var workspace = new DirectoryInfo(targetPath);
    konFiles = workspace
        .EnumerateFiles("*.kon", SearchOption.AllDirectories)
        .OrderBy(static file => file.FullName, StringComparer.Ordinal)
        .ToArray();
}
else if (File.Exists(targetPath))
{
    var file = new FileInfo(targetPath);
    if (!string.Equals(file.Extension, ".kon", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"File '{file.FullName}' is not a .kon script.");
        Environment.ExitCode = 1;
        return;
    }

    konFiles = new[] { file };
}
else
{
    Console.Error.WriteLine($"Path '{targetPath}' does not exist.");
    Environment.ExitCode = 1;
    return;
}

if (konFiles.Length == 0)
{
    Console.Error.WriteLine($"No .kon files found under '{targetPath}'.");
    Environment.ExitCode = 1;
    return;
}

foreach (var file in konFiles)
{
    Console.WriteLine($"\n=== {file.FullName} ===");
    var source = await File.ReadAllTextAsync(file.FullName);
    RowLangModule module;
    try
    {
        module = RowLangScript.Compile(source);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Compilation failed: {ex.Message}");
        Environment.ExitCode = 1;
        continue;
    }

    var context = module.CreateExecutionContext();
    var runs = module.RunDirectives;
    if (runs.IsDefaultOrEmpty || runs.Length == 0)
    {
        Console.WriteLine("(no run directives found)");
        continue;
    }

    foreach (var (directive, result) in module.ExecuteRuns(context))
    {
        Console.WriteLine($"-> {directive.ClassName}.{directive.MemberName}() = {FormatValue(result)}");
    }
}

static string FormatValue(Value value) => value switch
{
    StringValue str => str.Value,
    IntValue number => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
    BoolValue boolean => boolean.Value ? "true" : "false",
    ObjectValue obj => $"<{obj.Class.Name}>",
    _ => value.ToString() ?? string.Empty,
};
