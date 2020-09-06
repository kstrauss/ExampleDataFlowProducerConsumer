using System;
using System.IO;

namespace ExampleDataFlowProducerConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Example Producer/Consumer Example ReadTextFiles -> PDFFiles");
            if (args.Length != 2)
            {
                Console.WriteLine("insufficient args");
                Console.WriteLine("arg1 = source directory of files or zip of files");
                Console.WriteLine("arg2 = either destination directory for CSV formatted, or if ends in a .zip extension will create a zip with the output CSV files");
                return;
            }
            // need much better argument handling
            if (IsValidDestinationArg(args[0]) && IsValidDestinationArg(args[1]))
            {
                var sourceDir = Path.GetFullPath(args[0]);
                var destDir = Path.GetFullPath(args[1]);
                Console.WriteLine("Converting Text files in {0} and will place PDF versions in {1}",
                    sourceDir, destDir);
                ProgramWorkers.ProcessFileTreeToPDF(sourceDir, destDir);
            }
            else
            {
                Console.Error.WriteLine("One of the given directories does not exist");
                Console.Error.WriteLine($"Argument #2 is valid ? {IsValidDestinationArg(args[1])}");
            }
        }

        static bool IsValidDestinationArg(string arg)
        {
            return Directory.Exists(arg) || Path.GetExtension(arg).Equals(".zip", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
