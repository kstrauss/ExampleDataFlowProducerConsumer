using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
                Console.WriteLine("arg1 = source directory of files");
                Console.WriteLine("arg2 = either destination directory for CSV formatted, or if ends in a .zip extension will create a zip with the output CSV files");
                return;
            }
            // need much better argument handling
            if (Directory.Exists(args[0]) && isValidDestinationArg(args[1]))
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
                Console.Error.WriteLine($"Argument #2 is valid ? {isValidDestinationArg(args[1])}");
            }
        }

        static bool isValidDestinationArg(string arg)
        {
            return Directory.Exists(arg) || Path.GetExtension(arg).Equals(".zip", StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public static class ProgramWorkers
    {

        /// <summary>
        /// Will spawn a parallel job each group of files. A "group" is considered by
        /// the name of the file excluding the extension. For example one set of
        /// text files will be EPASomeName.001 -> up to EPASomeName.999 this
        /// would be one group and the a second one would be EPABigInflate.001 -> EPABigInflate.999
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="destPath"></param>
        /// <param name="fileFileter"></param>
        public static void ProcessFileTreeToPDF(string rootPath, string destPath, string fileFileter = "*")
        {
            var files = Directory.GetFiles(rootPath, searchPattern: fileFileter);
            var fg = files.GroupBy(f => Path.GetFileNameWithoutExtension(f));
            var buffer = new BufferBlock<InputDataHolder>();
            //Parallel.ForEach(fg, g=> CreateCSVFiles(rootPath, destPath, g));
            Task<int> consumeTask = null;

            //should use delegates or interface here, write to a zip if passed a zip extension
            if (Path.GetExtension(destPath).ToLower() == ".zip")
            {
                consumeTask = ConsumeAsyncZip(buffer, destPath);
            }
            else if (Directory.Exists(destPath))
            {
                consumeTask = ConsumeAsyncPdf(buffer, destPath);
            }
            else
            {
                throw new Exception("Cannot decide to zip or csv to file");
            }
            var loopResult = Parallel.ForEach(fg, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = Environment.ProcessorCount // put a limit because in some cases we have seen over parallization
            }, g => {
                var r = ProcessConversion(rootPath, g);

                // inefficiently keep trying to post to the buffer, in case we get an error (shouldn't but...)
                while (!buffer.Post(r))
                {
                    Console.Error.WriteLine("Could not post to buffer");
                    Task.Delay(500).Wait(); //wait for a half second
                }
            });

            buffer.Complete(); // let the buffer know, there should be no more additions

            var result = consumeTask.Result; // should be able to finish and drain the buffer

            Console.WriteLine($"added {result} entries to {destPath}");
        }

        /// <summary>
        /// Consumes from a ISourceBlock (the buffer) and will create a zip
        /// file by given name of the different output PDF text files
        /// </summary>
        /// <param name="source"></param>
        /// <param name="zipFile"></param>
        /// <returns>a count of of output files within the zip</returns>
        static async Task<int> ConsumeAsyncZip(ISourceBlock<InputDataHolder> source, string zipFile)
        {
            int count = 0;
            
            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    while (await source.OutputAvailableAsync())
                    {
                        var dd = await source.ReceiveAsync();
                        WriteTextWriterToZipFile(archive, dd.Item2, dd.Item1);
                        Console.WriteLine("ZipWriter consumed {0} source files", count++);
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Consumes/processes string data from the ISourceBlock/Buffer
        /// and will write out the data to the specified directory path as PDF files
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destDir"></param>
        /// <returns>Count of EPA files written out</returns>
        static async Task<int> ConsumeAsyncPdf(ISourceBlock<InputDataHolder> source, string destDir)
        {
            int count = 0;
            while (await source.OutputAvailableAsync())
            {
                var dd = await source.ReceiveAsync();
                var outPath = Path.Combine(destDir, dd.Item1 + ".pdf");
                using (var fs = new FileStream(outPath, FileMode.Create))
                {
                    CreateDoc(String.Join(", ", dd.Item2), fs);
                    Console.WriteLine("pdf file written to {0}");
                }
            }
            return count;
        }

        /// <summary>
        /// Converts a "Group" of text formatted files that have been provided. The expectation
        /// is that the files are text formatted and the grouping contain scenarios that are denoted by the extension of
        /// each file. For example extensions of ".001"->".999"
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="groupOfFiles"></param>
        /// <returns></returns>
        public static InputDataHolder ProcessConversion(string rootPath, IGrouping<string, string> groupOfFiles)
        {
            List<String> csvs = new List<String>();
            foreach (var f in groupOfFiles.OrderbyNumericExtension())
            {
                var temp = File.ReadAllLines( Path.Combine(rootPath, f));
                csvs.AddRange(temp);
                temp = null;
            }
            Console.WriteLine($"have {csvs.Count} entries");
            return new InputDataHolder(groupOfFiles.Key, csvs);
        }

        /// <summary>
        /// Writes the provided data to a ziparchive with an entry label of the fname
        /// </summary>
        /// <param name="zip"></param>
        /// /// <param name="data"></param>
        /// <param name="fname"></param>
        public static void WriteTextWriterToZipFile(ZipArchive zip, List<string> data, string fname)
        {
            ZipArchiveEntry readmeEntry = GetEntryFromZip(zip, fname + ".pdf");
            using (var zipStream = readmeEntry.Open())
            using (var pdfWriter = new PdfWriter(zipStream))
            {
                //write the document pdf to the stream
                var w = new PdfWriter(zipStream);
                CreateDoc(String.Join(", ", data),zipStream);
            }
        }

        public static void CreateDoc(string value, Stream destStream)
        {
            var pdfDoc = new PdfDocument(new PdfWriter(destStream));
            var document = new Document(pdfDoc);
            var pageSize = pdfDoc.GetDefaultPageSize();
            float width = pageSize.GetWidth() - document.GetLeftMargin() - document.GetRightMargin();

            SolidLine line = new SolidLine();
            AddParagraphWithTabs(document, value, line, width);

            // Draw a custom line to fill both sides, as it is described in iText5 example
            MyLine customLine = new MyLine();
            AddParagraphWithTabs(document, value, customLine, width);

            document.Close();
        }
        private static void AddParagraphWithTabs(Document document, string text, ILineDrawer line, float width)
        {
            List<TabStop> tabStops = new List<TabStop>();

            // Create a TabStop at the middle of the page
            tabStops.Add(new TabStop(width / 2, TabAlignment.CENTER, line));

            // Create a TabStop at the end of the page
            tabStops.Add(new TabStop(width, TabAlignment.LEFT, line));

            Paragraph p = new Paragraph().AddTabStops(tabStops);
            p
                .Add(new Tab())
                .Add(text)
                .Add(new Tab());
            document.Add(p);
        }

        private class MyLine : ILineDrawer
        {
            private float lineWidth = 1;
            private float offset = 2.02f;
            private Color color = ColorConstants.BLACK;

            public void Draw(PdfCanvas canvas, iText.Kernel.Geom.Rectangle drawArea)
            {
                float coordY = drawArea.GetY() + lineWidth / 2 + offset;
                canvas
                    .SaveState()
                    .SetStrokeColor(color)
                    .SetLineWidth(lineWidth)
                    .MoveTo(drawArea.GetX(), coordY)
                    .LineTo(drawArea.GetX() + drawArea.GetWidth(), coordY)
                    .Stroke()
                    .RestoreState();
            }

            public float GetLineWidth()
            {
                return lineWidth;
            }

            public void SetLineWidth(float lineWidth)
            {
                this.lineWidth = lineWidth;
            }

            public Color GetColor()
            {
                return color;
            }

            public void SetColor(Color color)
            {
                this.color = color;
            }

            public float GetOffset()
            {
                return offset;
            }

            public void SetOffset(float offset)
            {
                this.offset = offset;
            }
        }

        /// <summary>
        /// Either open an existing entry or create a new entry
        /// </summary>
        /// <param name="zip"></param>
        /// <param name="entryName"></param>
        /// <returns></returns>
        static ZipArchiveEntry GetEntryFromZip(ZipArchive zip, string entryName)
        {
            var r = zip.GetEntry(entryName);
            if (r != null)
                return r;
            return zip.CreateEntry(entryName);
        }
    }

    /// <summary>
    /// A set of utility functions that is an external class so that they can be
    /// tested
    /// </summary>
    public static class SimpleUtilityExtensions
    {
        /// <summary>
        /// Order a list of filenames whose extensions are numeric (int) in ascending order
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public static IEnumerable<string> OrderbyNumericExtension(this IEnumerable<string> inputs)
        {
            return inputs.OrderBy(ff => int.Parse(GetExtensionWithNoDot(ff)));
        }

        /// <summary>
        /// removes the first character from Path.Extension so it
        /// does not include the "." character.
        /// </summary>
        /// <param name="filenameWithExtension"></param>
        /// <returns></returns>
        private static string GetExtensionWithNoDot(string filenameWithExtension)
        {
            return Path.GetExtension(filenameWithExtension).Substring(1);
        }
    }

    /// <summary>
    /// A Tuple subclass that contains the EPA name with the data that represents the EPA data converted to a CSV format
    /// </summary>
    public class InputDataHolder : Tuple<string, List<String>>
    {
        public InputDataHolder(string item1, List<String> item2) : base(item1, item2)
        {
        }
    }
}
