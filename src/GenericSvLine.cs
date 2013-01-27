using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marxama
{
    /// <summary>
    /// Encapsulates a parsed (C)sv line, with its fields accessible by index and header name, or
    /// simply by iterating.
    /// </summary>
    public class GenericSvLine : IEnumerable<string>
    {
        private string[] fields;

        public string[] Fields
        {
            get
            {
                return fields;
            }
        }

        // Used to point header names to indices in the fields array
        private Dictionary<string, int> headers;

        internal GenericSvLine(string[] fields, Dictionary<string, int> headers = null)
        {
            this.fields = fields;
            this.headers = headers;
        }

        /// <summary>
        /// Gets a field based on its position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string this[int index]
        {
            get
            {
                return fields[index];
            }
        }

        /// <summary>
        /// Gets the field for the given header name.
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public string this[string header]
        {
            get
            {
                if (headers == null)
                {
                    throw new NullReferenceException("Tried to access field through header name, but headers are not set up");
                }
                else if (!headers.ContainsKey(header))
                {
                    throw new KeyNotFoundException("Couldn't find header name " + header);
                }
                return fields[headers[header]];
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return (new List<string>(fields)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var field in this)
            {
                builder.Append(field);
                builder.Append("\t");
            }
            return builder.ToString();
        }
    }
}
