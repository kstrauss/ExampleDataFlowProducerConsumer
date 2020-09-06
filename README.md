# ExampleDataFlowProducerConsumer

Shows a simple Producer Consumer example for a friend. In this case he wanted to read from a zip file and write out to a zip file. Use Microsoft Dataflow (https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library)for this simple example. Error handling is not where it should be for the async code. On my machine (8 core Ryzen 3700x) this keeps the CPUs about 60% busy. Most of the time obviously is IO.
