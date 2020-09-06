using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExampleDataFlowProducerConsumer
{
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
}
