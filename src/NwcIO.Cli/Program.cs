using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using NwcIO;
using NwcIO.Internal.Json;
using NwcIO.Model;

namespace NwcIO.Cli;

internal static class Program
{
    private const string UsageText = """
        Usage: nwcio <input.nwc|input.nwd> [output.json] [options]
               nwcio --help | --version

        Reads an Autodesk Navisworks (.nwc / .nwd) file and exports its scene tree,
        properties, 3D transforms, and metadata to JSON without requiring Navisworks to be installed.

        Arguments:
          <input>            Path to the .nwc or .nwd file to read.
          [output]           Destination path for the exported JSON file.
                             Default: <input-without-ext>.json (or standard output if '-').

        Options:
          --catalog          Include raw container stream catalog (offsets, sizes, kinds)
          --no-transforms    Exclude 3D transform matrices from JSON export
          --no-tree          Exclude hierarchical scene graph tree from JSON export
          --no-indent        Write compact JSON instead of pretty-printed indented JSON
          --report           Print summary report to standard error
          -h, --help         Show this help message and exit
          -v, --version      Show version information and exit

        Exit codes:
          0  Success
          1  Usage error (missing or invalid arguments)
          2  File not found
          3  Data format error or parsing failure
        """;

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(UsageText);
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Contains("--version") || args.Contains("-v"))
        {
            var version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString()
                ?? "0.1.0";
            Console.WriteLine($"nwcio {version}");
            return 0;
        }

        string? inputPath = null;
        string? outputPath = null;
        bool includeCatalog = false;
        bool includeTransforms = true;
        bool includeTree = true;
        bool indent = true;
        bool printReport = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--catalog":
                    includeCatalog = true;
                    break;
                case "--no-transforms":
                    includeTransforms = false;
                    break;
                case "--no-tree":
                    includeTree = false;
                    break;
                case "--no-indent":
                    indent = false;
                    break;
                case "--report":
                    printReport = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"Error: Unrecognized option '{arg}'.");
                        Console.Error.WriteLine(UsageText);
                        return 1;
                    }

                    if (inputPath is null)
                    {
                        inputPath = arg;
                    }
                    else if (outputPath is null)
                    {
                        outputPath = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Error: Unexpected extra argument '{arg}'.");
                        return 1;
                    }

                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            Console.Error.WriteLine("Error: Missing input file path.");
            Console.Error.WriteLine(UsageText);
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file '{inputPath}' does not exist.");
            return 2;
        }

        outputPath ??= Path.ChangeExtension(inputPath, ".json");

        var sw = Stopwatch.StartNew();
        NwcDocument doc;
        try
        {
            doc = NwcDocument.Open(inputPath);
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"Error: File '{inputPath}' is not a valid Navisworks container: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading '{inputPath}': {ex.Message}");
            return 3;
        }

        var exportOptions = new NwcJsonExportOptions
        {
            Indented = indent,
            IncludeTransforms = includeTransforms,
            IncludeSceneTree = includeTree,
            IncludeStreamCatalog = includeCatalog,
        };

        try
        {
            if (outputPath == "-")
            {
                using var stdout = Console.OpenStandardOutput();
                JsonExporter.Write(doc, stdout, exportOptions);
            }
            else
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonExporter.WriteFile(doc, outputPath, exportOptions);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting JSON to '{outputPath}': {ex.Message}");
            return 3;
        }

        sw.Stop();

        if (printReport)
        {
            int treeNodeCount = CountNodes(doc.RootNode);
            Console.Error.WriteLine("=== NwcIO Execution Report ===");
            Console.Error.WriteLine($"Input:            {inputPath}");
            Console.Error.WriteLine($"Source document:  {doc.FileInfo.SourcePath ?? "<unknown>"}");
            Console.Error.WriteLine($"Loader plugin:    {doc.FileInfo.LoaderPlugin ?? "<unknown>"}");
            Console.Error.WriteLine($"Timestamp:        {doc.FileInfo.SourceTimestampUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) ?? "<unknown>"}");
            Console.Error.WriteLine($"Scene tree nodes: {treeNodeCount}");
            Console.Error.WriteLine($"Property records: {doc.PropertyRecords.Count}");
            Console.Error.WriteLine($"GUIDs:            {doc.Guids.Count}");
            Console.Error.WriteLine($"3D Transforms:    {doc.Transforms.Count}");
            Console.Error.WriteLine($"Elapsed:          {sw.ElapsedMilliseconds} ms");
            Console.Error.WriteLine($"Output:           {outputPath}");
        }
        else
        {
            Console.WriteLine($"Exported {doc.PropertyRecords.Count} records, {doc.Guids.Count} GUIDs, {doc.Transforms.Count} transforms to '{outputPath}' in {sw.ElapsedMilliseconds} ms.");
        }

        return 0;
    }

    private static int CountNodes(NwcNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }

        return count;
    }
}
