using System;
using System.Collections.Generic;
using System.IO;

namespace TransactionGenerator
{
    public static class StreamReaderExtensions
    {
        public static IEnumerable<string> ReadLines(this StreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}