<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.FileSystem.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.dll</Reference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

/*
	Used to create sample test files
*/
void Main()
{
	const int MAXFiles = 1000;
	const int MAXScenarios = 100;
	const string destPath = @"C:\temp\ExampleSourceContent";

	using (var zipArchve = ZipFile.Open(Path.Combine(destPath, "SourceData.zip"), ZipArchiveMode.Create))
	{

		var tasks = new List<Task>(MAXFiles * MAXScenarios);
		for (int scenario = 0; scenario < MAXScenarios; scenario++)
		{
			var filenameBaseTemplate = "Scenario_{0}.{1}";
			for (int fCount = 0; fCount < MAXFiles; fCount++)
			{
				var fname = String.Format(filenameBaseTemplate, scenario, fCount);
				var entry = zipArchve.CreateEntry(fname, CompressionLevel.Optimal);
				using (var zstream = entry.Open())
				using (var sw = new StreamWriter(zstream))
				{
					tasks.Add(sw.WriteLineAsync($"hello world This is file {fCount} in scenario {scenario}"));
				}
			}
		}
		Task.WaitAll(tasks.ToArray());
	}
	
	
}

// Define other methods and classes here
