using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MinimalDatabase.Paging
{
    public abstract class Page
    {
        public abstract void ReadFromPersistence(BinaryReader reader);
        public abstract void WriteToPersistence(BinaryWriter writer);
    }
}
