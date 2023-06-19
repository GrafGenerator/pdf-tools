using CommandLine;

namespace DotnetSolutions.PdfsMerger;

public class CommandLineArguments
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