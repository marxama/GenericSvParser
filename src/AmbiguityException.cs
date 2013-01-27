using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marxama
{
    class AmbiguityException : Exception
    {
        public AmbiguityException(string p)
            : base(p)
        {
        }
    }
}
