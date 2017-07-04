using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MinimalDatabase
{
    public interface IRecordSerializer
    {
        void WriteEntry(object value, BinaryWriter writer);
        object ReadEntry(BinaryReader reader);
        uint BytesPerEntry { get; }
    }
}
