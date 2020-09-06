using System;
using System.Collections.Generic;

namespace ExampleDataFlowProducerConsumer
{
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
