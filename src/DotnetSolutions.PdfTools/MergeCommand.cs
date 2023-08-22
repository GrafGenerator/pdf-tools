using CommandLine;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DotnetSolutions.PdfTools;

public class MergeCommand
{
    private readonly MergeOptions _options;

    public MergeCommand(MergeOptions options)
    {
        _options = options;
    }

    public void Run()
    {
        var doConsoleOutput = !_options.Silent;
        var sourceDirectory = new DirectoryInfo(_options.SourceDirectoryPath);

        if (doConsoleOutput)
        {
            Console.WriteLine(
                $"Searching PDF files in directory ({(_options.UseRecursiveSearch ? "recursive search" : "this directory only")}):");
            Console.WriteLine(sourceDirectory.FullName);
            Console.WriteLine();
        }
        
        var sortInfo = ParseSorting(_options.Sort);

        if (doConsoleOutput)
        {
            Console.Write($"Using sorting:");
            
            if (sortInfo.SortType == SortType.None)
            {
                Console.WriteLine("None");
            }
            else
            {
                Console.Write(sortInfo.SortDirection == SortDirection.Asc ? "Ascending by " : "Descending by ");
                Console.Write(sortInfo.SortType);
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        
        var sortFn = sortInfo.SortType switch
        {
            SortType.None => GetSortFn<bool>(sortInfo, null),
            SortType.DateCreated => GetSortFn(sortInfo, x => x.CreationTimeUtc),
            SortType.DateModified => GetSortFn(sortInfo, x => x.LastWriteTimeUtc),
            SortType.FileName => GetSortFn(sortInfo, x => x.Name),
            SortType.FilePath => GetSortFn(sortInfo, x => x.FullName),
        };

        var files = sourceDirectory.GetFiles("*.pdf",
            _options.UseRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        var sortedFiles = sortFn(files).ToArray();

        if (doConsoleOutput)
        {
            Console.WriteLine($"Found {sortedFiles.Length} files.");
            Console.WriteLine();
        }
        
        var outputDocument = new PdfDocument();

        Console.WriteLine("Processing files.");
        foreach (var fileInfo in sortedFiles)
        {
            Console.WriteLine($"Processing {fileInfo.Name} ({Path.GetRelativePath(_options.SourceDirectoryPath, fileInfo.FullName)})");
            
            using var fileStream = fileInfo.OpenRead();
            var fileDocument = PdfReader.Open(fileStream, PdfDocumentOpenMode.Import);
            outputDocument.Version = Math.Max(outputDocument.Version, fileDocument.Version);
            foreach (var page in fileDocument.Pages)
            {
                outputDocument.AddPage(page);
            }
        }
        
        Console.WriteLine();
        Console.WriteLine();

        outputDocument.Options.FlateEncodeMode = PdfFlateEncodeMode.BestSpeed;
        outputDocument.Options.NoCompression = true;
        outputDocument.Options.CompressContentStreams = false;

        var outputFilePath = string.IsNullOrEmpty(_options.OutputFilePath)
            ? Path.Combine(_options.SourceDirectoryPath, "output.pdf")
            : _options.OutputFilePath;

        var outputFileInfo = new FileInfo(outputFilePath);
        Console.WriteLine($"Save result document to: {outputFileInfo.FullName}");

        using var outputFileStream = outputFileInfo.Open(FileMode.Create, FileAccess.Write);
        outputDocument.Save(outputFileStream, true);
        
        Console.WriteLine();
        Console.WriteLine("Done.");
    }
    
    private static Func<IEnumerable<FileInfo>, IEnumerable<FileInfo>> GetSortFn<TKey>(
        SortInfo sortInfo, Func<FileInfo, TKey>? keyFn) => keyFn == null
        ? inputs => inputs
        : sortInfo.SortDirection == SortDirection.Asc
            ? inputs => inputs.OrderBy(keyFn)
            : inputs => inputs.OrderByDescending(keyFn);

    private static SortInfo ParseSorting(string? input)
    {
        var result = new SortInfo();

        if (string.IsNullOrEmpty(input))
        {
            result.SortDirection = SortDirection.Asc;
            result.SortType = SortType.FileName;

            return result;
        }
        
        var parts = input.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            result.SortDirection = SortDirection.Asc;
            result.SortType = SortType.FileName;
        }
        else
        {
            var directionString = parts.Length > 1 ? parts[1].ToLowerInvariant() : "asc";
            result.SortDirection = Enum.Parse<SortDirection>(directionString);

            result.SortType = Enum.Parse<SortType>(parts[0]);
        }

        return result;
    }
}

[Verb("merge", HelpText = "Scan directory for PDF files and merge them together.")]
public class MergeOptions
{
    [Option('i', "input", Required = true, HelpText = "Path to the directory with source PDF files.")]
    public string SourceDirectoryPath { get; set; }
            
    [Option('r', "recursive", Required = false, Default = true, HelpText = "Defines whether recursive directory search is required. True by default.")]
    public bool UseRecursiveSearch { get; set; }
            
    [Option('s', "sort", Required = false, Default = null, HelpText = "Defines sorting for files in form \"type[ direction]\". Type=DateCreated|DateModified|Name|Path. Name sorts by file name, Path sorts by file path. Direction=Asc|Desc, optional, Asc is default.")]
    public string Sort { get; set; }

    [Option('o', "output", Required = false, HelpText = "Merged PDF file path.")]
    public string OutputFilePath { get; set; } = null!;
    
    [Option( "silent", Required = false, Default = false, HelpText = "No output to the console.")]
    public bool Silent { get; set; }
}