using System.Reflection;
using CommandLine;

namespace DotnetSolutions.PdfTools;

public class Program
{
    static void Main(string[] args)
    {
        var types = LoadVerbs();
        Parser.Default.ParseArguments(args, types)
            .WithParsed(DoProcess);
    }

    private	static Type[] LoadVerbs()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();		 
    }
    
    private static void DoProcess(object options)
    {
        switch (options)
        {
            case MergeOptions mergeOptions: 
                new MergeCommand(mergeOptions).Run();
                break;
            
            default:
                Console.WriteLine("Unknown options type.");
                break;
        }
    }
}