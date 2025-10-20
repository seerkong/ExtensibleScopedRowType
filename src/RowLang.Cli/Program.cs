using RowLang.Core.Runtime;
using RowLang.Core.Scripting;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: rowlang <directory>");
    Console.WriteLine("Compiles and executes run directives in KON modules found in the directory.");
    return;
}

var workspace = new DirectoryInfo(args[0]);
if (!workspace.Exists)
{
    Console.Error.WriteLine($"Directory '{workspace.FullName}' does not exist.");
    Environment.ExitCode = 1;
    return;
}

var konFiles = workspace
    .EnumerateFiles("*.kon", SearchOption.AllDirectories)
    .OrderBy(static file => file.FullName, StringComparer.Ordinal)
    .ToArray();

if (konFiles.Length == 0)
{
    Console.Error.WriteLine($"No .kon files found under '{workspace.FullName}'.");
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
