using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalDatabase.Paging
{
    public class PageReference
    {
        public uint Index;

        public PageReference(uint index)
        {
            Index = index;
        }
    }
}
