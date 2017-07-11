using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase.Internal
{
    public static class MathHelper
    {
        public static uint DivCeil(long dividend, uint divisor)
        {
            uint result = (uint)(dividend / divisor);
            if (dividend % divisor > 0)
                ++result;

            return result;
        }
    }
}
