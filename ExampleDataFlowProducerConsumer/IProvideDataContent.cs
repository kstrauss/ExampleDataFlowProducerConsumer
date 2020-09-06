using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace ExampleDataFlowProducerConsumer
{
    /// <summary>
    /// Used to provide an abstraction on whether you are reading from files or from
    /// an archive (or potentially anything else)
    /// </summary>
    interface IProvideDataContent :IDisposable
    {
        Task<InputDataHolder> Process(string key, IList<string> listOFiles);
    }

    public class ZipDataContent : IProvideDataContent
    {
        readonly string zipPath;
        public ZipDataContent(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentException($"File [{path}] does not exist");
            zipPath = path;
        }

        public void Dispose()
        {
            return;
        }

        public async Task<InputDataHolder> Process(string key, IList<string> listOFiles)
        {
            List<String> csvs = new List<String>();
            using (var zipArchive = ZipFile.OpenRead(zipPath))
            {
                foreach (var f in listOFiles.OrderbyNumericExtension())
                {
                    using (var s = zipArchive.GetEntry(f).Open())
                    using (var sr = new StreamReader(s))
                    {
                        var temp = sr.ReadToEndAsync();
                        csvs.Add(await temp);
                    }
                }
            }
            return new InputDataHolder(key, csvs);
        }
    }

    public class FileDataContent : IProvideDataContent
    {
        readonly string rootPath;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">full path of passed file</param>
        public FileDataContent(string path)
        {
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory [{path}] does not exist");
            rootPath = Path.GetFullPath(path);
        }

        public void Dispose()
        {
            return;
        }
        /// <summary>
        /// Converts a "Group" of text formatted files that have been provided. The expectation
        /// is that the files are text formatted and the grouping contain scenarios that are denoted by the extension of
        /// each file. For example extensions of ".001"->".999"
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="listOFiles"></param>
        /// <returns></returns>
        public async Task<InputDataHolder> Process(string key, IList<string> listOFiles)
        {
            List<String> csvs = new List<String>();
            foreach (var f in listOFiles.OrderbyNumericExtension())
            {
                var temp = File.ReadAllLinesAsync(Path.Combine(rootPath, f));
                csvs.AddRange(await temp);
            }
            //Console.WriteLine($"have {csvs.Count} entries");
            return new InputDataHolder(key, csvs);
        }
    }
}
