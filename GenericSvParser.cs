using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Marxama
{
    /// <summary>
    /// A parser for Csv like files. The separator may be any sequence of non-alphanumeric characters, and is
    /// determined by the parser.
    /// </summary>
    /// <remarks>
    /// Use the static parse method to parse a given file or array of lines and obtain an instance of this class. 
    /// The input should be a Csv-like file, but the separator may be any sequence of non-alphanumeric characters.
    /// Alphanumeric characters are currently not supported.
    /// The separator is chosen as the sequence of non-alphanumeric characters which occurs most frequently, with the
    /// same number of occurences on each line. In case of several candidates, the longest one is chosen. If ambiguity
    /// still exists, an AmbiguityException is thrown, unless specified to be suppressed, in which case any of the
    /// potential separators may be used.
    /// 
    /// If headers are turned on, they need to be distinct.
    /// 
    /// Implements IEnumerable, and yields GenericSvLine objects, which in turn may be iterated, or accessed through index
    /// or header name.
    /// </remarks>
    public class GenericSvParser : IEnumerable<GenericSvLine>
    {
        private List<GenericSvLine> lines;
        private string[] headers;
        private string separator;

        public string[] Headers
        {
            get
            {
                return headers;
            }
        }

        public string Separator
        {
            get
            {
                return separator;
            }
        }

        private GenericSvParser()
        {
            lines = new List<GenericSvLine>();
        }
  
        /// <summary>
        /// Tries to parse the file specified, and if successful, returns an instance of
        /// GenericSvParser, to be used to access each line of the file, and for each line, its
        /// fields, accessible through integer indices and, if applicable, header names.
        /// </summary>
        /// <param name="filename">The path of the file to parse.</param>
        /// <param name="useHeaders">Whether or not the first line contains header names. The header names need to be distinct.</param>
        /// <param name="suppressAmbiguityExceptions">Whether or not to suppress AmbiguityExceptions, which are
        /// thrown whenever it's impossible to select a separator out of a number of alternatives. Will use any
        /// of the possible separators, without any guarantees as to its attributes.</param>
        /// <returns>An instance of GenericSvParser, used to obtain each field of each line of the parsed file.</returns>
        public static GenericSvParser Parse(string filename, bool useHeaders = true, bool suppressAmbiguityExceptions = false)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("File " + filename + " not found");
            }

            string[] linesInFile = File.ReadAllLines(filename, Encoding.UTF8);
            linesInFile = filterEmptyLines(linesInFile);
            return Parse(linesInFile, useHeaders, suppressAmbiguityExceptions);
        }

        private static string[] filterEmptyLines(string[] lines)
        {
            return lines.ToList().FindAll((line) => line.Trim().Length > 0 ).ToArray();
        }

        /// <summary>
        /// Tries to parse the lines specified, and if successful, returns an instance of
        /// GenericSvParser, to be used to access each line of the file, and for each line, its
        /// fields, accessible through integer indices and, if applicable, header names.
        /// </summary>
        /// <param name="lines">An array of Csv lines.</param>
        /// <param name="useHeaders">Whether or not the first line contains header names. The header names need to be distinct.</param>
        /// <param name="suppressAmbiguityExceptions">Whether or not to suppress AmbiguityExceptions, which are
        /// thrown whenever it's impossible to select a separator out of a number of alternatives. Will use any
        /// of the possible separators, without any guarantees as to its attributes.</param>
        /// <returns>An instance of GenericSvParser, used to obtain each field of each line of the parsed file.</returns>
        public static GenericSvParser Parse(string[] lines, bool useHeaders = true, bool suppressAmbiguityExceptions = false)
        {
            GenericSvParser parser = new GenericSvParser();
            parser.separator = SeparatorExtractor.GetSeparator(lines, useHeaders, suppressAmbiguityExceptions);

            // Split each line into an string array consisting of the line's fields, based on the acquired separator.
            List<string[]> parsedLines = new List<string[]>();
            foreach (string line in lines)
            {
                parsedLines.Add(line.Split(new string[] { parser.separator }, StringSplitOptions.None));               
            }


            Dictionary<string, int> headersToIndices = null;
            if (useHeaders)
            {
                parser.headers = parsedLines[0];
                // We don't want to add the headers line to the list of parsed lines, so we remove it here.
                parsedLines.RemoveAt(0);
                headersToIndices = new Dictionary<string, int>();
                for (int i = 0; i < parser.headers.Length; i++)
                {
                    string tmp = parser.headers[i];
                    headersToIndices.Add(parser.headers[i], i);
                }
            }


            foreach (string[] fields in parsedLines)
            {
                parser.lines.Add(new GenericSvLine(fields, headersToIndices));
            }

            return parser;
        }


        /// <summary>
        /// Formats the parsed contents and returns it as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            /* (do note that since this class is immutable, we might want to cache the output of ToString()
             * to a private field, in case ToString() will be called repeatedly on the same instance)
             */
            
            if (lines.Count == 0)
            {
                return "";
            }

            int[] columnWidths = new int[lines[0].Fields.Length];
            for (int i = 0; i < columnWidths.Length; i++)
            {
                columnWidths[i] = lines.Max((line) => line.Fields[i].Length);
                if (Headers != null)
                {
                    columnWidths[i] = Math.Max(columnWidths[i], Headers[i].Length);
                }

                columnWidths[i] += 2;
            }

            int totalWidth = columnWidths.Aggregate((a, b) => a + b);
            totalWidth += columnWidths.Length * 2 + 1;
            string asteriskForTotalWidth = new string('*', totalWidth);

            StringBuilder builder = new StringBuilder();
            if (Headers != null)
            {
                builder.AppendLine(asteriskForTotalWidth);
                for (int i = 0; i < Headers.Length; i++)
                {
                    string header = Headers[i];
                    builder.Append("* ");
                    builder.Append(header);
                    builder.Append(new string(' ', columnWidths[i] - header.Length));
                }
                builder.AppendLine("*");
            }
            builder.AppendLine(asteriskForTotalWidth);

            foreach (var line in lines)
            {
                for (int i = 0; i < line.Fields.Length; i++)
                {
                    string field = line.Fields[i];
                    builder.Append("* ");
                    builder.Append(field);
                    builder.Append(new string(' ', columnWidths[i] - field.Length));
                }
                builder.AppendLine("*");
            }
            builder.AppendLine(asteriskForTotalWidth);

            return builder.ToString();
        }

        public IEnumerator<GenericSvLine> GetEnumerator()
        {
            return lines.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return lines.GetEnumerator();
        }
    }
}
